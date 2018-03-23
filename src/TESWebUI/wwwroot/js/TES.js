
function calculateAndDisplayRoute(directionsService, map, origin, destination, toll) {
    var directionsDisplay = new google.maps.DirectionsRenderer;
    directionsDisplay.setMap(map);
    directionsService.route({
        origin: origin,
        destination: destination,
        travelMode: 'DRIVING',
        avoidTolls: toll
    },
        function (response, status) {
            if (status === 'OK') {
                directionsDisplay.setDirections(response);
            } else {
                window.alert('Directions request failed due to ' + status);
            }
        });
}


function auto_grow(element) {
    element.style.height = "5px";
    element.style.height = (element.scrollHeight) + "px";
}
