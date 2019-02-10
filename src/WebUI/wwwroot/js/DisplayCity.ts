function displayCity(item, map) {

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
                    
                    updateCity(index, city, map, "✓");

                    map.setCenter(markers[index].getPosition());
                }
            });
        },

        error(req, status, errorObj) {
            displayCityAjaxCalls.pop();
            var alertMessage = JSON.parse(req.responseText);
            alert(alertMessage);
        }
    }));
}