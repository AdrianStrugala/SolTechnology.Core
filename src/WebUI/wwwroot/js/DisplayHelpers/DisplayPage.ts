/// <reference path="../../lib/jquery/jquery.d.ts" />

function displayPage(pathList, map) {

    //Initialize display
    optimalCost = 0;
    optimalTime = 0;
    totalCost = 0;
    var noOfPaths = pathList.length;

    clearMap();

    //Read information
    for (var i = 0; i < noOfPaths; i++) {

        optimalCost += pathList[i].OptimalCost;
        optimalTime += pathList[i].OptimalDistance;
        totalCost += pathList[i].Cost;     
        displayRoute(directionsService, map, pathList[i]);

        updateCity(i, pathList[i].StartingCity, map);
    }
    updateCity(markers.length - 1, pathList[noOfPaths - 1].EndingCity, map);

    //Retry display if error happened
    if (pathsToRetry.length > 0) {
        sleep(1000);
        for (var i = 0; i < pathsToRetry.length; i++) {
            displayRoute(directionsService, map, pathsToRetry[i]);
        }
    }
    pathsToRetry = [];

    //Adjust map bounds
    var bounds = new google.maps.LatLngBounds();
    for (var i = 0; i < markers.length; i++) {
        bounds.extend(markers[i].position);
    }
    map.fitBounds(bounds);

    //Finalize display
    displayPathsSummary(pathList);
    displaySummaryInfo(optimalTime, optimalCost);

    (<HTMLInputElement>$("#costSlider")[0]).value = String(Math.ceil(optimalCost));
    (<HTMLInputElement>$("#costSlider")[0]).max = String(Math.ceil(totalCost));
    $("#limitValue")[0].innerHTML = (<HTMLInputElement>$("#costSlider")[0]).value + " €";
}