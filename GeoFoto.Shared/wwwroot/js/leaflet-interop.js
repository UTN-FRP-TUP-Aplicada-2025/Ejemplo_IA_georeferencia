window.leafletInterop = {
    map: null,
    markers: [],
    _clusterGroup: null,
    _dotnetRef: null,

    initMap: function (elementId, lat, lng, zoom) {
        try {
            if (this.map) {
                this.map.remove();
                this.map = null;
            }
            var el = document.getElementById(elementId);
            if (!el) return;

            this.map = L.map(elementId, { zoomControl: true }).setView([lat, lng], zoom);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OSM</a>',
                maxZoom: 19
            }).addTo(this.map);

            this._clusterGroup = L.markerClusterGroup({
                maxClusterRadius: 60,
                spiderfyOnMaxZoom: true,
                showCoverageOnHover: false,
                iconCreateFunction: function (cluster) {
                    var count = cluster.getChildCount();
                    var size = count < 10 ? 'small' : count < 50 ? 'medium' : 'large';
                    return L.divIcon({
                        html: '<div class="cluster-' + size + '"><span>' + count + '</span></div>',
                        className: 'marker-cluster',
                        iconSize: L.point(40, 40)
                    });
                }
            });
            this.map.addLayer(this._clusterGroup);
        } catch (e) { console.error('[GeoFoto] initMap:', e); }
    },

    addMarkers: function (puntos, dotnetRef) {
        if (!this.map || !this._clusterGroup) return;
        try {
            this._dotnetRef = dotnetRef;
            this._clusterGroup.clearLayers();
            this.markers = [];

            puntos.forEach(function (p) {
                var color = '#4CAF50'; // default green for synced
                if (p.syncStatus === 'Conflict') color = '#F44336';
                else if (p.syncStatus === 'PendingCreate' || p.syncStatus === 'PendingUpdate') color = '#FF9800';

                var icon = L.divIcon({
                    html: '<div style="background:' + color + ';width:14px;height:14px;' +
                        'border-radius:50%;border:2px solid white;' +
                        'box-shadow:0 1px 4px rgba(0,0,0,.4);"></div>',
                    className: '',
                    iconSize: [18, 18],
                    iconAnchor: [9, 9]
                });

                var marker = L.marker([parseFloat(p.latitud), parseFloat(p.longitud)], { icon: icon })
                    .bindPopup(p.nombre || ('Punto #' + p.id + ' (' + p.cantidadFotos + ' fotos)'));

                marker.on('click', function () {
                    if (dotnetRef) {
                        dotnetRef.invokeMethodAsync('OnMarkerClicked', p.id)
                            .catch(function (e) { console.warn('[GeoFoto]', e); });
                    }
                });

                this.markers.push(marker);
            }.bind(this));

            this._clusterGroup.addLayers(this.markers);

            if (puntos.length > 0 && this._clusterGroup.getBounds().isValid()) {
                this.map.fitBounds(this._clusterGroup.getBounds().pad(0.1), { maxZoom: 14 });
            }
        } catch (e) { console.error('[GeoFoto] addMarkers:', e); }
    },

    centerOn: function (lat, lng) {
        if (this.map) this.map.flyTo([lat, lng], 16, { duration: 0.8 });
    },

    invalidateSize: function () {
        if (this.map) {
            this.map.invalidateSize();
        }
    }
};
