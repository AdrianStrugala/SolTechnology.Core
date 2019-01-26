function displayPathInfo(path, list) {
    var hours = Math.floor(path.OptimalDistance / 3600);
    var minutes = Math.floor((path.OptimalDistance - Math.floor(hours) * 3600) / 60);
    var seconds = (path.OptimalDistance % 60);
    var routeString = "From " +
        path.StartingCity.Name +
        " to " +
        path.EndingCity.Name +
        ". Cost of fee: " +
        path.OptimalCost.toFixed(2) +
        " €." +
        " Time: " +
        Math.floor(hours) +
        ":" +
        pad2(Math.floor(minutes)) +
        ":" +
        pad2(Math.floor(seconds)) +
        "\n";
    var li = document.createElement("li");
    var text = document.createTextNode(routeString);
    //text.href = "#";
    li.appendChild(text);
    list.appendChild(li);
}
//# sourceMappingURL=DisplayPathInfo.js.map