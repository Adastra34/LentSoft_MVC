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

// Searchable Selects Logic
document.addEventListener("DOMContentLoaded", function () {
    initializeSearchableSelects();

    // Reset search inputs when clicking buttons that open modals
    document.addEventListener("click", function(event) {
        var target = event.target;
        if (target.matches("button") || target.closest("button") || target.matches("a") || target.closest("a")) {
            var el = target.closest("button") || target.closest("a") || target;
            var onclickAttr = el.getAttribute("onclick");
            if (onclickAttr && (onclickAttr.indexOf("modal-") > -1 || onclickAttr.indexOf("abrirModal") > -1)) {
                resetSearchableSelects();
            }
        }
    });
});

function initializeSearchableSelects() {
    var selects = document.querySelectorAll(".searchable-select");
    selects.forEach(function (select) {
        if (select.dataset.searchInitialized) return;
        select.dataset.searchInitialized = "true";

        // Create search input
        var searchInput = document.createElement("input");
        searchInput.type = "text";
        searchInput.placeholder = "🔍 Buscar...";
        searchInput.className = "form-input select-search-input";
        
        searchInput.style.width = "100%";
        searchInput.style.padding = "0.4rem 0.6rem";
        searchInput.style.marginBottom = "0.4rem";
        searchInput.style.border = "1px solid var(--purple-200)";
        searchInput.style.borderRadius = "0.375rem";
        searchInput.style.fontSize = "0.85rem";
        searchInput.style.boxSizing = "border-box";

        // Insert before the select
        select.parentNode.insertBefore(searchInput, select);

        // Store original options
        var originalList = [];
        for (var i = 0; i < select.options.length; i++) {
            originalList.push({
                value: select.options[i].value,
                text: select.options[i].text,
                selected: select.options[i].selected
            });
        }
        select.dataset.originalOptions = JSON.stringify(originalList);

        // Filter handler
        searchInput.addEventListener("input", function () {
            var filter = searchInput.value.toLowerCase();
            var originalOptions = JSON.parse(select.dataset.originalOptions);
            var currentValue = select.value;

            // Clear select
            select.innerHTML = "";

            var matchedCount = 0;
            originalOptions.forEach(function (opt) {
                if (opt.text.toLowerCase().indexOf(filter) > -1) {
                    var newOpt = new Option(opt.text, opt.value);
                    if (opt.value === currentValue) {
                        newOpt.selected = true;
                    }
                    select.add(newOpt);
                    matchedCount++;
                }
            });

            if (matchedCount > 0 && !Array.from(select.options).some(o => o.value === currentValue)) {
                select.selectedIndex = 0;
                select.dispatchEvent(new Event('change'));
            }
        });
    });
}

function resetSearchableSelects() {
    var searchInputs = document.querySelectorAll(".select-search-input");
    searchInputs.forEach(function (input) {
        input.value = "";
        input.dispatchEvent(new Event('input'));
    });
}
