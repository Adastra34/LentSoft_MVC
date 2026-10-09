/**
 * LentSoft - Funciones Compartidas para Filtrado y Búsqueda de Tablas
 * Compartido entre Admin, Optómetra y Ventas.
 */

function filterTable(inputId, tableId) {
    const input = document.getElementById(inputId);
    const filter = input ? input.value.toLowerCase().trim() : '';
    const table = document.getElementById(tableId);
    if (!table) return;

    const tbody = table.querySelector('tbody') || table;
    const tr = tbody.getElementsByTagName('tr');
    let visibleCount = 0;

    // Buscar o eliminar fila de 'sin resultados' previa
    let noResultsRow = tbody.querySelector('.tr-no-results');

    for (let i = 0; i < tr.length; i++) {
        // Ignorar encabezados o la fila de 'sin resultados'
        if (tr[i].classList.contains('tr-no-results') || tr[i].parentElement.tagName.toLowerCase() === 'thead') {
            continue;
        }

        let visible = false;
        const td = tr[i].getElementsByTagName('td');
        if (td.length === 0) continue;

        for (let j = 0; j < td.length; j++) {
            if (td[j]) {
                const txtValue = td[j].textContent || td[j].innerText;
                if (txtValue.toLowerCase().indexOf(filter) > -1) {
                    visible = true;
                    break;
                }
            }
        }

        tr[i].style.display = visible ? '' : 'none';
        if (visible) visibleCount++;
    }

    // Mostrar fila de 'sin resultados' si ninguna fila coincide
    if (visibleCount === 0 && filter.length > 0) {
        if (!noResultsRow) {
            noResultsRow = document.createElement('tr');
            noResultsRow.className = 'tr-no-results';
            const colSpan = tr[0] ? tr[0].getElementsByTagName('td').length || 8 : 8;
            noResultsRow.innerHTML = `<td colspan="${colSpan}" style="text-align:center; padding: 2rem; color: #6B7280; font-size: 0.9rem;">
                🔍 No se encontraron registros que coincidan con <strong>"${filter}"</strong>
            </td>`;
            tbody.appendChild(noResultsRow);
        } else {
            noResultsRow.style.display = '';
            noResultsRow.querySelector('strong').textContent = `"${filter}"`;
        }
    } else if (noResultsRow) {
        noResultsRow.style.display = 'none';
    }
}

function filterTableByValue(inputId, tableId, value) {
    const input = document.getElementById(inputId);
    if (input) {
        input.value = value;
        filterTable(inputId, tableId);
    }
}

// Helper para Toast Notifications (Reemplazo de alert())
function showToast(message, type = 'info') {
    let container = document.getElementById('lentsoft-toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'lentsoft-toast-container';
        container.style.cssText = 'position: fixed; top: 1.5rem; right: 1.5rem; z-index: 99999; display: flex; flex-direction: column; gap: 0.5rem; max-width: 380px; pointer-events: none;';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    const bg = type === 'success' ? '#10B981' : (type === 'error' ? '#EF4444' : '#6D28D9');
    const icon = type === 'success' ? '✅' : (type === 'error' ? '⚠️' : 'ℹ️');

    toast.style.cssText = `background: ${bg}; color: white; padding: 0.85rem 1.15rem; border-radius: 0.5rem; box-shadow: 0 10px 15px -3px rgba(0,0,0,0.2); font-size: 0.88rem; font-weight: 600; display: flex; align-items: center; gap: 0.6rem; pointer-events: auto; animation: slideIn 0.25s ease; transition: opacity 0.3s;`;
    toast.innerHTML = `<span>${icon}</span><span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}
