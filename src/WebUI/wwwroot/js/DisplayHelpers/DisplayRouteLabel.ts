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
         //   size: new google.maps.Size(1000,1000),
            url: "../images/labelFrame.jpg"
        },
        label: {
            text: text.toString(),
            fontWeight: "bold"
        }
    }) as any);

    return marker;
}
