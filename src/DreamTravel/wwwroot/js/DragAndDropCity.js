function allowDrop(ev) {
    ev.preventDefault();
}
function drag(ev) {
    ev.dataTransfer.setData("sourceIndex", getIndexOfCity(ev.target.value));
}
function drop(ev, map) {
    ev.preventDefault();
    var sourceIndex = ev.dataTransfer.getData("sourceIndex");
    moveCity(parseInt(sourceIndex), parseInt(ev.target.id), map);
}
//# sourceMappingURL=DragAndDropCity.js.map