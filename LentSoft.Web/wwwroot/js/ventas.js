/* ==========================================================================
   LentSoft MVC — Script Centralizado del Módulo de Ventas (ventas.js)
   Aislado para las vistas de Ventas (Secciones 4.1, 4.3, 4.4 y 4.7)
   ========================================================================== */

const spriteUrl = window.LENTSOFT_SPRITE_URL || '/img/icons/sprite.svg';
var dtVentas = null;
var currentFiltroRapido = '';

// ── Utilidad de Usabilidad (4.4): Spinner de Carga y Bloqueo Anti Doble-Clic ──
function activarEstadoCargaBoton(btn, textoCarga) {
    if (!btn || btn.dataset.loadingActive === 'true') return false;
    btn.dataset.loadingActive = 'true';
    btn.dataset.originalHtml = btn.innerHTML;
    btn.disabled = true;
    btn.classList.add('ventas-is-loading');
    const label = textoCarga || 'Procesando...';
    btn.innerHTML = '<span class="ventas-btn-spinner" aria-hidden="true"></span><span>' + label + '</span>';
    return true;
}

function restaurarEstadoCargaBoton(btn) {
    if (!btn) return;
    btn.dataset.loadingActive = 'false';
    btn.disabled = false;
    btn.classList.remove('ventas-is-loading');
    if (btn.dataset.originalHtml) {
        btn.innerHTML = btn.dataset.originalHtml;
    }
}

