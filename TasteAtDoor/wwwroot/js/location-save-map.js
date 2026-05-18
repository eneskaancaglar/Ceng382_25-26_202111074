let locationMap;
let locationMarker;
let locationGeocoder;

async function initLocationSaveMap() {
    const mapElement = document.getElementById("locationMap");
    const latitudeInput = document.getElementById("Latitude");
    const longitudeInput = document.getElementById("Longitude");
    const addressInput = document.getElementById("Address");
    const selectedAddressText = document.getElementById("selectedAddressText");

    if (!mapElement || !latitudeInput || !longitudeInput) {
        console.error("Location map elements not found.");
        return;
    }

    const { Map } = await google.maps.importLibrary("maps");
    const { AdvancedMarkerElement } = await google.maps.importLibrary("marker");
    const { Geocoder } = await google.maps.importLibrary("geocoding");

    locationGeocoder = new Geocoder();

    const defaultPosition = {
        lat: 39.9208,
        lng: 32.8541
    };

    const savedLatitude = parseCoordinate(latitudeInput.value);
    const savedLongitude = parseCoordinate(longitudeInput.value);

    const startPosition =
        !Number.isNaN(savedLatitude) && !Number.isNaN(savedLongitude)
            ? { lat: savedLatitude, lng: savedLongitude }
            : defaultPosition;

    locationMap = new Map(mapElement, {
        center: startPosition,
        zoom: 13,
        mapId: "DEMO_MAP_ID"
    });

    locationMarker = new AdvancedMarkerElement({
        map: locationMap,
        position: startPosition,
        title: "Selected location"
    });

    if (!Number.isNaN(savedLatitude) && !Number.isNaN(savedLongitude)) {
        setLocationInputs(startPosition.lat, startPosition.lng, addressInput?.value || "");
    } else {
        clearLocationInputs();
    }

    locationMap.addListener("click", async function (event) {
        const position = {
            lat: event.latLng.lat(),
            lng: event.latLng.lng()
        };

        locationMarker.position = position;
        locationMap.setCenter(position);
        locationMap.setZoom(15);

        const address = await getAddressFromCoordinates(position);

        setLocationInputs(position.lat, position.lng, address);
    });

    async function getAddressFromCoordinates(position) {
        try {
            const response = await locationGeocoder.geocode({
                location: position
            });

            if (response.results && response.results.length > 0) {
                return response.results[0].formatted_address;
            }
        } catch (error) {
            console.error("Address could not be found:", error);
        }

        return "";
    }

    function setLocationInputs(latitude, longitude, address) {
        latitudeInput.value = formatCoordinateForAspNet(latitude);
        longitudeInput.value = formatCoordinateForAspNet(longitude);

        if (addressInput) {
            addressInput.value = address || "";
        }

        if (selectedAddressText) {
            selectedAddressText.textContent =
                address || `${latitudeInput.value}, ${longitudeInput.value}`;
        }
    }

    function clearLocationInputs() {
        latitudeInput.value = "";
        longitudeInput.value = "";

        if (addressInput) {
            addressInput.value = "";
        }
    }

    function parseCoordinate(value) {
        if (!value) {
            return NaN;
        }

        return parseFloat(value.toString().replace(",", "."));
    }

    function formatCoordinateForAspNet(value) {
        return Number(value).toFixed(6).replace(".", ",");
    }
}

window.initLocationSaveMap = initLocationSaveMap;
