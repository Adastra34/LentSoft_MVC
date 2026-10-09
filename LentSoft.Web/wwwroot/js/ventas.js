/**
 * LentSoft - Funciones de Control y Lógica de Negocio de Ventas (ISO/IEC 25010)
 */

function toggleCustomDates(val) {
    const divDesde = document.getElementById('divFechaDesde');
    const divHasta = document.getElementById('divFechaHasta');
    if (!divDesde || !divHasta) return;

    if (val === 'personalizado') {
        divDesde.style.display = 'block';
        divHasta.style.display = 'block';
    } else {
        divDesde.style.display = 'none';
        divHasta.style.display = 'none';
    }
}

function openAbonoModal(ventaId, total, saldo) {
    const inputId = document.getElementById('abonoVentaId');
    const displaySaldo = document.getElementById('abonoSaldoPendienteDisplay');
    const inputMonto = document.getElementById('abonoMontoInput');
    const modal = document.getElementById('modal-abono');

    if (inputId) inputId.value = ventaId;
    if (displaySaldo) displaySaldo.innerText = '$ ' + parseFloat(saldo).toLocaleString('es-CO');
    if (inputMonto) {
        inputMonto.value = saldo;
        inputMonto.max = saldo;
    }
    if (modal) modal.style.display = 'flex';
}

function openTarjetaModal(ventaId, saldo) {
    const inputId = document.getElementById('tarjetaVentaId');
    const inputMonto = document.getElementById('tarjetaMontoInput');
    const modal = document.getElementById('modal-tarjeta');
    const btnSubmit = document.getElementById('btnSubmitTarjeta');

    if (inputId) inputId.value = ventaId;
    if (inputMonto) inputMonto.value = saldo;
    if (btnSubmit) {
        btnSubmit.disabled = false;
        btnSubmit.innerHTML = '💳 Procesar Pago con Tarjeta';
    }

    // Generar clave de idempotencia única para este intento de cobro
    const idempotencyInput = document.getElementById('tarjetaClaveIdempotencia');
    if (idempotencyInput) {
        idempotencyInput.value = 'txn_' + ventaId + '_' + Date.now() + '_' + Math.random().toString(36).substring(2, 9);
    }

    if (modal) modal.style.display = 'flex';
}

function onInvoiceOrderChange(selectElem) {
    if (!selectElem) return;
    const selectedOpt = selectElem.options[selectElem.selectedIndex];
    if (!selectedOpt) return;

    const total = parseFloat(selectedOpt.getAttribute('data-total') || '0');
    const metodo = selectedOpt.getAttribute('data-metodo') || 'Efectivo';
    const saldo = parseFloat(selectedOpt.getAttribute('data-saldopendiente') || '0');

    const metodoInput = document.getElementById('invoiceMetodoPagoInput');
    if (metodoInput) metodoInput.value = metodo;

    const estadoSelect = document.getElementById('invoiceEstadoSelect');
    if (estadoSelect) {
        if (saldo <= 0) {
            estadoSelect.value = 'pagada';
        } else if (saldo < total) {
            estadoSelect.value = 'parcial';
        } else {
            estadoSelect.value = 'pendiente';
        }
    }

    if (total > 0) {
        const subtotal = (total / 1.19).toFixed(2);
        const impuestos = (total - subtotal).toFixed(2);
        const subtotalElem = document.getElementById('newInvoiceSubtotal');
        const impElem = document.getElementById('newInvoiceImpuestos');
        const totElem = document.getElementById('newInvoiceTotal');

        if (subtotalElem) subtotalElem.value = subtotal;
        if (impElem) impElem.value = impuestos;
        if (totElem) totElem.value = total.toFixed(2);
    }
}

function confirmDeleteInvoice(id, numero) {
    if (confirm('¿Está seguro de eliminar la factura ' + numero + '?')) {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = '/Invoice/Delete/' + id;

        const tokenElem = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenElem) {
            const hiddenToken = document.createElement('input');
            hiddenToken.type = 'hidden';
            hiddenToken.name = '__RequestVerificationToken';
            hiddenToken.value = tokenElem.value;
            form.appendChild(hiddenToken);
        }

        document.body.appendChild(form);
        form.submit();
    }
}

function filterClientSideFacturas(filterVal) {
    const filter = (filterVal || '').toLowerCase();
    const table = document.getElementById('tableFacturas');
    if (!table) return;
    const tbody = table.getElementsByTagName('tbody')[0];
    if (!tbody) return;
    const trs = tbody.getElementsByTagName('tr');

    for (let i = 0; i < trs.length; i++) {
        let show = false;
        const tds = trs[i].getElementsByTagName('td');
        if (tds.length <= 1) continue;
        for (let j = 0; j < tds.length - 1; j++) {
            if (tds[j] && tds[j].textContent.toLowerCase().includes(filter)) {
                show = true;
                break;
            }
        }
        trs[i].style.display = show ? '' : 'none';
    }
}

