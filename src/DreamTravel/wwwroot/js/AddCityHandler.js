function addCityHandler() {

    noOfCityRows++;

    var div = document.createElement("div");
    div.className = "cityBlock";
    div.id = "cityBlock" + noOfCityRows.toString();

    var hr = document.createElement("hr");
    hr.className = "line";

    var hr2 = document.createElement("hr");
    hr2.className = "line";

    var textArea = document.createElement("textarea");
    textArea.className = "city";
    textArea.id = noOfCityRows.toString();
    textArea.rows = "1";
    var att = document.createAttribute("onchange");
    att.value = "displayCityHandler(this)";
    textArea.setAttributeNode(att);

    var button = document.createElement("button");
    button.type = "button";
    button.className = "btn btn-danger";
    var attOnClick = document.createAttribute("onclick");
    attOnClick.value = "removeCityHandler(this.id)";
    button.setAttributeNode(attOnClick);
    button.id = noOfCityRows.toString();
    button.innerHTML = "X";

    div.appendChild(hr);
    div.appendChild(textArea);
    div.appendChild(button);
    div.appendChild(hr2);

    listOfCities.appendChild(div);
    textArea.focus();

    cities.push(null);
    markers.push(null);

    if (noOfCityRows >= 2) {
        $("#runTSPBtn")[0].disabled = false;
    }
}



//<div class="cityBlock">
//    <hr class="line">
//    <textarea class="city" id="cityRow1" rows="1" onchange="displayCityHandler(this)"></textarea>
//    <button type="button" class="btn btn-danger" onclick="" id="removeCity1">X</button>
//    <hr class="line">
//</div>