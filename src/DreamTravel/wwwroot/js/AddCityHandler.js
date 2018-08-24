function addCityHandler() {

    noOfCityRows++;

    var div = document.createElement("div");
    div.className = "cityBlock";

    var hr = document.createElement("hr");
    hr.ClassName = "line";

    var hr2 = document.createElement("hr");
    hr.ClassName = "line";

    var textArea = document.createElement("textarea");
    textArea.className = "city";
    textArea.id = "cityRow" + noOfCityRows.toString();
    textArea.rows = "1";
    var att = document.createAttribute("onchange");
    att.value = "displayCityHandler(this)";
    textArea.setAttributeNode(att);

    var button = document.createElement("button");
    button.type = "button";
    button.className = "btn btn-danger";
    button.id = "removeCity" + noOfCityRows.toString();
    var attOnClick = document.createAttribute("onclick");
    attOnClick.value = "";
    button.setAttributeNode(attOnClick);
    button.innerHTML = "X";

    div.appendChild(hr);
    div.appendChild(textArea);
    div.appendChild(button);
    div.appendChild(hr2);

    listOfCities.appendChild(div);
    textArea.focus();

    if (noOfCityRows > 2) {
        $("#runTSPBtn")[0].disabled = false;
    }
}



//<div class="cityBlock">
//    <hr class="line">
//    <textarea class="city" id="cityRow1" rows="1" onchange="displayCityHandler(this)"></textarea>
//    <button type="button" class="btn btn-danger" onclick="" id="removeCity1">X</button>
//    <hr class="line">
//</div>