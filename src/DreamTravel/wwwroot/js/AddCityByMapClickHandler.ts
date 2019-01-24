function addCityByMapClickHandler(position, map) {

    if ((<HTMLInputElement>$("#listOfCities").children().eq(0).children()[1]).value !== "") {
        addCityHandler(map);
    }

    displayCityAjaxCalls.push($.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'api/FindNameOfCity',
        headers: {
            'Authorization': 'DreamAuthentication U29sVWJlckFsbGVz'
        },
        data: { lat: position.lat(), lng: position.lng(), sessionId: sessionId },
        success(msg) {

            var city = JSON.parse(msg);


            $("#listOfCities").children().each(function (index) {
                if ($(this).attr('id') == "cityRow" + noOfCityRows.toString()) {

                    if (markers[index] != null) {
                        markers[index].setMap(null);
                    }

                    cities[index] = city;
                    markers[index] = displayMarkerHandler(map,
                        city.Latitude,
                        city.Longitude,
                        "✓");

                    (<HTMLInputElement>$("#listOfCities").children().eq(index).children()[1]).value = city.Name;

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