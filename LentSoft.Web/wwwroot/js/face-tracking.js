import { FaceLandmarker, FilesetResolver } from "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.8/vision_bundle.js";

let faceLandmarker = null;
let activeLoop = false;
let currentMonturaImg = new Image();
let currentMonturaUrl = null;
let animationFrameId = null;
let videoEl = null;
let canvasEl = null;
let ctx = null;

// Initialize FaceLandmarker
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
            numFaces: 1
        });
        console.log("MediaPipe FaceLandmarker initialized successfully.");
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
    ctx = canvas.getContext('2d');
    activeLoop = true;

    // Run loop
    function renderLoop() {
        if (!activeLoop) return;

        // Ensure video is ready and playing
        if (videoEl && videoEl.readyState === videoEl.HAVE_ENOUGH_DATA) {
            // Update canvas size to match raw video frame dimensions
            if (canvasEl.width !== videoEl.videoWidth || canvasEl.height !== videoEl.videoHeight) {
                canvasEl.width = videoEl.videoWidth;
                canvasEl.height = videoEl.videoHeight;
            }

            try {
                const startTimeMs = performance.now();
                const results = faceLandmarker.detectForVideo(videoEl, startTimeMs);

                // Clear canvas
                ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);

                if (results && results.faceLandmarks && results.faceLandmarks.length > 0 && currentMonturaUrl) {
                    // Extract landmarks for eyes (468: left iris center, 473: right iris center)
                    const landmarks = results.faceLandmarks[0];
                    if (landmarks[468] && landmarks[473]) {
                        const leftEye = landmarks[468];
                        const rightEye = landmarks[473];

                        // Convert normalized coordinates to canvas space
                        const lx = leftEye.x * canvasEl.width;
                        const ly = leftEye.y * canvasEl.height;
                        const rx = rightEye.x * canvasEl.width;
                        const ry = rightEye.y * canvasEl.height;

                        // Midpoint (nose bridge / center of eyes)
                        const midX = (lx + rx) / 2;
                        const midY = (ly + ry) / 2;

                        // Calculate scale (pupillary distance)
                        const dx = rx - lx;
                        const dy = ry - ly;
                        const dist = Math.sqrt(dx * dx + dy * dy);

                        // Calculate rotation angle
                        const angle = Math.atan2(dy, dx);

                        // Draw glasses overlay if image is loaded
                        if (currentMonturaImg.complete && currentMonturaImg.naturalWidth > 0) {
                            ctx.save();
                            ctx.translate(midX, midY);
                            ctx.rotate(angle);

                            // Goggles width is typically ~2.3 times the pupillary distance
                            const glassesWidth = dist * 2.3;
                            const glassesHeight = glassesWidth * (currentMonturaImg.naturalHeight / currentMonturaImg.naturalWidth);

                            // Shift slightly down so the bridge sits on the nose bridge (~12% of height)
                            const yOffset = glassesHeight * 0.12;

                            ctx.drawImage(
                                currentMonturaImg,
                                -glassesWidth / 2,
                                -glassesHeight / 2 + yOffset,
                                glassesWidth,
                                glassesHeight
                            );
                            ctx.restore();
                        }
                    }
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
}

// Change active glasses overlay image
function cambiarMontura(url) {
    if (!url) {
        currentMonturaUrl = null;
        currentMonturaImg = new Image();
        if (ctx && canvasEl) {
            ctx.clearRect(0, 0, canvasEl.width, canvasEl.height);
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
    }
}

// Expose faceTracking namespace globally so standard script blocks in Razor views can call it
window.faceTracking = {
    init: initLandmarker,
    iniciarDeteccion: iniciarDeteccion,
    detenerDeteccion: detenerDeteccion,
    cambiarMontura: cambiarMontura,
    isReady: () => faceLandmarker !== null
};
