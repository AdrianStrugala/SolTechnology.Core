/// <reference path="../lib/googleMaps/googleMaps.d.ts" />

function displayMarker(map, latitude, longtitude, label) {
    var marker = new google.maps.Marker(({
        position: {
            lat: latitude,
            lng: longtitude
        },
        map: map,
        draggable: true,
        label: {
            text: label.toString(),
            color: "white",
            fontWeight: "bold"
        }
    }) as any);

    marker.addListener('dragend', findCityName);
    return marker;
}
