function addCityByMapClick(position, map) {
    displayCityAjaxCalls.push($.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'api/FindNameOfCity',
        headers: {
            'Authorization': 'DreamAuthentication U29sVWJlckFsbGVz'
        },
        data: { lat: position.lat(), lng: position.lng(), sessionId: sessionId },
        success: function (msg) {
            var city = JSON.parse(msg);
            if (getCityNameFromPanel(0) !== "") {
                addCity(map);
            }
            $("#listOfCities").children().each(function (index) {
                if ($(this).attr('id') == "cityRow" + noOfCityRows.toString()) {
                    updateCity(index, city, map, "✓");
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
//# sourceMappingURL=AddCityByMapClick.js.map