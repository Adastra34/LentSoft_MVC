// muestra-montura.js - Controlador UI de la previsualización AR de monturas
const getLang = () => (typeof getCurrentLang === 'function' ? getCurrentLang() : 'es');

let currentSelectedId = (typeof window !== 'undefined' && window.MONTURA_DATA && window.MONTURA_DATA.defaultId) ? window.MONTURA_DATA.defaultId : 0;
let cameraStream = null;
let isStarting = false;

function getProducts() {
    if (typeof window !== 'undefined' && window.MONTURA_DATA && Array.isArray(window.MONTURA_DATA.products)) {
        return window.MONTURA_DATA.products;
    }
    return [];
}

// Helper para liberar los tracks de la cámara de forma segura
function releaseStream() {
    if (cameraStream) {
        try {
            cameraStream.getTracks().forEach(track => track.stop());
        } catch (e) {
            console.warn("Error deteniendo tracks:", e);
        }
        cameraStream = null;
    }
    const video = document.getElementById('webcamVideo');
    if (video) {
        video.srcObject = null;
    }
}

// Helper para gestionar el estado del botón de cámara sin duplicar HTML/íconos
function setStartBtn(state) {
    const startBtn = document.getElementById('startCameraBtn');
    if (!startBtn) return;
    const lang = getLang();
    const isEn = lang === 'en';

    switch (state) {
        case 'loading':
            startBtn.disabled = true;
            startBtn.style.opacity = '0.7';
            startBtn.style.cursor = 'wait';
            startBtn.innerHTML = isEn ? 'Starting camera and AR... ⏳' : 'Iniciando cámara y AR... ⏳';
            break;
        case 'unavailable':
            startBtn.disabled = true;
            startBtn.style.opacity = '0.6';
            startBtn.style.cursor = 'not-allowed';
            startBtn.innerHTML = isEn ? 'Preview not available for this frame' : 'Previsualización no disponible para esta montura';
            break;
        case 'idle':
        default:
            startBtn.disabled = false;
            startBtn.style.opacity = '1';
            startBtn.style.cursor = 'pointer';
            startBtn.innerHTML = isEn ? 'Start Camera 📸' : 'Activar Cámara 📸';
            break;
    }
}

// Espera a que el módulo faceTracking esté disponible globalmente
function ensureFaceTracking(timeoutMs = 8000) {
    if (typeof window !== 'undefined' && window.faceTracking) {
        return Promise.resolve(window.faceTracking);
    }
    let listener = null;
    let timer = null;
    const eventPromise = new Promise(resolve => {
        listener = () => resolve(window.faceTracking);
        window.addEventListener('faceTrackingModuleLoaded', listener, { once: true });
    });
    const timeoutPromise = new Promise((_, reject) => {
        timer = setTimeout(() => reject(new Error('FACE_TRACKING_TIMEOUT')), timeoutMs);
    });
    return Promise.race([eventPromise, timeoutPromise]).finally(() => {
        if (timer) clearTimeout(timer);
        if (listener) {
            window.removeEventListener('faceTrackingModuleLoaded', listener);
        }
    });
}

// Inicialización de la vista con producto preseleccionado
document.addEventListener('DOMContentLoaded', () => {
    if (typeof window !== 'undefined' && window.MONTURA_DATA && window.MONTURA_DATA.defaultId) {
        currentSelectedId = window.MONTURA_DATA.defaultId;
    }
    if (currentSelectedId !== 0) {
        selectProduct(currentSelectedId);
    }
});

