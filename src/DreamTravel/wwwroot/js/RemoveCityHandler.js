/// <reference path="../lib/jquery/jquery.d.ts" />
/// <reference path="./GlobalVariables.ts"/>
function removeCityHandler(city) {
    $("#listOfCities").children().each(function (index) {
        if ($(this).attr('id').toString() == "cityBlock" + city.id.toString()) {
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
//# sourceMappingURL=RemoveCityHandler.js.map