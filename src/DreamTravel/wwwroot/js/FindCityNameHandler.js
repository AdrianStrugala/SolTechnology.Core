function FindCityNameHandler(event) {
    var index;

    for (var i = 0; i < markers.length; i++) {
        if (event.latLng.lat() == markers[i].getPosition().lat() && event.latLng.lng() == markers[i].getPosition().lng()) {
            index = i;
        }
    }

    $.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'TSP/FindCityByLocation',
        headers: {
            'Authorization': 'DreamAuthentication U29sVWJlckFsbGVz'
        },
        data: { lat: event.latLng.lat(), lng: event.latLng.lng(), sessionId: sessionId },
        success: function (msg) {

            var city = JSON.parse(msg);
            cities[index] = city;
            $("#listOfCities").children()[index].value = city.Name;
        },

        error: function (req, status, errorObj) {
            var alertMessage = JSON.parse(req.responseText);
            alert(alertMessage);
        }
    });

}