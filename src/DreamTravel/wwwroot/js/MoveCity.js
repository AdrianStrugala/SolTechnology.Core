function moveCity(sourceIndex, targetIndex, map) {
    if (sourceIndex > targetIndex) {
        var temp = cities[sourceIndex];
        for (var i = sourceIndex; i > targetIndex; i--) {
            cities[i] = cities[i - 1];
        }
        cities[targetIndex] = temp;
        $("#optimizeRoad")[0].checked = false;
        runTSP(map);
    }
    if (sourceIndex < targetIndex) {
        targetIndex = targetIndex - 1;
        var temp = cities[sourceIndex];
        for (var i = sourceIndex; i < targetIndex; i++) {
            cities[i] = cities[i + 1];
        }
        cities[targetIndex] = temp;
        $("#optimizeRoad")[0].checked = false;
        runTSP(map);
    }
}
//# sourceMappingURL=MoveCity.js.map