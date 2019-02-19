function clearMap() {
    for (var i = 0; i < paths.length; i++) {
        paths[i].setMap(null);
    }
    paths = [];
    pathsToRetry = [];

    $("#pathsSummaryBody")[0].firstChild.remove();

    for (var i = 0; i < routeLabels.length; i++) {
        if (routeLabels[i] != null) {
            routeLabels[i].setMap(null);
        }
    }
    routeLabels = [];
}