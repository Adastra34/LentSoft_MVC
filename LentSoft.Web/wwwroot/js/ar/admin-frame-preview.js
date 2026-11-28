// admin-frame-preview.js - Módulo de administración de modelos 3D/2D y prueba en vivo
import * as THREE from '/lib/three/three.module.min.js';
import { GLTFLoader } from '/lib/three/GLTFLoader.js';
import { OrbitControls } from '/lib/three/OrbitControls.js';
import { FaceTryOn } from './face-tracker-core.js';

const data = window.ADMIN_FRAME_MODELS || { featuredProductId: null, items: [] };
let selectedItem = null;
let previousValidItemSnapshot = null;

// Estado Three.js (Vista 3D)
let renderer3d = null;
let scene3d = null;
let camera3d = null;
let controls3d = null;
let anim3dId = null;
let current3dGroup = null;
let transparentBg = false;

// Estado cámara rostro
let faceInstance = null;

const el = (id) => document.getElementById(id);

function getCsrfToken() {
    const input = document.querySelector('#adminFrameCsrfForm input[name="__RequestVerificationToken"]');
    return input ? input.value : '';
}

function getSliderValues() {
    return {
        scale: parseFloat(el('adjScale').value) || 1.0,
        offsetX: parseFloat(el('adjOffsetX').value) || 0,
        offsetY: parseFloat(el('adjOffsetY').value) || 0,
        offsetZ: parseFloat(el('adjOffsetZ').value) || 0,
        rotationX: parseFloat(el('adjRotX').value) || 0,
        rotationY: parseFloat(el('adjRotY').value) || 0,
        rotationZ: parseFloat(el('adjRotZ').value) || 0
    };
}

function setSliderValues(item) {
    const map = {
        adjScale: item.scale ?? 1.0,
        adjOffsetX: item.offsetX ?? 0,
        adjOffsetY: item.offsetY ?? 0,
        adjOffsetZ: item.offsetZ ?? 0,
        adjRotX: item.rotationX ?? 0,
        adjRotY: item.rotationY ?? 0,
        adjRotZ: item.rotationZ ?? 0
    };
    Object.entries(map).forEach(([id, val]) => {
        if (el(id)) el(id).value = val;
        if (el(id + 'Num')) el(id + 'Num').value = val;
    });
}

function bindSliderPair(id) {
    const range = el(id);
    const num = el(id + 'Num');
    if (!range || !num) return;
    const onInput = (src, dst) => {
        dst.value = src.value;
        applyLiveAdjustments();
    };
    range.addEventListener('input', () => onInput(range, num));
    num.addEventListener('input', () => onInput(num, range));
}

['adjScale', 'adjOffsetX', 'adjOffsetY', 'adjOffsetZ', 'adjRotX', 'adjRotY', 'adjRotZ'].forEach(bindSliderPair);

function applyLiveAdjustments() {
    const adj = getSliderValues();
    if (current3dGroup) {
        current3dGroup.scale.setScalar(adj.scale);
        current3dGroup.position.set(adj.offsetX, adj.offsetY, adj.offsetZ);
        const deg = Math.PI / 180;
        current3dGroup.rotation.set(adj.rotationX * deg, adj.rotationY * deg, adj.rotationZ * deg);
    }
    if (faceInstance) {
        faceInstance.updateConfig({
            scale3d: adj.scale,
            offsetX: adj.offsetX,
            offsetY: adj.offsetY,
            offsetZ: adj.offsetZ,
            rotationX: adj.rotationX,
            rotationY: adj.rotationY,
            rotationZ: adj.rotationZ
        });
    }
}

function showValidation(ok, message) {
    const box = el('modelValidationStatus');
    if (!box) return;
    box.style.display = 'block';
    box.style.padding = '0.6rem 0.85rem';
    box.style.borderRadius = '0.5rem';
    if (ok) {
        box.style.background = '#ecfdf5';
        box.style.color = '#065f46';
        box.style.border = '1px solid #a7f3d0';
        box.textContent = `✔ ${message}`;
    } else {
        box.style.background = '#fef2f2';
        box.style.color = '#991b1b';
        box.style.border = '1px solid #fecaca';
        box.textContent = `✖ ${message}`;
    }
}

function dispose3DObject(obj) {
    if (!obj) return;
    obj.traverse((child) => {
        if (child.geometry) child.geometry.dispose();
        if (child.material) {
            const mats = Array.isArray(child.material) ? child.material : [child.material];
            mats.forEach((m) => {
                if (m && typeof m.dispose === 'function') {
                    Object.keys(m).forEach((k) => {
                        if (m[k] && typeof m[k].dispose === 'function') m[k].dispose();
                    });
                    m.dispose();
                }
            });
        }
    });
}

