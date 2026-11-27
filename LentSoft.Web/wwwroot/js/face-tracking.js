// face-tracking.js - Adaptador de window.faceTracking sobre el núcleo compartido face-tracker-core.js
import {
    validarCompatibilidad,
    initLandmarker,
    isLandmarkerReady,
    FaceTryOn
} from './ar/face-tracker-core.js';

let activeInstance = null;
let pendingFrame = {
    url: null,
    type: 'png',
    config: {},
    fallbackUrl: null
};

export function obtenerCalibracionDesdeVista(canvasEl) {
    const el = (canvasEl && (canvasEl.getAttribute('data-escala') || canvasEl.dataset?.escala))
        ? canvasEl
        : document.getElementById('cameraPreviewContainer');

    if (el) {
        const dEscala = el.getAttribute('data-escala') || el.dataset?.escala;
        const dOffsetX = el.getAttribute('data-offset-x') || el.dataset?.offsetX;
        const dOffsetY = el.getAttribute('data-offset-y') || el.dataset?.offsetY;

        const parsedEscala = parseFloat(dEscala);
        const parsedOffsetX = parseFloat(dOffsetX);
        const parsedOffsetY = parseFloat(dOffsetY);

        return {
            escala: !isNaN(parsedEscala) && parsedEscala > 0 ? parsedEscala : 2.30,
            offsetX: !isNaN(parsedOffsetX) ? parsedOffsetX : 0.00,
            offsetY: !isNaN(parsedOffsetY) ? parsedOffsetY : 0.12
        };
    }

    return { escala: 2.30, offsetX: 0.00, offsetY: 0.12 };
}

function handleStateChange(state, detail) {
    const warningEl = document.getElementById('noFaceWarning');
    const lowLightEl = document.getElementById('lowLightWarning');

    if (state === 'no-face' && warningEl) {
        warningEl.style.display = 'block';
    } else if (state === 'active' && warningEl) {
        warningEl.style.display = 'none';
    } else if (state === 'low-light' && lowLightEl) {
        lowLightEl.style.display = detail ? 'block' : 'none';
    } else if (state === 'stopped') {
        if (warningEl) warningEl.style.display = 'none';
        if (lowLightEl) lowLightEl.style.display = 'none';
    }
}

function iniciarDeteccion(video, canvas) {
    if (!isLandmarkerReady()) {
        console.warn('FaceLandmarker aún no está listo.');
        return;
    }

    if (activeInstance) {
        activeInstance.stopLoop();
    }

    const viewCfg = obtenerCalibracionDesdeVista(canvas);
    activeInstance = FaceTryOn.mount({
        video,
        canvas,
        frameSrc: pendingFrame.url,
        frameType: pendingFrame.type,
        fallbackSrc: pendingFrame.fallbackUrl,
        config: { ...viewCfg, ...pendingFrame.config },
        onState: handleStateChange
    });

    activeInstance.startLoop();
}

function detenerDeteccion() {
    if (activeInstance) {
        activeInstance.stopLoop();
    }
    handleStateChange('stopped');
}

function cambiarMontura(url, config = {}) {
    const viewCfg = obtenerCalibracionDesdeVista();
    const resolvedConfig = {
        escala: config.escala ?? viewCfg.escala,
        offsetX: config.offsetX ?? viewCfg.offsetX,
        offsetY: config.offsetY ?? viewCfg.offsetY,
        offsetZ: config.offsetZ ?? 0,
        rotationX: config.rotationX ?? 0,
        rotationY: config.rotationY ?? 0,
        rotationZ: config.rotationZ ?? 0,
        scale3d: config.scale3d ?? 1.0
    };

    const type = (config.modelType || '').toLowerCase() === 'glb' ? 'glb' : 'png';
    const fallbackUrl = config.fallbackUrl || null;

    pendingFrame = { url, type, config: resolvedConfig, fallbackUrl };

    if (activeInstance) {
        activeInstance.setFrame(url, type, resolvedConfig, fallbackUrl);
    }
}

window.faceTracking = {
    init: initLandmarker,
    iniciarDeteccion,
    detenerDeteccion,
    cambiarMontura,
    validarCompatibilidad,
    obtenerCalibracionDesdeVista,
    isReady: isLandmarkerReady,
    getTargetFps: () => (activeInstance ? activeInstance.getTargetFps() : 30)
};

window.dispatchEvent(new CustomEvent('faceTrackingModuleLoaded'));
