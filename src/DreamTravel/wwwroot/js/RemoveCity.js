/// <reference path="../lib/jquery/jquery.d.ts" />
/// <reference path="./GlobalVariables.ts"/>
function removeCity(city) {
    $("#listOfCities").children().each(function (index) {
        if ($(this).attr('id').toString() == "cityRow" + city.id.toString()) {
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
//# sourceMappingURL=RemoveCity.js.map