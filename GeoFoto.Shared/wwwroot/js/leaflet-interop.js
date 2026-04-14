window.leafletInterop = {
    map: null,
    markers: [],
    _clusterGroup: null,

    initMap: function (elementId, lat, lng, zoom) {
        if (this.map) {
            this.map.remove();
        }
        this.map = L.map(elementId).setView([lat, lng], zoom);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OSM</a>',
            maxZoom: 19
        }).addTo(this.map);

        this._clusterGroup = L.markerClusterGroup();
        this.map.addLayer(this._clusterGroup);
    },

    addMarkers: function (puntos, dotnetRef) {
        // Clear existing markers
        this._clusterGroup.clearLayers();
        this.markers = [];

        puntos.forEach(p => {
            var marker = L.marker([p.latitud, p.longitud])
                .bindPopup(p.nombre || `Punto #${p.id} (${p.cantidadFotos} fotos)`);

            marker.on('click', function () {
                dotnetRef.invokeMethodAsync('OnMarkerClicked', p.id);
            });

            this.markers.push(marker);
        });

        this._clusterGroup.addLayers(this.markers);

        if (puntos.length > 0) {
            this.map.fitBounds(this._clusterGroup.getBounds().pad(0.1));
        }
    },

    invalidateSize: function () {
        if (this.map) {
            this.map.invalidateSize();
        }
    }
};
