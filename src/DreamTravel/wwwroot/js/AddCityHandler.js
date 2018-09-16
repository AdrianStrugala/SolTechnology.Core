/// <reference path="./RemoveCityHandler.ts"/>
function addCityHandler(map) {
    noOfCityRows++;
    var div = document.createElement("div");
    div.className = "cityRow";
    div.id = "cityRow" + noOfCityRows.toString();
    var hr = document.createElement("hr");
    hr.className = "line";
    var hr2 = document.createElement("hr");
    hr2.className = "line";
    var textArea = document.createElement("textarea");
    textArea.className = "cityText";
    textArea.id = noOfCityRows.toString();
    textArea.rows = 1;
    textArea.onchange = function () { displayCityHandler(this, map); };
    var button = document.createElement("button");
    button.type = "button";
    button.className = "btn btn-danger";
    button.onclick = function () { removeCityHandler(this); };
    button.id = noOfCityRows.toString();
    button.innerHTML = "X";
    div.appendChild(hr);
    div.appendChild(textArea);
    div.appendChild(button);
    div.appendChild(hr2);
    $("#listOfCities")[0].appendChild(div);
    textArea.focus();
    cities.push(null);
    markers.push(null);
    if (noOfCityRows >= 2) {
        $("#runTSPBtn")[0].disabled = false;
    }
}
//# sourceMappingURL=AddCityHandler.js.map