function ensure3DViewer() {
    const canvas = el('admin3DCanvas');
    const container = el('viewer3DContainer');
    if (!canvas || !container) return;

    if (!renderer3d) {
        renderer3d = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
        renderer3d.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));

        scene3d = new THREE.Scene();
        camera3d = new THREE.PerspectiveCamera(45, container.clientWidth / container.clientHeight, 0.01, 100);
        camera3d.position.set(0, 0.15, 2.2);

        scene3d.add(new THREE.AmbientLight(0xffffff, 1.3));
        const dir = new THREE.DirectionalLight(0xffffff, 1.5);
        dir.position.set(2, 3, 3);
        scene3d.add(dir);

        controls3d = new OrbitControls(camera3d, canvas);
        controls3d.enableDamping = true;
        controls3d.dampingFactor = 0.08;

        const animate = () => {
            anim3dId = requestAnimationFrame(animate);
            if (controls3d) controls3d.update();
            if (renderer3d && scene3d && camera3d) {
                renderer3d.render(scene3d, camera3d);
            }
        };
        animate();
    }

    const w = container.clientWidth || 600;
    const h = container.clientHeight || 320;
    renderer3d.setSize(w, h, false);
    camera3d.aspect = w / h;
    camera3d.updateProjectionMatrix();
}

function clear3DModel() {
    if (scene3d && current3dGroup) {
        scene3d.remove(current3dGroup);
        dispose3DObject(current3dGroup);
        current3dGroup = null;
    }
}

async function loadModelIn3DViewer(url, type, isValidationUpload = false) {
    ensure3DViewer();
    clear3DModel();
    const info = el('viewer3DInfo');

    if (!url || type !== 'glb') {
        if (info) {
            info.textContent = url
                ? 'Este producto usa una imagen 2D (.png). Sube un .glb para inspeccionar en 3D o usa "Probar en rostro".'
                : 'Sin modelo 3D cargado. Sube un archivo .glb para previsualizarlo.';
        }
        return true;
    }

    if (info) info.textContent = 'Cargando modelo 3D...';
    const loader = new GLTFLoader();

    try {
        const gltf = await new Promise((resolve, reject) => loader.load(url, resolve, undefined, reject));
        const root = gltf.scene;
        let meshCount = 0;
        let triangleCount = 0;

        root.traverse((obj) => {
            if (obj.isMesh && obj.geometry) {
                meshCount++;
                const geom = obj.geometry;
                triangleCount += geom.index
                    ? geom.index.count / 3
                    : (geom.attributes.position ? geom.attributes.position.count / 3 : 0);
            }
        });

        if (meshCount === 0) {
            dispose3DObject(root);
            throw new Error('El archivo .glb no contiene mallas visibles.');
        }

        const box = new THREE.Box3().setFromObject(root);
        const size = new THREE.Vector3();
        const center = new THREE.Vector3();
        box.getSize(size);
        box.getCenter(center);

        const maxDim = Math.max(size.x, size.y, size.z, 0.001);
        const norm = 1.2 / maxDim;
        root.position.sub(center);

        const wrapper = new THREE.Group();
        const inner = new THREE.Group();
        inner.add(root);
        inner.scale.setScalar(norm);
        wrapper.add(inner);

        scene3d.add(wrapper);
        current3dGroup = wrapper;
        applyLiveAdjustments();

        if (info) info.textContent = '';

        const triRounded = Math.round(triangleCount);
        let msg = `El modelo cargó correctamente — ${meshCount} malla(s), ${triRounded.toLocaleString()} triángulos (${size.x.toFixed(2)} × ${size.y.toFixed(2)} × ${size.z.toFixed(2)}).`;
        if (triRounded > 300000) {
            msg += ' ⚠ Aviso: supera 300k triángulos y puede ir lento en móviles.';
        }
        showValidation(true, msg);
        return true;
    } catch (err) {
        console.error('Error al validar GLB en Three.js:', err);
        if (info) info.textContent = 'No se pudo renderizar el modelo 3D.';
        showValidation(false, err?.message || 'Archivo .glb corrupto o inválido.');
        if (isValidationUpload) {
            // No conservar un modelo corrupto en servidor
            await fetch('/FramePreviewAdmin/DeleteModel', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getCsrfToken() },
                body: `productId=${selectedItem.productId}`
            });
            if (previousValidItemSnapshot) {
                Object.assign(selectedItem, previousValidItemSnapshot);
            }
        }
        return false;
    }
}