// ── Inicialización de DataTables Server-Side para Tabla de Ventas ──
$(document).ready(function () {
    if ($('#tableVentasSales').length && !$.fn.DataTable.isDataTable('#tableVentasSales')) {
        dtVentas = $('#tableVentasSales').DataTable({
            serverSide: true,
            processing: true,
            ajax: {
                url: '/Ventas/ListarVentas',
                type: 'POST',
                data: function (d) {
                    d.__RequestVerificationToken = $('input[name="__RequestVerificationToken"]').first().val();
                    var estVal = $('#filtroEstadoVenta').val();
                    var estPagoVal = $('#filtroEstadoPago').val();
                    d.estado = estVal;
                    d.filtroEstado = estVal;
                    d.estadoPago = estPagoVal;
                    d.filtroEstadoPago = estPagoVal;
                    d.filtroRapido = currentFiltroRapido;
                    d.fechaDesde = $('#filtroFechaDesde').val();
                    d.fechaHasta = $('#filtroFechaHasta').val();
                }
            },
            language: {
                url: '/lib/datatables/es-ES.json',
                emptyTable: 'No hay ventas registradas aún en el sistema. Utilice el botón «Registrar Nueva Venta» para crear el primer pedido.',
                zeroRecords: 'No se encontraron ventas que coincidan con la búsqueda o los filtros seleccionados (ID pedido, cliente, documento, teléfono o estado).',
                searchPlaceholder: 'Ej: #ORD-0001, nombre, C.C., teléfono...'
            },
            columns: [
                {
                    data: 'id',
                    name: 'Id',
                    render: function (data) {
                        return '<span style="font-weight: 700; color: var(--purple-700);">#ORD-' + String(data).padStart(4, '0') + '</span>';
                    }
                },
                {
                    data: 'clienteNombre',
                    name: 'Cliente',
                    render: function (data, type, row) {
                        var html = '<div style="font-weight: 600; color: var(--purple-900);">' + (data || 'Cliente General') + '</div>';
                        if (row.clienteDocumento) {
                            html += '<div style="font-size: 0.74rem; color: var(--gray-500);">C.C. ' + row.clienteDocumento + (row.clienteTelefono ? ' · Tel: ' + row.clienteTelefono : '') + '</div>';
                        } else if (row.clienteTelefono) {
                            html += '<div style="font-size: 0.74rem; color: var(--gray-500);">Tel: ' + row.clienteTelefono + '</div>';
                        }
                        return html;
                    }
                },
                {
                    data: 'fechaPedido',
                    name: 'Fecha',
                    render: function (data) {
                        if (!data) return '-';
                        var d = new Date(data);
                        return '<span style="color: var(--gray-700);">' + d.toLocaleDateString('es-CO') + ' ' + d.toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit' }) + '</span>';
                    }
                },
                {
                    data: 'total',
                    name: 'Total',
                    className: 'dt-body-right',
                    render: function (data) {
                        return '<strong style="color: var(--purple-900);">$ ' + parseFloat(data || 0).toLocaleString('es-CO') + '</strong>';
                    }
                },
                {
                    data: 'saldoPendiente',
                    name: 'Saldo',
                    className: 'dt-body-right',
                    render: function (data) {
                        var val = parseFloat(data || 0);
                        var color = val > 0 ? '#DC2626' : '#16A34A';
                        return '<strong style="color: ' + color + '; font-weight: 800;">$ ' + val.toLocaleString('es-CO') + '</strong>';
                    }
                },
                {
                    data: 'estadoPago',
                    name: 'EstadoPago',
                    className: 'dt-body-center',
                    render: function (data) {
                        var st = (data || 'pendiente').toLowerCase();
                        var bg = st === 'pagado' ? 'var(--success)' : (st === 'abonado' || st === 'parcial' ? '#D97706' : '#DC2626');
                        return '<span style="background-color: ' + bg + '; color: white; padding: 0.25rem 0.55rem; border-radius: 9999px; font-size: 0.72rem; font-weight: 700; text-transform: capitalize;">' + (data || 'Pendiente') + '</span>';
                    }
                },
                {
                    data: 'estado',
                    name: 'Estado',
                    className: 'dt-body-center',
                    render: function (data) {
                        var st = (data || 'pendiente').toLowerCase();
                        var bg = st === 'entregado' ? '#047857' : (st === 'cancelado' ? '#DC2626' : 'var(--purple-100)');
                        var color = (st === 'entregado' || st === 'cancelado') ? 'white' : 'var(--purple-800)';
                        return '<span style="background-color: ' + bg + '; color: ' + color + '; padding: 0.25rem 0.55rem; border-radius: 9999px; font-size: 0.72rem; font-weight: 600; text-transform: capitalize;">' + (data || 'Pendiente') + '</span>';
                    }
                },
                {
                    data: 'formulaDetalle',
                    name: 'Formula',
                    className: 'dt-body-center',
                    orderable: false,
                    render: function (data, type, row) {
                        if (row.formulaOpticaId) {
                            return '<button type="button" class="btn btn-sm" onclick="openFormulaModal(' + row.id + ', ' + row.clienteId + ', ' + row.formulaOpticaId + ', \'' + (row.formulaDetalle || '') + '\')" style="background:#EDE9FE; color:#6D28D9; border:1px solid #C4B5FD; padding:0.2rem 0.5rem; border-radius:0.375rem; font-size:0.75rem; font-weight:700; cursor:pointer; display:inline-flex; align-items:center; gap:0.25rem;" title="Ver fórmula vinculada"><svg class="icon icon-sm" aria-hidden="true"><use href="' + spriteUrl + '#icon-stethoscope"></use></svg> ' + (row.formulaDetalle || ('#' + row.formulaOpticaId)) + '</button>';
                        }
                        return '<button type="button" class="btn btn-sm" onclick="openFormulaModal(' + row.id + ', ' + row.clienteId + ', null, \'\')" style="background:white; color:var(--gray-500); border:1px dashed var(--gray-300); padding:0.2rem 0.45rem; border-radius:0.375rem; font-size:0.72rem; cursor:pointer; display:inline-flex; align-items:center; gap:0.25rem;" title="Vincular fórmula óptica"><svg class="icon icon-sm" aria-hidden="true"><use href="' + spriteUrl + '#icon-plus"></use></svg> Vincular</button>';
                    }
                },
                {
                    data: 'id',
                    name: 'Acciones',
                    orderable: false,
                    className: 'dt-body-center',
                    render: function (data, type, row) {
                        var estadoLower = (row.estado || '').toLowerCase();
                        var html = '<div style="display: flex; gap: 0.35rem; justify-content: center; align-items: center; flex-wrap: wrap;">';

                        if (row.saldoPendiente > 0 && estadoLower !== 'cancelado') {
                            var numPed = '#ORD-' + String(row.id).padStart(4, '0');
                            var nomCli = (row.clienteNombre || 'Cliente').replace(/'/g, "\\'");
                            html += '<button type="button" class="btn-action" style="background:#2563EB; color:white; border:none; padding:0.25rem 0.55rem; border-radius:0.375rem; font-size:0.75rem; font-weight:700; cursor:pointer; display:inline-flex; align-items:center; gap:0.25rem;" onclick="openAbonoModal(' + row.id + ', \'' + numPed + '\', \'' + nomCli + '\', ' + row.saldoPendiente + ')"><svg class="icon icon-sm" aria-hidden="true"><use href="' + spriteUrl + '#icon-banknote"></use></svg> Abono</button>';
                            html += '<button type="button" class="btn-action" style="background:#6D28D9; color:white; border:none; padding:0.25rem 0.55rem; border-radius:0.375rem; font-size:0.75rem; font-weight:700; cursor:pointer; display:inline-flex; align-items:center; gap:0.25rem;" onclick="openTarjetaModal(' + row.id + ', ' + row.saldoPendiente + ')"><svg class="icon icon-sm" aria-hidden="true"><use href="' + spriteUrl + '#icon-credit-card"></use></svg> Tarjeta</button>';
                        }

                        if (estadoLower !== 'cancelado' && estadoLower !== 'entregado') {
                            var numPedEst = '#ORD-' + String(row.id).padStart(4, '0');
                            html += '<button type="button" class="btn-action" style="background:var(--purple-100); color:var(--purple-900); border:1px solid var(--purple-300); padding:0.25rem 0.55rem; border-radius:0.375rem; font-size:0.75rem; font-weight:600; cursor:pointer; display:inline-flex; align-items:center; gap:0.25rem;" onclick="openCambiarEstadoModal(' + row.id + ', \'' + numPedEst + '\', \'' + row.estado + '\', ' + row.saldoPendiente + ', ' + row.totalAbonos + ')"><svg class="icon icon-sm" aria-hidden="true"><use href="' + spriteUrl + '#icon-refresh-cw"></use></svg> Estado</button>';
                        }

                        if (row.tieneFactura && row.facturaId) {
                            html += '<a href="/Invoice/DownloadPdf/' + row.facturaId + '" target="_blank" style="background:#F3E8FF; color:#581C87; border:1px solid #D8B4FE; padding:0.25rem 0.55rem; border-radius:0.375rem; font-size:0.75rem; font-weight:700; text-decoration:none; display:inline-flex; align-items:center; gap:0.25rem;" title="Ver Factura ' + (row.facturaNumero || '') + '"><svg class="icon icon-sm" aria-hidden="true"><use href="' + spriteUrl + '#icon-file-text"></use></svg> ' + (row.facturaNumero || 'Factura') + '</a>';
                        }

                        html += '</div>';
                        return html;
                    }
                }
            ],
            pageLength: 10,
            lengthMenu: [10, 25, 50, 100],
            order: [[2, 'desc']],
            responsive: true
        });
    }

    // Activar sub-tab de inventario si viene en URL
    var urlParams = new URLSearchParams(window.location.search);
    var subtab = urlParams.get('subtab') || window.LENTSOFT_INITIAL_SUBTAB || '';
    if (subtab && ['productos', 'historial', 'pedidos', 'pedidos-ventas'].includes(subtab)) {
        switchInventorySubTab(subtab === 'pedidos-ventas' ? 'pedidos' : subtab);
    }
});

// ── Funciones de Filtros y Exportación Completa CSV (Sección 4.3) ──
function recargarTablaVentas(btnElem) {
    if (btnElem) {
        activarEstadoCargaBoton(btnElem, 'Actualizando...');
        setTimeout(function () { restaurarEstadoCargaBoton(btnElem); }, 500);
    }
    if (dtVentas) {
        dtVentas.ajax.reload();
    }
}

function aplicarFiltroRapido(valor, btn) {
    currentFiltroRapido = valor;
    $('.btn-filtro-rapido').css({ 'background': 'white', 'color': 'var(--purple-900)' });
    $(btn).css({ 'background': 'var(--purple-600)', 'color': 'white' });
    recargarTablaVentas();
}

function limpiarFiltrosVentas(btnElem) {
    currentFiltroRapido = '';
    $('#filtroFechaDesde').val('');
    $('#filtroFechaHasta').val('');
    $('#filtroEstadoVenta').val('');
    $('#filtroEstadoPago').val('');
    $('.btn-filtro-rapido').css({ 'background': 'white', 'color': 'var(--purple-900)' });
    $('.btn-filtro-rapido').first().css({ 'background': 'var(--purple-600)', 'color': 'white' });
    if (dtVentas) {
        dtVentas.search('');
    }
    recargarTablaVentas(btnElem);
}

function exportarVentasCsvCompleto(btnElem) {
    if (btnElem) {
        activarEstadoCargaBoton(btnElem, 'Exportando CSV...');
        setTimeout(function () { restaurarEstadoCargaBoton(btnElem); }, 1800);
    }

    var searchVal = dtVentas ? (dtVentas.search() || '') : '';
    var estado = $('#filtroEstadoVenta').val() || '';
    var estadoPago = $('#filtroEstadoPago').val() || '';
    var fechaDesde = $('#filtroFechaDesde').val() || '';
    var fechaHasta = $('#filtroFechaHasta').val() || '';

    var params = new URLSearchParams();
    if (searchVal) params.set('searchValue', searchVal);
    if (estado) params.set('estado', estado);
    if (estadoPago) params.set('estadoPago', estadoPago);
    if (fechaDesde) params.set('fechaDesde', fechaDesde);
    if (fechaHasta) params.set('fechaHasta', fechaHasta);
    if (currentFiltroRapido) params.set('filtroRapido', currentFiltroRapido);

    var url = '/Ventas/ExportarVentasCsv' + (params.toString() ? ('?' + params.toString()) : '');
    window.location.href = url;
}

// ── Modales de Ventas ──
function openAbonoModal(ventaId, pedidoNum, clienteNombre, saldo) {
    document.getElementById('abonoVentaId').value = ventaId;
    document.getElementById('abonoPedidoText').innerText = pedidoNum;
    document.getElementById('abonoClienteText').innerText = clienteNombre;
    document.getElementById('abonoSaldoText').innerText = '$ ' + parseFloat(saldo).toLocaleString('es-CO');
    var inputMonto = document.getElementById('abonoMontoInput');
    inputMonto.value = saldo;
    inputMonto.max = saldo;
    var btnSubmit = document.getElementById('btnConfirmarAbono');
    restaurarEstadoCargaBoton(btnSubmit);
    document.getElementById('modal-abono').style.display = 'flex';
}

function handleMetodoPagoChange(val) {
    if (val === 'Tarjeta de Crédito / Débito') {
        var ventaId = document.getElementById('abonoVentaId').value;
        var saldo = parseFloat(document.getElementById('abonoMontoInput').value || 0);
        document.getElementById('modal-abono').style.display = 'none';
        openTarjetaModal(ventaId, saldo);
    }
}

function openTarjetaModal(ventaId, saldo) {
    document.getElementById('tarjetaVentaId').value = ventaId;
    document.getElementById('tarjetaMontoDisplay').innerText = '$ ' + parseFloat(saldo).toLocaleString('es-CO');
    document.getElementById('tarjetaMontoInput').value = saldo;
    document.getElementById('tarjetaNumero').value = '';
    document.getElementById('tarjetaTitular').value = '';
    document.getElementById('tarjetaExp').value = '';
    document.getElementById('tarjetaCvv').value = '';
    document.getElementById('tarjetaLuhnMsg').innerText = '';
    document.getElementById('tarjetaMarcaBadge').innerText = 'Tarjeta';
    var btnSubmit = document.getElementById('btnCobrarTarjetaSubmit');
    restaurarEstadoCargaBoton(btnSubmit);
    document.getElementById('modal-tarjeta').style.display = 'flex';
}

function openCambiarEstadoModal(orderId, pedidoNum, estadoActual, saldo, totalAbonos) {
    document.getElementById('cambiarEstadoOrderId').value = orderId;
    document.getElementById('cambiarEstadoPedidoLabel').textContent = pedidoNum;
    document.getElementById('cambiarEstadoActualLabel').textContent = estadoActual;

    var sel = document.getElementById('cambiarEstadoSelect');
    sel.value = estadoActual.toLowerCase();

    var opPagado = sel.querySelector('option[value="pagado"]');
    var opCancelado = sel.querySelector('option[value="cancelado"]');
    if (opPagado) {
        opPagado.disabled = (saldo > 0);
        opPagado.text = saldo > 0 ? 'Pagado (Bloqueado: Saldo pendiente > 0)' : 'Pagado';
    }
    if (opCancelado) {
        opCancelado.disabled = (totalAbonos > 0);
        opCancelado.text = totalAbonos > 0 ? 'Cancelado (Bloqueado: Ya tiene abonos)' : 'Cancelado';
    }

    document.getElementById('modal-cambiar-estado').style.display = 'flex';
}

function openFormulaModal(orderId, clienteId, formulaId, formulaDetalle) {
    document.getElementById('vincularFormulaOrderId').value = orderId;
    var contenido = document.getElementById('formulaModalContenido');
    if (formulaId) {
        contenido.innerHTML = '<div style="background:#FAF5FF; border:1px solid var(--purple-300); padding:0.75rem; border-radius:0.5rem; margin-bottom:0.75rem;"><strong style="color:var(--purple-900);">Fórmula Actual:</strong> ' + formulaDetalle + '</div>';
    } else {
        contenido.innerHTML = '<div style="background:#F3F4F6; border:1px solid #E5E7EB; padding:0.75rem; border-radius:0.5rem; margin-bottom:0.75rem; color:#6B7280;">Sin fórmula óptica vinculada a este pedido.</div>';
    }

    var sel = document.getElementById('vincularFormulaSelect');
    sel.innerHTML = '<option value="">-- Cargando fórmulas del paciente... --</option>';

    if (clienteId) {
        fetch('/Ventas/GetFormulasPorPaciente?userId=' + clienteId)
            .then(function (r) { return r.json(); })
            .then(function (formulas) {
                sel.innerHTML = '<option value="">-- Sin Fórmula (Desvincular) --</option>';
                if (formulas && formulas.length > 0) {
                    formulas.forEach(function (f) {
                        var opt = document.createElement('option');
                        opt.value = f.id;
                        opt.textContent = '#' + f.id + ' — ' + (f.tipoLente || 'Fórmula') + ' (' + (f.fecha || '') + ')';
                        if (formulaId && f.id == formulaId) {
                            opt.selected = true;
                        }
                        sel.appendChild(opt);
                    });
                }
            })
            .catch(function (err) {
                sel.innerHTML = '<option value="">-- Sin Fórmula (Desvincular) --</option>';
                console.error('Error al cargar fórmulas:', err);
            });
    } else {
        sel.innerHTML = '<option value="">-- Sin Fórmula (Desvincular) --</option>';
    }

    document.getElementById('modal-formula').style.display = 'flex';
}

function actualizarInfoFactura(selectElem) {
    var selectedOpt = selectElem.options[selectElem.selectedIndex];
    var total = parseFloat(selectedOpt.getAttribute('data-total') || '0');
    var cliente = selectedOpt.getAttribute('data-cliente') || '-';
    var metodo = selectedOpt.getAttribute('data-metodo') || 'Efectivo';

    document.getElementById('facturaModalCliente').innerText = cliente;
    document.getElementById('facturaModalMetodo').innerText = metodo;
    document.getElementById('facturaModalTotal').innerText = 'COP $' + total.toLocaleString('es-CO');
}

function confirmDeleteInvoice(id, numero, btnElem) {
    if (confirm('¿Está seguro de eliminar la factura ' + numero + '?')) {
        if (btnElem) activarEstadoCargaBoton(btnElem, 'Eliminando...');
        var form = document.createElement('form');
        form.method = 'POST';
        form.action = '/Invoice/Delete/' + id;

        var tokenElem = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenElem) {
            var hiddenToken = document.createElement('input');
            hiddenToken.type = 'hidden';
            hiddenToken.name = '__RequestVerificationToken';
            hiddenToken.value = tokenElem.value;
            form.appendChild(hiddenToken);
        }

        document.body.appendChild(form);
        form.submit();
    }
}

