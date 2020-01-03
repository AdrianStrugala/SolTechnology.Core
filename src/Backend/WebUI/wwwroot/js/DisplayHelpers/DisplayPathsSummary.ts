function displayPathsSummary(pathList) {

    var list = document.createElement("ul");

    for (var i = 0; i < pathList.length; i++) {

        var hours = Math.floor(pathList[i].OptimalDistance / 3600);
        var minutes = Math.floor((pathList[i].OptimalDistance - Math.floor(hours) * 3600) / 60);
        var seconds = (pathList[i].OptimalDistance % 60);

        var routeString =
            "From " +
                pathList[i].StartingCity.Name +
                " to " +
                pathList[i].EndingCity.Name +
                ". Cost of toll fee: ";

        if (totalCost < 0) {
            routeString += "unknown";
        }
        else {
            routeString += pathList[i].OptimalCost.toFixed(2);
        }
       
        routeString +=
            " €." +
            " Time: " +
            Math.floor(hours) +
            ":" +
            pad2(Math.floor(minutes)) +
            ":" +
            pad2(Math.floor(seconds)) +
            "h\n";

        var li = document.createElement("li");
        var text = document.createTextNode(routeString);
        li.appendChild(text);
        list.appendChild(li);
    }

    $("#pathsSummaryBody")[0].appendChild(list);
}