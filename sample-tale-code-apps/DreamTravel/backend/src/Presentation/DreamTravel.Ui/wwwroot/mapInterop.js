window.mapInterop = {
    initMap: (lat, lng, zoom) => {
        // inicjalizacja mapy i warstw
        window._map = new google.maps.Map(
            document.getElementById('map'),
            { center: { lat, lng }, zoom }
        );
        window._markers = [];
        window._polylines = [];
        window._infoWindow = new google.maps.InfoWindow();
    },

    drawIntersection: (lat, lng, title) => {
        // marker dla skrzyżowania
        const m = new google.maps.Marker({
            position: { lat, lng },
            map: window._map,
            title
        });
        window._markers.push(m);
    },

    drawStreet: (fromLat, fromLng, toLat, toLng, color, data) => {
        // polyline dla ulicy
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

        // zachowaj dane w linii
        line._streetData = data;

        // hover → pokaż InfoWindow
        line.addListener('mouseover', (e) => {
            const d = line._streetData;
            const content = `
        <div style="min-width:200px; font-size:14px;">
          <b>Id:</b> ${d.id}<br/>
          <b>Nazwa:</b> ${d.name || '—'}<br/>
          <b>Długość:</b> ${d.length?.toFixed(1) || '—'} m<br/>
          <b>Lanes:</b> ${d.lanes ?? '—'}<br/>
          <b>Oneway:</b> ${d.oneway ?? '—'}<br/>
          <b>Bridge:</b> ${d.bridge ?? '—'}<br/>
          <b>Tunnel:</b> ${d.tunnel ?? '—'}<br/>
          <b>Highway:</b> ${d.highway || '—'}<br/>
          <b>Ref:</b> ${d.ref || '—'}<br/>
        </div>`;
            window._infoWindow.setContent(content);
            window._infoWindow.setPosition(e.latLng);
            window._infoWindow.open(window._map);
        });

        // mouseout → schowaj okno
        line.addListener('mouseout', () => {
            window._infoWindow.close();
        });

        window._polylines.push(line);
    },

    clearMarkers: () => {
        // usuń wszystkie markery
        window._markers.forEach(m => m.setMap(null));
        window._markers = [];
    },

    clearStreets: () => {
        // usuń wszystkie linie
        window._polylines.forEach(l => l.setMap(null));
        window._polylines = [];
    }
};
