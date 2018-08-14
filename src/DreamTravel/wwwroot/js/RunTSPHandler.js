function runTSPHandler(){
    document.getElementById("loader").style.display = "block";

    $.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'TSP/CalculateBestPath',
        data: { cities: cities, sessionId: sessionId },
        success: function (msg) {

            optimalCost = 0;
            optimalTime = 0;
            totalCost = 0;

            var pathList = JSON.parse(msg);
            var noOfPaths = pathList.length;
            var list = document.getElementById("projectSelectorDropdown");

            cleanMapHandler(list);

            for (var i = 0; i < noOfPaths; i++) {
                optimalCost += pathList[i].OptimalCost;
                optimalTime += pathList[i].OptimalDistance;
                totalCost += pathList[i].Cost;
                writePathInfoHandler(pathList[i], list);
                displayRouteHandler(directionsService, map, pathList[i]);

                markers[i].setMap(null);
                markers[i] = displayMarkerHandler(map,
                    pathList[i].StartingCity.Latitude,
                    pathList[i].StartingCity.Longitude,
                    i);
            }
            markers[markers.length - 1].setMap(null);
            markers[markers.length - 1] = displayMarkerHandler(map,
                pathList[noOfPaths - 1].EndingCity.Latitude,
                pathList[noOfPaths - 1].EndingCity.Longitude,
                noOfPaths);

            var bounds = new google.maps.LatLngBounds();
            for (var i = 0; i < markers.length; i++) {
                bounds.extend(markers[i].position);
            }
            map.fitBounds(bounds);
            writeSummaryInfoHandler(optimalTime, optimalCost);

            document.getElementById("costSlider").setAttribute('value', optimalCost);
            document.getElementById("costSlider").setAttribute('max', Math.ceil(totalCost));
            document.getElementById("limitValue").innerHTML =
                document.getElementById("costSlider").value + " €";

            document.getElementById("listOfCitiesBtn").style.display = "initial";
            document.getElementById("costLimiBtn").style.display = "initial";
            document.getElementById("loader").style.display = "none";
        },
        error: function (req, status, errorObj) {
            document.getElementById("loader").style.display = "none";
            var alertMessage = JSON.parse(req.responseText);
            alert(alertMessage);
        }
    });
}