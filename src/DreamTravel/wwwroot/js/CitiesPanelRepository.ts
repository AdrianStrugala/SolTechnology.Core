function getCityNameFromPanel(index) {
    return (<HTMLInputElement>$("#listOfCities").children().eq(index).children()[0]).value;
}

function setCityNameOnPanel(index, name) {
    (<HTMLInputElement>$("#listOfCities").children().eq(index).children()[0]).value = name;
}