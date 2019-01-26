function displayCity(item, map) {
    displayCityAjaxCalls.push($.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'api/FindLocationOfCity',
        headers: {
            'Authorization': 'DreamAuthentication U29sVWJlckFsbGVz'
        },
        data: { name: item.value, sessionId: sessionId },
        success: function (msg) {
            var city = JSON.parse(msg);
            $("#listOfCities").children().each(function (index) {
                if ($(this).attr('id') == "cityRow" + item.id && index < cities.length) {
                    if (markers[index] != null) {
                        markers[index].setMap(null);
                    }
                    cities[index] = city;
                    markers[index] = displayMarker(map, city.Latitude, city.Longitude, "✓");
                    map.setCenter(markers[index].getPosition());
                }
            });
        },
        error: function (req, status, errorObj) {
            displayCityAjaxCalls.pop();
            var alertMessage = JSON.parse(req.responseText);
            alert(alertMessage);
        }
    }));
}
//# sourceMappingURL=DisplayCity.js.map