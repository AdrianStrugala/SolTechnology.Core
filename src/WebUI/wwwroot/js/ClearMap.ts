function cleanMap(list) {
    for (var i = 0; i < paths.length; i++) {
        paths[i].setMap(null);
    }
    paths = [];
    pathsToRetry = [];

    list.innerHTML = "";

    for (var i = 0; i < routeLabels.length; i++) {
        if (routeLabels[i] != null) {
            routeLabels[i].setMap(null);
        }
    }
    routeLabels = [];
   
    paths = [];
}