function displayMarkerHandler(map, latitude, longtitude, number) {
    var marker = new window.google.maps.Marker({
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
    });

    marker.addListener('dragend', FindCityNameHandler);
    return marker;
}
