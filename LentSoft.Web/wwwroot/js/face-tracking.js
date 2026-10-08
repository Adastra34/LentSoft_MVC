// face-tracking.js - LentSoft AR Try-On Module
// Diseñado para alto rendimiento en celulares de gama baja, lazy loading, suavizado y tolerancia a fallos.

let FaceLandmarkerModule = null;
let FilesetResolverModule = null;
let faceLandmarker = null;
let activeLoop = false;
let currentMonturaImg = new Image();
let currentMonturaUrl = null;
let animationFrameId = null;
let videoEl = null;
let canvasEl = null;
let ctx = null;

// Configuración de calibración de overlay activa
let currentOverlayConfig = {
    escala: 2.30,
    offsetX: 0.00,
    offsetY: 0.12
};

// Control de FPS y rendimiento adaptativo
let targetFps = 30; // Objetivo inicial 30 FPS
let targetIntervalMs = 1000 / targetFps;
let lastDetectTimestamp = 0;
let recentDetectTimes = [];
const MAX_SAMPLES = 10;

// Variables para suavizado (Smoothing)
let smoothedState = {
    midX: null,
    midY: null,
    width: null,
    height: null,
    angle: null,
    initialized: false
};
const SMOOTH_FACTOR = 0.40; // 0 = sin cambio, 1 = instantáneo sin filtro

// Tolerancia a pérdida de rostro (Frame drop grace period)
let lostFaceFrames = 0;
const MAX_GRACE_FRAMES = 12; // ~400ms de persistencia para evitar parpadeos
let lastKnownValidState = null;

// Detección de poca luz (Low light)
let lightCheckCounter = 0;
let lightCanvas = null;
let lightCtx = null;
let isLowLight = false;

// 1. Validación de compatibilidad antes de iniciar
export function validarCompatibilidad() {
    // A) Conexión segura (HTTPS o localhost)
    const isLocalhost = window.location.hostname === 'localhost' || 
                          window.location.hostname === '127.0.0.1' || 
                          window.location.hostname === '[::1]';
    const isHttps = window.location.protocol === 'https:';
    const isSecure = window.isSecureContext || isLocalhost || isHttps;

    if (!isSecure) {
        return {
            compatible: false,
            tipo: 'insecure_context',
            mensaje: 'Se requiere una conexión segura (HTTPS o localhost) para acceder a la cámara web. Por favor ingresa usando https://'
        };
    }

    // B) Soporte de getUserMedia (WebRTC)
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        return {
            compatible: false,
            tipo: 'no_getusermedia',
            mensaje: 'Tu navegador o dispositivo no soporta acceso a la cámara mediante WebRTC. Te recomendamos actualizar tu navegador (Chrome, Safari, Firefox o Edge).'
        };
    }

    // C) Soporte de WebAssembly
    const hasWasm = typeof WebAssembly === 'object' && typeof WebAssembly.instantiate === 'function';
    if (!hasWasm) {
        return {
            compatible: false,
            tipo: 'no_wasm',
            mensaje: 'Tu navegador no tiene activado el soporte para WebAssembly, necesario para el detector facial en tiempo real. Por favor habilita WebAssembly o usa un navegador moderno.'
        };
    }

    return { compatible: true };
}

// 2. Carga perezosa (Lazy load) de MediaPipe
async function initLandmarker() {
    if (faceLandmarker) return faceLandmarker;

    try {
        console.log("Cargando módulos de MediaPipe Vision (Lazy Load)...");
        // Import dinámico para no bloquear la carga inicial de la página
        const visionBundle = await import("https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.8/vision_bundle.mjs");
        FaceLandmarkerModule = visionBundle.FaceLandmarker;
        FilesetResolverModule = visionBundle.FilesetResolver;

        const vision = await FilesetResolverModule.forVisionTasks(
            "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.8/wasm"
        );

        faceLandmarker = await FaceLandmarkerModule.createFromOptions(vision, {
            baseOptions: {
                modelAssetPath: "https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task",
                delegate: "GPU"
            },
            runningMode: "VIDEO",
            numFaces: 1
        });

        console.log("MediaPipe FaceLandmarker inicializado con éxito.");
        return faceLandmarker;
    } catch (error) {
        console.error("Error al inicializar MediaPipe FaceLandmarker:", error);
        throw error;
    }
}

// 3. Función auxiliar para interpolación lineal (Lerp)
function lerp(start, end, factor) {
    return start + (end - start) * factor;
}

