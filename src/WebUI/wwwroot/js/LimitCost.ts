function limitCost(map) {

    $("#loader")[0].style.display = "block";
    var limit = (<HTMLInputElement>$("#costSlider")[0]).value;

    $.ajax({
        type: 'POST',
        dataType: 'html',
        url: window.location + 'api/LimitCost',
        headers: {
            'Authorization': 'DreamAuthentication U29sVWJlckFsbGVz'
        },
        data: { costLimit: limit, sessionId: sessionId },
        success(msg) {

            var pathList = JSON.parse(msg);
            displayPage(pathList, map);


            $("#listOfCitiesBtn")[0].style.display = "initial";
            $("#loader")[0].style.display = "none";
        },
        error(req, status, errorObj) {
            $("#loader")[0].style.display = "none";
            var alertMessage = JSON.parse(req.responseText);
            alert(alertMessage);
        }
    });
}