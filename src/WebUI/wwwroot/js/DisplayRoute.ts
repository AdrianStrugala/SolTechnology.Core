/// <reference path="../lib/googleMaps/googleMaps.d.ts" />

function displayRoute(directionsService, map, path) {

    //TODO Route Display details after click

    var roadColour = "black";
    var isToll = false;

    if (path.OptimalCost === 0) {
        isToll = true;
        roadColour = "#0080ff";
    }

    var directionsDisplay = new google.maps.DirectionsRenderer({
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
        origin: new google.maps.LatLng(path.StartingCity.Latitude, path.StartingCity.Longitude),
        destination: new google.maps.LatLng(path.EndingCity.Latitude, path.EndingCity.Longitude),
        travelMode: 'DRIVING',
        avoidTolls: isToll
    },
        (response, status) => {

            if (status === 'OK') {

                var middleRoad = response.routes[0].overview_path[response.routes[0].overview_path.length / 2];

                var hours = Math.floor(path.OptimalDistance / 3600);
                var minutes = Math.floor((path.OptimalDistance - Math.floor(hours) * 3600) / 60);
                var seconds = (path.OptimalDistance % 60);

                var routeString =
                    path.StartingCity.Name +
                    " -> " +
                    path.EndingCity.Name +
                    "\ (Cost of fee: " +
                    path.OptimalCost.toFixed(2) +
                    " €." +
                    " Time: " +
                    Math.floor(hours) +
                    ":" +
                    pad2(Math.floor(minutes)) +
                    ":" +
                    pad2(Math.floor(seconds)) +
                    "h)";

                routeLabels.push(displayRouteLabel(map, middleRoad.lat(), middleRoad.lng(), routeString));

                directionsDisplay.setDirections(response);

            } else if (status === 'OVER_QUERY_LIMIT') {
                pathsToRetry.push(path);
            } else {
                window.alert('Directions request failed due to ' + status);
            }
        });

    paths.push(directionsDisplay);
}