// 4. Muestreo de luminancia para detectar poca luz
function checkLuminance(video) {
    try {
        if (!lightCanvas) {
            lightCanvas = document.createElement('canvas');
            lightCanvas.width = 16;
            lightCanvas.height = 16;
            lightCtx = lightCanvas.getContext('2d', { willReadFrequently: true });
        }
        if (!lightCtx || !video.videoWidth) return;

        lightCtx.drawImage(video, 0, 0, 16, 16);
        const imgData = lightCtx.getImageData(0, 0, 16, 16).data;
        let totalLuma = 0;
        const count = imgData.length / 4;

        for (let i = 0; i < imgData.length; i += 4) {
            const r = imgData[i];
            const g = imgData[i + 1];
            const b = imgData[i + 2];
            // Estándar ITU-R BT.601
            totalLuma += 0.299 * r + 0.587 * g + 0.114 * b;
        }

        const avgLuma = totalLuma / count;
        const lowLightEl = document.getElementById('lowLightWarning');
        if (avgLuma < 38) { // Umbral de baja iluminación
            if (!isLowLight) {
                isLowLight = true;
                if (lowLightEl) lowLightEl.style.display = 'block';
            }
        } else if (avgLuma > 48) {
            if (isLowLight) {
                isLowLight = false;
                if (lowLightEl) lowLightEl.style.display = 'none';
            }
        }
    } catch (e) {
        // En caso de contexto restringido no bloquear
    }
}

// 5. Iniciar ciclo de detección optimizado
function iniciarDeteccion(video, canvas) {
    if (!faceLandmarker) {
        console.warn("FaceLandmarker aún no está listo.");
        return;
    }

    videoEl = video;
    canvasEl = canvas;
    ctx = canvas.getContext('2d');
    activeLoop = true;
    lastDetectTimestamp = performance.now();
    recentDetectTimes = [];
    lostFaceFrames = 0;
    lastKnownValidState = null;
    smoothedState.initialized = false;

    function renderLoop(currentTimestamp) {
        if (!activeLoop) return;

        if (videoEl && videoEl.readyState >= 2) {
            // Sincronizar dimensiones del canvas con el video de forma óptima
            if (canvasEl.width !== videoEl.videoWidth || canvasEl.height !== videoEl.videoHeight) {
                canvasEl.width = videoEl.videoWidth || 640;
                canvasEl.height = videoEl.videoHeight || 480;
            }

            const elapsedSinceLastDetect = currentTimestamp - lastDetectTimestamp;

            // Ejecutar detección solo a la tasa configurada (24-30 fps o menor en dispositivos lentos)
            if (elapsedSinceLastDetect >= targetIntervalMs) {
                lastDetectTimestamp = currentTimestamp;
                const detectStart = performance.now();

                try {
                    const results = faceLandmarker.detectForVideo(videoEl, currentTimestamp);
                    const detectDuration = performance.now() - detectStart;

                    // Ajuste adaptativo de FPS si el dispositivo es lento
                    recentDetectTimes.push(detectDuration);
                    if (recentDetectTimes.length > MAX_SAMPLES) recentDetectTimes.shift();

                    const avgDetectDuration = recentDetectTimes.reduce((a, b) => a + b, 0) / recentDetectTimes.length;
                    if (avgDetectDuration > 40 && targetFps > 18) {
                        // Dispositivo lento detectado: bajar frecuencia a 20 FPS para no congelar
                        targetFps = 20;
                        targetIntervalMs = 1000 / targetFps;
                    } else if (avgDetectDuration > 55 && targetFps > 15) {
                        targetFps = 15;
                        targetIntervalMs = 1000 / targetFps;
                    } else if (avgDetectDuration < 20 && targetFps < 30) {
                        // Dispositivo recuperado
                        targetFps = 30;
                        targetIntervalMs = 1000 / targetFps;
                    }

                    // Muestrear iluminación cada ~30 fotogramas
                    lightCheckCounter++;
                    if (lightCheckCounter % 30 === 0) {
                        checkLuminance(videoEl);
                    }

                    const hasFace = results && results.faceLandmarks && results.faceLandmarks.length > 0;

                    if (hasFace) {
                        lostFaceFrames = 0;
                        const landmarks = results.faceLandmarks[0];

                        // Landmarks de iris (468: centro iris izq, 473: centro iris der)
                        if (landmarks[468] && landmarks[473]) {
                            const lx = landmarks[468].x * canvasEl.width;
                            const ly = landmarks[468].y * canvasEl.height;
                            const rx = landmarks[473].x * canvasEl.width;
                            const ry = landmarks[473].y * canvasEl.height;

                            const rawMidX = (lx + rx) / 2;
                            const rawMidY = (ly + ry) / 2;
                            const dx = rx - lx;
                            const dy = ry - ly;
                            const rawDist = Math.sqrt(dx * dx + dy * dy);
                            const rawAngle = Math.atan2(dy, dx);

                            const escala = currentOverlayConfig.escala || 2.30;
                            const rawWidth = rawDist * escala;
                            const ratio = (currentMonturaImg.naturalWidth > 0)
                                ? (currentMonturaImg.naturalHeight / currentMonturaImg.naturalWidth)
                                : 0.45;
                            const rawHeight = rawWidth * ratio;

                            // Suavizado (Smoothing)
                            if (!smoothedState.initialized) {
                                smoothedState.midX = rawMidX;
                                smoothedState.midY = rawMidY;
                                smoothedState.width = rawWidth;
                                smoothedState.height = rawHeight;
                                smoothedState.angle = rawAngle;
                                smoothedState.initialized = true;
                            } else {
                                smoothedState.midX = lerp(smoothedState.midX, rawMidX, SMOOTH_FACTOR);
                                smoothedState.midY = lerp(smoothedState.midY, rawMidY, SMOOTH_FACTOR);
                                smoothedState.width = lerp(smoothedState.width, rawWidth, SMOOTH_FACTOR);
                                smoothedState.height = lerp(smoothedState.height, rawHeight, SMOOTH_FACTOR);
                                smoothedState.angle = lerp(smoothedState.angle, rawAngle, SMOOTH_FACTOR);
                            }

                            lastKnownValidState = {
                                midX: smoothedState.midX,
                                midY: smoothedState.midY,
                                width: smoothedState.width,
                                height: smoothedState.height,
                                angle: smoothedState.angle
                            };
                        }
                    } else {
                        lostFaceFrames++;
                    }

                    // Manejo del aviso de "Rostro no detectado" (con umbral de gracia)
                    const warningEl = document.getElementById('noFaceWarning');
                    if (warningEl) {
                        const showWarning = lostFaceFrames > MAX_GRACE_FRAMES;
                        warningEl.style.display = showWarning ? 'block' : 'none';
                    }

                } catch (err) {
                    console.error("Error en detección de video:", err);
                }
            }

            // Renderizado sobre canvas (suavizado y con persistencia por pérdida temporal)
            ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);

            const hasValidOverlay = currentMonturaUrl && currentMonturaImg.complete && currentMonturaImg.naturalWidth > 0;
            const canRenderFrame = (lostFaceFrames <= MAX_GRACE_FRAMES) && lastKnownValidState && hasValidOverlay;

            if (canRenderFrame) {
                const s = lastKnownValidState;
                // Si está perdiendo frames, aplicar desvanecimiento progresivo
                const alpha = Math.max(0.2, 1.0 - (lostFaceFrames / (MAX_GRACE_FRAMES + 1)) * 0.7);

                ctx.save();
                ctx.globalAlpha = alpha;
                ctx.translate(s.midX, s.midY);
                ctx.rotate(s.angle);

                const offsetX = s.width * (currentOverlayConfig.offsetX || 0.00);
                const offsetY = s.height * (currentOverlayConfig.offsetY !== undefined ? currentOverlayConfig.offsetY : 0.12);

                ctx.drawImage(
                    currentMonturaImg,
                    -s.width / 2 + offsetX,
                    -s.height / 2 + offsetY,
                    s.width,
                    s.height
                );
                ctx.restore();
            }
        }

        animationFrameId = requestAnimationFrame(renderLoop);
    }

    animationFrameId = requestAnimationFrame(renderLoop);
}

