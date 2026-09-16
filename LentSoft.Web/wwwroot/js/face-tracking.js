import { FaceLandmarker, FilesetResolver } from "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.8/vision_bundle.mjs";

let faceLandmarker = null;
let activeLoop = false;
let currentMonturaImg = new Image();
let currentMonturaUrl = null;
let animationFrameId = null;
let videoEl = null;
let canvasEl = null;
let ctx = null;
let overlayContainerEl = null;
let overlayImgEl = null;

// Initialize FaceLandmarker with 3D transformation matrix support
async function initLandmarker() {
    try {
        const vision = await FilesetResolver.forVisionTasks(
            "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.8/wasm"
        );
        faceLandmarker = await FaceLandmarker.createFromOptions(vision, {
            baseOptions: {
                modelAssetPath: "https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task",
                delegate: "GPU"
            },
            runningMode: "VIDEO",
            numFaces: 1,
            outputFacialTransformationMatrixes: true
        });
        console.log("MediaPipe FaceLandmarker initialized successfully with 3D transformation matrices.");
    } catch (error) {
        console.error("Error initializing MediaPipe FaceLandmarker:", error);
        throw error;
    }
}

// Start detection loop
function iniciarDeteccion(video, canvas) {
    if (!faceLandmarker) {
        console.error("FaceLandmarker is not initialized yet.");
        return;
    }
    videoEl = video;
    canvasEl = canvas;
    ctx = canvas ? canvas.getContext('2d') : null;
    activeLoop = true;

    // Locate or create the 3D overlay container and img element
    overlayContainerEl = document.getElementById('glassesOverlayContainer');
    if (!overlayContainerEl && videoEl && videoEl.parentElement) {
        overlayContainerEl = document.createElement('div');
        overlayContainerEl.id = 'glassesOverlayContainer';
        overlayContainerEl.style.cssText = 'position: absolute; top: 0; left: 0; width: 100%; height: 100%; pointer-events: none; perspective: 1000px; transform: scaleX(-1); overflow: hidden; z-index: 3;';
        videoEl.parentElement.appendChild(overlayContainerEl);
    }
    if (overlayContainerEl) {
        overlayContainerEl.style.display = 'block';
        if (!overlayContainerEl.style.perspective) {
            overlayContainerEl.style.perspective = '1000px';
        }
    }

    overlayImgEl = document.getElementById('glassesOverlayImg');
    if (!overlayImgEl && overlayContainerEl) {
        overlayImgEl = document.createElement('img');
        overlayImgEl.id = 'glassesOverlayImg';
        overlayImgEl.alt = 'Glasses Overlay';
        overlayImgEl.style.cssText = 'position: absolute; top: 0; left: 0; transform-style: preserve-3d; transform-origin: 0 0 0; display: none; pointer-events: none;';
        overlayContainerEl.appendChild(overlayImgEl);
    }
    if (overlayImgEl && currentMonturaUrl) {
        overlayImgEl.src = currentMonturaUrl;
    }

    // Detection & Render Loop
    function renderLoop() {
        if (!activeLoop) return;

        // Ensure video is ready and playing
        if (videoEl && videoEl.readyState === videoEl.HAVE_ENOUGH_DATA) {
            // Update canvas size to match raw video frame dimensions
            if (canvasEl && (canvasEl.width !== videoEl.videoWidth || canvasEl.height !== videoEl.videoHeight)) {
                canvasEl.width = videoEl.videoWidth;
                canvasEl.height = videoEl.videoHeight;
            }

            try {
                const startTimeMs = performance.now();
                const results = faceLandmarker.detectForVideo(videoEl, startTimeMs);

                // Clear canvas (preserve canvas for other uses if needed)
                if (ctx && canvasEl) {
                    ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);
                }

                const hasFace = results && results.faceLandmarks && results.faceLandmarks.length > 0;

                // Show/hide warning overlay safely
                const warningEl = document.getElementById('noFaceWarning');
                if (warningEl) {
                    const currentDisplay = warningEl.style.display;
                    const targetDisplay = hasFace ? 'none' : 'block';
                    if (currentDisplay !== targetDisplay) {
                        warningEl.style.display = targetDisplay;
                    }
                }

                let rendered = false;

                if (hasFace && currentMonturaUrl && overlayImgEl) {
                    // Extract landmarks for eyes (468: left iris center, 473: right iris center)
                    const landmarks = results.faceLandmarks[0];
                    if (landmarks[468] && landmarks[473]) {
                        const leftEye = landmarks[468];
                        const rightEye = landmarks[473];

                        // Dimensions in video frame coordinate space
                        const cw = canvasEl ? canvasEl.width : videoEl.videoWidth;
                        const ch = canvasEl ? canvasEl.height : videoEl.videoHeight;

                        const lx = leftEye.x * cw;
                        const ly = leftEye.y * ch;
                        const rx = rightEye.x * cw;
                        const ry = rightEye.y * ch;

                        // Midpoint (nose bridge / center of eyes)
                        const midX = (lx + rx) / 2;
                        const midY = (ly + ry) / 2;

                        // Pupillary distance
                        const dx = rx - lx;
                        const dy = ry - ly;
                        const dist = Math.sqrt(dx * dx + dy * dy);

                        // Sizing consistent with previous 2D model
                        const naturalWidth = currentMonturaImg.naturalWidth || 500;
                        const naturalHeight = currentMonturaImg.naturalHeight || 200;
                        const glassesWidth = dist * 2.3;
                        const glassesHeight = glassesWidth * (naturalHeight / naturalWidth);
                        const yOffset = glassesHeight * 0.12;

                        // Scale to overlay container dimensions if container differs from raw video pixels
                        const containerW = overlayContainerEl ? (overlayContainerEl.clientWidth || cw) : cw;
                        const containerH = overlayContainerEl ? (overlayContainerEl.clientHeight || ch) : ch;
                        const scaleFactorX = containerW / cw;
                        const scaleFactorY = containerH / ch;

                        const posX = midX * scaleFactorX;
                        const posY = midY * scaleFactorY;
                        const gWidth = glassesWidth * scaleFactorX;
                        const gHeight = glassesHeight * scaleFactorY;
                        const gYOffset = yOffset * scaleFactorY;

                        // Local bridge anchor on the overlay image:
                        // Center horizontally, and offset down to nose bridge height
                        const cx = gWidth / 2;
                        const cy = gHeight / 2 - gYOffset;

                        // Extract 3D head pose transformation matrix (column-major, 16 values)
                        const rawMatrix = results.facialTransformationMatrixes?.[0];
                        const matrixData = rawMatrix ? (rawMatrix.data || rawMatrix) : null;

                        let R00, R01, R02;
                        let R10, R11, R12;
                        let R20, R21, R22;

                        if (matrixData && matrixData.length === 16) {
                            // MediaPipe 4x4 matrix:
                            // Column 0: [m0, m1, m2, 0]
                            // Column 1: [m4, m5, m6, 0]
                            // Column 2: [m8, m9, m10, 0]
                            // Column 3: [m12, m13, m14, 1]
                            //
                            // MediaPipe metric 3D space has +Y pointing UP (towards forehead)
                            // CSS viewport 3D space has +Y pointing DOWN (towards chin).
                            // Transforming via Sy * R * Sy maps the rotation correctly into CSS 3D space:
                            const m0 = matrixData[0],  m1 = matrixData[1],  m2 = matrixData[2];
                            const m4 = matrixData[4],  m5 = matrixData[5],  m6 = matrixData[6];
                            const m8 = matrixData[8],  m9 = matrixData[9],  m10 = matrixData[10];

                            R00 = m0;   R01 = -m4;  R02 = m8;
                            R10 = -m1;  R11 = m5;   R12 = -m9;
                            R20 = m2;   R21 = -m6;  R22 = m10;

                            // Normalize columns to enforce orthonormal rotation and avoid scale skewing
                            const l0 = Math.hypot(R00, R10, R20) || 1;
                            R00 /= l0; R10 /= l0; R20 /= l0;

                            const l1 = Math.hypot(R01, R11, R21) || 1;
                            R01 /= l1; R11 /= l1; R21 /= l1;

                            const l2 = Math.hypot(R02, R12, R22) || 1;
                            R02 /= l2; R12 /= l2; R22 /= l2;
                        } else {
                            // Fallback: 2D roll angle calculation
                            const angle = Math.atan2(dy, dx);
                            const cos = Math.cos(angle);
                            const sin = Math.sin(angle);

                            R00 = cos;  R01 = -sin; R02 = 0;
                            R10 = sin;  R11 = cos;  R12 = 0;
                            R20 = 0;    R21 = 0;    R22 = 1;
                        }

                        // Compute translation (Tx, Ty, Tz) so that anchor (cx, cy, 0) maps to (posX, posY, 0)
                        const deltaX = R00 * cx + R01 * cy;
                        const deltaY = R10 * cx + R11 * cy;
                        const deltaZ = R20 * cx + R21 * cy;

                        const Tx = posX - deltaX;
                        const Ty = posY - deltaY;
                        const Tz = -deltaZ;

                        // CSS matrix3d format (column-major):
                        // matrix3d(a1, b1, c1, d1, a2, b2, c2, d2, a3, b3, c3, d3, a4, b4, c4, d4)
                        const matrix3dStr = `matrix3d(${R00.toFixed(6)}, ${R10.toFixed(6)}, ${R20.toFixed(6)}, 0, ${R01.toFixed(6)}, ${R11.toFixed(6)}, ${R21.toFixed(6)}, 0, ${R02.toFixed(6)}, ${R12.toFixed(6)}, ${R22.toFixed(6)}, 0, ${Tx.toFixed(4)}, ${Ty.toFixed(4)}, ${Tz.toFixed(4)}, 1)`;

                        overlayImgEl.style.width = `${gWidth}px`;
                        overlayImgEl.style.height = `${gHeight}px`;
                        overlayImgEl.style.transformOrigin = '0 0 0';
                        overlayImgEl.style.transform = matrix3dStr;
                        overlayImgEl.style.transformStyle = 'preserve-3d';
                        overlayImgEl.style.display = 'block';

                        rendered = true;
                    }
                }

                if (!rendered && overlayImgEl) {
                    overlayImgEl.style.display = 'none';
                }
            } catch (err) {
                console.error("Error during detectForVideo:", err);
            }
        }

        animationFrameId = requestAnimationFrame(renderLoop);
    }

    renderLoop();
}

