window.geoFotoUtils = {

    // Obtener los insets reales del sistema (status bar y nav bar)
    getWindowInsets: function () {
        var style = getComputedStyle(document.documentElement);
        return {
            top:    parseInt(style.getPropertyValue('--sat') || '0'),
            bottom: parseInt(style.getPropertyValue('--sab') || '0'),
            left:   parseInt(style.getPropertyValue('--sal') || '0'),
            right:  parseInt(style.getPropertyValue('--sar') || '0')
        };
    },

    // Aplicar insets al body como CSS variables
    applyInsets: function () {
        var meta = document.querySelector('meta[name="viewport"]');
        if (meta) {
            meta.content = 'width=device-width, initial-scale=1.0, viewport-fit=cover';
        }
        document.documentElement.style.setProperty(
            '--status-bar-height',
            'env(safe-area-inset-top, 24px)');
        document.documentElement.style.setProperty(
            '--nav-bar-height',
            'env(safe-area-inset-bottom, 16px)');
    }
};

// Aplicar automáticamente al cargar
window.addEventListener('load', function () { window.geoFotoUtils.applyInsets(); });
