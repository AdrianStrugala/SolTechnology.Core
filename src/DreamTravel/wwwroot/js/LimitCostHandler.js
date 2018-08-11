function limitCostHandler() {

    document.getElementById("loader").style.display = "block";
    var limit = document.getElementById("costSlider").value;

    $.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'TSP/LimitCost',
        data: { costLimit: limit, sessionId: sessionId },
        success: function (msg) {

            var totalCost = 0;
            var totalTime = 0;

            var pathList = JSON.parse(msg);
            var noOfPaths = pathList.length;
            var list = document.getElementById("projectSelectorDropdown");

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

            document.getElementById("listOfCitiesBtn").style.display = "initial";
            document.getElementById("loader").style.display = "none";
        },
        error: function (req, status, errorObj) {
            document.getElementById("loader").style.display = "none";
            var alertMessage = JSON.parse(req.responseText);
            alert(alertMessage);
        }
    });
}