window.mapInterop = {
    initMap: (lat, lng, zoom) => {
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

    drawStreet: (fromLat, fromLng, toLat, toLng, data, trafficValue) => {
        // compute strokeColor based on speed (m/s)
        let color = '#007bff'; // default
        if (trafficValue != null && !isNaN(trafficValue)) {
            if (trafficValue >= 10) color = '#28a745'; // ≥10 m/s → green
            else if (trafficValue >= 5) color = '#ffc107'; // 5–10 m/s → yellow
            else color = '#dc3545'; // <5 m/s → red
        }

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

        // store metadata
        line._streetData = data;          // your existing data object
        line._trafficValue = trafficValue; // numeric traffic metric

        // hover InfoWindow 
        line.addListener('mouseover', e => {
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
          <b>Speed:</b> ${line._trafficValue != null ? (line._trafficValue * 3.6).toFixed(1) + ' km/h' : 'n/a'}
        </div>`;
            window._infoWindow.setContent(content);
            window._infoWindow.setPosition(e.latLng);
            window._infoWindow.open(window._map);
        });
        line.addListener('mouseout', () => window._infoWindow.close());

        window._polylines.push(line);
    },

    clearMarkers: () => {
        // usuń wszystkie markery
        window._markers.forEach(m => m.setMap(null));
        window._markers = [];
    },

    clearStreets: () => {
        window._polylines.forEach(l => l.setMap(null));
        window._polylines = [];
    },
};
