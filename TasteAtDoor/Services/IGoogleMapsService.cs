namespace TasteAtDoor.Services
{
    public interface IGoogleMapsService
    {
        Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string address);

        Task<double?> GetRouteDistanceKmAsync(
            double originLat,
            double originLng,
            double destinationLat,
            double destinationLng);

        double CalculateDistanceKm(
            double lat1,
            double lon1,
            double lat2,
            double lon2);
    }
}