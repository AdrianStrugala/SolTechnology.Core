function cleanMap(list) {
    for (var i = 0; i < paths.length; i++) {
        paths[i].setMap(null);
    }
    paths = [];
    pathsToRetry = [];
    list.innerHTML = "";
}
//# sourceMappingURL=ClearMap.js.map