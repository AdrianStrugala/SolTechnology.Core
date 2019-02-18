/// <reference path="../../lib/googleMaps/googleMaps.d.ts" />

function displayRouteLabel(map, latitude, longtitude, text) {

    var marker = new google.maps.Marker(({
        position: {
            lat: latitude,
            lng: longtitude
        },
        map: map,
        opacity: 0.8,
        icon: {
            labelOrigin: new google.maps.Point(0, 0),
            origin: new google.maps.Point(0, 0),
            scaledSize: new google.maps.Size(30, 10, "em", "em"),
            url: "../images/labelFrame.jpg"
        },
        label: {
            text: text.toString(),
            fontWeight: "bold"
        }
    }) as any);

    return marker;
}