function stopAdminFace() {
    if (faceInstance) {
        faceInstance.dispose();
        faceInstance = null;
    }
}

function selectProduct(productId) {
    const item = data.items.find((i) => i.productId === productId);
    if (!item) return;

    stopAdminFace();
    selectedItem = item;
    previousValidItemSnapshot = { ...item };

    el('frameEditorEmpty').style.display = 'none';
    el('frameEditorContent').style.display = 'block';
    el('modelValidationStatus').style.display = 'none';

    el('editorProductName').textContent = item.nombre;
    el('editorProductStatus').textContent = `Estado actual: ${item.statusLabel}`;
    setSliderValues(item);

    // Resaltar ítem seleccionado
    document.querySelectorAll('.js-frame-item-btn').forEach((btn) => {
        const active = parseInt(btn.dataset.productId, 10) === productId;
        btn.style.borderColor = active ? 'var(--purple-700)' : 'var(--purple-200)';
        btn.style.background = active ? 'white' : 'var(--purple-50)';
    });

    // Activar pestaña 3D por defecto
    switchViewerTab('3d');
    const src = item.modelPath || item.imagenOverlayUrl || '';
    loadModelIn3DViewer(src, item.modelType, false);
}

function switchViewerTab(tab) {
    const is3d = tab === '3d';
    el('viewer3DContainer').style.display = is3d ? 'block' : 'none';
    el('viewerFaceContainer').style.display = is3d ? 'none' : 'block';
    el('tabViewer3D').className = is3d ? 'inv-btn inv-btn-primary' : 'inv-btn inv-btn-secondary';
    el('tabViewerFace').className = is3d ? 'inv-btn inv-btn-secondary' : 'inv-btn inv-btn-primary';

    if (is3d) {
        stopAdminFace();
        ensure3DViewer();
    } else {
        startAdminFace();
    }
}

async function startAdminFace() {
    if (!selectedItem) return;
    stopAdminFace();

    const src = selectedItem.modelPath || selectedItem.imagenOverlayUrl || selectedItem.imagenUrl || '';
    const type = selectedItem.modelPath ? selectedItem.modelType : 'png';
    const adj = getSliderValues();

    faceInstance = FaceTryOn.mount({
        video: el('adminFaceVideo'),
        canvas: el('adminFaceCanvas'),
        frameSrc: src,
        frameType: type,
        fallbackSrc: selectedItem.imagenOverlayUrl || selectedItem.imagenUrl || '',
        config: {
            scale3d: adj.scale,
            offsetX: adj.offsetX,
            offsetY: adj.offsetY,
            offsetZ: adj.offsetZ,
            rotationX: adj.rotationX,
            rotationY: adj.rotationY,
            rotationZ: adj.rotationZ
        },
        onState: (state, detail) => {
            const st = el('adminFaceState');
            if (!st) return;
            if (state === 'requesting') st.textContent = 'Solicitando permiso…';
            else if (state === 'active') st.textContent = 'Cámara activa';
            else if (state === 'no-face') st.textContent = 'No se detectó rostro';
            else if (state === 'error') st.textContent = detail || 'Error de cámara';
            else st.textContent = '';
        }
    });

    await faceInstance.start();
}

// Eventos UI
document.querySelectorAll('.js-frame-item-btn').forEach((btn) => {
    btn.addEventListener('click', () => selectProduct(parseInt(btn.dataset.productId, 10)));
});

function filterList() {
    const q = (el('frameAdminSearch')?.value || '').toLowerCase().trim();
    const f = el('frameAdminFilter')?.value || 'all';
    document.querySelectorAll('.js-frame-item-btn').forEach((btn) => {
        const matchQ = !q || (btn.dataset.name || '').includes(q);
        const matchF = f === 'all' || btn.dataset.status === f;
        btn.style.display = (matchQ && matchF) ? 'flex' : 'none';
    });
}
el('frameAdminSearch')?.addEventListener('input', filterList);
el('frameAdminFilter')?.addEventListener('change', filterList);

el('tabViewer3D')?.addEventListener('click', () => switchViewerTab('3d'));
el('tabViewerFace')?.addEventListener('click', () => switchViewerTab('face'));
el('btnStopAdminFace')?.addEventListener('click', () => switchViewerTab('3d'));

