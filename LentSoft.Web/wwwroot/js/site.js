// Minimal JS for UI interactions only
document.addEventListener('DOMContentLoaded', function() {
    // Toggle Password Visibility
    const togglePassword = document.getElementById('togglePassword');
    const password = document.getElementById('password');
    if (togglePassword && password) {
        togglePassword.addEventListener('click', function() {
            const type = password.getAttribute('type') === 'password' ? 'text' : 'password';
            password.setAttribute('type', type);
            this.textContent = type === 'password' ? '👁️' : '👁️‍🗨️';
        });
    }

    const toggleRegisterPassword = document.getElementById('toggleRegisterPassword');
    const registerPassword = document.getElementById('registerPassword');
    if (toggleRegisterPassword && registerPassword) {
        toggleRegisterPassword.addEventListener('click', function() {
            const type = registerPassword.getAttribute('type') === 'password' ? 'text' : 'password';
            registerPassword.setAttribute('type', type);
            this.textContent = type === 'password' ? '👁️' : '👁️‍🗨️';
        });
    }

    // Set min date for datetime-local inputs to local client current time
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    const nowISO = now.toISOString().slice(0, 16);
    document.querySelectorAll('input[type="datetime-local"]').forEach(input => {
        input.setAttribute('min', nowISO);
    });
});

// Interceptar envío de formularios para mostrar confirmación con Alertify.js
function confirmDelete(event, message, form) {
    event.preventDefault();
    alertify.confirm(message, function() {
        form.submit();
    }).set('labels', {ok:'Aceptar', cancel:'Cancelar'}).set('title', 'Confirmar Acción');
}

// Agregar producto al carrito de forma asíncrona
async function addToCart(productId, cantidad = 1) {
    if (!window.isAuthenticated) {
        if (typeof alertify !== 'undefined') {
            alertify.error('Inicia sesión para agregar al carrito');
        } else {
            alert('Inicia sesión para agregar al carrito');
        }
        setTimeout(() => {
            window.location.href = '/Auth/Login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
        }, 1000);
        return;
    }

    try {
        const resp = await fetch('/Cart/AddToCart', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'productId=' + productId + '&cantidad=' + cantidad
        });
        const data = await resp.json();
        if (data.success) {
            if (typeof alertify !== 'undefined') {
                alertify.success('🛒 ' + data.message);
            } else {
                alert('🛒 ' + data.message);
            }
        } else {
            if (typeof alertify !== 'undefined') {
                alertify.error('Error: ' + data.error);
            } else {
                alert('Error: ' + data.error);
            }
        }
    } catch (e) {
        console.error('Error adding to cart', e);
    }
}

// Buscador dinámico reactivo para elementos select con la clase .searchable-select
function initSearchableSelects() {
    document.querySelectorAll('select.searchable-select').forEach(function(select) {
        if (select.nextElementSibling && select.nextElementSibling.classList.contains('searchable-wrapper')) {
            return; // Ya inicializado
        }

        // Crear wrapper y campo de búsqueda
        var wrapper = document.createElement('div');
        wrapper.className = 'searchable-wrapper';
        wrapper.style.position = 'relative';
        wrapper.style.width = select.style.width || '100%';
        wrapper.style.marginTop = '0.25rem';

        var searchInput = document.createElement('input');
        searchInput.type = 'text';
        searchInput.placeholder = '🔍 Escribe para buscar...';
        searchInput.className = 'form-input no-print';
        searchInput.style.width = '100%';
        searchInput.style.padding = '0.4rem 0.6rem';
        searchInput.style.fontSize = '0.85rem';
        searchInput.style.borderRadius = '0.375rem';
        searchInput.style.border = '1px solid var(--purple-300)';
        searchInput.style.boxSizing = 'border-box';
        searchInput.style.marginBottom = '0.25rem';

        // Insertar antes del select
        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(searchInput);
        wrapper.appendChild(select);

        // Guardar opciones originales
        var originalOptions = Array.from(select.options);

        // Evento de búsqueda
        searchInput.addEventListener('input', function() {
            var filter = searchInput.value.toLowerCase().trim();
            select.innerHTML = '';
            
            var matched = originalOptions.filter(function(opt) {
                return opt.text.toLowerCase().includes(filter) || opt.value === '';
            });

            matched.forEach(function(opt) {
                select.appendChild(opt);
            });
        });

        // Guardar referencia al input en el select para poder resetearlo
        select.searchField = searchInput;
        select.originalOptionsList = originalOptions;
    });
}

// Resetear y limpiar búsquedas al recargar/abrir modales
function resetSearchableSelects() {
    document.querySelectorAll('select.searchable-select').forEach(function(select) {
        if (select.searchField && select.originalOptionsList) {
            select.searchField.value = '';
            select.innerHTML = '';
            select.originalOptionsList.forEach(function(opt) {
                select.appendChild(opt);
            });
        }
    });
}

document.addEventListener('DOMContentLoaded', initSearchableSelects);
