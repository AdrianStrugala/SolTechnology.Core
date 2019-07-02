/// <reference path="../../lib/jquery/jquery.d.ts" />
/// <reference path="../GlobalVariables.ts"/>
/// <reference path="../DragAndDropCity.ts"/>

function addCity(map) {

    var tr = document.createElement("tr");
    tr.className = "list-group-item cityRow";
    tr.id = "cityRow" + noOfCityRows.toString();

    var i = document.createElement("i");
    i.className = "fas fa-arrows-alt handle";

    var textArea = document.createElement("textarea");
    textArea.className = "cityText";
    textArea.id = noOfCityRows.toString();
    textArea.rows = 1;
    textArea.onchange = function () {
        findAndDisplayCity(this, map);
    }

    var button = document.createElement("button");
    button.type = "button";
    button.className = "btn btn-danger";
    button.onclick = function () {
        var index = findIndexByCity(this);
        removeCity(index);
    }
    button.id = noOfCityRows.toString();
    button.innerHTML = "X";

    tr.appendChild(i);
    tr.appendChild(textArea);
    tr.appendChild(button);

    $("#listOfCities")[0].appendChild(tr);
    textArea.focus();

    cities.push(null);
    markers.push(null);

    noOfCityRows++;

    if (noOfCityRows > 1) {
        (<HTMLInputElement>$("#runTSPBtn")[0]).disabled = false;
    }
}

function updateCity(index, city, map, label = index) {

    if (markers[index] != null) {
        markers[index].setMap(null);
    }
    markers[index] = displayMarker(map, city.Latitude, city.Longitude, label);
    cities[index] = city;
    setCityNameOnPanel(index, city.Name);
}

function findIndexByCity(city): number {
    var result = -1;
    $("#listOfCities").children().each(function (index) {
        if ($(this).attr('id').toString() == "cityRow" + city.id.toString()) {
            result = index;
        }
    });
    return result;
}

function removeCity(index) {
    if (markers[index] != null) {
        markers[index].setMap(null);
    }
    markers.splice(index, 1);
    cities.splice(index, 1);
    while ($("#listOfCities").children()[index].firstChild) {
        $("#listOfCities").children()[index].removeChild($("#listOfCities").children()[index].firstChild);
    }
    $("#listOfCities").children()[index].remove();

}