el('btnToggle3DBackground')?.addEventListener('click', () => {
    transparentBg = !transparentBg;
    el('viewer3DContainer').classList.toggle('checkerboard-bg', transparentBg);
});

el('btnUploadModel')?.addEventListener('click', async () => {
    if (!selectedItem) return;
    const fileInput = el('modelFileInput');
    const file = fileInput?.files?.[0];
    if (!file) {
        showValidation(false, 'Selecciona un archivo .glb o .png primero.');
        return;
    }

    const fd = new FormData();
    fd.append('productId', selectedItem.productId);
    fd.append('modelFile', file);
    const adj = getSliderValues();
    Object.entries(adj).forEach(([k, v]) => fd.append(k, String(v)));

    el('btnUploadModel').disabled = true;
    try {
        const resp = await fetch('/FramePreviewAdmin/UploadModel', {
            method: 'POST',
            headers: { 'RequestVerificationToken': getCsrfToken() },
            body: fd
        });
        const res = await resp.json();
        if (!resp.ok || !res.success) {
            showValidation(false, res.message || 'No se pudo subir el modelo.');
            return;
        }

        previousValidItemSnapshot = { ...selectedItem };
        selectedItem.modelPath = res.entry.modelPath;
        selectedItem.modelType = res.entry.modelType;
        selectedItem.statusCode = res.entry.modelType;
        selectedItem.statusLabel = res.entry.modelType === 'glb' ? 'Modelo 3D' : 'Imagen 2D';
        el('editorProductStatus').textContent = `Estado actual: ${selectedItem.statusLabel}`;

        if (res.entry.modelType === 'glb') {
            await loadModelIn3DViewer(res.entry.modelPath, 'glb', true);
        } else {
            showValidation(true, 'Imagen 2D PNG subida correctamente.');
            loadModelIn3DViewer(res.entry.modelPath, 'png', false);
        }
        if (typeof alertify !== 'undefined') alertify.success(res.message);
    } catch (err) {
        console.error(err);
        showValidation(false, 'Error de red al subir el modelo.');
    } finally {
        el('btnUploadModel').disabled = false;
    }
});

el('btnSaveAdjustments')?.addEventListener('click', async () => {
    if (!selectedItem) return;
    const adj = getSliderValues();
    const params = new URLSearchParams({ productId: selectedItem.productId, ...adj });
    const resp = await fetch('/FramePreviewAdmin/SaveAdjustments', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getCsrfToken() },
        body: params.toString()
    });
    const res = await resp.json();
    if (res.success) {
        Object.assign(selectedItem, adj);
        if (typeof alertify !== 'undefined') alertify.success(res.message);
    } else {
        if (typeof alertify !== 'undefined') alertify.error(res.message || 'Error al guardar.');
    }
});

el('btnResetAdjustments')?.addEventListener('click', () => {
    setSliderValues({ scale: 1.0, offsetX: 0, offsetY: 0, offsetZ: 0, rotationX: 0, rotationY: 0, rotationZ: 0 });
    applyLiveAdjustments();
});

el('btnDeleteModel')?.addEventListener('click', async () => {
    if (!selectedItem || !window.confirm('¿Eliminar el modelo personalizado de esta montura?')) return;
    const resp = await fetch('/FramePreviewAdmin/DeleteModel', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getCsrfToken() },
        body: `productId=${selectedItem.productId}`
    });
    const res = await resp.json();
    if (res.success) {
        selectedItem.modelPath = '';
        selectedItem.modelType = selectedItem.imagenOverlayUrl ? 'png' : '';
        selectedItem.statusLabel = selectedItem.imagenOverlayUrl ? 'Imagen 2D (Catálogo)' : 'Sin modelo';
        el('editorProductStatus').textContent = `Estado actual: ${selectedItem.statusLabel}`;
        clear3DModel();
        if (typeof alertify !== 'undefined') alertify.success(res.message);
    }
});

el('btnMarkFeatured')?.addEventListener('click', async () => {
    if (!selectedItem) return;
    const resp = await fetch('/FramePreviewAdmin/SetFeatured', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getCsrfToken() },
        body: `productId=${selectedItem.productId}`
    });
    const res = await resp.json();
    if (res.success && typeof alertify !== 'undefined') {
        alertify.success(res.message);
    }
});

window.addEventListener('pagehide', () => {
    stopAdminFace();
    if (anim3dId) cancelAnimationFrame(anim3dId);
    clear3DModel();
    if (renderer3d) renderer3d.dispose();
});
