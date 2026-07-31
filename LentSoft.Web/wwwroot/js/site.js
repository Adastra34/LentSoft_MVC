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
