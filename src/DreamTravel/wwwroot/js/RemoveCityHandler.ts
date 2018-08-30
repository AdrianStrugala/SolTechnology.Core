function removeCityHandler(city) {

    $("#listOfCities").children().each(function (index) {
        if ($(this).attr('id').toString() == "cityBlock" + city.id.toString()) {

            markers[index].setMap(null);
            markers.splice(index);

            cities.splice(index);

            
            while ($("#listOfCities").children()[index].firstChild) {
                $("#listOfCities").children()[index].removeChild($("#listOfCities").children()[index].firstChild);
            }
            $("#listOfCities").children()[index].remove();

        }
    });
    
    noOfCityRows--;
}