// ── Validación Luhn & Tarjeta Client-Side ──
function validarLuhn(numero) {
    var digits = (numero || '').replace(/\D/g, '');
    if (digits.length < 13 || digits.length > 19) return false;
    var sum = 0;
    var alternate = false;
    for (var i = digits.length - 1; i >= 0; i--) {
        var n = parseInt(digits.charAt(i), 10);
        if (alternate) {
            n *= 2;
            if (n > 9) n = (n % 10) + 1;
        }
        sum += n;
        alternate = !alternate;
    }
    return (sum % 10 === 0);
}

function detectarFranquicia(numero) {
    var clean = (numero || '').replace(/\D/g, '');
    if (clean.startsWith('4')) return 'Visa';
    if (/^5[1-5]/.test(clean)) return 'Mastercard';
    if (/^3[47]/.test(clean)) return 'American Express';
    if (/^(6011|65)/.test(clean)) return 'Discover';
    return 'Tarjeta';
}

function formatearYDetectarTarjeta(input) {
    var val = input.value.replace(/\D/g, '').substring(0, 16);
    var formatted = val.match(/.{1,4}/g)?.join(' ') || val;
    input.value = formatted;

    var marca = detectarFranquicia(val);
    document.getElementById('tarjetaMarcaBadge').textContent = marca;

    var msg = document.getElementById('tarjetaLuhnMsg');
    if (val.length >= 13) {
        if (validarLuhn(val)) {
            msg.innerHTML = '<svg class="icon icon-sm" aria-hidden="true" style="vertical-align: -0.125em;"><use href="' + spriteUrl + '#icon-check"></use></svg> Tarjeta válida (' + marca + ')';
            msg.style.color = '#16A34A';
        } else {
            msg.innerHTML = '<svg class="icon icon-sm" aria-hidden="true" style="vertical-align: -0.125em;"><use href="' + spriteUrl + '#icon-x"></use></svg> Número de tarjeta no válido (Algoritmo de Luhn)';
            msg.style.color = '#DC2626';
        }
    } else {
        msg.textContent = '';
    }
}

function formatearExpiracion(input) {
    var val = input.value.replace(/\D/g, '').substring(0, 4);
    if (val.length >= 3) {
        input.value = val.substring(0, 2) + '/' + val.substring(2);
    } else {
        input.value = val;
    }
}

function validarTarjetaCliente(e) {
    var num = document.getElementById('tarjetaNumero').value.replace(/\D/g, '');
    if (!validarLuhn(num)) {
        showToast('El número de tarjeta no es válido según el algoritmo de Luhn.', 'error');
        e.preventDefault();
        return false;
    }
    var exp = document.getElementById('tarjetaExp').value;
    if (!/^\d{2}\/\d{2}$/.test(exp)) {
        showToast('La fecha de expiración debe tener el formato MM/YY.', 'error');
        e.preventDefault();
        return false;
    }
    var cvv = document.getElementById('tarjetaCvv').value;
    if (cvv.length < 3 || cvv.length > 4) {
        showToast('El CVV debe tener 3 o 4 dígitos.', 'error');
        e.preventDefault();
        return false;
    }
    var btn = document.getElementById('btnCobrarTarjetaSubmit');
    activarEstadoCargaBoton(btn, 'Procesando cobro...');
    return true;
}

// ── Gestión de Sub-tabs de Inventario (Ventas) ──
function switchInventorySubTab(subtabName) {
    var normalized = subtabName === 'pedidos-ventas' ? 'pedidos' : subtabName;
    const tabs = ['productos', 'historial', 'pedidos'];
    tabs.forEach(t => {
        const content = document.getElementById('subtab-content-' + t);
        const btnNav = document.getElementById('nav-subtab-' + t);

        if (t === normalized) {
            if (content) content.style.display = 'block';
            if (btnNav) {
                btnNav.style.color = 'var(--purple-700)';
                btnNav.style.borderBottom = '3px solid var(--purple-600)';
                btnNav.classList.add('active');
            }
        } else {
            if (content) content.style.display = 'none';
            if (btnNav) {
                btnNav.style.color = 'var(--gray-500)';
                btnNav.style.borderBottom = 'none';
                btnNav.classList.remove('active');
            }
        }
    });

    if (window.history && window.history.pushState) {
        const currentUrl = new URL(window.location.href);
        currentUrl.searchParams.set('section', 'inventarios');
        currentUrl.searchParams.set('subtab', normalized);
        window.history.pushState({ path: currentUrl.toString() }, '', currentUrl.toString());
    }
}

// ── Pedidos de Venta (SalesOrder — Sección 4.1 y 4.2): Búsqueda en Servidor y Selección por ProductId ──
function submitPedidosVentasFilters() {
    const search = document.getElementById('searchPedidosVentas')?.value || '';
    applyPedidosVentasParam('pedSearch', search);
}

function applyPedidosVentasParam(param, value) {
    const url = new URL(window.location.href);
    url.searchParams.set('section', 'inventarios');
    url.searchParams.set('subtab', 'pedidos');
    url.searchParams.set('pedPage', '1');
    if (value !== undefined && value !== null && value !== '') {
        url.searchParams.set(param, value);
    } else {
        url.searchParams.delete(param);
    }
    window.location.href = url.toString();
}

function onSalesOrderProductChange(selectElem, precioInputId, totalInputId, qtyInputId, stockHelpId) {
    if (!selectElem) return;
    const opt = selectElem.options[selectElem.selectedIndex];
    const precioInput = document.getElementById(precioInputId);
    const totalInput = document.getElementById(totalInputId);
    const qtyInput = document.getElementById(qtyInputId);
    const stockHelp = stockHelpId ? document.getElementById(stockHelpId) : null;

    if (!opt || !opt.value) {
        if (precioInput) precioInput.value = '0';
        if (totalInput) totalInput.value = 'COP $0';
        if (qtyInput) qtyInput.removeAttribute('max');
        if (stockHelp) stockHelp.textContent = '';
        return;
    }

    const precio = parseFloat(opt.getAttribute('data-precio') || '0');
    const stock = parseInt(opt.getAttribute('data-stock') || '0', 10);

    if (precioInput) precioInput.value = precio;
    if (qtyInput) {
        qtyInput.setAttribute('max', stock);
        if (parseInt(qtyInput.value || '1', 10) > stock && stock > 0) {
            qtyInput.value = stock;
        }
    }
    if (stockHelp) {
        stockHelp.textContent = 'Stock disponible en catálogo: ' + stock + ' unidades · Precio unitario oficial: COP $' + precio.toLocaleString('es-CO');
    }

    recalcularTotalPedidoModal(precioInputId, qtyInputId, totalInputId);
}

function recalcularTotalPedidoModal(precioInputId, qtyInputId, totalInputId) {
    const precio = parseFloat(document.getElementById(precioInputId)?.value || '0');
    const cant = parseInt(document.getElementById(qtyInputId)?.value || '1', 10);
    const totalEl = document.getElementById(totalInputId);
    if (totalEl) {
        const total = Math.max(0, precio * Math.max(0, cant));
        totalEl.value = 'COP $' + Math.round(total).toLocaleString('es-CO');
    }
}

function openEditSalesOrderModal(id, numero, cliente, productoNombre, cantidad, precio, estado, notas) {
    document.getElementById('editSalesOrderId').value = id;
    document.getElementById('editSalesOrderNumero').value = numero || '';
    document.getElementById('editSalesOrderCliente').value = cliente || '';
    document.getElementById('editSalesOrderCantidad').value = cantidad || 1;
    document.getElementById('editSalesOrderPrecio').value = precio || 0;
    document.getElementById('editSalesOrderEstado').value = (estado || 'pendiente').toLowerCase();
    document.getElementById('editSalesOrderNotas').value = notas || '';

    const selectProd = document.getElementById('editSalesOrderProductId');
    if (selectProd) {
        let matchedVal = '';
        const targetName = (productoNombre || '').trim().toLowerCase();
        Array.from(selectProd.options).forEach(opt => {
            const optName = (opt.getAttribute('data-nombre') || '').trim().toLowerCase();
            if (optName && optName === targetName) {
                matchedVal = opt.value;
            }
        });
        selectProd.value = matchedVal;
        onSalesOrderProductChange(selectProd, 'editSalesOrderPrecio', 'editSalesOrderTotalDisplay', 'editSalesOrderCantidad', 'editSalesOrderStockHelp');
    }

    recalcularTotalPedidoModal('editSalesOrderPrecio', 'editSalesOrderCantidad', 'editSalesOrderTotalDisplay');
    document.getElementById('modal-editar-pedido-venta').style.display = 'flex';
}

