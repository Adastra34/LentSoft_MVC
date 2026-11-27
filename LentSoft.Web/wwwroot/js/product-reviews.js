// product-reviews.js - Interactividad accesible y actualización AJAX de reseñas
(function () {
    function initReviewUI(root) {
        if (!root) return;

        // 1. Selector de estrellas accesible por teclado
        root.querySelectorAll('.star-picker').forEach((picker) => {
            const hiddenInput = picker.querySelector('.js-rating-input');
            const buttons = Array.from(picker.querySelectorAll('.js-star-btn'));

            const setRating = (val) => {
                if (hiddenInput) hiddenInput.value = String(val);
                buttons.forEach((btn) => {
                    const btnVal = parseInt(btn.dataset.value || '0', 10);
                    const active = btnVal <= val;
                    btn.style.color = active ? '#f59e0b' : '#d1d5db';
                    btn.style.borderColor = btnVal === val ? 'var(--purple-600)' : 'var(--purple-200)';
                    btn.setAttribute('aria-checked', btnVal === val ? 'true' : 'false');
                    btn.setAttribute('tabindex', btnVal === val ? '0' : '-1');
                });
            };

            setRating(parseInt(hiddenInput?.value || '5', 10));

            buttons.forEach((btn, idx) => {
                btn.addEventListener('click', () => {
                    setRating(parseInt(btn.dataset.value || '5', 10));
                });

                btn.addEventListener('keydown', (e) => {
                    if (e.key === 'ArrowRight' || e.key === 'ArrowUp') {
                        e.preventDefault();
                        const next = buttons[Math.min(buttons.length - 1, idx + 1)];
                        setRating(parseInt(next.dataset.value || '5', 10));
                        next.focus();
                    } else if (e.key === 'ArrowLeft' || e.key === 'ArrowDown') {
                        e.preventDefault();
                        const prev = buttons[Math.max(0, idx - 1)];
                        setRating(parseInt(prev.dataset.value || '1', 10));
                        prev.focus();
                    }
                });
            });
        });

        // 2. Contador de caracteres
        root.querySelectorAll('.js-review-textarea').forEach((ta) => {
            const counter = ta.parentElement?.querySelector('.js-char-counter');
            if (!counter) return;
            const updateCounter = () => {
                counter.textContent = `${ta.value.length} / 1000`;
            };
            ta.addEventListener('input', updateCounter);
            updateCounter();
        });

        // 3. Toggle edición inline
        root.querySelectorAll('.js-toggle-edit-btn').forEach((btn) => {
            btn.addEventListener('click', () => {
                const form = root.querySelector(`#editForm-${btn.dataset.reviewId}`);
                if (form) form.style.display = form.style.display === 'none' ? 'block' : 'none';
            });
        });

        root.querySelectorAll('.js-cancel-edit-btn').forEach((btn) => {
            btn.addEventListener('click', () => {
                const form = root.querySelector(`#editForm-${btn.dataset.reviewId}`);
                if (form) form.style.display = 'none';
            });
        });

        // 4. Ver más reseñas
        const showMoreBtn = root.querySelector('#showMoreReviewsBtn');
        if (showMoreBtn) {
            showMoreBtn.addEventListener('click', () => {
                root.querySelectorAll('.js-review-extra').forEach((el) => {
                    el.style.display = 'block';
                });
                showMoreBtn.style.display = 'none';
            });
        }

        // 5. Envío AJAX con fallback a envío normal
        const handleAjaxForm = (form, confirmMsg) => {
            form.addEventListener('submit', async (e) => {
                if (confirmMsg && !window.confirm(confirmMsg)) {
                    e.preventDefault();
                    return;
                }
                if (!window.fetch) return; // fallback clásico
                e.preventDefault();

                const productId = form.dataset.productId;
                const submitBtn = form.querySelector('button[type="submit"]');
                if (submitBtn) submitBtn.disabled = true;

                try {
                    const resp = await fetch(form.action, {
                        method: 'POST',
                        headers: {
                            'X-Requested-With': 'XMLHttpRequest',
                            'Accept': 'application/json'
                        },
                        body: new FormData(form)
                    });

                    const data = await resp.json();
                    if (!resp.ok || !data.success) {
                        if (typeof alertify !== 'undefined') alertify.error(data.message || 'Ocurrió un error.');
                        if (submitBtn) submitBtn.disabled = false;
                        return;
                    }

                    if (typeof alertify !== 'undefined') alertify.success(data.message);
                    await refreshReviews(productId, data.averageRating, data.totalReviews);
                } catch (err) {
                    console.error('Error en solicitud de reseña:', err);
                    form.submit();
                }
            });
        };

        root.querySelectorAll('.js-review-form').forEach((f) => handleAjaxForm(f, null));
        root.querySelectorAll('.js-review-edit-form').forEach((f) => handleAjaxForm(f, null));
        root.querySelectorAll('.js-review-delete-form').forEach((f) =>
            handleAjaxForm(f, '¿Estás seguro de que deseas eliminar esta reseña?')
        );
    }

    async function refreshReviews(productId, averageRating, totalReviews) {
        const container = document.getElementById('productReviewsContainer');
        if (!container) {
            window.location.reload();
            return;
        }

        const resp = await fetch(`/Reviews/Partial?productId=${encodeURIComponent(productId)}`, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        if (resp.ok) {
            container.innerHTML = await resp.text();
            initReviewUI(container);
        }

        // Actualizar estrellas y texto en la cabecera del producto
        const headerRating = document.getElementById('productRatingSummaryHeader');
        if (headerRating && typeof averageRating === 'number' && typeof totalReviews === 'number') {
            const avgText = headerRating.querySelector('.js-header-avg');
            const countText = headerRating.querySelector('.js-header-count');
            if (avgText) avgText.textContent = totalReviews > 0 ? averageRating.toFixed(1) : '0.0';
            if (countText) countText.textContent = String(totalReviews);
            const stars = headerRating.querySelectorAll('.rating-stars .icon, .rating-stars svg');
            stars.forEach((star, index) => {
                const pos = index + 1;
                const filled = totalReviews > 0 && (pos <= Math.floor(averageRating) || (pos === Math.floor(averageRating) + 1 && (averageRating - Math.floor(averageRating)) >= 0.5));
                star.classList.toggle('icon-fill', filled);
            });
        }
    }

    document.addEventListener('DOMContentLoaded', () => {
        initReviewUI(document.getElementById('productReviewsContainer'));
    });
})();
