function cleanMapHandler(list) {
    for (var i = 0; i < routes.length; i++) {
        routes[i].setMap(null);
    }
    routes = [];
    pathsToRetry = [];

    list.innerHTML = "";
}