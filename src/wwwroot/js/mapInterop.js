window.mapInterop = {
    // ... existing code ...
    
    updateTrafficColors: function() {
        if (!this.streets) return;
        
        this.streets.forEach(street => {
            if (street.polyline) {
                const color = this.getColorForSpeed(street.data.speed);
                street.polyline.setOptions({ strokeColor: color });
            }
        });
    },
    
    getColorForSpeed: function(speed) {
        if (speed === null || speed === undefined) return '#007bff'; // No data (blue)
        if (speed > 50) return '#28a745';  // > 50 km/h (green)
        if (speed > 30) return '#ffc107';  // 30-50 km/h (yellow)
        if (speed > 10) return '#dc3545';  // 10-30 km/h (red)
        return '#000000';                   // < 10 km/h (black)
    }
};
