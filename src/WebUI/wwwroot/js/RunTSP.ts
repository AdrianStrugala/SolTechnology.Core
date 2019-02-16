/// <reference path="./RemoveCityByIndex.ts"/>
declare const Promise: any;

function runTSP(map) {
    $("#loader")[0].style.display = "block";

    // wait for all Display City ajax calls to finish
    Promise.all(displayCityAjaxCalls).then(() => {

        //remove all empty cities
        var citiesToRemove = [];
        for (var i = 0; i < cities.length; i++) {
            if (cities[i] === null) {
                citiesToRemove.push(i);
            }
        }
        for (var i = citiesToRemove.length - 1; i >= 0; i--) {
            removeCityByIndex(citiesToRemove[i]);
        }

        var optimizeRoadChck = (<HTMLInputElement>$("#optimizeRoad")[0]).checked;

        //Request
        $.ajax({
            type: 'POST',
            dataType: 'html',
            url: window.location + 'api/CalculateBestPath',
            headers: {
                'Authorization': 'DreamAuthentication U29sVWJlckFsbGVz'
            },
            data: { cities: cities, sessionId: sessionId, optimizePath: optimizeRoadChck },
            success(msg) {

                var pathList = JSON.parse(msg);
                displayPage(pathList, map);

                $("#listOfCitiesBtn")[0].style.display = "initial";
                $("#costLimiBtn")[0].style.display = "initial";
                $("#loader")[0].style.display = "none";
            },

            error(req, status, errorObj) {
                $("#loader")[0].style.display = "none";
                var alertMessage = JSON.parse(req.responseText);
                alert(alertMessage);
            }
        });
    });
}