// face-tracker-core.js - Núcleo compartido de Face Tracking 2D y 3D para LentSoft AR
// Reutilizado por la mini prueba de inicio, MuestraMontura y el visor de prueba del Admin.

let FaceLandmarkerModule = null;
let FilesetResolverModule = null;
let faceLandmarker = null;
let initPromise = null;

// Carga perezosa de Three.js (solo cuando se usa un modelo .glb)
let threeLibPromise = null;
async function loadThreeLibs() {
    if (!threeLibPromise) {
        threeLibPromise = Promise.all([
            import('/lib/three/three.module.min.js'),
            import('/lib/three/GLTFLoader.js')
        ]).then(([THREE, loaderMod]) => ({
            THREE,
            GLTFLoader: loaderMod.GLTFLoader
        })).catch((err) => {
            threeLibPromise = null;
            throw err;
        });
    }
    return threeLibPromise;
}

export function validarCompatibilidad() {
    const isLocalhost = window.location.hostname === 'localhost' ||
        window.location.hostname === '127.0.0.1' ||
        window.location.hostname === '[::1]';
    const isHttps = window.location.protocol === 'https:';
    const isSecure = window.isSecureContext || isLocalhost || isHttps;

    if (!isSecure) {
        return {
            compatible: false,
            tipo: 'insecure_context',
            mensaje: 'Se requiere una conexión segura (HTTPS o localhost) para acceder a la cámara web.'
        };
    }

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        return {
            compatible: false,
            tipo: 'no_getusermedia',
            mensaje: 'Tu navegador no soporta acceso a la cámara mediante WebRTC. Prueba con Chrome, Edge o Safari actualizado.'
        };
    }

    const hasWasm = typeof WebAssembly === 'object' && typeof WebAssembly.instantiate === 'function';
    if (!hasWasm) {
        return {
            compatible: false,
            tipo: 'no_wasm',
            mensaje: 'Tu navegador no tiene activado soporte para WebAssembly, necesario para el reconocimiento facial.'
        };
    }

    return { compatible: true };
}

async function createLandmarkerWithDelegate(faceLandmarkerClass, vision, modelPath) {
    try {
        return await faceLandmarkerClass.createFromOptions(vision, {
            baseOptions: { modelAssetPath: modelPath, delegate: 'GPU' },
            runningMode: 'VIDEO',
            numFaces: 1
        });
    } catch (gpuError) {
        console.warn('Fallo al inicializar FaceLandmarker con GPU. Reintentando con CPU...', gpuError);
        return await faceLandmarkerClass.createFromOptions(vision, {
            baseOptions: { modelAssetPath: modelPath, delegate: 'CPU' },
            runningMode: 'VIDEO',
            numFaces: 1
        });
    }
}

async function loadLandmarkerFromSources(bundleUrl, wasmUrl, modelPath) {
    const visionBundle = await import(bundleUrl);
    FaceLandmarkerModule = visionBundle.FaceLandmarker;
    FilesetResolverModule = visionBundle.FilesetResolver;
    const vision = await FilesetResolverModule.forVisionTasks(wasmUrl);
    return await createLandmarkerWithDelegate(FaceLandmarkerModule, vision, modelPath);
}

export async function initLandmarker() {
    if (faceLandmarker) return faceLandmarker;
    if (initPromise) return initPromise;

    initPromise = (async () => {
        try {
            faceLandmarker = await loadLandmarkerFromSources(
                '/lib/mediapipe/vision_bundle.mjs',
                '/lib/mediapipe/wasm',
                '/lib/mediapipe/models/face_landmarker.task'
            );
            return faceLandmarker;
        } catch (localError) {
            console.warn('MediaPipe local falló, reintentando con CDN...', localError);
            try {
                faceLandmarker = await loadLandmarkerFromSources(
                    'https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.8/vision_bundle.mjs',
                    'https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.8/wasm',
                    'https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task'
                );
                return faceLandmarker;
            } catch (cdnError) {
                console.error('Error definitivo al inicializar MediaPipe FaceLandmarker:', cdnError);
                throw cdnError;
            }
        } finally {
            if (!faceLandmarker) {
                initPromise = null;
            }
        }
    })();

    return initPromise;
}

export function isLandmarkerReady() {
    return faceLandmarker !== null;
}

function lerp(start, end, factor) {
    return start + (end - start) * factor;
}

