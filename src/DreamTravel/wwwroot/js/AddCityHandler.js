function addCityHandler() {

    noOfCityRows++;

    var textArea = document.createElement("textarea");
    textArea.className = "city";
    var attId = document.createAttribute("id");
    attId.value = "cityRow" + noOfCityRows.toString();
    textArea.setAttributeNode(attId);
    textArea.rows = "1";
    var att = document.createAttribute("onchange");
    att.value = "displayCityHandler(this)";
    textArea.setAttributeNode(att);
    listOfCities.appendChild(textArea);
    textArea.focus();
}