// ── Filtrado de menús desplegables ──
function toggleFilterMenu(submodule) {
    const menu = document.getElementById('filter-menu-' + submodule);
    if (!menu) return;
    const isVisible = menu.style.display === 'block';
    document.querySelectorAll('[id^="filter-menu-"]').forEach(m => m.style.display = 'none');
    menu.style.display = isVisible ? 'none' : 'block';
}

function toggleMenu(menuId) {
    const menu = document.getElementById(menuId);
    if (!menu) return;
    const isShown = menu.style.display === 'block';
    document.querySelectorAll('.inv-dropdown-menu').forEach(m => m.style.display = 'none');
    menu.style.display = isShown ? 'none' : 'block';
}

// ── Alternar Estado de Producto en Ventas ──
async function toggleProductStatus(id, btnElem) {
    if (!activarEstadoCargaBoton(btnElem, '...')) return;
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        const res = await fetch('/Ventas/ToggleProductStatus/' + id, {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token
            }
        });
        const data = await res.json();
        restaurarEstadoCargaBoton(btnElem);
        if (data.success) {
            btnElem.innerText = data.active ? 'Activo' : 'Inactivo';
            btnElem.className = 'inv-chip inv-chip-btn ' + (data.active ? 'inv-chip-success' : 'inv-chip-error');
            const row = btnElem.closest('tr');
            if (row) {
                row.setAttribute('data-estado', data.active ? 'activo' : 'inactivo');
            }
            showToast(data.message, 'success');
        } else {
            showToast(data.message || 'Error al cambiar estado', 'error');
        }
    } catch (e) {
        restaurarEstadoCargaBoton(btnElem);
        showToast('Error de comunicación con el servidor', 'error');
    }
}

function showToast(msg, type = 'info') {
    if (typeof alertify !== 'undefined') {
        if (type === 'success') alertify.success(msg);
        else if (type === 'error') alertify.error(msg);
        else if (type === 'warning') (alertify.warning ? alertify.warning(msg) : alertify.error(msg));
        else alertify.message(msg);
        return;
    }
    let container = document.getElementById('inv-toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'inv-toast-container';
        container.style.cssText = 'position: fixed; bottom: 24px; right: 24px; z-index: 99999; display: flex; flex-direction: column; gap: 8px; max-width: 380px; pointer-events: none; font-family: system-ui, -apple-system, sans-serif;';
        document.body.appendChild(container);
    }
    const toast = document.createElement('div');
    const bg = type === 'success' ? '#059669' : (type === 'error' ? '#DC2626' : (type === 'warning' ? '#D97706' : '#7C3AED'));
    toast.style.cssText = `pointer-events: auto; padding: 12px 18px; border-radius: 8px; color: white; font-weight: 500; font-size: 0.875rem; box-shadow: 0 10px 25px -5px rgba(0,0,0,0.3); transition: all 0.25s ease; opacity: 0; transform: translateY(12px); display: flex; align-items: center; gap: 10px; background: ${bg};`;
    toast.innerHTML = `<span>${msg}</span>`;
    container.appendChild(toast);
    requestAnimationFrame(() => {
        toast.style.opacity = '1';
        toast.style.transform = 'translateY(0)';
    });
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateY(12px)';
        setTimeout(() => toast.remove(), 250);
    }, 4000);
}

// ── Funciones de Inventario / Kardex / Productos en Ventas ──
function handleManualMovTipoChange(val) {
    const motivoSelect = document.getElementById('manualMovMotivo');
    const lbl = document.getElementById('lblManualMovMotivo');
    if (!motivoSelect) return;

    if (val === 'Salida') {
        lbl.textContent = 'Motivo * (Obligatorio en Salidas)';
        motivoSelect.required = true;
        if (motivoSelect.value === 'Compra') {
            motivoSelect.value = 'Merma';
        }
    } else if (val === 'Entrada') {
        lbl.textContent = 'Motivo *';
        motivoSelect.required = true;
        if (motivoSelect.value === 'Merma' || motivoSelect.value === 'Daño') {
            motivoSelect.value = 'Compra';
        }
    } else {
        lbl.textContent = 'Motivo *';
        motivoSelect.value = 'Ajuste por conteo';
    }
}

function exportKardexTableExcel() {
    if (typeof XLSX === 'undefined') {
        showToast('Librería de exportación no cargada', 'error');
        return;
    }
    const prodNombre = document.getElementById('kardex-prod-nombre')?.textContent || 'Producto';
    const prodSku = document.getElementById('kardex-prod-sku')?.textContent || '';
    const rows = document.querySelectorAll('#kardex-tbody tr');
    if (!rows || rows.length === 0) {
        showToast('No hay datos para exportar', 'warning');
        return;
    }

    const data = [
        [`Kardex de Producto: ${prodNombre} (${prodSku})`],
        [`Generado: ${new Date().toLocaleString()}`],
        [],
        ['Fecha y Hora', 'Tipo', 'Cantidad', 'Saldo Resultante', 'Motivo', 'Responsable', 'Referencia y Nota']
    ];

    rows.forEach(r => {
        const cols = r.querySelectorAll('td');
        if (cols.length >= 7) {
            data.push([
                cols[0].innerText.trim(),
                cols[1].innerText.trim(),
                cols[2].innerText.trim(),
                cols[3].innerText.trim(),
                cols[4].innerText.trim(),
                cols[5].innerText.trim(),
                cols[6].innerText.trim()
            ]);
        }
    });

    const ws = XLSX.utils.aoa_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Kardex');
    XLSX.writeFile(wb, `LentSoft_Kardex_${prodNombre.replace(/[^a-zA-Z0-9]/g, '_')}_${new Date().toISOString().slice(0, 10)}.xlsx`);
    showToast('Kardex exportado exitosamente', 'success');
}

function previewProductImage(input, previewImgId, containerId, infoId) {
    const file = input.files && input.files[0];
    const previewImg = document.getElementById(previewImgId);
    const container = document.getElementById(containerId);
    const infoText = infoId ? document.getElementById(infoId) : null;

    if (!file) return;

    const validTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/jpg'];
    const fileExt = '.' + file.name.split('.').pop().toLowerCase();
    const validExts = ['.jpg', '.jpeg', '.png', '.webp'];

    if (!validTypes.includes(file.type.toLowerCase()) && !validExts.includes(fileExt)) {
        showToast('Formato no permitido. Solo se aceptan imágenes JPG, PNG o WEBP.', 'error');
        input.value = '';
        if (infoText) infoText.textContent = '';
        return;
    }

    if (file.size > 5 * 1024 * 1024) {
        showToast('El archivo supera el tamaño máximo permitido de 5 MB.', 'error');
        input.value = '';
        if (infoText) infoText.textContent = '';
        return;
    }

    const reader = new FileReader();
    reader.onload = function (e) {
        if (previewImg) previewImg.src = e.target.result;
        if (container) container.style.display = 'block';
        if (infoText) infoText.textContent = `${file.name} (${(file.size / 1024).toFixed(0)} KB)`;
    };
    reader.readAsDataURL(file);
}

function clearProductImagePreview(inputId, previewImgId, containerId, infoId, fallbackUrl = '') {
    const input = document.getElementById(inputId);
    if (input) input.value = '';
    const previewImg = document.getElementById(previewImgId);
    const container = document.getElementById(containerId);
    const infoText = infoId ? document.getElementById(infoId) : null;

    if (fallbackUrl && fallbackUrl.trim() !== '') {
        if (previewImg) previewImg.src = fallbackUrl;
        if (container) container.style.display = 'block';
        if (infoText) infoText.textContent = 'Imagen actual';
    } else {
        if (previewImg) previewImg.src = '';
        if (container) container.style.display = 'none';
        if (infoText) infoText.textContent = '';
    }
}

function openEditProductModal(id, nombre, descripcion, precio, precioDescuento, categoria, marca, stock, activo, imagenUrl, sku, costoCompra, stockMinimo, ubicacion, supplierId, rowVersion) {
    document.getElementById('editProductId').value = id;
    document.getElementById('editProductNombre').value = nombre || '';
    document.getElementById('editProductDescripcion').value = descripcion || '';
    document.getElementById('editProductPrecio').value = precio || 0;
    document.getElementById('editProductPrecioDescuento').value = precioDescuento || '';
    document.getElementById('editProductCategoria').value = categoria || 'lentes-sol';
    document.getElementById('editProductMarca').value = marca || '';
    document.getElementById('editProductStock').value = stock || 0;
    document.getElementById('editProductActivo').checked = (activo === true || activo === 'true');
    document.getElementById('editProductExistingImagenUrl').value = imagenUrl || '';

    if (document.getElementById('editProductSku')) document.getElementById('editProductSku').value = sku || '';
    if (document.getElementById('editProductCostoCompra')) document.getElementById('editProductCostoCompra').value = costoCompra || '';
    if (document.getElementById('editProductStockMinimo')) document.getElementById('editProductStockMinimo').value = (stockMinimo !== undefined && stockMinimo !== null) ? stockMinimo : 10;
    if (document.getElementById('editProductUbicacion')) document.getElementById('editProductUbicacion').value = ubicacion || '';
    if (document.getElementById('editProductSupplierId')) document.getElementById('editProductSupplierId').value = supplierId || '';
    if (document.getElementById('editProductRowVersion')) document.getElementById('editProductRowVersion').value = rowVersion || '';

    const fileInput = document.getElementById('editProductImagenArchivo');
    if (fileInput) fileInput.value = '';

    const previewContainer = document.getElementById('editProductPreviewContainer');
    const previewImg = document.getElementById('preview-editar-producto');
    const previewInfo = document.getElementById('editProductImageInfo');

    if (imagenUrl && imagenUrl.trim() !== '') {
        if (previewImg) previewImg.src = imagenUrl;
        if (previewContainer) previewContainer.style.display = 'block';
        if (previewInfo) previewInfo.textContent = 'Imagen actual guardada';
    } else {
        if (previewImg) previewImg.src = '';
        if (previewContainer) previewContainer.style.display = 'none';
        if (previewInfo) previewInfo.textContent = '';
    }

    document.getElementById('modal-editar-producto').style.display = 'flex';
}

