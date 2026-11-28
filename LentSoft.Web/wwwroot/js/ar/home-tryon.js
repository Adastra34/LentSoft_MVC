// home-tryon.js - Mini prueba de Face Tracking en vivo en la página de inicio (carga diferida)
(function () {
    const startBtn = document.getElementById('homeTryOnStartBtn');
    const stopBtn = document.getElementById('homeTryOnStopBtn');
    const videoEl = document.getElementById('homeTryOnVideo');
    const canvasEl = document.getElementById('homeTryOnCanvas');
    const placeholderEl = document.getElementById('homeTryOnPlaceholder');
    const statusBadge = document.getElementById('homeTryOnStatus');
    const errorBox = document.getElementById('homeTryOnError');
    const errorMsgEl = document.getElementById('homeTryOnErrorMsg');
    const retryBtn = document.getElementById('homeTryOnRetryBtn');
    const sectionEl = document.getElementById('homeTryOnSection');

    if (!startBtn || !videoEl || !canvasEl) return;

    let tryOnInstance = null;
    let isBusy = false;

    function setStatus(text, tone) {
        if (!statusBadge) return;
        statusBadge.textContent = text;
        statusBadge.style.display = text ? 'inline-flex' : 'none';
        if (tone === 'active') {
            statusBadge.style.background = 'rgba(16, 185, 129, 0.9)';
        } else if (tone === 'warn') {
            statusBadge.style.background = 'rgba(217, 119, 6, 0.92)';
        } else {
            statusBadge.style.background = 'rgba(88, 28, 135, 0.9)';
        }
    }

    function showError(msg) {
        if (errorMsgEl) errorMsgEl.textContent = msg;
        if (errorBox) errorBox.style.display = 'flex';
        if (placeholderEl) placeholderEl.style.display = 'none';
        if (videoEl) videoEl.style.display = 'none';
        if (canvasEl) canvasEl.style.display = 'none';
        if (stopBtn) stopBtn.style.display = 'none';
        setStatus('', '');
    }

    function resetToPlaceholder() {
        if (errorBox) errorBox.style.display = 'none';
        if (placeholderEl) placeholderEl.style.display = 'flex';
        if (videoEl) videoEl.style.display = 'none';
        if (canvasEl) canvasEl.style.display = 'none';
        if (stopBtn) stopBtn.style.display = 'none';
        setStatus('', '');
        startBtn.disabled = false;
        startBtn.textContent = 'Activar cámara';
    }

    async function startDemo() {
        if (isBusy) return;
        isBusy = true;
        startBtn.disabled = true;
        startBtn.textContent = 'Cargando...';
        if (errorBox) errorBox.style.display = 'none';
        setStatus('Solicitando permiso…', 'info');

        try {
            const { FaceTryOn } = await import('/js/ar/face-tracker-core.js');
            const cfg = window.HOME_TRYON_DATA || {};

            if (tryOnInstance) {
                tryOnInstance.dispose();
                tryOnInstance = null;
            }

            tryOnInstance = FaceTryOn.mount({
                video: videoEl,
                canvas: canvasEl,
                frameSrc: cfg.frameSrc || '',
                frameType: cfg.frameType || 'png',
                fallbackSrc: cfg.fallbackSrc || '',
                config: cfg.config || {},
                onState: (state, detail) => {
                    if (state === 'requesting') {
                        setStatus('Solicitando permiso…', 'info');
                    } else if (state === 'active') {
                        if (placeholderEl) placeholderEl.style.display = 'none';
                        videoEl.style.display = 'block';
                        canvasEl.style.display = 'block';
                        if (stopBtn) stopBtn.style.display = 'inline-flex';
                        setStatus('Cámara activa', 'active');
                    } else if (state === 'no-face') {
                        setStatus('No se detectó rostro', 'warn');
                    } else if (state === 'error') {
                        showError(detail || 'No se pudo iniciar la cámara.');
                    } else if (state === 'stopped') {
                        resetToPlaceholder();
                    }
                }
            });

            await tryOnInstance.start();
        } catch (err) {
            console.error('Error en mini prueba AR de inicio:', err);
            showError('No se pudo inicializar el visor de prueba virtual.');
        } finally {
            isBusy = false;
            if (!tryOnInstance || !tryOnInstance.isRunning()) {
                startBtn.disabled = false;
                startBtn.textContent = 'Activar cámara';
            }
        }
    }

    function stopDemo() {
        if (tryOnInstance) {
            tryOnInstance.stop();
        }
        resetToPlaceholder();
    }

    startBtn.addEventListener('click', startDemo);
    if (stopBtn) stopBtn.addEventListener('click', stopDemo);
    if (retryBtn) retryBtn.addEventListener('click', startDemo);

    // Detener cámara automáticamente al ocultar la pestaña o salir
    document.addEventListener('visibilitychange', () => {
        if (document.hidden && tryOnInstance && tryOnInstance.isRunning()) {
            stopDemo();
        }
    });
    window.addEventListener('pagehide', () => {
        if (tryOnInstance) tryOnInstance.dispose();
    });

    // Detener cámara al salir de la sección mediante scroll
    if (sectionEl && 'IntersectionObserver' in window) {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (!entry.isIntersecting && tryOnInstance && tryOnInstance.isRunning()) {
                    stopDemo();
                }
            });
        }, { threshold: 0.05 });
        observer.observe(sectionEl);
    }
})();
