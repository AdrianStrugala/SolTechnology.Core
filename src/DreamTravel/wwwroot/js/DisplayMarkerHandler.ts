/// <reference path="../lib/googleMaps/googleMaps.d.ts" />

function displayMarkerHandler(map, latitude, longtitude, number) {
    var marker = new google.maps.Marker(({
        position: {
            lat: latitude,
            lng: longtitude
        },
        map: map,
        draggable: true,
        label: {
            text: number.toString(),
            color: "white",
            fontWeight: "bold"
        }
    }) as any);

    marker.addListener('dragend', findCityNameHandler);
    return marker;
}
