/// <reference path="../lib/jquery/jquery.d.ts" />
/// <reference path="./GlobalVariables.ts"/>

function updateCity(index, city, map, label = index) {

    if (markers[index] != null) {
        markers[index].setMap(null);
    }
    markers[index] = displayMarker(map, city.Latitude, city.Longitude, label);
    cities[index] = city;
    setCityNameOnPanel(index, city.Name);
}