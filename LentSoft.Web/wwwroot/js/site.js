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