function submitProductFilters() {
    const search = document.getElementById('searchProducts')?.value || '';
    applyProdParam('prodSearch', search);
}

function applyProdParam(param, value) {
    const url = new URL(window.location.href);
    url.searchParams.set('section', 'inventarios');
    url.searchParams.set('subtab', 'productos');
    url.searchParams.set('prodPage', '1');
    if (value !== undefined && value !== null && value !== '') {
        url.searchParams.set(param, value);
    } else {
        url.searchParams.delete(param);
    }
    window.location.href = url.toString();
}

function toggleSelectAllProducts(master) {
    const cbs = document.querySelectorAll('.inv-product-select-cb');
    cbs.forEach(cb => cb.checked = master.checked);
    updateBulkProductsBar();
}

function updateBulkProductsBar() {
    const selected = document.querySelectorAll('.inv-product-select-cb:checked');
    const bar = document.getElementById('inv-products-bulk-bar');
    const countSpan = document.getElementById('inv-products-bulk-count');
    const masterCb = document.getElementById('selectAllProducts');
    const allCbs = document.querySelectorAll('.inv-product-select-cb');

    if (countSpan) countSpan.textContent = selected.length;
    if (bar) {
        if (selected.length > 0) bar.classList.add('active');
        else bar.classList.remove('active');
    }
    if (masterCb && allCbs.length > 0) {
        masterCb.checked = selected.length === allCbs.length;
        masterCb.indeterminate = selected.length > 0 && selected.length < allCbs.length;
    }
}

function clearSelectedProducts() {
    const cbs = document.querySelectorAll('.inv-product-select-cb');
    cbs.forEach(cb => cb.checked = false);
    const masterCb = document.getElementById('selectAllProducts');
    if (masterCb) {
        masterCb.checked = false;
        masterCb.indeterminate = false;
    }
    updateBulkProductsBar();
}

async function bulkToggleProducts(activate) {
    const selected = Array.from(document.querySelectorAll('.inv-product-select-cb:checked')).map(cb => parseInt(cb.value, 10));
    if (selected.length === 0) {
        showToast('Selecciona al menos un producto', 'warning');
        return;
    }
    const actionText = activate ? 'activar' : 'inactivar';
    if (!confirm(`¿Estás seguro de que deseas ${actionText} ${selected.length} producto(s)?`)) {
        return;
    }
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    try {
        const res = await fetch('/Ventas/BulkToggleStatus', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ ids: selected, activo: activate })
        });
        const data = await res.json();
        if (data.success) {
            showToast(data.message, 'success');
            setTimeout(() => window.location.reload(), 600);
        } else {
            showToast(data.message || 'Error en la acción masiva', 'error');
        }
    } catch (e) {
        showToast('Error de comunicación con el servidor', 'error');
    }
}

function exportSelectedProductsExcel() {
    const selectedCheckboxes = document.querySelectorAll('.inv-product-select-cb:checked');
    if (selectedCheckboxes.length === 0) {
        showToast('Selecciona al menos un producto para exportar', 'warning');
        return;
    }
    if (typeof XLSX === 'undefined') {
        showToast('Librería de exportación no cargada', 'error');
        return;
    }
    const selectedIds = new Set(Array.from(selectedCheckboxes).map(cb => cb.value));
    const table = document.getElementById('tableProducts');
    if (!table) return;

    const data = [
        ['ID', 'Producto', 'SKU', 'Categoría', 'Marca', 'Proveedor', 'Precio', 'Stock', 'Ubicación', 'Estado']
    ];

    const rows = table.querySelectorAll('tbody tr');
    rows.forEach(tr => {
        const cb = tr.querySelector('.inv-product-select-cb');
        if (cb && selectedIds.has(cb.value)) {
            const id = cb.value;
            const nombre = tr.querySelector('[data-label="Producto"] a')?.innerText?.trim() || '';
            const ubicacion = tr.getAttribute('data-ubicacion') || '';
            const sku = tr.querySelector('[data-label="SKU"]')?.innerText?.trim() || '';
            const cat = tr.querySelector('[data-label="Categoría"]')?.innerText?.trim() || '';
            const marca = tr.querySelector('[data-label="Marca"]')?.innerText?.trim() || '';
            const prov = tr.querySelector('[data-label="Proveedor"]')?.innerText?.trim() || '';
            const precio = tr.querySelector('[data-label="Precio"]')?.innerText?.trim() || '';
            const stock = tr.querySelector('[data-label="Stock"] span')?.innerText?.trim() || '';
            const estado = tr.querySelector('[data-label="Estado"]')?.innerText?.trim() || '';

            data.push([id, nombre, sku, cat, marca, prov, precio, stock, ubicacion, estado]);
        }
    });

    const ws = XLSX.utils.aoa_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Productos');
    XLSX.writeFile(wb, `LentSoft_Productos_Seleccionados_${new Date().toISOString().slice(0, 10)}.xlsx`);
    showToast('Exportación completada exitosamente', 'success');
}

function submitMovFilters() {
    const s = document.getElementById('searchHistorial')?.value || '';
    applyMovParam('movSearch', s);
}

function applyMovParam(param, value) {
    const url = new URL(window.location.href);
    url.searchParams.set('section', 'inventarios');
    url.searchParams.set('subtab', 'historial');
    url.searchParams.set('movPage', '1');
    if (value !== undefined && value !== null && value !== '') {
        url.searchParams.set(param, value);
    } else {
        url.searchParams.delete(param);
    }
    window.location.href = url.toString();
}

async function exportAllMovimientosExcel() {
    if (typeof XLSX === 'undefined') {
        showToast('Librería de exportación no cargada', 'error');
        return;
    }

    showToast('Preparando exportación completa de movimientos...', 'info');

    try {
        const url = new URL(window.location.href);
        const search = url.searchParams.get('movSearch') || '';
        const tipo = url.searchParams.get('movTipo') || '';
        const prodId = url.searchParams.get('movProductId') || '';
        const fIni = url.searchParams.get('movFechaInicio') || '';
        const fFin = url.searchParams.get('movFechaFin') || '';
        const sort = url.searchParams.get('movSort') || 'desc';

        const fetchUrl = `/Ventas/ExportarExcelDatos?search=${encodeURIComponent(search)}&tipo=${encodeURIComponent(tipo)}&productId=${encodeURIComponent(prodId)}&fechaInicio=${encodeURIComponent(fIni)}&fechaFin=${encodeURIComponent(fFin)}&sort=${encodeURIComponent(sort)}`;

        const res = await fetch(fetchUrl);
        const json = await res.json();

        if (!json.success || !json.data || json.data.length === 0) {
            showToast('No hay datos para exportar con los filtros seleccionados', 'warning');
            return;
        }

        const excelRows = [
            ['ID Mov', 'Fecha y Hora', 'Producto', 'SKU', 'Tipo', 'Cantidad', 'Stock Resultante', 'Motivo', 'Responsable', 'Referencia', 'Nota']
        ];

        json.data.forEach(m => {
            excelRows.push([
                '#MOV-' + m.id,
                m.fecha,
                m.producto,
                m.sku,
                m.tipo,
                m.cantidad,
                m.stockResultante,
                m.motivo,
                m.responsable,
                m.referencia,
                m.nota
            ]);
        });

        const ws = XLSX.utils.aoa_to_sheet(excelRows);
        const wb = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(wb, ws, 'Historial_Movimientos');
        XLSX.writeFile(wb, `LentSoft_Historial_Movimientos_${new Date().toISOString().slice(0, 10)}.xlsx`);
        showToast(`Exportación completada con éxito (${json.data.length} movimientos).`, 'success');
    } catch (e) {
        showToast('Error al exportar los datos: ' + e.message, 'error');
    }
}

