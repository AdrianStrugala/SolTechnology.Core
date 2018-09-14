function writeSummaryInfoHandler(totalTime, totalCost) {
    var totalHours = Math.floor(totalTime / 3600);
    var totalMinutes = Math.floor((totalTime - Math.floor(totalHours) * 3600) / 60);
    var totalSeconds = (totalTime % 60);

    $('#infoText').html(
        "Total cost of toll fee: " +
        totalCost.toFixed(2) +
        " €. \n" +
        "Total travel time: " +
        Math.floor(totalHours) +
        ":" +
        pad2(Math.floor(totalMinutes)) +
        ":" +
        pad2(Math.floor(totalSeconds)) +
        ".");
}