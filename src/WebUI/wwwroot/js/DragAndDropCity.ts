function drop(evt, map) {
    //                    ev.preventDefault();
    var sourceIndex = evt.oldIndex;
    var targetIndex = evt.newIndex;


    //If move up
    if (sourceIndex > targetIndex) {
        var temp = cities[sourceIndex];

        for (var i = sourceIndex; i > targetIndex; i--) {
            cities[i] = cities[i - 1];
        }
        cities[targetIndex] = temp;
    }
    //If move down
    else if (sourceIndex < targetIndex) {
        var temp = cities[sourceIndex];

        for (var i = sourceIndex; i <= targetIndex; i++) {
            cities[i] = cities[i + 1];
        }
        cities[targetIndex] = temp;
    }

    (<HTMLInputElement>$("#optimizeRoad")[0]).checked = false;
    runTSP(map);
}