async function abrirModalKardex(productId) {
    const modal = document.getElementById('modal-kardex');
    if (!modal) return;

    modal.style.display = 'flex';
    const loading = document.getElementById('kardex-loading');
    const content = document.getElementById('kardex-content');
    if (loading) loading.style.display = 'block';
    if (content) content.style.display = 'none';

    try {
        const res = await fetch(`/Ventas/KardexJson?productId=${productId}`);
        const data = await res.json();

        if (!data.success) {
            showToast(data.message || 'No se pudo cargar el Kardex', 'error');
            modal.style.display = 'none';
            return;
        }

        const p = data.product;
        document.getElementById('kardex-prod-nombre').textContent = p.nombre || 'Producto';
        document.getElementById('kardex-prod-sku').textContent = p.sku ? 'SKU: ' + p.sku : 'Sin SKU';
        const kardexStockEl = document.getElementById('kardex-prod-stock');
        if (kardexStockEl) {
            kardexStockEl.textContent = p.stock + ' unidades';
            kardexStockEl.className = 'inv-chip ' + (p.stock === 0 ? 'inv-chip-error' : (p.stock <= p.stockMinimo ? 'inv-chip-warning' : 'inv-chip-success'));
        }
        document.getElementById('kardex-prod-stockmin').textContent = p.stockMinimo + ' unidades';
        document.getElementById('kardex-prod-precio').textContent = '$' + Number(p.precio).toLocaleString();
        document.getElementById('kardex-prod-prov').textContent = p.proveedor || 'Sin proveedor';

        const tbody = document.getElementById('kardex-tbody');
        if (tbody) {
            tbody.innerHTML = '';
            if (!data.movimientos || data.movimientos.length === 0) {
                tbody.innerHTML = '<tr><td colspan="7" style="text-align: center; color: var(--gray-500); padding: 1.5rem;">No se registran movimientos para este producto.</td></tr>';
            } else {
                data.movimientos.forEach(m => {
                    const tr = document.createElement('tr');
                    const isSalida = m.tipo.toLowerCase() === 'salida' || m.tipo.toLowerCase() === 'baja';
                    const cantColor = isSalida ? 'var(--inv-red)' : 'var(--inv-green)';
                    const cantSign = isSalida ? '-' : '+';
                    const tLower = (m.tipo || '').toLowerCase();
                    let chipClass = 'inv-chip-gray';
                    if (tLower === 'entrada' || tLower === 'alta' || tLower === 'reactivación' || tLower === 'reactivacion') chipClass = 'inv-chip-success';
                    else if (tLower === 'salida' || tLower === 'baja') chipClass = 'inv-chip-error';
                    else if (tLower === 'ajuste' || tLower === 'edición' || tLower === 'edicion') chipClass = 'inv-chip-warning';

                    tr.innerHTML = `
                        <td style="font-size: 0.85rem; color: var(--gray-600);">${m.fecha}</td>
                        <td><span class="inv-chip ${chipClass}" data-no-i18n>${m.tipo}</span></td>
                        <td style="font-weight: 600; color: ${cantColor}; text-align: center;">${cantSign}${m.cantidad}</td>
                        <td style="font-weight: 700; color: var(--purple-900); text-align: center;">${m.stockResultante} unid.</td>
                        <td><span class="inv-chip inv-chip-gray">${m.motivo || 'Otro'}</span></td>
                        <td style="font-size: 0.85rem; color: var(--gray-700);">${m.responsable || 'Admin'}</td>
                        <td style="font-size: 0.8rem; color: var(--gray-500);">${m.referencia ? 'Ref: ' + m.referencia + ' ' : ''}${m.nota ? '<svg class="icon icon-sm" aria-hidden="true" style="vertical-align: -0.15em;"><use href="' + spriteUrl + '#icon-message-square"></use></svg> ' + m.nota : ''}</td>
                    `;
                    tbody.appendChild(tr);
                });
            }
        }

        if (loading) loading.style.display = 'none';
        if (content) content.style.display = 'block';
    } catch (e) {
        showToast('Error al obtener datos del Kardex', 'error');
        modal.style.display = 'none';
    }
}

// ── Modal Registrar Nueva Venta (Múltiples Productos y Fórmula Óptica) ──
let saleItems = [];
let currentClientFormula = null;
let allRawProducts = [];

async function addSaleItem(btnElem) {
    const prodSelect = document.getElementById('newSaleProdSelect');
    const qtyInput = document.getElementById('newSaleQtyInput');
    if (!prodSelect || !qtyInput) return;

    const productId = parseInt(prodSelect.value || '0', 10);
    const cantidad = parseInt(qtyInput.value || '1', 10);

    if (productId <= 0 || cantidad <= 0) {
        showToast('Seleccione un producto válido y una cantidad mayor a 0', 'error');
        return;
    }

    const existingIndex = saleItems.findIndex(i => i.productId === productId);
    let totalQty = cantidad;
    if (existingIndex >= 0) {
        totalQty += saleItems[existingIndex].cantidad;
    }

    if (btnElem) activarEstadoCargaBoton(btnElem, 'Validando...');

    try {
        const response = await fetch(`/Product/CheckStock?productId=${productId}&quantity=${totalQty}`);
        if (!response.ok) throw new Error('Error al consultar stock.');
        const result = await response.json();

        if (btnElem) restaurarEstadoCargaBoton(btnElem);

        if (!result.success) {
            showToast(result.message, 'error');
            return;
        }

        if (!result.sufficient) {
            const alreadyInGrid = existingIndex >= 0 ? saleItems[existingIndex].cantidad : 0;
            const msg = `Stock insuficiente en el servidor. Disponible real: ${result.available}.` +
                (alreadyInGrid > 0 ? ` Ya tienes ${alreadyInGrid} en la lista.` : '');
            showToast(msg, 'error');
            return;
        }

        const opt = prodSelect.options[prodSelect.selectedIndex];
        const nombre = opt.getAttribute('data-nombre');
        const categoria = opt.getAttribute('data-categoria') || '';
        const precio = parseFloat(opt.getAttribute('data-precio') || '0');
        const iva = parseFloat(opt.getAttribute('data-iva') || '19');

        if (existingIndex >= 0) {
            saleItems[existingIndex].cantidad = totalQty;
            saleItems[existingIndex].subtotal = saleItems[existingIndex].cantidad * precio;
        } else {
            saleItems.push({
                productId: productId,
                nombre: nombre,
                categoria: categoria,
                cantidad: cantidad,
                precioUnitario: precio,
                ivaPercent: iva,
                subtotal: precio * cantidad
            });
        }

        prodSelect.value = '';
        qtyInput.value = 1;

        const stockMsg = document.getElementById('stock-availability-msg');
        if (stockMsg) stockMsg.innerText = '';
        qtyInput.removeAttribute('max');

        renderSaleItems();
        evaluarAlertaLenteGraduado();
    } catch (err) {
        if (btnElem) restaurarEstadoCargaBoton(btnElem);
        console.error(err);
        showToast('Error al validar el stock en el servidor.', 'error');
    }
}

function removeSaleItem(index) {
    saleItems.splice(index, 1);
    renderSaleItems();
    evaluarAlertaLenteGraduado();
}

function renderSaleItems() {
    const tbody = document.getElementById('saleItemsTableBody');
    if (!tbody) return;

    if (saleItems.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6" style="padding: 1rem; text-align: center; color: var(--gray-500); font-style: italic;" data-i18n="modal.sale.noitems">No ha agregado productos a esta venta aún.</td></tr>`;
    } else {
        tbody.innerHTML = saleItems.map((item, idx) => `
            <tr style="border-bottom: 1px solid var(--purple-100);">
                <td style="padding: 0.5rem 0.75rem; font-weight: 600; color: var(--purple-900);">${item.nombre}</td>
                <td style="padding: 0.5rem; text-align: center;">${item.cantidad}</td>
                <td style="padding: 0.5rem; text-align: right;">COP $${Math.round(item.precioUnitario).toLocaleString('es-CO')}</td>
                <td style="padding: 0.5rem; text-align: center;"><span style="background: var(--purple-100); color: var(--purple-800); padding: 0.15rem 0.45rem; border-radius: 0.25rem; font-weight: 700; font-size: 0.75rem;">${item.ivaPercent}%</span></td>
                <td style="padding: 0.5rem 0.75rem; text-align: right; font-weight: 700;">COP $${Math.round(item.subtotal).toLocaleString('es-CO')}</td>
                <td style="padding: 0.5rem; text-align: center;">
                    <button type="button" onclick="removeSaleItem(${idx})" style="background: none; border: none; color: var(--error); cursor: pointer; font-size: 1rem; font-weight: bold;">&times;</button>
                </td>
            </tr>
        `).join('');
    }

    if (typeof window.applyTranslations === 'function' && typeof window.getCurrentLang === 'function') {
        window.applyTranslations(window.getCurrentLang());
    }

    calculateSaleTotals();
}

function calculateSaleTotals() {
    const subtotalSum = saleItems.reduce((acc, i) => acc + i.subtotal, 0);
    const descSelect = document.getElementById('descuentoSelect');
    const descPercent = parseInt(descSelect ? descSelect.value : '0', 10);

    const totalFinal = subtotalSum * (1 - (descPercent / 100));

    const subtotalDisplay = document.getElementById('subtotalDisplay');
    const totalDisplay = document.getElementById('totalDisplay');
    const jsonInput = document.getElementById('saleItemsJson');

    if (subtotalDisplay) subtotalDisplay.value = 'COP $' + Math.round(subtotalSum).toLocaleString('es-CO');
    if (totalDisplay) totalDisplay.value = 'COP $' + Math.round(totalFinal).toLocaleString('es-CO');
    if (jsonInput) jsonInput.value = JSON.stringify(saleItems.map(i => ({ productId: i.productId, cantidad: i.cantidad })));
}

function setCustomerFieldsReadOnly(isReadOnly) {
    const fieldIds = ['saleNombre', 'saleApellido', 'saleCedula', 'saleTelefono', 'saleDireccion'];
    fieldIds.forEach(id => {
        const input = document.getElementById(id);
        if (input) {
            input.readOnly = isReadOnly;
            if (isReadOnly) {
                input.style.backgroundColor = 'var(--gray-100)';
                input.style.cursor = 'not-allowed';
            } else {
                input.style.backgroundColor = 'white';
                input.style.cursor = 'text';
            }
        }
    });
}

