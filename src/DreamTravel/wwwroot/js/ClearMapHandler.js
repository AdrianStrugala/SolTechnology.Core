function cleanMapHandler(list) {
    for (var i = 0; i < paths.length; i++) {
        paths[i].setMap(null);
    }
    paths = [];
    pathsToRetry = [];
    list.innerHTML = "";
}
//# sourceMappingURL=ClearMapHandler.js.map