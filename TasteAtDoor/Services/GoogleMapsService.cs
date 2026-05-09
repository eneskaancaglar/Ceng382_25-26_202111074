using System.Text;
using System.Text.Json;

namespace TasteAtDoor.Services
{
    public class GoogleMapsService : IGoogleMapsService
    {
        private readonly IConfiguration _configuration;

        public GoogleMapsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return (null, null);
            }

            var apiKey = _configuration["GoogleMaps:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (null, null);
            }

            var url =
                $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={Uri.EscapeDataString(apiKey)}";

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return (null, null);
            }

            var json = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("status", out var statusElement))
            {
                return (null, null);
            }

            var status = statusElement.GetString();

            if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null);
            }

            if (!root.TryGetProperty("results", out var resultsElement) ||
                resultsElement.GetArrayLength() == 0)
            {
                return (null, null);
            }

            var firstResult = resultsElement[0];

            if (!firstResult.TryGetProperty("geometry", out var geometryElement))
            {
                return (null, null);
            }

            if (!geometryElement.TryGetProperty("location", out var locationElement))
            {
                return (null, null);
            }

            if (!locationElement.TryGetProperty("lat", out var latElement))
            {
                return (null, null);
            }

            if (!locationElement.TryGetProperty("lng", out var lngElement))
            {
                return (null, null);
            }

            return (latElement.GetDouble(), lngElement.GetDouble());
        }

        public async Task<double?> GetRouteDistanceKmAsync(
            double originLat,
            double originLng,
            double destinationLat,
            double destinationLng)
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return null;
            }

            var requestBody = new
            {
                origin = new
                {
                    location = new
                    {
                        latLng = new
                        {
                            latitude = originLat,
                            longitude = originLng
                        }
                    }
                },
                destination = new
                {
                    location = new
                    {
                        latLng = new
                        {
                            latitude = destinationLat,
                            longitude = destinationLng
                        }
                    }
                },
                travelMode = "DRIVE",
                routingPreference = "TRAFFIC_UNAWARE",
                computeAlternativeRoutes = false,
                languageCode = "en-US",
                units = "METRIC"
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var httpClient = new HttpClient();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://routes.googleapis.com/directions/v2:computeRoutes");

            request.Headers.Add("X-Goog-Api-Key", apiKey);
            request.Headers.Add("X-Goog-FieldMask", "routes.distanceMeters");

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(responseJson);
            var root = document.RootElement;

            if (!root.TryGetProperty("routes", out var routesElement) ||
                routesElement.ValueKind != JsonValueKind.Array ||
                routesElement.GetArrayLength() == 0)
            {
                return null;
            }

            var firstRoute = routesElement[0];

            if (!firstRoute.TryGetProperty("distanceMeters", out var distanceElement))
            {
                return null;
            }

            var distanceMeters = distanceElement.GetDouble();

            return distanceMeters / 1000.0;
        }

        public double CalculateDistanceKm(
            double lat1,
            double lon1,
            double lat2,
            double lon2)
        {
            const double earthRadiusKm = 6371.0;

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) *
                Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }
    }
}