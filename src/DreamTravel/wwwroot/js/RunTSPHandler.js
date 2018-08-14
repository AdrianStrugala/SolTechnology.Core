function runTSPHandler(){
    $("#loader")[0].style.display = "block";

    //Request
    $.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'TSP/CalculateBestPath',
        data: { cities: cities, sessionId: sessionId },
        success: function (msg) {

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

            //Adjust map bounds
            var bounds = new google.maps.LatLngBounds();
            for (var i = 0; i < markers.length; i++) {
                bounds.extend(markers[i].position);
            }
            map.fitBounds(bounds);

            //Finalize display
            writeSummaryInfoHandler(optimalTime, optimalCost);
            $("#costSlider")[0].setAttribute('value', optimalCost);
            $("#costSlider")[0].setAttribute('max', Math.ceil(totalCost));
            $("#limitValue")[0].innerHTML = $("#costSlider")[0].value + " €";

            $("#listOfCitiesBtn")[0].style.display = "initial";
            $("#costLimiBtn")[0].style.display = "initial";
            $("#loader")[0].style.display = "none";
        },

        error: function (req, status, errorObj) {
            $("#loader")[0].style.display = "none";
            var alertMessage = JSON.parse(req.responseText);
            alert(alertMessage);
        }
    });
}