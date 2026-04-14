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

    centrarEnUbicacion: function (dotnetRef) {
        if (!this.map) return;
        if (!navigator.geolocation) {
            if (dotnetRef) dotnetRef.invokeMethodAsync('OnGpsError', 'GPS no soportado');
            return;
        }
        navigator.geolocation.getCurrentPosition(
            function (pos) {
                var lat = pos.coords.latitude;
                var lng = pos.coords.longitude;
                var acc = pos.coords.accuracy;

                this.map.flyTo([lat, lng], 16, { duration: 1.2 });

                if (this._gpsCircle) this._gpsCircle.remove();
                if (this._gpsMarker) this._gpsMarker.remove();

                this._gpsCircle = L.circle([lat, lng], {
                    radius: acc,
                    color: '#2196F3', fillColor: '#2196F3',
                    fillOpacity: 0.12, weight: 1.5
                }).addTo(this.map);

                var gpsIcon = L.divIcon({
                    html: '<div style="width:14px;height:14px;background:#2196F3;' +
                          'border:2px solid white;border-radius:50%;' +
                          'box-shadow:0 0 0 4px rgba(33,150,243,.3)"></div>',
                    className: '', iconSize: [18,18], iconAnchor: [9,9]
                });
                this._gpsMarker = L.marker([lat, lng], { icon: gpsIcon })
                    .bindPopup('<b>Tu ubicación</b><br>Precisión: ' + Math.round(acc) + 'm')
                    .addTo(this.map);

                if (dotnetRef)
                    dotnetRef.invokeMethodAsync('OnGpsOk', lat, lng, acc);
            }.bind(this),
            function (err) {
                var msgs = {
                    1: 'Permiso de ubicación denegado',
                    2: 'Posición no disponible',
                    3: 'Tiempo de espera agotado'
                };
                if (dotnetRef)
                    dotnetRef.invokeMethodAsync('OnGpsError', msgs[err.code] || err.message);
            },
            { enableHighAccuracy: true, timeout: 10000, maximumAge: 30000 }
        );
    },

    centrarEnCoordenadas: function (lat, lng, zoom) {
        if (this.map) this.map.flyTo([lat, lng], zoom || 16, { duration: 0.8 });
    },

    mostrarPosicionGps: function (lat, lng, precision) {
        if (!this.map) return;

        if (this._gpsCircle) this._gpsCircle.remove();
        if (this._gpsMarker) this._gpsMarker.remove();

        this._gpsCircle = L.circle([lat, lng], {
            radius: precision,
            color: '#2196F3', fillColor: '#2196F3',
            fillOpacity: 0.12, weight: 1.5
        }).addTo(this.map);

        var gpsIcon = L.divIcon({
            html: '<div style="width:14px;height:14px;background:#2196F3;' +
                  'border:2px solid white;border-radius:50%;' +
                  'box-shadow:0 0 0 4px rgba(33,150,243,.3)"></div>',
            className: '', iconSize: [18,18], iconAnchor: [9,9]
        });
        this._gpsMarker = L.marker([lat, lng], { icon: gpsIcon })
            .bindPopup('<b>Tu ubicación</b><br>Precisión: ' + Math.round(precision) + 'm')
            .addTo(this.map);

        this.map.flyTo([lat, lng], 16, { duration: 1.2 });
    },

    invalidateSize: function () {
        if (this.map) {
            this.map.invalidateSize();
        }
    }
};
