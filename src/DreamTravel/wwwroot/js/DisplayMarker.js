/// <reference path="../lib/googleMaps/googleMaps.d.ts" />
function displayMarker(map, latitude, longtitude, number) {
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
    }));
    marker.addListener('dragend', findCityName);
    return marker;
}
//# sourceMappingURL=DisplayMarker.js.map