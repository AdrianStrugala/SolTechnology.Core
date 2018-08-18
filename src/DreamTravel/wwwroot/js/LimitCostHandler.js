function limitCostHandler() {

    $("#loader")[0].style.display = "block";
    var limit = $("#costSlider")[0].value;

    $.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'TSP/LimitCost',
        headers: {
            'Authorization': 'DreamAuthentication TestAuthentication'
        },
        data: { costLimit: limit, sessionId: sessionId },
        success: function (msg) {

            var totalCost = 0;
            var totalTime = 0;

            var pathList = JSON.parse(msg);
            var noOfPaths = pathList.length;
            var list = $("#projectSelectorDropdown")[0];

            cleanMapHandler(list);

            for (var i = 0; i < noOfPaths; i++) {
                totalCost += pathList[i].OptimalCost;
                totalTime += pathList[i].OptimalDistance;

                writePathInfoHandler(pathList[i], list);
                displayRouteHandler(directionsService, map, pathList[i]);

                displayMarkerHandler(map,
                    pathList[i].StartingCity.Latitude,
                    pathList[i].StartingCity.Longitude,
                    i);
            }

            if (pathsToRetry.length > 0) {
                sleep(1000);
                for (var i = 0; i < pathsToRetry.length; i++) {
                    displayRouteHandler(directionsService, map, pathsToRetry[i]);
                }
                pathsToRetry = [];
            }

            displayMarkerHandler(map,
                pathList[noOfPaths - 1].EndingCity.Latitude,
                pathList[noOfPaths - 1].EndingCity.Longitude,
                noOfPaths);
            writeSummaryInfoHandler(totalTime, totalCost);

            $("#listOfCitiesBtn")[0].style.display = "initial";
            $("#loader")[0].style.display = "none";
        },
        error: function (req, status, errorObj) {
            $("#loader")[0].style.display = "none";
            var alertMessage = JSON.parse(req.responseText);
            alert(alertMessage);
        }
    });
}