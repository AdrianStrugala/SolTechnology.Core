function displayCityHandler(item, map) {

    displayCityAjaxCalls.push($.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'api/FindLocationOfCity',
        headers: {
            'Authorization': 'DreamAuthentication U29sVWJlckFsbGVz'
        },
        data: { name: item.value, sessionId: sessionId },
        success(msg) {

            var city = JSON.parse(msg);

            $("#listOfCities").children().each(function (index) {
                if ($(this).attr('id') == "cityRow" + item.id && index < cities.length) {

                    if (markers[index] != null) {
                        markers[index].setMap(null);
                    }
                    cities[index] = city;
                    markers[index] = displayMarkerHandler(map,
                        city.Latitude,
                        city.Longitude,
                        "✓");

                    map.setCenter(markers[index].getPosition());
                }
            });
        },

        error(req, status, errorObj) {
            var alertMessage = JSON.parse(req.responseText);
            alert(alertMessage);
        }
    }));
}