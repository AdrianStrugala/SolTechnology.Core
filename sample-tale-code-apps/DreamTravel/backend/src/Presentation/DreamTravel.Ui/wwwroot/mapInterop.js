window.mapInterop = {
    initMap: (lat, lng, zoom) => {
        window._map = new google.maps.Map(
            document.getElementById('map'),
            { center: { lat, lng }, zoom }
        );
    },

    drawIntersection: (lat, lng, title) => {
        new google.maps.Marker({
            position: { lat, lng },
            map: window._map,
            title: title
        });
    },

    drawStreet: (fromLat, fromLng, toLat, toLng, color = "#FF0000") => {
        new google.maps.Polyline({
            path: [
                { lat: fromLat, lng: fromLng },
                { lat: toLat, lng: toLng }
            ],
            map: window._map,
            strokeColor: color,
            strokeOpacity: 0.8,
            strokeWeight: 2
        });
    }
};