// 6. Detener detección y limpiar recursos de forma segura
function detenerDeteccion() {
    activeLoop = false;
    if (animationFrameId) {
        cancelAnimationFrame(animationFrameId);
        animationFrameId = null;
    }
    if (ctx && canvasEl) {
        ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);
    }
    const warningEl = document.getElementById('noFaceWarning');
    if (warningEl) warningEl.style.display = 'none';

    const lowLightEl = document.getElementById('lowLightWarning');
    if (lowLightEl) lowLightEl.style.display = 'none';

    lostFaceFrames = 0;
    lastKnownValidState = null;
    smoothedState.initialized = false;
}

// 7. Cambiar montura activa con soporte de parámetros de calibración
function cambiarMontura(url, config = {}) {
    // Actualizar configuración de calibración
    currentOverlayConfig = {
        escala: typeof config.escala === 'number' && config.escala > 0 ? config.escala : (parseFloat(config.escala) || 2.30),
        offsetX: typeof config.offsetX === 'number' ? config.offsetX : (parseFloat(config.offsetX) || 0.00),
        offsetY: typeof config.offsetY === 'number' ? config.offsetY : (parseFloat(config.offsetY) || 0.12)
    };

    if (!url || typeof url !== 'string' || url.trim() === '') {
        currentMonturaUrl = null;
        currentMonturaImg = new Image();
        if (ctx && canvasEl) {
            ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);
        }
        return;
    }

    if (currentMonturaUrl !== url) {
        currentMonturaUrl = url;
        const img = new Image();
        img.src = url;
        img.onload = () => {
            currentMonturaImg = img;
            console.log(`Montura overlay cargada correctamente: ${url}`);
        };
        img.onerror = () => {
            console.warn(`No se pudo cargar la imagen overlay: ${url}. La previsualización no fallará.`);
            currentMonturaUrl = null;
            if (ctx && canvasEl) {
                ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);
            }
        };
    }
}

// 8. API pública global expuesta en window.faceTracking
window.faceTracking = {
    init: initLandmarker,
    iniciarDeteccion: iniciarDeteccion,
    detenerDeteccion: detenerDeteccion,
    cambiarMontura: cambiarMontura,
    validarCompatibilidad: validarCompatibilidad,
    isReady: () => faceLandmarker !== null,
    getTargetFps: () => targetFps
};

window.dispatchEvent(new CustomEvent('faceTrackingModuleLoaded'));
