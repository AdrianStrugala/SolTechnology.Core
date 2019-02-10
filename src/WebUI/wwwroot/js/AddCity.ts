/// <reference path="./RemoveCity.ts"/>

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
    textArea.onchange = function () { displayCity(this, map); }
    textArea.draggable = true;
    textArea.ondragstart = ev => { drag(ev) }

    var button = document.createElement("button");
    button.type = "button";
    button.className = "btn btn-danger";
    button.onclick = function () { removeCity(this); }
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