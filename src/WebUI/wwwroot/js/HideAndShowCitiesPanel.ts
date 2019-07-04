/// <reference path="../lib/jquery/jquery.d.ts" />

function hideCitiesPanel() {
    $("#listOfCitiesPanel")[0].style.display = "none";
    $("#hide-cities-panel-btn")[0].style.display = "none";
    $("#show-cities-panel-btn")[0].style.display = "block";

    $("#map")[0].style.marginLeft = "0";
    $("#map")[0].style.width = "100vw";
}

function showCitiesPanel() {
    $("#listOfCitiesPanel")[0].style.display = "block";
    $("#hide-cities-panel-btn")[0].style.display = "block";
    $("#show-cities-panel-btn")[0].style.display = "none";

    $("#map")[0].style.marginLeft = "16rem";
    $("#map")[0].style.width = "calc(100vw - 16rem)";
}
