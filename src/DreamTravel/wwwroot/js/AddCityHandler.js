function addCityHandler() {

    noOfCityRows++;

    var textArea = document.createElement("textarea");
    textArea.className = "city";
    textArea.id = "cityRow" + noOfCityRows.toString();
    textArea.rows = "1";
    var att = document.createAttribute("onchange");
    att.value = "displayCityHandler(this)";
    textArea.setAttributeNode(att);
    listOfCities.appendChild(textArea);
    textArea.focus();
}