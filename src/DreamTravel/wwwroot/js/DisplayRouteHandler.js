function displayRouteHandler(directionsService, map, path) {

    //TODO Route Display details after click

    var roadColour = "black";
    var isToll = false;

    if (path.OptimalCost === 0) {
        isToll = true;
        roadColour = "#0080ff";
    }

    var directionsDisplay = new window.google.maps.DirectionsRenderer({
        suppressMarkers: true,
        preserveViewport: true,
        polylineOptions: {
            strokeColor: roadColour,
            strokeWeight: 6,
            strokeOpacity: 0.6
        }
    });
    directionsDisplay.setMap(map);

    directionsService.route({
            origin: new window.google.maps.LatLng(path.StartingCity.Latitude, path.StartingCity.Longitude),
            destination: new window.google.maps.LatLng(path.EndingCity.Latitude, path.EndingCity.Longitude),
            travelMode: 'DRIVING',
            avoidTolls: isToll
        },
        function (response, status) {
            if (status === 'OK') {
                directionsDisplay.setDirections(response);
            } else if (status === 'OVER_QUERY_LIMIT') {
                pathsToRetry.push(path);
            } else {
                window.alert('Directions request failed due to ' + status);
            }
        });

    routes.push(directionsDisplay);
}