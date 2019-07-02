function getCityNameFromPanel(index) {
    return (<HTMLInputElement>$("#listOfCities").children().eq(index).children()[1]).value;
}

function setCityNameOnPanel(index, name) {
    (<HTMLInputElement>$("#listOfCities").children().eq(index).children()[1]).value = name;
}