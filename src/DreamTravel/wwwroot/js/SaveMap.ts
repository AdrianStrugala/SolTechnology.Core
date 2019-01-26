import * as html2canvas from '../lib/html2canvas.js';

function saveMap() {

    //transform is needed because of bug with google maps
    var transform = $(".gm-style>div:first>div:first>div:last>div").css("transform")
    var comp = transform.split(","); //split up the transform matrix
    var mapleft = parseFloat(comp[4]); //get left value
    var maptop = parseFloat(comp[5]); //get top value
    $(".gm-style>div:first>div:first>div:last>div").css({ //get the map container. not sure if stable
        "transform": "none",
        "left": mapleft,
        "top": maptop,
    });


    html2canvas(document.body,
        {
            useCORS: true
        }).then(function (canvas) {

            var timeStamp = new Date().toLocaleDateString() + ":" + new Date().toLocaleTimeString();
            var fileName = "newTravel-" + timeStamp + '.jpg';

            var noOfCities = $("#listOfCities").children().length;
            if (noOfCities >= 2) {

                var firstCityName = (<HTMLInputElement>$("#listOfCities").children().eq(0).children()[0]).value;
                var lastCityName = (<HTMLInputElement>$("#listOfCities").children().eq(noOfCities - 1).children()[0]).value;
                fileName = firstCityName + "-" + lastCityName + "-" + timeStamp + ".jpg";
            }


            //transform back
            $(".gm-style>div:first>div:first>div:last>div").css({
                left: 0,
                top: 0,
                "transform": transform
            });


            //invoke save file
            var a = document.createElement('a');
            a.href = canvas.toDataURL("image/jpeg").replace("image/jpeg", "image/octet-stream");
            a.download = fileName;
            a.click();
        });
};