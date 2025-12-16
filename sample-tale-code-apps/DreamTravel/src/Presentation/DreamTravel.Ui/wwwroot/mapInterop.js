window.mapInterop = {
    loadGoogleMapsScript: async (apiKey) => {
        // Check if Google Maps is already loaded
        if (window.google && window.google.maps) {
            return Promise.resolve();
        }

        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}`;
            script.async = true;
            script.defer = true;
            script.onload = () => resolve();
            script.onerror = () => reject(new Error('Failed to load Google Maps script'));
            document.head.appendChild(script);
        });
    },

    initMap: (lat, lng, zoom) => {
        window._map = new google.maps.Map(
            document.getElementById('map'),
            { center: { lat, lng }, zoom }
        );
        window._markers = [];
        window._polylines = [];
        window._newStreets = [];
        window._infoWindow = new google.maps.InfoWindow();
        window._selectedIntersections = [];
        window._addStreetMode = false;
    },

    drawIntersection: (lat, lng, title) => {
        // marker dla skrzyżowania
        const m = new google.maps.Marker({
            position: { lat, lng },
            map: window._map,
            title
        });

        // Add click event listener for creating new streets
        m.addListener('click', () => {
            if (!window._addStreetMode) return;

            const intersection = { id: title, lat, lng };

            // If this is the first intersection or a different one than last selected
            if (window._selectedIntersections.length === 0 ||
                window._selectedIntersections[window._selectedIntersections.length - 1].id !== intersection.id) {

                window._selectedIntersections.push(intersection);

                // Highlight selected marker
                m.setIcon({
                    path: google.maps.SymbolPath.CIRCLE,
                    scale: 8,
                    fillColor: "#ff0000",
                    fillOpacity: 0.6,
                    strokeWeight: 1,
                    strokeColor: "#ff0000"
                });

                // If we have two points, draw a new street
                if (window._selectedIntersections.length === 2) {
                    const [from, to] = window._selectedIntersections;
                    window.mapInterop.drawNewStreet(from.lat, from.lng, to.lat, to.lng, from.id, to.id);

                    // Reset selections after drawing
                    window._selectedIntersections = [];
                    window._markers.forEach(marker => {
                        marker.setIcon(null);
                    });
                }
            }
        });

        window._markers.push(m);
    },

    drawStreet: (fromLat, fromLng, toLat, toLng, data, trafficValue) => {
        // convert m/s to km/h
        let color = '#007bff'; // BLUE = no data
        if (trafficValue != null && !isNaN(trafficValue)) {
            const kmh = trafficValue * 3.6;
            if (kmh > 50) color = '#28a745'; // GREEN
            else if (kmh >= 30) color = '#ffc107'; // YELLOW
            else if (kmh >= 10) color = '#dc3545'; // RED
            else color = '#000000'; // BLACK
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

    drawNewStreet: (fromLat, fromLng, toLat, toLng, fromId, toId) => {
        const line = new google.maps.Polyline({
            map: window._map,
            path: [
                { lat: fromLat, lng: fromLng },
                { lat: toLat, lng: toLng }
            ],
            strokeColor: '#9c27b0', // Purple for new streets
            strokeOpacity: 0.8,
            strokeWeight: 3,
            strokeDashArray: [4, 4] // Dashed line
        });

        // Calculate distance
        const calculateDistance = (lat1, lng1, lat2, lng2) => {
            const R = 6371000; // Earth radius in meters
            const dLat = (lat2 - lat1) * Math.PI / 180;
            const dLng = (lng2 - lng1) * Math.PI / 180;
            const a =
                Math.sin(dLat/2) * Math.sin(dLat/2) +
                Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
                Math.sin(dLng/2) * Math.sin(dLng/2);
            const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
            return R * c;
        };

        const distance = calculateDistance(fromLat, fromLng, toLat, toLng);

        // Mock data for new street
        const streetData = {
            id: `new-${Date.now()}`,
            name: 'Nowa ulica',
            length: distance,
            lanes: 2,
            oneway: false,
            bridge: false,
            tunnel: false,
            highway: 'residential',
            ref: '',
            fromId: fromId,
            toId: toId
        };

        line._streetData = streetData;

        // Add hover info window
        line.addListener('mouseover', e => {
            const d = line._streetData;
            const content = `
        <div style="min-width:200px; font-size:14px;">
          <b>Id:</b> ${d.id}<br/>
          <b>Nazwa:</b> ${d.name}<br/>
          <b>Długość:</b> ${d.length.toFixed(1)} m<br/>
          <b>Z:</b> ${d.fromId}<br/>
          <b>Do:</b> ${d.toId}<br/>
          <b>Status:</b> Nowa (nie zapisana)<br/>
        </div>`;
            window._infoWindow.setContent(content);
            window._infoWindow.setPosition(e.latLng);
            window._infoWindow.open(window._map);
        });
        line.addListener('mouseout', () => window._infoWindow.close());

        window._newStreets.push(line);
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

    clearNewStreets: () => {
        window._newStreets.forEach(l => l.setMap(null));
        window._newStreets = [];
    },

    setAddStreetMode: (enabled) => {
        window._addStreetMode = enabled;
        // Clear any selected intersections
        window._selectedIntersections = [];
        // Reset marker icons
        window._markers.forEach(marker => {
            marker.setIcon(null);
        });
    },

    // ========================================
    // TRIP PLANNER FUNCTIONS (Multi-instance support)
    // ========================================

    _maps: {}, // Store multiple map instances by ID

    initMapById: (mapId, lat, lng, zoom) => {
        const mapElement = document.getElementById(mapId);
        if (!mapElement) {
            console.error(`Map element with id '${mapId}' not found`);
            return;
        }

        const map = new google.maps.Map(mapElement, {
            center: { lat, lng },
            zoom
        });

        window.mapInterop._maps[mapId] = {
            map: map,
            markers: [],
            directionsRenderers: [],
            infoWindow: new google.maps.InfoWindow()
        };

        return mapId;
    },

    addTspMarker: (mapId, lat, lng, label, color = '#FF0000', cityData = null) => {
        const mapData = window.mapInterop._maps[mapId];
        if (!mapData) {
            console.error(`Map with id '${mapId}' not initialized`);
            return;
        }

        const marker = new google.maps.Marker({
            position: { lat, lng },
            map: mapData.map,
            label: {
                text: label,
                color: 'white',
                fontWeight: 'bold',
                fontSize: '14px'
            },
            icon: {
                path: google.maps.SymbolPath.CIRCLE,
                scale: 10,
                fillColor: color,
                fillOpacity: 1,
                strokeWeight: 2,
                strokeColor: 'white'
            }
        });

        // Add hover info window with city statistics
        if (cityData) {
            marker.addListener('mouseover', () => {
                let infoContent = `
                    <div style="min-width: 200px; font-size: 14px;">
                        <b>${cityData.name || 'Unknown'}</b><br/>
                        <b>Country:</b> ${cityData.country || 'N/A'}<br/>
                        <b>Coordinates:</b> ${lat.toFixed(4)}, ${lng.toFixed(4)}<br/>`;

                if (cityData.searchStatistics && cityData.searchStatistics.length > 0) {
                    const latestStat = cityData.searchStatistics[0];
                    infoContent += `<b>Search Count:</b> ${latestStat.searchCount}<br/>`;
                }

                infoContent += `</div>`;

                mapData.infoWindow.setContent(infoContent);
                mapData.infoWindow.open(mapData.map, marker);
            });

            marker.addListener('mouseout', () => {
                mapData.infoWindow.close();
            });
        }

        mapData.markers.push(marker);
        return marker;
    },

    drawDirectionsPath: async (mapId, startLat, startLng, endLat, endLng, options = {}) => {
        const mapData = window.mapInterop._maps[mapId];
        if (!mapData) {
            console.error(`Map with id '${mapId}' not initialized`);
            return Promise.reject(new Error('Map not initialized'));
        }

        const directionsService = new google.maps.DirectionsService();
        const directionsRenderer = new google.maps.DirectionsRenderer({
            suppressMarkers: true,
            preserveViewport: true,
            polylineOptions: {
                strokeColor: options.color || '#0080ff',
                strokeWeight: 6,
                strokeOpacity: 0.6
            }
        });

        directionsRenderer.setMap(mapData.map);

        return new Promise((resolve, reject) => {
            const makeRequest = () => {
                directionsService.route({
                    origin: { lat: startLat, lng: startLng },
                    destination: { lat: endLat, lng: endLng },
                    travelMode: google.maps.TravelMode.DRIVING,
                    avoidTolls: options.avoidTolls || false,
                    avoidFerries: true
                }, (response, status) => {
                    if (status === 'OK') {
                        directionsRenderer.setDirections(response);
                        mapData.directionsRenderers.push(directionsRenderer);
                        resolve(true);
                    } else if (status === 'OVER_QUERY_LIMIT') {
                        // Retry after 1 second
                        console.warn('Google Maps API rate limit hit, retrying in 1s...');
                        setTimeout(makeRequest, 1000);
                    } else {
                        console.error(`Directions request failed: ${status}`);
                        reject(new Error(`Directions request failed: ${status}`));
                    }
                });
            };

            makeRequest();
        });
    },

    clearTspMarkers: (mapId) => {
        const mapData = window.mapInterop._maps[mapId];
        if (!mapData) return;

        mapData.markers.forEach(marker => marker.setMap(null));
        mapData.markers = [];
    },

    clearDirections: (mapId) => {
        const mapData = window.mapInterop._maps[mapId];
        if (!mapData) return;

        mapData.directionsRenderers.forEach(renderer => renderer.setMap(null));
        mapData.directionsRenderers = [];
    },

    fitBoundsToMarkers: (mapId) => {
        const mapData = window.mapInterop._maps[mapId];
        if (!mapData || mapData.markers.length === 0) return;

        const bounds = new google.maps.LatLngBounds();
        mapData.markers.forEach(marker => {
            bounds.extend(marker.getPosition());
        });

        mapData.map.fitBounds(bounds);
    },

    requestGeolocation: (mapId) => {
        if (!navigator.geolocation) {
            return Promise.reject(new Error('Geolocation not supported'));
        }

        return new Promise((resolve, reject) => {
            navigator.geolocation.getCurrentPosition(
                (position) => {
                    const result = {
                        lat: position.coords.latitude,
                        lng: position.coords.longitude
                    };
                    resolve(result);
                },
                (error) => {
                    reject(new Error(`Geolocation error: ${error.message}`));
                }
            );
        });
    },

    setMapCenter: (mapId, lat, lng) => {
        const mapData = window.mapInterop._maps[mapId];
        if (!mapData) return;

        mapData.map.setCenter({ lat, lng });
    },

    attachMapClickListener: (mapId, dotnetHelper, callbackMethodName) => {
        const mapData = window.mapInterop._maps[mapId];
        if (!mapData) {
            console.error(`Map with id '${mapId}' not initialized`);
            return;
        }

        mapData.map.addListener('click', (event) => {
            const lat = event.latLng.lat();
            const lng = event.latLng.lng();
            dotnetHelper.invokeMethodAsync(callbackMethodName, lat, lng);
        });
    }
};
