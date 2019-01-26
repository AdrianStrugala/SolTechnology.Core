function getCityNameFromPanel(index) {
    return $("#listOfCities").children().eq(index).children()[0].value;
}
function setCityNameOnPanel(index, name) {
    $("#listOfCities").children().eq(index).children()[0].value = name;
}
//# sourceMappingURL=CitiesPanelRepository.js.map