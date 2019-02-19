/// <reference path="../../lib/jquery/jquery.d.ts" />
/// <reference path="../GlobalVariables.ts"/>
/// <reference path="../DragAndDropCity.ts"/>

function addCity(map) {

    var div = document.createElement("div");
    div.className = "cityRow";
    div.id = "cityRow" + noOfCityRows.toString();

    var hr = document.createElement("hr");
    hr.className = "line";
    hr.id = (noOfCityRows +1).toString(); //line is below city
    hr.ondrop = ev => { drop(ev, map) };
    hr.ondragover = ev => { allowDrop(ev) };

    var textArea = document.createElement("textarea");
    textArea.className = "cityText";
    textArea.id = noOfCityRows.toString();
    textArea.rows = 1;
    textArea.onchange = function () {
        findAndDisplayCity(this, map);
    }
    textArea.draggable = true;
    textArea.ondragstart = ev => { drag(ev) }

    var button = document.createElement("button");
    button.type = "button";
    button.className = "btn btn-danger";
    button.onclick = function () {
        var index = findIndexByCity(this);
        removeCity(index);
    }
    button.id = noOfCityRows.toString();
    button.innerHTML = "X";

    div.appendChild(textArea);
    div.appendChild(button);
    div.appendChild(hr);

    $("#listOfCities")[0].appendChild(div);
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