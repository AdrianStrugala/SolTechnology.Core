function displayCityHandler (item) {

    $.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'TSP/FindCity',
        data: { name: item.value, sessionId: sessionId },
        success: function (msg) {

            var city = JSON.parse(msg);

            var found = false;
            $("#listOfCities").children().each(function (index) {
                if ($(this).attr('id') == item.id && index < cities.length) {

                    markers[index].setMap(null);
                    cities[index] = city;
                    alert(cities[0]);
                    markers[index] = displayMarkerHandler(map,
                        city.Latitude,
                        city.Longitude,
                        "✓");

                    found = true;
                }

            });

            if (!found) {
                cities.push(city);
                markers.push(displayMarkerHandler(map,
                    city.Latitude,
                    city.Longitude,
                    "✓"));
            }

            map.setCenter(markers[markers.length - 1].getPosition());
        },

        error: function (req, status, errorObj) {
            var alertMessage = JSON.parse(req.responseText);
            alert(alertMessage);
        }
    });
}