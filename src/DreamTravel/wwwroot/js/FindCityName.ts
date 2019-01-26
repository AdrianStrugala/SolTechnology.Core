function findCityName(event) {
    var index;

    //get index of event source city
    for (var i = 0; i < markers.length; i++) {
        if (markers[i] != null) {
            if (event.latLng.lat() == markers[i].getPosition().lat() &&
                event.latLng.lng() == markers[i].getPosition().lng()) {
                index = i;
            }
        }
    }

    $.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'api/FindNameOfCity',
        headers: {
            'Authorization': 'DreamAuthentication U29sVWJlckFsbGVz'
        },
        data: { lat: event.latLng.lat(), lng: event.latLng.lng(), sessionId: sessionId },
        success(msg) {

            var city = JSON.parse(msg);
            cities[index] = city;
            (<HTMLInputElement>$("#listOfCities").children().eq(index).children()[1]).value = city.Name;
        },

        error(req, status, errorObj) {
            var alertMessage = JSON.parse(req.responseText);
            alert(alertMessage);
        }
    });

}