// Selección de producto en galería o variante
function selectProduct(id) {
    const productsData = getProducts();
    const product = productsData.find(p => p.id === id);
    if (!product) return;

    currentSelectedId = id;
    const lang = getLang();
    const translate = typeof translateProductText === 'function' ? translateProductText : (t => t);

    // Actualizar atributos de datos para calibración en canvas y contenedor
    const previewContainer = document.getElementById('cameraPreviewContainer');
    if (previewContainer) {
        previewContainer.setAttribute('data-escala', product.escalaOverlay);
        previewContainer.setAttribute('data-offset-x', product.offsetXOverlay);
        previewContainer.setAttribute('data-offset-y', product.offsetYOverlay);
    }
    const canvasEl = document.getElementById('webcamCanvas');
    if (canvasEl) {
        canvasEl.setAttribute('data-escala', product.escalaOverlay);
        canvasEl.setAttribute('data-offset-x', product.offsetXOverlay);
        canvasEl.setAttribute('data-offset-y', product.offsetYOverlay);
    }

    // Actualizar bordes de selección en galería
    productsData.forEach(p => {
        const card = document.getElementById('product-card-' + p.id);
        if (card) {
            card.style.borderColor = 'var(--gray-100)';
            card.style.boxShadow = 'var(--shadow-sm)';
        }
    });

    const selectedCard = document.getElementById('product-card-' + id);
    if (selectedCard) {
        selectedCard.style.borderColor = 'var(--purple-600)';
        selectedCard.style.boxShadow = 'var(--shadow-lg)';
    }

    // Actualizar tarjeta izquierda de detalle
    const nameEl = document.getElementById('selectedName');
    const colorEl = document.getElementById('selectedColor');
    const styleEl = document.getElementById('selectedStyle');
    const sizeEl = document.getElementById('selectedSize');
    const descEl = document.getElementById('selectedDesc');

    if (nameEl) nameEl.textContent = translate(product.nombre, lang);
    if (colorEl) colorEl.textContent = translate(product.color, lang);
    if (styleEl) styleEl.textContent = translate(product.estilo, lang);
    if (sizeEl) sizeEl.textContent = translate(product.tamanio, lang);
    if (descEl) descEl.textContent = translate(product.descripcion, lang);

    // Actualizar precio con soporte de descuentos
    const priceEl = document.getElementById('selectedPrice');
    const discountRow = document.getElementById('selectedDiscountRow');
    const regularPriceEl = document.getElementById('selectedRegularPrice');
    const discountBadgeEl = document.getElementById('selectedDiscountBadge');

    if (priceEl) {
        priceEl.textContent = product.precioStr;
        priceEl.style.transform = 'scale(1.08)';
        setTimeout(() => { priceEl.style.transform = 'scale(1)'; }, 200);
    }

    if (product.hasDiscount) {
        if (regularPriceEl) regularPriceEl.textContent = product.precioRegularStr;
        if (discountBadgeEl) discountBadgeEl.textContent = `-${product.descuentoPorcentaje}%`;
        if (discountRow) discountRow.style.display = 'flex';
    } else {
        if (discountRow) discountRow.style.display = 'none';
    }

    // Sección de variantes / colores relacionados
    const variantsSection = document.getElementById('variantsSection');
    const variantsList = document.getElementById('variantsList');
    if (variantsSection && variantsList) {
        const relatedVariants = productsData.filter(p => p.estilo === product.estilo || p.id === product.id);
        if (relatedVariants.length > 1) {
            variantsSection.style.display = 'block';
            variantsList.innerHTML = '';
            relatedVariants.forEach(v => {
                const chip = document.createElement('button');
                chip.type = 'button';
                chip.textContent = v.color || v.nombre;
                chip.title = `${v.nombre} - ${v.precioStr}`;
                const isCurrent = v.id === product.id;
                chip.style.cssText = `
                    padding: 0.3rem 0.65rem;
                    border-radius: 9999px;
                    font-size: 0.75rem;
                    font-weight: 600;
                    cursor: pointer;
                    border: 1.5px solid ${isCurrent ? 'var(--purple-600)' : 'var(--purple-200)'};
                    background: ${isCurrent ? 'var(--purple-600)' : 'var(--purple-50)'};
                    color: ${isCurrent ? 'white' : 'var(--purple-800)'};
                    transition: all 0.15s;
                `;
                chip.onclick = () => selectProduct(v.id);
                variantsList.appendChild(chip);
            });
        } else {
            variantsSection.style.display = 'none';
        }
    }

    // Actualizar aviso de disponibilidad AR y estado del botón
    const arNotice = document.getElementById('arUnavailableNotice');
    const effectiveOverlay = product.modelPath || product.imagenOverlayUrl || '';
    const hasOverlay = Boolean(effectiveOverlay && effectiveOverlay.trim() !== '');

    if (arNotice) {
        arNotice.style.display = hasOverlay ? 'none' : 'flex';
    }

    setStartBtn(hasOverlay ? 'idle' : 'unavailable');

    // Botón Agregar al Carrito
    const cartBtn = document.getElementById('selectedAddToCartBtn');
    if (cartBtn) {
        if (product.hasStock) {
            cartBtn.disabled = false;
            cartBtn.style.opacity = '1';
            cartBtn.style.cursor = 'pointer';
            cartBtn.textContent = lang === 'en' ? '🛒 Add to Cart' : '🛒 Agregar al Carrito';
        } else {
            cartBtn.disabled = true;
            cartBtn.style.opacity = '0.5';
            cartBtn.style.cursor = 'not-allowed';
            cartBtn.textContent = lang === 'en' ? 'Sold Out' : 'Agotado';
        }
    }

    // Actualizar overlay en vivo si el tracking está activo
    if (typeof window !== 'undefined' && window.faceTracking) {
        window.faceTracking.cambiarMontura(product.imagenOverlayUrl, {
            escala: product.escalaOverlay,
            offsetX: product.offsetXOverlay,
            offsetY: product.offsetYOverlay
        });
    }
}

