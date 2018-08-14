function displayMarkerHandler(map, latitude, longtitude, number) {

    //TODO Marker manipulation

    var marker = new window.google.maps.Marker({
        position: {
            lat: latitude,
            lng: longtitude
        },
        map: map,
        label: {
            text: number.toString(),
            color: "white",
            fontWeight: "bold"
        }
    });
    return marker;
}