function populateCustomerData(option) {
    if (!option || !option.value) {
        document.getElementById('selectedUserId').value = '';
        document.getElementById('saleNombre').value = '';
        document.getElementById('saleApellido').value = '';
        document.getElementById('saleCedula').value = '';
        document.getElementById('saleTelefono').value = '';
        document.getElementById('saleDireccion').value = '';
        setCustomerFieldsReadOnly(false);
        consultarFormulaCliente(null);
        return;
    }

    const id = option.value;
    const nombre = option.getAttribute('data-nombre') || '';
    const apellido = option.getAttribute('data-apellido') || '';
    const cedula = option.getAttribute('data-cedula') || '';
    const telefono = option.getAttribute('data-telefono') || '';
    const direccion = option.getAttribute('data-direccion') || '';

    document.getElementById('selectedUserId').value = id;
    document.getElementById('saleNombre').value = nombre;
    document.getElementById('saleApellido').value = apellido;
    document.getElementById('saleCedula').value = cedula;
    document.getElementById('saleTelefono').value = telefono;
    document.getElementById('saleDireccion').value = direccion;

    setCustomerFieldsReadOnly(true);
    consultarFormulaCliente(id);
}

function onClientSelectedByName(selectEl) {
    const val = selectEl.value;
    const cedulaSelect = document.getElementById('userSelectCedula');
    if (cedulaSelect) cedulaSelect.value = val;

    const option = selectEl.options[selectEl.selectedIndex];
    populateCustomerData(option);
}

function onClientSelectedByCedula(selectEl) {
    const val = selectEl.value;
    const nombreSelect = document.getElementById('userSelectNombre');
    if (nombreSelect) nombreSelect.value = val;

    const option = selectEl.options[selectEl.selectedIndex];
    populateCustomerData(option);
}

function validateSaleForm() {
    if (saleItems.length === 0) {
        showToast('Debe agregar al menos un producto a la venta.', 'error');
        return false;
    }
    const btn = document.getElementById('btnRegistrarVentaSubmit');
    if (btn) activarEstadoCargaBoton(btn, 'Registrando venta...');
    return true;
}

function initProductosCache() {
    if (allRawProducts.length > 0) return;
    const prodSelect = document.getElementById('newSaleProdSelect');
    if (!prodSelect) return;

    Array.from(prodSelect.querySelectorAll('option')).forEach(opt => {
        if (!opt.value) return;
        allRawProducts.push({
            value: opt.value,
            nombre: opt.getAttribute('data-nombre') || opt.text,
            categoria: (opt.getAttribute('data-categoria') || '').toLowerCase(),
            descripcion: (opt.getAttribute('data-descripcion') || '').toLowerCase(),
            proteccion: (opt.getAttribute('data-proteccion') || '').toLowerCase(),
            material: (opt.getAttribute('data-material') || '').toLowerCase(),
            precio: opt.getAttribute('data-precio') || '0',
            iva: opt.getAttribute('data-iva') || '19',
            stock: opt.getAttribute('data-stock') || '0',
            rawText: opt.text
        });
    });
}

function isPrescriptionProduct(categoria, nombre) {
    const text = ((categoria || '') + ' ' + (nombre || '')).toLowerCase();
    return text.includes('lente') || text.includes('contacto') || text.includes('graduad') || text.includes('formulad') || text.includes('oftalmic') || text.includes('oftálmic');
}

function evaluarCompatibilidadProducto(prod, formula) {
    if (!formula) return { compatible: true, esRecomendado: false, razon: '' };

    const tipo = (formula.tipoLente || '').toLowerCase();
    const obs = (formula.observaciones || '').toLowerCase();
    const cat = prod.categoria;
    const nom = prod.nombre.toLowerCase();
    const desc = prod.descripcion;
    const prot = prod.proteccion;
    const fullText = `${cat} ${nom} ${desc} ${prot}`;

    const esFormulaContacto = tipo.includes('contacto');
    if (esFormulaContacto) {
        if (cat === 'lentes-contacto' || fullText.includes('contacto')) {
            if (tipo.includes('blando') && fullText.includes('blando')) {
                return { compatible: true, esRecomendado: true, razon: 'Lente de contacto blando formulado' };
            }
            if (tipo.includes('rígido') && fullText.includes('rígido')) {
                return { compatible: true, esRecomendado: true, razon: 'Lente de contacto rígido formulado' };
            }
            return { compatible: true, esRecomendado: true, razon: 'Lente de contacto compatible' };
        }
        if (fullText.includes('solución') || fullText.includes('solucion') || fullText.includes('líquido') || fullText.includes('gotas') || fullText.includes('limpiador')) {
            return { compatible: true, esRecomendado: false, razon: 'Solución/Cuidado de lentes de contacto' };
        }
        return { compatible: false, esRecomendado: false, razon: '' };
    }

    if (cat === 'lentes-graduados' || fullText.includes('graduad') || fullText.includes('oftalmic') || fullText.includes('oftálmic')) {
        if (tipo.includes('progresivo') && (fullText.includes('progresiv') || fullText.includes('multifocal'))) {
            return { compatible: true, esRecomendado: true, razon: 'Lente Progresivo sugerido' };
        }
        if (tipo.includes('bifocal') && fullText.includes('bifocal')) {
            return { compatible: true, esRecomendado: true, razon: 'Lente Bifocal sugerido' };
        }
        if ((tipo.includes('antirreflejo') || obs.includes('antirreflejo')) && (fullText.includes('antirreflejo') || fullText.includes('ar'))) {
            return { compatible: true, esRecomendado: true, razon: 'Tratamiento Antirreflejo formulado' };
        }
        if ((tipo.includes('azul') || tipo.includes('blue') || obs.includes('azul') || obs.includes('blue')) && (fullText.includes('azul') || fullText.includes('blue'))) {
            return { compatible: true, esRecomendado: true, razon: 'Filtro Luz Azul formulado' };
        }
        if (tipo.includes('monofocal')) {
            return { compatible: true, esRecomendado: true, razon: 'Lente Monofocal compatible' };
        }
        return { compatible: true, esRecomendado: true, razon: 'Lente Graduado compatible' };
    }

    if (cat === 'monturas' || fullText.includes('montura') || fullText.includes('marco')) {
        return { compatible: true, esRecomendado: false, razon: 'Montura apta para graduación' };
    }

    if (cat === 'accesorios' && (fullText.includes('estuche') || fullText.includes('paño') || fullText.includes('limpiador') || fullText.includes('cordón') || fullText.includes('cordon'))) {
        return { compatible: true, esRecomendado: false, razon: 'Accesorio compatible' };
    }

    if (cat === 'lentes-sol' && (fullText.includes('graduable') || fullText.includes('polarizado') || fullText.includes('uv'))) {
        return { compatible: true, esRecomendado: false, razon: 'Gafa con protección' };
    }

    return { compatible: false, esRecomendado: false, razon: '' };
}

function crearOptionElement(p, esEstrella, tag) {
    const opt = document.createElement('option');
    opt.value = p.value;
    opt.setAttribute('data-nombre', p.nombre);
    opt.setAttribute('data-categoria', p.categoria);
    opt.setAttribute('data-descripcion', p.descripcion);
    opt.setAttribute('data-proteccion', p.proteccion);
    opt.setAttribute('data-material', p.material);
    opt.setAttribute('data-precio', p.precio);
    opt.setAttribute('data-iva', p.iva);
    opt.setAttribute('data-stock', p.stock);

    const prefix = esEstrella ? '[Recomendado] ' : '';
    const suffix = tag ? ` [${tag}]` : '';
    opt.textContent = `${prefix}${p.rawText}${suffix}`;
    return opt;
}

function aplicarFiltroProductos(soloCompatibles) {
    initProductosCache();
    const select = document.getElementById('newSaleProdSelect');
    if (!select || allRawProducts.length === 0) return;

    const valorPrevio = select.value;
    select.innerHTML = '';

    const defaultOpt = document.createElement('option');
    defaultOpt.value = '';
    defaultOpt.textContent = '-- Seleccionar Producto --';
    select.appendChild(defaultOpt);

    const f = (currentClientFormula && currentClientFormula.encontrado && currentClientFormula.formula) ? currentClientFormula.formula : null;

    if (!f) {
        allRawProducts.forEach(p => {
            select.appendChild(crearOptionElement(p, false, ''));
        });
        if (valorPrevio) select.value = valorPrevio;
        return;
    }

    const evaluados = allRawProducts.map(p => {
        const evalResult = evaluarCompatibilidadProducto(p, f);
        return { product: p, ...evalResult };
    });

    const recomendados = evaluados.filter(e => e.esRecomendado);
    const compatiblesOtros = evaluados.filter(e => e.compatible && !e.esRecomendado);
    const noCompatibles = evaluados.filter(e => !e.compatible);

    if (soloCompatibles) {
        if (recomendados.length > 0) {
            const grpRec = document.createElement('optgroup');
            grpRec.label = `RECOMENDADOS SEGÚN FÓRMULA (${recomendados.length})`;
            recomendados.forEach(e => grpRec.appendChild(crearOptionElement(e.product, true, e.razon)));
            select.appendChild(grpRec);
        }

        if (compatiblesOtros.length > 0) {
            const grpComp = document.createElement('optgroup');
            grpComp.label = `MONTURAS Y COMPONENTES COMPATIBLES (${compatiblesOtros.length})`;
            compatiblesOtros.forEach(e => grpComp.appendChild(crearOptionElement(e.product, false, e.razon)));
            select.appendChild(grpComp);
        }

        if (recomendados.length === 0 && compatiblesOtros.length === 0) {
            const grpTodos = document.createElement('optgroup');
            grpTodos.label = 'TODOS LOS PRODUCTOS (Sin coincidencias estrictas)';
            allRawProducts.forEach(p => grpTodos.appendChild(crearOptionElement(p, false, '')));
            select.appendChild(grpTodos);
        }
    } else {
        if (recomendados.length > 0) {
            const grpRec = document.createElement('optgroup');
            grpRec.label = `RECOMENDADOS SEGÚN FÓRMULA (${recomendados.length})`;
            recomendados.forEach(e => grpRec.appendChild(crearOptionElement(e.product, true, e.razon)));
            select.appendChild(grpRec);
        }

        if (compatiblesOtros.length > 0) {
            const grpComp = document.createElement('optgroup');
            grpComp.label = `MONTURAS Y COMPATIBLES (${compatiblesOtros.length})`;
            compatiblesOtros.forEach(e => grpComp.appendChild(crearOptionElement(e.product, false, e.razon)));
            select.appendChild(grpComp);
        }

        if (noCompatibles.length > 0) {
            const grpOtros = document.createElement('optgroup');
            grpOtros.label = `OTROS PRODUCTOS DEL CATÁLOGO (${noCompatibles.length})`;
            noCompatibles.forEach(e => grpOtros.appendChild(crearOptionElement(e.product, false, '')));
            select.appendChild(grpOtros);
        }
    }

    if (valorPrevio) {
        select.value = valorPrevio;
    }
}