function disposeThreeObject(obj) {
    if (!obj) return;
    obj.traverse((child) => {
        if (child.geometry) {
            child.geometry.dispose();
        }
        if (child.material) {
            const mats = Array.isArray(child.material) ? child.material : [child.material];
            mats.forEach((m) => {
                if (m && typeof m.dispose === 'function') {
                    Object.keys(m).forEach((key) => {
                        const val = m[key];
                        if (val && typeof val.dispose === 'function') val.dispose();
                    });
                    m.dispose();
                }
            });
        }
    });
}

/**
 * Monta una instancia de prueba virtual sobre un <video> y un <canvas> (y soporte opcional 3D WebGL).
 */
export const FaceTryOn = {
    mount(options) {
        const videoEl = options.video;
        const canvasEl = options.canvas;
        const threeCanvasEl = options.threeCanvas || null;
        const onState = typeof options.onState === 'function' ? options.onState : () => {};
        const onError = typeof options.onError === 'function' ? options.onError : () => {};

        const ctx = canvasEl ? canvasEl.getContext('2d') : null;
        const SMOOTH_FACTOR = 0.40;
        const MAX_GRACE_FRAMES = 12;

        let activeLoop = false;
        let animationFrameId = null;
        let stream = null;
        let targetFps = 30;
        let targetIntervalMs = 1000 / targetFps;
        let lastDetectTimestamp = 0;
        let recentDetectTimes = [];
        let lostFaceFrames = 0;
        let lastKnownValidState = null;

        let lightCheckCounter = 0;
        let lightCanvas = null;
        let lightCtx = null;
        let isLowLight = false;

        let smoothedState = {
            midX: null,
            midY: null,
            width: null,
            height: null,
            angle: null,
            yaw: 0,
            pitch: 0,
            initialized: false
        };

        // Estado del frame activo (2D o 3D)
        let frameConfig = {
            escala: 2.30,
            offsetX: 0.00,
            offsetY: 0.12,
            offsetZ: 0.00,
            rotationX: 0.00,
            rotationY: 0.00,
            rotationZ: 0.00,
            scale3d: 1.00
        };
        let currentFrameUrl = null;
        let currentFallbackUrl = options.fallbackSrc || null;
        let currentFrameType = 'png'; // 'png' | 'glb'
        let currentMonturaImg = new Image();

        // Estado Three.js (para modelos .glb sobre rostro)
        let threeState = {
            renderer: null,
            scene: null,
            camera: null,
            modelGroup: null,
            loadedModel: null,
            webglFailed: false,
            loadingToken: 0,
            canvas: null
        };

        function getOrCreateThreeCanvas() {
            if (threeState.canvas) return threeState.canvas;
            if (threeCanvasEl) {
                threeState.canvas = threeCanvasEl;
                return threeState.canvas;
            }
            if (!canvasEl || !canvasEl.parentElement) return null;

            const c = document.createElement('canvas');
            c.className = 'ar-three-overlay';
            c.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;display:none;transform:scaleX(-1);z-index:2;';
            canvasEl.parentElement.insertBefore(c, canvasEl.nextSibling);
            threeState.canvas = c;
            return c;
        }

        function cleanupThreeModel() {
            if (threeState.modelGroup && threeState.loadedModel) {
                threeState.modelGroup.remove(threeState.loadedModel);
                disposeThreeObject(threeState.loadedModel);
                threeState.loadedModel = null;
            }
            if (threeState.renderer && threeState.scene && threeState.camera) {
                threeState.renderer.clear();
            }
            if (threeState.canvas) {
                threeState.canvas.style.display = 'none';
            }
        }

        function disposeThreeAll() {
            cleanupThreeModel();
            if (threeState.renderer) {
                threeState.renderer.dispose();
                threeState.renderer = null;
            }
            threeState.scene = null;
            threeState.camera = null;
            threeState.modelGroup = null;
        }

        async function loadThreeModel(glbUrl) {
            const token = ++threeState.loadingToken;
            if (threeState.webglFailed) {
                fallbackTo2D();
                return;
            }

            try {
                const { THREE, GLTFLoader } = await loadThreeLibs();
                if (token !== threeState.loadingToken) return;

                const targetCanvas = getOrCreateThreeCanvas();
                if (!targetCanvas) {
                    fallbackTo2D();
                    return;
                }

                if (!threeState.renderer) {
                    const renderer = new THREE.WebGLRenderer({
                        canvas: targetCanvas,
                        alpha: true,
                        antialias: true,
                        powerPreference: 'high-performance'
                    });
                    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
                    renderer.setClearColor(0x000000, 0);

                    const scene = new THREE.Scene();
                    const camera = new THREE.PerspectiveCamera(45, 1, 0.01, 100);
                    camera.position.set(0, 0, 2);

                    const ambient = new THREE.AmbientLight(0xffffff, 1.35);
                    scene.add(ambient);
                    const dirLight = new THREE.DirectionalLight(0xffffff, 1.4);
                    dirLight.position.set(0.5, 1.5, 2.5);
                    scene.add(dirLight);
                    const fillLight = new THREE.DirectionalLight(0xf3e8ff, 0.55);
                    fillLight.position.set(-1, -0.5, 1.5);
                    scene.add(fillLight);

                    const modelGroup = new THREE.Group();
                    scene.add(modelGroup);

                    threeState.renderer = renderer;
                    threeState.scene = scene;
                    threeState.camera = camera;
                    threeState.modelGroup = modelGroup;
                }

                cleanupThreeModel();

                const loader = new GLTFLoader();
                const gltf = await new Promise((resolve, reject) => {
                    loader.load(glbUrl, resolve, undefined, reject);
                });

                if (token !== threeState.loadingToken) {
                    disposeThreeObject(gltf.scene);
                    return;
                }

                // Normalizar el modelo para que su ancho estándar sea 1.0 unidad centrada
                const wrapper = new THREE.Group();
                const root = gltf.scene;
                const box = new THREE.Box3().setFromObject(root);
                const size = new THREE.Vector3();
                const center = new THREE.Vector3();
                box.getSize(size);
                box.getCenter(center);

                const maxDim = Math.max(size.x, size.y, size.z, 0.001);
                const normScale = 1.0 / maxDim;
                root.position.sub(center);
                wrapper.add(root);
                wrapper.scale.setScalar(normScale);

                threeState.modelGroup.add(wrapper);
                threeState.loadedModel = wrapper;
                if (activeLoop && targetCanvas) {
                    targetCanvas.style.display = 'block';
                }
            } catch (err) {
                console.warn('Fallo al cargar modelo 3D o WebGL en rostro; usando respaldo 2D:', err);
                threeState.webglFailed = true;
                fallbackTo2D();
            }
        }

        function fallbackTo2D() {
            currentFrameType = 'png';
            cleanupThreeModel();
            if (currentFallbackUrl) {
                load2DImage(currentFallbackUrl);
            }
        }

        function load2DImage(url) {
            if (!url || typeof url !== 'string' || url.trim() === '') {
                currentMonturaImg = new Image();
                if (ctx && canvasEl) ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);
                return;
            }
            const img = new Image();
            img.src = url;
            img.onload = () => {
                currentMonturaImg = img;
            };
            img.onerror = () => {
                currentMonturaImg = new Image();
                if (ctx && canvasEl) ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);
            };
        }

        function updateConfig(newConfig = {}) {
            if (newConfig.escala !== undefined && newConfig.escala !== null && !isNaN(parseFloat(newConfig.escala))) {
                frameConfig.escala = parseFloat(newConfig.escala);
            }
            if (newConfig.scale3d !== undefined && newConfig.scale3d !== null && !isNaN(parseFloat(newConfig.scale3d))) {
                frameConfig.scale3d = parseFloat(newConfig.scale3d);
            }
            if (newConfig.offsetX !== undefined && newConfig.offsetX !== null && !isNaN(parseFloat(newConfig.offsetX))) {
                frameConfig.offsetX = parseFloat(newConfig.offsetX);
            }
            if (newConfig.offsetY !== undefined && newConfig.offsetY !== null && !isNaN(parseFloat(newConfig.offsetY))) {
                frameConfig.offsetY = parseFloat(newConfig.offsetY);
            }
            if (newConfig.offsetZ !== undefined && newConfig.offsetZ !== null && !isNaN(parseFloat(newConfig.offsetZ))) {
                frameConfig.offsetZ = parseFloat(newConfig.offsetZ);
            }
            if (newConfig.rotationX !== undefined && newConfig.rotationX !== null && !isNaN(parseFloat(newConfig.rotationX))) {
                frameConfig.rotationX = parseFloat(newConfig.rotationX);
            }
            if (newConfig.rotationY !== undefined && newConfig.rotationY !== null && !isNaN(parseFloat(newConfig.rotationY))) {
                frameConfig.rotationY = parseFloat(newConfig.rotationY);
            }
            if (newConfig.rotationZ !== undefined && newConfig.rotationZ !== null && !isNaN(parseFloat(newConfig.rotationZ))) {
                frameConfig.rotationZ = parseFloat(newConfig.rotationZ);
            }
        }

        function setFrame(src, type = 'png', config = {}, fallbackUrl = null) {
            updateConfig(config);
            if (fallbackUrl !== null) {
                currentFallbackUrl = fallbackUrl;
            }
            const normalizedType = (type || '').toLowerCase() === 'glb' ? 'glb' : 'png';

            if (!src || typeof src !== 'string' || src.trim() === '') {
                currentFrameUrl = null;
                currentFrameType = 'png';
                cleanupThreeModel();
                load2DImage('');
                return;
            }

            if (normalizedType === 'glb') {
                currentFrameUrl = src;
                currentFrameType = 'glb';
                if (ctx && canvasEl) ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);
                loadThreeModel(src);
            } else {
                currentFrameUrl = src;
                currentFrameType = 'png';
                cleanupThreeModel();
                load2DImage(src);
            }
        }

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
                for (let i = 0; i < imgData.length; i += 4) {
                    totalLuma += 0.299 * imgData[i] + 0.587 * imgData[i + 1] + 0.114 * imgData[i + 2];
                }
                const avgLuma = totalLuma / (imgData.length / 4);
                if (avgLuma < 38 && !isLowLight) {
                    isLowLight = true;
                    onState('low-light', true);
                } else if (avgLuma > 48 && isLowLight) {
                    isLowLight = false;
                    onState('low-light', false);
                }
            } catch {
                // ignorar en contextos restringidos
            }
        }

        function render3D(s) {
            const { renderer, scene, camera, modelGroup, loadedModel, canvas: tCanvas } = threeState;
            if (!renderer || !scene || !camera || !modelGroup || !loadedModel || !tCanvas) return;

            const w = canvasEl.width || 640;
            const h = canvasEl.height || 480;
            if (tCanvas.width !== w || tCanvas.height !== h) {
                tCanvas.width = w;
                tCanvas.height = h;
                renderer.setSize(w, h, false);
                camera.aspect = w / h;
                camera.updateProjectionMatrix();
            }

            // Convertir coordenadas de pantalla a mundo en z=0 para cámara perspectiva (fov=45, z=2)
            const vFov = (camera.fov * Math.PI) / 180;
            const worldHeight = 2 * Math.tan(vFov / 2) * camera.position.z;
            const worldWidth = worldHeight * camera.aspect;

            const nx = (s.midX / w) - 0.5;
            const ny = 0.5 - (s.midY / h);
            const worldX = nx * worldWidth + frameConfig.offsetX * 0.5;
            const worldY = ny * worldHeight - frameConfig.offsetY * 0.5;
            const worldZ = frameConfig.offsetZ * 0.5;

            const frameWorldWidth = (s.width / w) * worldWidth * (frameConfig.scale3d || 1.0);
            modelGroup.position.set(worldX, worldY, worldZ);
            modelGroup.scale.setScalar(frameWorldWidth);

            const degToRad = Math.PI / 180;
            modelGroup.rotation.set(
                s.pitch + (frameConfig.rotationX * degToRad),
                s.yaw + (frameConfig.rotationY * degToRad),
                -s.angle + (frameConfig.rotationZ * degToRad)
            );

            tCanvas.style.display = 'block';
            renderer.render(scene, camera);
        }

        function startDetectionLoop() {
            if (!faceLandmarker || !videoEl || !canvasEl || !ctx) return;

            activeLoop = true;
            lastDetectTimestamp = performance.now();
            recentDetectTimes = [];
            lostFaceFrames = 0;
            lastKnownValidState = null;
            smoothedState.initialized = false;

            if (currentFrameType === 'glb' && threeState.canvas && threeState.loadedModel) {
                threeState.canvas.style.display = 'block';
            }

            function renderLoop(currentTimestamp) {
                if (!activeLoop) return;

                if (videoEl && videoEl.readyState >= 2) {
                    if (canvasEl.width !== videoEl.videoWidth || canvasEl.height !== videoEl.videoHeight) {
                        canvasEl.width = videoEl.videoWidth || 640;
                        canvasEl.height = videoEl.videoHeight || 480;
                    }

                    const elapsed = currentTimestamp - lastDetectTimestamp;
                    if (elapsed >= targetIntervalMs) {
                        lastDetectTimestamp = currentTimestamp;
                        const detectStart = performance.now();

                        try {
                            const results = faceLandmarker.detectForVideo(videoEl, currentTimestamp);
                            const detectDuration = performance.now() - detectStart;

                            recentDetectTimes.push(detectDuration);
                            if (recentDetectTimes.length > 10) recentDetectTimes.shift();
                            const avgDur = recentDetectTimes.reduce((a, b) => a + b, 0) / recentDetectTimes.length;
                            if (avgDur > 40 && targetFps > 18) {
                                targetFps = 20;
                                targetIntervalMs = 1000 / targetFps;
                            } else if (avgDur < 20 && targetFps < 30) {
                                targetFps = 30;
                                targetIntervalMs = 1000 / targetFps;
                            }

                            lightCheckCounter++;
                            if (lightCheckCounter % 30 === 0) checkLuminance(videoEl);

                            const hasFace = results && results.faceLandmarks && results.faceLandmarks.length > 0;
                            if (hasFace) {
                                if (lostFaceFrames > MAX_GRACE_FRAMES) {
                                    onState('active');
                                }
                                lostFaceFrames = 0;
                                const lm = results.faceLandmarks[0];
                                if (lm[468] && lm[473]) {
                                    const lx = lm[468].x * canvasEl.width;
                                    const ly = lm[468].y * canvasEl.height;
                                    const rx = lm[473].x * canvasEl.width;
                                    const ry = lm[473].y * canvasEl.height;

                                    const rawMidX = (lx + rx) / 2;
                                    const rawMidY = (ly + ry) / 2;
                                    const dx = rx - lx;
                                    const dy = ry - ly;
                                    const rawDist = Math.sqrt(dx * dx + dy * dy);
                                    const rawAngle = Math.atan2(dy, dx);

                                    // Estimación simple de yaw/pitch para 3D usando profundidad z de iris
                                    const dz = (lm[473].z || 0) - (lm[468].z || 0);
                                    const rawYaw = Math.atan2(dz, Math.max(0.01, Math.abs(lm[473].x - lm[468].x)));
                                    const rawPitch = lm[6] && lm[10] ? Math.atan2((lm[6].z || 0) - (lm[10].z || 0), 0.15) * 0.4 : 0;

                                    const rawWidth = rawDist * (frameConfig.escala || 2.30);
                                    const ratio = (currentMonturaImg.naturalWidth > 0)
                                        ? (currentMonturaImg.naturalHeight / currentMonturaImg.naturalWidth)
                                        : 0.45;
                                    const rawHeight = rawWidth * ratio;

                                    if (!smoothedState.initialized) {
                                        smoothedState = {
                                            midX: rawMidX,
                                            midY: rawMidY,
                                            width: rawWidth,
                                            height: rawHeight,
                                            angle: rawAngle,
                                            yaw: rawYaw,
                                            pitch: rawPitch,
                                            initialized: true
                                        };
                                    } else {
                                        smoothedState.midX = lerp(smoothedState.midX, rawMidX, SMOOTH_FACTOR);
                                        smoothedState.midY = lerp(smoothedState.midY, rawMidY, SMOOTH_FACTOR);
                                        smoothedState.width = lerp(smoothedState.width, rawWidth, SMOOTH_FACTOR);
                                        smoothedState.height = lerp(smoothedState.height, rawHeight, SMOOTH_FACTOR);
                                        smoothedState.angle = lerp(smoothedState.angle, rawAngle, SMOOTH_FACTOR);
                                        smoothedState.yaw = lerp(smoothedState.yaw, rawYaw, SMOOTH_FACTOR);
                                        smoothedState.pitch = lerp(smoothedState.pitch, rawPitch, SMOOTH_FACTOR);
                                    }

                                    lastKnownValidState = { ...smoothedState };
                                }
                            } else {
                                lostFaceFrames++;
                                if (lostFaceFrames === MAX_GRACE_FRAMES + 1) {
                                    onState('no-face');
                                }
                            }
                        } catch (err) {
                            console.error('Error en detección facial:', err);
                        }
                    }

                    ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);

                    const showOverlay = (lostFaceFrames <= MAX_GRACE_FRAMES) && lastKnownValidState;
                    if (showOverlay && currentFrameType === 'glb' && threeState.loadedModel) {
                        render3D(lastKnownValidState);
                    } else if (showOverlay && currentFrameUrl && currentMonturaImg.complete && currentMonturaImg.naturalWidth > 0) {
                        if (threeState.canvas) threeState.canvas.style.display = 'none';
                        const s = lastKnownValidState;
                        const alpha = Math.max(0.2, 1.0 - (lostFaceFrames / (MAX_GRACE_FRAMES + 1)) * 0.7);
                        ctx.save();
                        ctx.globalAlpha = alpha;
                        ctx.translate(s.midX, s.midY);
                        ctx.rotate(s.angle);
                        const ox = s.width * (frameConfig.offsetX || 0);
                        const oy = s.height * (frameConfig.offsetY || 0.12);
                        ctx.drawImage(currentMonturaImg, -s.width / 2 + ox, -s.height / 2 + oy, s.width, s.height);
                        ctx.restore();
                    } else if (threeState.renderer && threeState.canvas) {
                        threeState.renderer.clear();
                    }
                }

                animationFrameId = requestAnimationFrame(renderLoop);
            }

            animationFrameId = requestAnimationFrame(renderLoop);
        }

        function stopDetectionLoop() {
            activeLoop = false;
            if (animationFrameId) {
                cancelAnimationFrame(animationFrameId);
                animationFrameId = null;
            }
            if (ctx && canvasEl) ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);
            if (threeState.renderer && threeState.canvas) {
                threeState.renderer.clear();
                threeState.canvas.style.display = 'none';
            }
            lostFaceFrames = 0;
            lastKnownValidState = null;
            smoothedState.initialized = false;
        }

        async function startCameraAndTracking() {
            const check = validarCompatibilidad();
            if (!check.compatible) {
                onState('error', check.mensaje);
                onError(new Error(check.mensaje));
                return false;
            }

            onState('requesting');
            try {
                stream = await navigator.mediaDevices.getUserMedia({
                    video: { facingMode: 'user', width: { ideal: 640 }, height: { ideal: 480 } },
                    audio: false
                });

                videoEl.srcObject = stream;
                videoEl.setAttribute('playsinline', '');
                videoEl.muted = true;
                await videoEl.play();

                await Promise.race([
                    initLandmarker(),
                    new Promise((_, reject) => setTimeout(() => reject(new Error('Tiempo de espera agotado al cargar el detector facial.')), 20000))
                ]);

                if (options.frameSrc) {
                    setFrame(options.frameSrc, options.frameType || 'png', options.config || {}, options.fallbackSrc || null);
                }

                startDetectionLoop();
                onState('active');
                return true;
            } catch (err) {
                stopAll();
                let msg = 'No se pudo activar la cámara.';
                if (err?.name === 'NotAllowedError' || err?.name === 'PermissionDeniedError') {
                    msg = 'Permiso de cámara denegado. Habilita el acceso a la cámara en tu navegador para probar las gafas.';
                } else if (err?.name === 'NotFoundError' || err?.name === 'DevicesNotFoundError') {
                    msg = 'No se detectó ninguna cámara conectada en este dispositivo.';
                } else if (err?.name === 'NotReadableError' || err?.name === 'TrackStartError') {
                    msg = 'La cámara está siendo usada por otra aplicación. Ciérrala e intenta nuevamente.';
                } else if (err?.message) {
                    msg = err.message;
                }
                onState('error', msg);
                onError(err);
                return false;
            }
        }

        function stopAll() {
            stopDetectionLoop();
            if (stream) {
                try {
                    stream.getTracks().forEach((t) => t.stop());
                } catch { /* ignorar */ }
                stream = null;
            }
            if (videoEl) {
                videoEl.srcObject = null;
            }
            onState('stopped');
        }

        // Inicializar frame inicial si fue provisto
        if (options.frameSrc) {
            setFrame(options.frameSrc, options.frameType || 'png', options.config || {}, options.fallbackSrc || null);
        }

        return {
            start: startCameraAndTracking,
            stop: stopAll,
            startLoop: startDetectionLoop,
            stopLoop: stopDetectionLoop,
            setFrame,
            updateConfig,
            dispose: () => {
                stopAll();
                disposeThreeAll();
            },
            isRunning: () => activeLoop,
            getTargetFps: () => targetFps
        };
    }
};