// Escuchar cambios globales de idioma
window.addEventListener('languageChanged', () => {
    if (currentSelectedId) {
        selectProduct(currentSelectedId);
    }
});

// Agregar selección al carrito
function addSelectedToCart() {
    if (typeof addToCart === 'function') {
        addToCart(currentSelectedId);
    } else {
        console.warn("Función addToCart no disponible globalmente.");
    }
}

// Inicio de cámara con captura segura de errores y lazy loading de MediaPipe
async function startCamera() {
    if (isStarting) return;
    const lang = getLang();

    try {
        isStarting = true;
        setStartBtn('loading');

        const placeholder = document.getElementById('cameraPlaceholder');
        const video = document.getElementById('webcamVideo');
        const errorContainer = document.getElementById('cameraError');
        const controls = document.getElementById('cameraControls');
        const canvas = document.getElementById('webcamCanvas');

        if (errorContainer) errorContainer.style.display = 'none';

        // 1. Validaciones previas de compatibilidad (HTTPS, getUserMedia, WebAssembly)
        let trackingModule;
        try {
            trackingModule = await ensureFaceTracking(8000);
        } catch (faceErr) {
            console.error("Error al cargar módulo de reconocimiento facial:", faceErr);
            showCameraError(
                lang === 'en'
                    ? 'Could not load the facial recognition module. Reload the page with Ctrl+F5 and try again.'
                    : 'No se pudo cargar el módulo de reconocimiento facial. Recarga la página con Ctrl+F5 e intenta de nuevo.',
                lang === 'en' ? 'Facial Recognition Error' : 'Error en Reconocimiento Facial'
            );
            return;
        }

        if (trackingModule && trackingModule.validarCompatibilidad) {
            const check = trackingModule.validarCompatibilidad();
            if (!check.compatible) {
                showCameraError(check.mensaje, lang === 'en' ? 'System Incompatibility' : 'Incompatibilidad de Sistema');
                return;
            }
        } else if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            showCameraError(
                lang === 'en'
                    ? 'Your browser does not support camera access via WebRTC. Please use a modern browser like Chrome, Safari, Firefox, or Edge.'
                    : 'Tu navegador no soporta el acceso a la cámara mediante WebRTC. Por favor utiliza un navegador moderno como Chrome, Safari, Firefox o Edge.',
                lang === 'en' ? 'Unsupported Browser' : 'Navegador no compatible'
            );
            return;
        }

        // 2. Verificar overlay del producto seleccionado
        const productsData = getProducts();
        const product = productsData.find(p => p.id === currentSelectedId);
        if (!product || !product.imagenOverlayUrl || product.imagenOverlayUrl.trim() === '') {
            showCameraError(
                lang === 'en'
                    ? 'This frame does not have an AR preview image available.'
                    : 'Esta montura no dispone de una imagen de previsualización AR.',
                lang === 'en' ? 'No Preview Available' : 'Montura sin previsualización'
            );
            return;
        }

        // Liberar stream anterior antes de solicitar uno nuevo
        releaseStream();

        try {
            cameraStream = await navigator.mediaDevices.getUserMedia({
                video: {
                    facingMode: "user",
                    width: { ideal: 640, max: 640 },
                    height: { ideal: 480, max: 480 },
                    frameRate: { ideal: 30, max: 30 }
                }
            });

            video.srcObject = cameraStream;
            await video.play();

            video.style.display = 'block';
            if (placeholder) placeholder.style.display = 'none';
            if (controls) controls.style.display = 'block';

            // 3. Lazy load del modelo MediaPipe (local primero, fallback CDN) con timeout de 20s
            try {
                const tracking = await ensureFaceTracking(8000);
                if (!tracking.isReady()) {
                    await Promise.race([
                        tracking.init(),
                        new Promise((_, reject) =>
                            setTimeout(() => reject(new Error('FACE_TRACKING_INIT_TIMEOUT')), 20000)
                        )
                    ]);
                }

                tracking.cambiarMontura(product.modelPath || product.imagenOverlayUrl, {
                    modelType: product.modelType || 'png',
                    fallbackUrl: product.imagenOverlayUrl || product.imagenUrl || '',
                    escala: product.escalaOverlay,
                    offsetX: product.offsetXOverlay,
                    offsetY: product.offsetYOverlay,
                    offsetZ: product.offsetZ || 0,
                    rotationX: product.rotationX || 0,
                    rotationY: product.rotationY || 0,
                    rotationZ: product.rotationZ || 0,
                    scale3d: product.scale3d || 1.0
                });

                tracking.iniciarDeteccion(video, canvas);
                if (canvas) canvas.style.display = 'block';
            } catch (trackErr) {
                console.error("Error al inicializar detector facial:", trackErr);
                releaseStream();
                stopCamera();
                showCameraError(
                    lang === 'en'
                        ? 'Could not initialize facial recognition model. Please verify your connection and try again.'
                        : 'No se pudo inicializar el modelo de reconocimiento facial. Por favor verifica tu conexión a internet y vuelve a intentar.',
                    lang === 'en' ? 'Facial Recognition Error' : 'Error en Reconocimiento Facial'
                );
            }
        } catch (mediaErr) {
            console.error("Error al acceder a la cámara:", mediaErr);
            releaseStream();
            setStartBtn('idle');

            if (mediaErr.name === 'NotAllowedError' || mediaErr.name === 'PermissionDeniedError') {
                showCameraError(
                    lang === 'en'
                        ? 'Camera permission denied. Click the lock/permission icon in your browser address bar to allow access.'
                        : 'Permiso de cámara denegado. Haz clic en el ícono de permisos/candado en la barra de direcciones de tu navegador y autoriza el acceso.',
                    lang === 'en' ? 'Permission Denied' : 'Permiso Denegado'
                );
            } else if (mediaErr.name === 'NotFoundError' || mediaErr.name === 'DevicesNotFoundError') {
                showCameraError(
                    lang === 'en'
                        ? 'No camera was detected on your device.'
                        : 'No se detectó ninguna cámara disponible en tu dispositivo.',
                    lang === 'en' ? 'Camera Not Found' : 'Cámara no Encontrada'
                );
            } else if (mediaErr.name === 'NotReadableError' || mediaErr.name === 'TrackStartError') {
                showCameraError(
                    lang === 'en'
                        ? 'The camera is currently being used by another application. Please close other camera apps and retry.'
                        : 'La cámara está siendo utilizada por otra aplicación. Por favor cierra otras apps que usen la cámara y reintenta.',
                    lang === 'en' ? 'Camera Busy' : 'Cámara Ocupada'
                );
            } else {
                showCameraError(
                    (lang === 'en' ? 'Error connecting to camera: ' : 'Ocurrió un inconveniente al conectar con la cámara: ') + (mediaErr.message || mediaErr.name || 'Error desconocido'),
                    lang === 'en' ? 'Connection Error' : 'Error de Conexión'
                );
            }
        }
    } catch (unexpectedErr) {
        console.error("Error no controlado en startCamera:", unexpectedErr);
        releaseStream();
        setStartBtn('idle');
        showCameraError(
            (lang === 'en' ? 'Unexpected camera error: ' : 'Ocurrió un error inesperado al iniciar la cámara: ') + (unexpectedErr.message || unexpectedErr),
            lang === 'en' ? 'Unexpected Error' : 'Error Inesperado'
        );
    } finally {
        isStarting = false;
    }
}

