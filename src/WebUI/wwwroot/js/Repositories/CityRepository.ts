/// <reference path="../../lib/jquery/jquery.d.ts" />
/// <reference path="../GlobalVariables.ts"/>
/// <reference path="../DragAndDropCity.ts"/>
/// <reference path="../DisplayCity.ts"/>

function addCity(map) {

    noOfCityRows++;

    var div = document.createElement("div");
    div.className = "cityRow";
    div.id = "cityRow" + noOfCityRows.toString();

    var hr = document.createElement("hr");
    hr.className = "line";
    hr.id = noOfCityRows.toString();
    hr.ondrop = ev => { drop(ev, map) };
    hr.ondragover = ev => { allowDrop(ev) };

    var textArea = document.createElement("textarea");
    textArea.className = "cityText";
    textArea.id = noOfCityRows.toString();
    textArea.rows = 1;
    textArea.onchange = function() {
        window.alert("dupa dx");
        displayCity(this, map);
    }
    textArea.draggable = true;
    textArea.ondragstart = ev => { drag(ev) }

    var button = document.createElement("button");
    button.type = "button";
    button.className = "btn btn-danger";
    button.onclick = function() {
        removeCity(this); 
        window.alert("dupa");
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

    if (noOfCityRows >= 2) {
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

function removeCity(city) {
    $("#listOfCities").children().each(function (index) {
        if ($(this).attr('id').toString() == "cityRow" + city.id.toString()) {

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
    });
}

function removeCityByIndex(index) {
    markers.splice(index, 1);
    cities.splice(index, 1);
    while ($("#listOfCities").children()[index].firstChild) {
        $("#listOfCities").children()[index].removeChild($("#listOfCities").children()[index].firstChild);
    }
    $("#listOfCities").children()[index].remove();
}