window.mapInterop = {
    initMap: (lat, lng, zoom) => {
        window._map = new google.maps.Map(
            document.getElementById('map'),
            { center: { lat, lng }, zoom }
        );
        window._markers = [];
        window._polylines = [];
    },
    drawIntersection: (lat, lng, title) => {
        const m = new google.maps.Marker({
            position: { lat, lng },
            map: window._map,
            title
        });
        window._markers.push(m);
    },
    drawStreet: (fromLat, fromLng, toLat, toLng, color = '#FF0000') => {
        const line = new google.maps.Polyline({
            map: window._map,
            path: [
                { lat: fromLat, lng: fromLng },
                { lat: toLat, lng: toLng }
            ],
            strokeColor: color,
            strokeOpacity: 0.8,
            strokeWeight: 2
        });
        window._polylines.push(line);
    },
    clearMarkers: () => {
        window._markers.forEach(m => m.setMap(null));
        window._markers = [];
    },
    clearStreets: () => {
        window._polylines.forEach(l => l.setMap(null));
        window._polylines = [];
    }
};
