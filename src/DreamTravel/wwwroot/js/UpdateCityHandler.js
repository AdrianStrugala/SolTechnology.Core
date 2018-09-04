/// <reference path="../lib/jquery/jquery.d.ts" />
/// <reference path="./GlobalVariables.ts"/>
function updateCityHandler(index, city, map) {
    if (markers[index] != null) {
        markers[index].setMap(null);
    }
    markers[index] = displayMarkerHandler(map, city.Latitude, city.Longitude, index);
    cities[index] = city;
    $("#listOfCities").children().eq(index).children()[1].value = city.Name;
}
//# sourceMappingURL=UpdateCityHandler.js.map