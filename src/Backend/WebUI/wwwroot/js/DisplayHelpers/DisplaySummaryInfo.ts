function displaySummaryInfo(totalTime, totalCost) {
    var totalHours = Math.floor(totalTime / 3600);
    var totalMinutes = Math.floor((totalTime - Math.floor(totalHours) * 3600) / 60);
    var totalSeconds = (totalTime % 60);


    var routeString = "Total cost of toll fee: ";

    if (totalCost < 0) {
        routeString += "unknown";
    }
    else {
        routeString += totalCost.toFixed(2);   
    }

    routeString += " €. \n" +
        "Total travel time: " +
        Math.floor(totalHours) +
        ":" +
        pad2(Math.floor(totalMinutes)) +
        ":" +
        pad2(Math.floor(totalSeconds)) +
        ".";


    $('#infoText').html(routeString);
}