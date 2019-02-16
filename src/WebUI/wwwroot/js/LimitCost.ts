function limitCost(map) {

    $("#loader")[0].style.display = "block";
    var limit = (<HTMLInputElement>$("#costSlider")[0]).value;

    $.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'api/LimitCost',
        headers: {
            'Authorization': 'DreamAuthentication U29sVWJlckFsbGVz'
        },
        data: { costLimit: limit, sessionId: sessionId },
        success(msg) {

            var pathList = JSON.parse(msg);
            displayPage(pathList, map);

//            var totalCost = 0;
//            var totalTime = 0;
//
//            
//            var noOfPaths = pathList.length;
//            var list = $("#projectSelectorDropdown")[0];
//
//            cleanMap(list);
//
//            for (var i = 0; i < noOfPaths; i++) {
//                totalCost += pathList[i].OptimalCost;
//                totalTime += pathList[i].OptimalDistance;
//
//                displayPathInfo(pathList[i], list);
//                displayRoute(directionsService, map, pathList[i]);
//
//                markers[i].setMap(null);
//                markers[i] = displayMarker(map,
//                    pathList[i].StartingCity.Latitude,
//                    pathList[i].StartingCity.Longitude,
//                    i);
//            }
//
//            if (pathsToRetry.length > 0) {
//                sleep(1000);
//                for (var i = 0; i < pathsToRetry.length; i++) {
//                    displayRoute(directionsService, map, pathsToRetry[i]);
//                }
//                pathsToRetry = [];
//            }
//            markers[markers.length - 1].setMap(null);
//            markers[markers.length - 1] = displayMarker(map,
//                pathList[noOfPaths - 1].EndingCity.Latitude,
//                pathList[noOfPaths - 1].EndingCity.Longitude,
//                noOfPaths);
//            displaySummaryInfo(totalTime, totalCost);

            $("#listOfCitiesBtn")[0].style.display = "initial";
            $("#loader")[0].style.display = "none";
        },
        error(req, status, errorObj) {
            $("#loader")[0].style.display = "none";
            var alertMessage = JSON.parse(req.responseText);
            alert(alertMessage);
        }
    });
}