// Stop detection
function detenerDeteccion() {
    activeLoop = false;
    if (animationFrameId) {
        cancelAnimationFrame(animationFrameId);
        animationFrameId = null;
    }
    if (ctx && canvasEl) {
        ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);
    }
    if (overlayImgEl) {
        overlayImgEl.style.display = 'none';
    }
    if (overlayContainerEl) {
        overlayContainerEl.style.display = 'none';
    }
    const warningEl = document.getElementById('noFaceWarning');
    if (warningEl) {
        warningEl.style.display = 'none';
    }
}

// Change active glasses overlay image
function cambiarMontura(url) {
    if (!url) {
        currentMonturaUrl = null;
        currentMonturaImg = new Image();
        if (ctx && canvasEl) {
            ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);
        }
        if (overlayImgEl) {
            overlayImgEl.src = '';
            overlayImgEl.style.display = 'none';
        }
        return;
    }

    if (currentMonturaUrl !== url) {
        currentMonturaUrl = url;
        currentMonturaImg = new Image();
        currentMonturaImg.src = url;
        currentMonturaImg.onload = () => {
            console.log(`Glasses overlay loaded: ${url}`);
        };
        currentMonturaImg.onerror = () => {
            console.warn(`Failed to load glasses overlay: ${url}`);
        };

        if (overlayImgEl) {
            overlayImgEl.src = url;
        }
    }
}

// Expose faceTracking namespace globally for Razor views
window.faceTracking = {
    init: initLandmarker,
    iniciarDeteccion: iniciarDeteccion,
    detenerDeteccion: detenerDeteccion,
    cambiarMontura: cambiarMontura,
    isReady: () => faceLandmarker !== null
};

// Dispatch event when module is fully loaded and window.faceTracking is assigned
window.dispatchEvent(new CustomEvent('faceTrackingModuleLoaded'));
