function allowDrop(ev) {
    ev.preventDefault();
}

function drag(ev) {

    //Find index of event source city
    var index = -1;
    for (var i = 0; i < cities.length; i++) {
        if (cities[i].Name == ev.target.value) {
            index = i;
        }
    }

    //Cache index
    ev.dataTransfer.setData("sourceIndex", index);
}

function drop(ev, map) {
    ev.preventDefault();
    var sourceIndex = parseInt(ev.dataTransfer.getData("sourceIndex"));
    var targetIndex = parseInt(ev.target.id);

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
        targetIndex = targetIndex - 1;
        var temp = cities[sourceIndex];

        for (var i = sourceIndex; i < targetIndex; i++) {
            cities[i] = cities[i + 1];
        }
        cities[targetIndex] = temp;
    }

    (<HTMLInputElement>$("#optimizeRoad")[0]).checked = false;
    runTSP(map);
}