// Detener cámara y limpiar estado
function stopCamera() {
    const placeholder = document.getElementById('cameraPlaceholder');
    const video = document.getElementById('webcamVideo');
    const controls = document.getElementById('cameraControls');
    const canvas = document.getElementById('webcamCanvas');

    if (typeof window !== 'undefined' && window.faceTracking) {
        window.faceTracking.detenerDeteccion();
    }

    if (canvas) {
        canvas.style.display = 'none';
    }

    releaseStream();

    if (video) {
        video.style.display = 'none';
    }
    if (placeholder) placeholder.style.display = 'flex';
    if (controls) controls.style.display = 'none';

    const productsData = getProducts();
    const product = productsData.find(p => p.id === currentSelectedId);
    const hasOverlay = Boolean(product && product.imagenOverlayUrl && product.imagenOverlayUrl.trim() !== '');
    setStartBtn(hasOverlay ? 'idle' : 'unavailable');
}

// Mostrar diálogo de error de cámara
function showCameraError(msg, title) {
    const lang = getLang();
    const resolvedTitle = title || (lang === 'en' ? 'Camera Access Unavailable' : 'Acceso a Cámara no Disponible');
    const errTitle = document.getElementById('cameraErrorTitle');
    const errMsg = document.getElementById('cameraErrorMessage');
    if (errTitle) errTitle.textContent = resolvedTitle;
    if (errMsg) errMsg.textContent = msg;

    const errorContainer = document.getElementById('cameraError');
    if (errorContainer) errorContainer.style.display = 'flex';

    setStartBtn('idle');
}

// Reintentar acceso a cámara
function retryCamera() {
    const errorContainer = document.getElementById('cameraError');
    if (errorContainer) errorContainer.style.display = 'none';
    startCamera();
}

// Exponer funciones globales para interacción con HTML onclick
window.selectProduct = selectProduct;
window.startCamera = startCamera;
window.stopCamera = stopCamera;
window.showCameraError = showCameraError;
window.retryCamera = retryCamera;
window.addSelectedToCart = addSelectedToCart;
window.ensureFaceTracking = ensureFaceTracking;
window.releaseStream = releaseStream;
window.setStartBtn = setStartBtn;
window.getLang = getLang;