function openDetallePedidoModal(orderId) {
    if (typeof ventasPedidosMap === 'undefined') return;
    const pedido = ventasPedidosMap[orderId];
    if (!pedido) return;

    const orderNumElem = document.getElementById('detalleModalOrderNumero');
    const clienteElem = document.getElementById('detalleModalCliente');
    const fechaElem = document.getElementById('detalleModalFecha');
    const totalElem = document.getElementById('detalleModalTotal');
    const orderIdInput = document.getElementById('detallePedidoOrderId');

    if (orderNumElem) orderNumElem.textContent = pedido.numero;
    if (clienteElem) clienteElem.textContent = pedido.cliente;
    if (fechaElem) fechaElem.textContent = pedido.fecha;
    if (totalElem) totalElem.textContent = pedido.total;
    if (orderIdInput) orderIdInput.value = pedido.id;

    const tbody = document.getElementById('detalleModalProductosBody');
    if (tbody) {
        tbody.innerHTML = '';
        if (pedido.items && pedido.items.length > 0) {
            pedido.items.forEach(item => {
                const tr = document.createElement('tr');
                tr.style.borderBottom = '1px solid var(--purple-100, #EDE9FE)';
                tr.innerHTML = `
                    <td style="padding: 0.6rem 0.75rem; font-weight: 500;">
                        ${item.nombre}
                        ${item.categoria ? `<span style="font-size: 0.72rem; color: var(--purple-600, #7C3AED); display: block;">(${item.categoria})</span>` : ''}
                    </td>
                    <td style="padding: 0.6rem 0.75rem; text-align: center;">${item.cantidad}</td>
                    <td style="padding: 0.6rem 0.75rem; text-align: right; font-weight: 600; color: var(--purple-900, #4C1D95);">${item.subtotal}</td>
                `;
                tbody.appendChild(tr);
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="3" style="padding: 1rem; text-align: center; color: var(--gray-500, #6B7280);">No hay productos en este pedido.</td></tr>';
        }
    }

    const seccionFormula = document.getElementById('seccionFormulaOptica');
    const formulaSelect = document.getElementById('detallePedidoFormulaSelect');
    const avisoSinFormulas = document.getElementById('avisoSinFormulas');
    const contenedorSelect = document.getElementById('contenedorSelectFormula');

    if (seccionFormula && formulaSelect) {
        if (pedido.requiereFormula) {
            seccionFormula.style.display = 'block';
            formulaSelect.innerHTML = '<option value="">Cargando fórmulas...</option>';
            if (avisoSinFormulas) avisoSinFormulas.style.display = 'none';
            if (contenedorSelect) contenedorSelect.style.display = 'block';

            fetch('/Ventas/GetFormulasPorPaciente?userId=' + pedido.userId)
                .then(res => res.json())
                .then(formulas => {
                    formulaSelect.innerHTML = '<option value="">-- Sin fórmula vinculada --</option>';
                    if (!formulas || formulas.length === 0) {
                        if (avisoSinFormulas) avisoSinFormulas.style.display = 'block';
                    } else {
                        if (avisoSinFormulas) avisoSinFormulas.style.display = 'none';
                        formulas.forEach(f => {
                            const opt = document.createElement('option');
                            opt.value = f.id;
                            opt.textContent = `${f.fecha} — ${f.tipoLente} (${f.estado})`;
                            if (pedido.formulaOpticaId && pedido.formulaOpticaId === f.id) {
                                opt.selected = true;
                            }
                            formulaSelect.appendChild(opt);
                        });
                    }
                })
                .catch(err => {
                    console.error('Error al cargar fórmulas del paciente:', err);
                    formulaSelect.innerHTML = '<option value="">Error al cargar fórmulas</option>';
                });
        } else {
            seccionFormula.style.display = 'none';
        }
    }

    const modal = document.getElementById('modal-detalle-pedido');
    if (modal) modal.style.display = 'flex';
}

function closeDetallePedidoModal() {
    const modal = document.getElementById('modal-detalle-pedido');
    if (modal) modal.style.display = 'none';
}

function openHistorialAbonosModal(orderId, cliente, total) {
    const modal = document.getElementById('modal-historial-abonos');
    if (modal) {
        modal.style.display = 'flex';
    } else {
        openDetallePedidoModal(orderId);
    }
}

// ── Control de Doble Envío en Pasarela de Pagos (Requisito Seguridad y Calidad) ──
document.addEventListener('DOMContentLoaded', function () {
    const formTarjeta = document.getElementById('formCobrarTarjeta');
    if (formTarjeta) {
        formTarjeta.addEventListener('submit', function (e) {
            const btnSubmit = document.getElementById('btnSubmitTarjeta');
            if (btnSubmit) {
                btnSubmit.disabled = true;
                btnSubmit.innerHTML = '⏳ Procesando pago seguro...';
            }
        });
    }

    // Inicializar listeners de cierre modal al hacer click fuera
    document.querySelectorAll('.sales-modal-overlay').forEach(modal => {
        modal.addEventListener('click', function (e) {
            if (e.target === modal) {
                modal.style.display = 'none';
            }
        });
    });
});
