function removeCityHandler(index) {
    markers[index].setMap(null);
    markers.splice(index, 1);
    cities.splice(index, 1);
    while ($("#listOfCities").children()[index].firstChild) {
        $("#listOfCities").children()[index].removeChild($("#listOfCities").children()[index].firstChild);
    }
    $("#listOfCities").children()[index].remove();
    noOfCityRows--;
}
//# sourceMappingURL=RemoveCityHandler.js.map