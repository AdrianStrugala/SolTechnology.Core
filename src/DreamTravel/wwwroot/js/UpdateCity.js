/// <reference path="../lib/jquery/jquery.d.ts" />
/// <reference path="./GlobalVariables.ts"/>
function updateCity(index, city, map) {
    if (markers[index] != null) {
        markers[index].setMap(null);
    }
    markers[index] = displayMarker(map, city.Latitude, city.Longitude, index);
    cities[index] = city;
    $("#listOfCities").children().eq(index).children()[1].value = city.Name;
}
//# sourceMappingURL=UpdateCity.js.map