function toggleFiltroFormula(checked) {
    aplicarFiltroProductos(checked);
    evaluarAlertaLenteGraduado();
}

async function consultarFormulaCliente(userId) {
    initProductosCache();
    const panel = document.getElementById('panelFormulaOptica');
    const badge = document.getElementById('formulaBadgeEstado');
    const contenido = document.getElementById('formulaDetalleContenido');
    const sinReg = document.getElementById('formulaSinRegistro');
    const hiddenId = document.getElementById('saleFormulaOpticaId');
    const filtroContainer = document.getElementById('filtroFormulaContainer');
    const filtroTipoNombre = document.getElementById('filtroFormulaTipoNombre');
    const chkSolo = document.getElementById('chkSoloCompatibles');

    if (!userId) {
        currentClientFormula = null;
        if (panel) panel.style.display = 'none';
        if (hiddenId) hiddenId.value = '';
        if (filtroContainer) filtroContainer.style.display = 'none';
        aplicarFiltroProductos(false);
        evaluarAlertaLenteGraduado();
        return;
    }

    try {
        const res = await fetch(`/Ventas/GetFormulaVigentePorCliente?userId=${encodeURIComponent(userId)}`);
        if (!res.ok) throw new Error('Error al consultar fórmula');
        const data = await res.json();
        currentClientFormula = data;

        if (panel) panel.style.display = 'block';

        if (data.encontrado && data.formula) {
            const f = data.formula;
            if (hiddenId) hiddenId.value = data.esVigente ? f.id : '';

            if (contenido) contenido.style.display = 'block';
            if (sinReg) sinReg.style.display = 'none';

            document.getElementById('formEsfOD').innerText = f.esferaOD || '-';
            document.getElementById('formCilOD').innerText = f.cilindroOD || '-';
            document.getElementById('formEjeOD').innerText = f.ejeOD || '-';

            document.getElementById('formEsfOI').innerText = f.esferaOI || '-';
            document.getElementById('formCilOI').innerText = f.cilindroOI || '-';
            document.getElementById('formEjeOI').innerText = f.ejeOI || '-';

            document.getElementById('formTipoLente').innerText = f.tipoLente || 'Estándar';
            document.getElementById('formDistPupilar').innerText = f.distanciaPupilar || 'N/A';
            document.getElementById('formFechaExamen').innerText = f.fechaFormato || f.fecha;

            if (badge) {
                if (data.esVigente) {
                    badge.innerText = 'VIGENTE';
                    badge.style.backgroundColor = '#10B981';
                    badge.style.color = 'white';
                } else {
                    badge.innerText = 'VENCIDA';
                    badge.style.backgroundColor = '#EF4444';
                    badge.style.color = 'white';
                }
            }

            if (filtroContainer) filtroContainer.style.display = 'block';
            if (filtroTipoNombre) filtroTipoNombre.innerText = f.tipoLente || 'Fórmula Registrada';
            if (chkSolo) chkSolo.checked = true;

            aplicarFiltroProductos(true);
        } else {
            if (hiddenId) hiddenId.value = '';
            if (contenido) contenido.style.display = 'none';
            if (sinReg) sinReg.style.display = 'block';
            if (badge) {
                badge.innerText = 'SIN FÓRMULA';
                badge.style.backgroundColor = '#9CA3AF';
                badge.style.color = 'white';
            }
            if (filtroContainer) filtroContainer.style.display = 'none';
            aplicarFiltroProductos(false);
        }
    } catch (e) {
        console.warn('No se pudo cargar la fórmula óptica:', e);
        currentClientFormula = null;
        if (panel) panel.style.display = 'none';
        if (filtroContainer) filtroContainer.style.display = 'none';
        aplicarFiltroProductos(false);
    }

    evaluarAlertaLenteGraduado();
}

function evaluarAlertaLenteGraduado() {
    const alerta = document.getElementById('alertaFormulaOptica');
    if (!alerta) return;

    const tieneFormulaVigente = currentClientFormula && currentClientFormula.encontrado && currentClientFormula.esVigente;
    const prodSelect = document.getElementById('newSaleProdSelect');
    let hayProductoGraduado = false;

    if (prodSelect && prodSelect.value) {
        const opt = prodSelect.options[prodSelect.selectedIndex];
        const cat = opt.getAttribute('data-categoria') || '';
        const nom = opt.getAttribute('data-nombre') || '';
        if (isPrescriptionProduct(cat, nom)) {
            hayProductoGraduado = true;
        }
    }

    if (!hayProductoGraduado && typeof saleItems !== 'undefined') {
        for (let item of saleItems) {
            if (isPrescriptionProduct(item.categoria, item.nombre)) {
                hayProductoGraduado = true;
                break;
            }
        }
    }

    if (hayProductoGraduado && !tieneFormulaVigente) {
        alerta.style.display = 'flex';
    } else {
        alerta.style.display = 'none';
    }
}

// ── Manejadores globales delegados (Edición, Eliminación, Cierre Escape y Bloqueo Doble Clic) ──
document.addEventListener('DOMContentLoaded', function () {
    initProductosCache();
    const prodSelect = document.getElementById('newSaleProdSelect');
    const qtyInput = document.getElementById('newSaleQtyInput');
    const stockMsg = document.getElementById('stock-availability-msg');

    if (prodSelect && qtyInput) {
        prodSelect.addEventListener('change', function () {
            evaluarAlertaLenteGraduado();
            if (!this.value) {
                qtyInput.removeAttribute('max');
                if (stockMsg) stockMsg.innerText = '';
                return;
            }
            const opt = this.options[this.selectedIndex];
            const stock = parseInt(opt.getAttribute('data-stock') || '0', 10);
            qtyInput.setAttribute('max', stock);
            if (stockMsg) {
                stockMsg.innerText = 'Stock disponible: ' + stock;
            }

            let val = parseInt(qtyInput.value || '1', 10);
            if (val > stock) {
                qtyInput.value = stock;
            }
        });

        qtyInput.addEventListener('input', function () {
            if (!prodSelect.value) return;
            const opt = prodSelect.options[prodSelect.selectedIndex];
            const stock = parseInt(opt.getAttribute('data-stock') || '0', 10);
            let val = parseInt(this.value || '1', 10);
            if (val > stock) {
                this.value = stock;
            }
            if (val < 1) {
                this.value = 1;
            }
        });
    }
});

document.addEventListener('click', function (e) {
    if (!e.target.closest('.inv-filter-btn') && !e.target.closest('.inv-dropdown-menu')) {
        document.querySelectorAll('.inv-dropdown-menu').forEach(m => m.style.display = 'none');
    }

    const btn = e.target.closest('[data-record]');
    if (!btn) return;
    let rec;
    try { rec = JSON.parse(btn.dataset.record); } catch { return; }

    if (btn.classList.contains('btn-edit-product')) {
        openEditProductModal(
            rec.id, rec.nombre, rec.descripcion, rec.precio,
            rec.precioDescuento, rec.categoria, rec.marca,
            rec.stock, rec.activo, rec.imagenUrl,
            rec.sku, rec.costoCompra, rec.stockMinimo,
            rec.ubicacion, rec.supplierId, rec.rowVersion
        );
    } else if (btn.classList.contains('btn-edit-sales-order')) {
        openEditSalesOrderModal(
            rec.id, rec.numeroPedido, rec.clienteNombre,
            rec.productoNombre, rec.cantidad, rec.precioUnitario,
            rec.estado, rec.notas
        );
    }
});

document.addEventListener('submit', function (e) {
    const form = e.target;
    if (form.classList.contains('form-delete-product')) {
        const nombre = form.dataset.nombre || '';
        if (!confirm('¿Deseas eliminar el producto ' + nombre + '?')) {
            e.preventDefault();
            return;
        }
    } else if (form.classList.contains('form-delete-sales-order')) {
        if (!confirm('¿Seguro que deseas eliminar este pedido de venta? Se devolverá el stock si el producto coincide exactamente en el catálogo.')) {
            e.preventDefault();
            return;
        }
    }

    // Bloqueo automático y spinner de carga en botones submit de formularios de Ventas (Sección 4.4)
    if (!e.defaultPrevented) {
        const submitBtn = form.querySelector('button[type="submit"]');
        if (submitBtn && submitBtn.dataset.loadingActive !== 'true') {
            const loadingText = submitBtn.getAttribute('data-loading-text') || 'Procesando...';
            activarEstadoCargaBoton(submitBtn, loadingText);
        }
    }
});

document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' || e.key === 'Esc') {
        const modals = document.querySelectorAll('[id^="modal-"]');
        modals.forEach(m => {
            if (m.style.display === 'flex' || m.style.display === 'block') {
                m.style.display = 'none';
            }
        });
    }
});

