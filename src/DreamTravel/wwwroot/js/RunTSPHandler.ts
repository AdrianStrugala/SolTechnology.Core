/// <reference path="./RemoveCityByIndexHandler.ts"/>
declare const Promise: any;

function runTSPHandler(map) {
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
            removeCityByIndexHandler(citiesToRemove[i]);
        }

        //Request
        $.ajax({
            type: 'POST',
            dataType: 'html',
            url: window.location + 'api/CalculateBestPath',
            headers: {
                'Authorization': 'DreamAuthentication U29sVWJlckFsbGVz'
            },
            data: { cities: cities, sessionId: sessionId },
            success(msg) {

                //Initialize display
                optimalCost = 0;
                optimalTime = 0;
                totalCost = 0;
                var pathList = JSON.parse(msg);
                var noOfPaths = pathList.length;
                var list = $("#projectSelectorDropdown")[0];

                cleanMapHandler(list);

                //Read information
                for (var i = 0; i < noOfPaths; i++) {

                    optimalCost += pathList[i].OptimalCost;
                    optimalTime += pathList[i].OptimalDistance;
                    totalCost += pathList[i].Cost;
                    writePathInfoHandler(pathList[i], list);
                    displayRouteHandler(directionsService, map, pathList[i]);

                    updateCityHandler(i, pathList[i].StartingCity, map);
                }
                updateCityHandler(markers.length - 1, pathList[noOfPaths - 1].EndingCity, map);

                //Adjust map bounds
                var bounds = new google.maps.LatLngBounds();
                for (var i = 0; i < markers.length; i++) {
                    bounds.extend(markers[i].position);
                }
                map.fitBounds(bounds);

                //Finalize display
                writeSummaryInfoHandler(optimalTime, optimalCost);

                (<HTMLInputElement>$("#costSlider")[0]).value = optimalCost;
                (<HTMLInputElement>$("#costSlider")[0]).max = String(Math.ceil(totalCost));
                $("#limitValue")[0].innerHTML = (<HTMLInputElement>$("#costSlider")[0]).value + " €";

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