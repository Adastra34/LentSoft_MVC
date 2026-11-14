    const appointmentsData = window.appointmentsData || [];
    const monthNames = ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"];

    let currentDate = new Date();
    let selectedDateStr = new Date().toISOString().split('T')[0];

    document.addEventListener('DOMContentLoaded', () => {
        renderCalendar();
        renderAgenda(selectedDateStr);
    });

    function renderCalendar() {
        const titleEl = document.getElementById('calendarTitle');
        const gridEl = document.getElementById('calendarGrid');
        if (!gridEl) return;

        gridEl.innerHTML = '';
        const year = currentDate.getFullYear();
        const month = currentDate.getMonth();

        titleEl.textContent = `${monthNames[month]} ${year}`;

        const firstDay = new Date(year, month, 1).getDay();
        const daysInMonth = new Date(year, month + 1, 0).getDate();

        // Filler empty cells before 1st day of month
        for (let i = 0; i < firstDay; i++) {
            const emptyCell = document.createElement('div');
            emptyCell.style.height = '36px';
            gridEl.appendChild(emptyCell);
        }

        // Days of month
        for (let day = 1; day <= daysInMonth; day++) {
            const dayCell = document.createElement('div');
            const formattedMonth = String(month + 1).padStart(2, '0');
            const formattedDay = String(day).padStart(2, '0');
            const dateStr = `${year}-${formattedMonth}-${formattedDay}`;

            const hasAppt = appointmentsData.some(a => a.fechaStr === dateStr);
            const isSelected = selectedDateStr === dateStr;

            dayCell.style.height = '36px';
            dayCell.style.display = 'flex';
            dayCell.style.flexDirection = 'column';
            dayCell.style.alignItems = 'center';
            dayCell.style.justifyContent = 'center';
            dayCell.style.borderRadius = '0.375rem';
            dayCell.style.cursor = 'pointer';
            dayCell.style.fontSize = '0.85rem';
            dayCell.style.position = 'relative';
            dayCell.style.fontWeight = isSelected ? '700' : '400';

            if (isSelected) {
                dayCell.style.backgroundColor = 'var(--purple-700)';
                dayCell.style.color = 'white';
            } else if (hasAppt) {
                dayCell.style.backgroundColor = 'var(--purple-100)';
                dayCell.style.color = 'var(--purple-900)';
            } else {
                dayCell.style.backgroundColor = 'transparent';
                dayCell.style.color = 'var(--gray-800)';
            }

            dayCell.innerHTML = `<span>${day}</span>`;

            if (hasAppt) {
                const dot = document.createElement('span');
                dot.style.width = '5px';
                dot.style.height = '5px';
                dot.style.borderRadius = '50%';
                dot.style.backgroundColor = isSelected ? '#FDE047' : 'var(--purple-600)';
                dot.style.marginTop = '2px';
                dayCell.appendChild(dot);
            }

            dayCell.onclick = () => {
                selectedDateStr = dateStr;
                renderCalendar();
                renderAgenda(dateStr);
            };

            gridEl.appendChild(dayCell);
        }
    }

    function prevMonth() {
        currentDate.setMonth(currentDate.getMonth() - 1);
        renderCalendar();
    }

    function nextMonth() {
        currentDate.setMonth(currentDate.getMonth() + 1);
        renderCalendar();
    }

    function renderAgenda(dateStr) {
        const agendaList = document.getElementById('agendaList');
        const badge = document.getElementById('agendaDateBadge');
        if (!agendaList) return;

        agendaList.innerHTML = '';
        const todayStr = new Date().toISOString().split('T')[0];

        if (badge) {
            if (dateStr === todayStr) {
                badge.textContent = 'Hoy';
            } else {
                const parts = dateStr.split('-');
                badge.textContent = `${parts[2]}/${parts[1]}/${parts[0]}`;
            }
        }

        const dayAppts = appointmentsData.filter(a => a.fechaStr === dateStr);

        if (dayAppts.length === 0) {
            agendaList.innerHTML = `
                <div style="flex: 1; display: flex; align-items: center; justify-content: center; padding: 2rem 0; color: var(--gray-500); font-size: 0.9rem; text-align: center;">
                    Sin citas para hoy
                </div>
            `;
            return;
        }

        dayAppts.forEach(app => {
            const item = document.createElement('div');
            item.style.padding = '0.75rem 1rem';
            item.style.border = '1px solid var(--purple-100)';
            item.style.borderRadius = '0.5rem';
            item.style.display = 'flex';
            item.style.justifyContent = 'space-between';
            item.style.alignItems = 'center';
            item.style.backgroundColor = 'var(--purple-50)';

            const statusClass = app.estado.toLowerCase();
            const bgBadge = statusClass === 'confirmada' ? 'var(--purple-200)' : statusClass === 'pendiente' ? '#FEF3C7' : '#D1FAE5';
            const fgBadge = statusClass === 'confirmada' ? 'var(--purple-800)' : statusClass === 'pendiente' ? '#92400E' : '#065F46';

            item.innerHTML = `
                <div>
                    <h4 style="margin: 0; font-size: 0.9rem; color: var(--purple-900); font-weight: 600;">${app.paciente}</h4>
                    <p style="margin: 0.2rem 0 0 0; font-size: 0.78rem; color: var(--gray-500);">${app.servicio} — <strong style="color: var(--purple-700);">${app.horaStr} hs</strong></p>
                </div>
                <span style="background-color: ${bgBadge}; color: ${fgBadge}; padding: 0.2rem 0.5rem; border-radius: 9999px; font-size: 0.72rem; font-weight: 600; text-transform: capitalize;">
                    ${app.estado}
                </span>
            `;
            agendaList.appendChild(item);
        });
    }

    function filterTable(inputId, tableId) {
        const input = document.getElementById(inputId);
        if (!input) return;
        const filter = input.value.toLowerCase();
        const table = document.getElementById(tableId);
        if (!table) return;
        const rows = table.querySelectorAll('tbody tr');
        rows.forEach(tr => {
            const text = tr.textContent.toLowerCase();
            tr.style.display = text.includes(filter) ? '' : 'none';
        });
    }

    function toggleFilterMenu(submodule) {
        const menu = document.getElementById('filter-menu-' + submodule);
        if (!menu) return;
        const isVisible = menu.style.display === 'block';
        document.querySelectorAll('[id^="filter-menu-"]').forEach(m => m.style.display = 'none');
        menu.style.display = isVisible ? 'none' : 'block';
    }

    function filterTableByValue(inputId, tableId, value) {
        const input = document.getElementById(inputId);
        if (input) {
            input.value = value;
            filterTable(inputId, tableId);
        }
        document.querySelectorAll('[id^="filter-menu-"]').forEach(m => m.style.display = 'none');
    }

    document.addEventListener('click', function(e) {
        if (!e.target.closest('[id^="filter-menu-"]') && !e.target.closest('.btn-filter-dropdown')) {
            document.querySelectorAll('[id^="filter-menu-"]').forEach(m => m.style.display = 'none');
        }
    });

    function abrirModalEditarPaciente(btn) {
        document.getElementById('edit-paciente-id').value = btn.getAttribute('data-id');
        document.getElementById('edit-paciente-nombre').value = btn.getAttribute('data-nombre');
        document.getElementById('edit-paciente-apellido').value = btn.getAttribute('data-apellido');
        document.getElementById('edit-paciente-email').value = btn.getAttribute('data-email');
        document.getElementById('edit-paciente-telefono').value = btn.getAttribute('data-telefono');
        document.getElementById('modal-editar-paciente').style.display = 'flex';
    }

    function abrirModalVerPaciente(btn) {
        const id = btn.getAttribute('data-id');
        const nombre = btn.getAttribute('data-nombre') + ' ' + btn.getAttribute('data-apellido');
        const tipoDoc = btn.getAttribute('data-tipodocumento');
        const numDoc = btn.getAttribute('data-numerodocumento');
        const email = btn.getAttribute('data-email');
        const telefono = btn.getAttribute('data-telefono') || 'N/A';
        const fechaNac = btn.getAttribute('data-fechanacimiento') || 'N/A';
        const genero = btn.getAttribute('data-genero') || 'N/A';
        const direccion = btn.getAttribute('data-direccion') || 'N/A';
        const eps = btn.getAttribute('data-eps') || 'N/A';
        const estado = btn.getAttribute('data-estadopaciente') || 'Activo';
        const observaciones = btn.getAttribute('data-observaciones') || 'Ninguna.';

        document.getElementById('ver-paciente-tipodoc').textContent = tipoDoc;
        document.getElementById('ver-paciente-numdoc').textContent = numDoc;
        document.getElementById('ver-paciente-nombre').textContent = nombre;
        document.getElementById('ver-paciente-fechanac').textContent = fechaNac;
        document.getElementById('ver-paciente-genero').textContent = genero;
        document.getElementById('ver-paciente-email').textContent = email;
        document.getElementById('ver-paciente-telefono').textContent = telefono;
        document.getElementById('ver-paciente-direccion').textContent = direccion;
        document.getElementById('ver-paciente-eps').textContent = eps;
        
        const estSpan = document.getElementById('ver-paciente-estado');
        estSpan.textContent = estado;
        if (estado.toLowerCase() === 'activo') {
            estSpan.style.color = '#065F46';
        } else {
            estSpan.style.color = '#991B1B';
        }
        
        document.getElementById('ver-paciente-observaciones').textContent = observaciones;
        
        const btnEdit = document.getElementById('btn-ver-editar-paciente');
        if (btnEdit) {
            btnEdit.onclick = function() {
                document.getElementById('modal-ver-paciente').style.display = 'none';
                abrirModalEditarPaciente(btn);
            };
        }

        document.getElementById('modal-ver-paciente').style.display = 'flex';
    }

    function filterCitasByEstado(estado, btn) {
        const tabs = document.querySelectorAll('.filter-tab');
        tabs.forEach(t => {
            t.classList.remove('active');
            t.style.backgroundColor = 'var(--purple-50)';
            t.style.color = 'var(--purple-700)';
            t.style.border = '1px solid var(--purple-200)';
        });
        btn.classList.add('active');
        btn.style.backgroundColor = 'var(--purple-600)';
        btn.style.color = 'white';
        btn.style.border = 'none';

        const rows = document.querySelectorAll('.cita-row');
        rows.forEach(row => {
            const rowEstado = row.getAttribute('data-estado');
            if (estado === 'Todos' || rowEstado === estado.toLowerCase()) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    }

    function abrirModalVerCita(btn) {
        document.getElementById('ver-cita-id').textContent = ' #CIT-' + btn.getAttribute('data-id');
        document.getElementById('ver-cita-paciente').textContent = btn.getAttribute('data-paciente');
        document.getElementById('ver-cita-servicio').textContent = btn.getAttribute('data-servicio');
        document.getElementById('ver-cita-fechahora').textContent = btn.getAttribute('data-fechahorastr');
        document.getElementById('ver-cita-estado').textContent = btn.getAttribute('data-estado');
        const notesEl = document.getElementById('ver-cita-notas');
        if (notesEl) {
            notesEl.textContent = btn.getAttribute('data-notas') || 'Ninguna nota cargada.';
        }
        
        const statusVal = btn.getAttribute('data-estado').toLowerCase();
        const estSpan = document.getElementById('ver-cita-estado');
        if (estSpan) {
            if (statusVal === 'confirmada' || statusVal === 'completada') {
                estSpan.style.color = '#065F46';
            } else if (statusVal === 'pendiente') {
                estSpan.style.color = '#92400E';
            } else {
                estSpan.style.color = '#991B1B';
            }
        }

        const btnEdit = document.getElementById('btn-ver-editar-cita');
        if (btnEdit) {
            btnEdit.onclick = function() {
                document.getElementById('modal-ver-cita').style.display = 'none';
                abrirModalEditarCita(btn);
            };
        }

        document.getElementById('modal-ver-cita').style.display = 'flex';
    }

    function abrirModalEditarCita(btn) {
        document.getElementById('edit-cita-id').value = btn.getAttribute('data-id');
        document.getElementById('edit-cita-userid').value = btn.getAttribute('data-userid');
        const elSrvOpto = document.getElementById('edit-cita-servicio');
        const srvValOpto = btn.getAttribute('data-servicio') || '';
        if (elSrvOpto) {
            if (srvValOpto && ![...elSrvOpto.options].some(o => o.value === srvValOpto)) {
                const opt = document.createElement('option');
                opt.value = srvValOpto;
                opt.textContent = srvValOpto;
                elSrvOpto.appendChild(opt);
            }
            elSrvOpto.value = srvValOpto;
        }
        document.getElementById('edit-cita-fechahora').value = btn.getAttribute('data-fechahora');
        document.getElementById('edit-cita-estado').value = btn.getAttribute('data-estado').toLowerCase();
        document.getElementById('edit-cita-notas').value = btn.getAttribute('data-notas');
        document.getElementById('modal-editar-cita').style.display = 'flex';
    }

    function abrirModalEditarHistorial(btn) {
        document.getElementById('edit-historial-id').value = btn.getAttribute('data-id');
        document.getElementById('edit-historial-userid').value = btn.getAttribute('data-userid');
        document.getElementById('edit-historial-fecha').value = btn.getAttribute('data-fecha');
        document.getElementById('edit-historial-diagnostico').value = btn.getAttribute('data-diagnostico');
        document.getElementById('edit-historial-tratamiento').value = btn.getAttribute('data-tratamiento');
        var ant = document.getElementById('edit-historial-antecedentes');
        if (ant) ant.value = btn.getAttribute('data-antecedentes') || '';
        var ex = document.getElementById('edit-historial-examenes');
        if (ex) ex.value = btn.getAttribute('data-examenes') || '';
        var obs = document.getElementById('edit-historial-observaciones');
        if (obs) obs.value = btn.getAttribute('data-observaciones') || '';
        var est = document.getElementById('edit-historial-estado');
        if (est) est.value = btn.getAttribute('data-estado') || 'Activo';

        // Campos de refracción
        var eod = document.getElementById('edit-historial-esferaod'); if (eod) eod.value = btn.getAttribute('data-esferaod') || '';
        var cod = document.getElementById('edit-historial-cilindrood'); if (cod) cod.value = btn.getAttribute('data-cilindrood') || '';
        var ejod = document.getElementById('edit-historial-ejeod'); if (ejod) ejod.value = btn.getAttribute('data-ejeod') || '';
        var eoi = document.getElementById('edit-historial-esferaoi'); if (eoi) eoi.value = btn.getAttribute('data-esferaoi') || '';
        var coi = document.getElementById('edit-historial-cilindrooi'); if (coi) coi.value = btn.getAttribute('data-cilindrooi') || '';
        var ejoi = document.getElementById('edit-historial-ejeoi'); if (ejoi) ejoi.value = btn.getAttribute('data-ejeoi') || '';

        document.getElementById('modal-editar-historial').style.display = 'flex';
    }

    function abrirModalVerExamen(btn) {
        document.getElementById('ver-examen-paciente').textContent = btn.getAttribute('data-paciente');
        document.getElementById('ver-examen-fecha').textContent = btn.getAttribute('data-fecha');
        document.getElementById('ver-examen-tipoexamen').textContent = btn.getAttribute('data-tipoexamen');
        document.getElementById('ver-examen-ojoderecho').textContent = btn.getAttribute('data-ojoderecho');
        document.getElementById('ver-examen-tonometriaod').textContent = btn.getAttribute('data-tonometriaod');
        document.getElementById('ver-examen-ojoizquierdo').textContent = btn.getAttribute('data-ojoizquierdo');
        document.getElementById('ver-examen-tonometriaoi').textContent = btn.getAttribute('data-tonometriaoi');
        document.getElementById('ver-examen-esferaod').textContent = btn.getAttribute('data-esferaod');
        document.getElementById('ver-examen-cilindrood').textContent = btn.getAttribute('data-cilindrood');
        document.getElementById('ver-examen-ejeod').textContent = btn.getAttribute('data-ejeod');
        document.getElementById('ver-examen-adicionod').textContent = btn.getAttribute('data-adicionod');
        document.getElementById('ver-examen-esferaoi').textContent = btn.getAttribute('data-esferaoi');
        document.getElementById('ver-examen-cilindrooi').textContent = btn.getAttribute('data-cilindrooi');
        document.getElementById('ver-examen-ejeoi').textContent = btn.getAttribute('data-ejeoi');
        document.getElementById('ver-examen-adicionoi').textContent = btn.getAttribute('data-adicionoi');
        document.getElementById('ver-examen-segmentoanterior').textContent = btn.getAttribute('data-segmentoanterior');
        document.getElementById('ver-examen-segmentoposterior').textContent = btn.getAttribute('data-segmentoposterior');
        document.getElementById('ver-examen-diagnostico').textContent = btn.getAttribute('data-diagnostico');
        document.getElementById('ver-examen-tratamiento').textContent = btn.getAttribute('data-tratamiento');
        document.getElementById('ver-examen-observaciones').textContent = btn.getAttribute('data-observaciones');
        document.getElementById('modal-ver-examen').style.display = 'flex';
    }

    function abrirModalEditarExamen(btn) {
        document.getElementById('edit-examen-id').value = btn.getAttribute('data-id');
        const userId = btn.getAttribute('data-userid');
        const pacienteNombre = btn.getAttribute('data-paciente') || '';
        seleccionarPacienteModal('edit-examen', userId, pacienteNombre, '');
        document.getElementById('edit-examen-fecha').value = btn.getAttribute('data-fecha');
        document.getElementById('edit-examen-tipoexamen').value = btn.getAttribute('data-tipoexamen');
        document.getElementById('edit-examen-ojoderecho').value = btn.getAttribute('data-ojoderecho');
        document.getElementById('edit-examen-ojoizquierdo').value = btn.getAttribute('data-ojoizquierdo');
        document.getElementById('edit-examen-tonometriaod').value = btn.getAttribute('data-tonometriaod');
        document.getElementById('edit-examen-tonometriaoi').value = btn.getAttribute('data-tonometriaoi');
        document.getElementById('edit-examen-esferaod').value = btn.getAttribute('data-esferaod');
        document.getElementById('edit-examen-cilindrood').value = btn.getAttribute('data-cilindrood');
        document.getElementById('edit-examen-ejeod').value = btn.getAttribute('data-ejeod');
        document.getElementById('edit-examen-adicionod').value = btn.getAttribute('data-adicionod');
        document.getElementById('edit-examen-esferaoi').value = btn.getAttribute('data-esferaoi');
        document.getElementById('edit-examen-cilindrooi').value = btn.getAttribute('data-cilindrooi');
        document.getElementById('edit-examen-ejeoi').value = btn.getAttribute('data-ejeoi');
        document.getElementById('edit-examen-adicionoi').value = btn.getAttribute('data-adicionoi');
        document.getElementById('edit-examen-segmentoanterior').value = btn.getAttribute('data-segmentoanterior');
        document.getElementById('edit-examen-segmentoposterior').value = btn.getAttribute('data-segmentoposterior');
        document.getElementById('edit-examen-diagnostico').value = btn.getAttribute('data-diagnostico');
        document.getElementById('edit-examen-tratamiento').value = btn.getAttribute('data-tratamiento');
        document.getElementById('edit-examen-observaciones').value = btn.getAttribute('data-observaciones');
        document.getElementById('modal-editar-examen').style.display = 'flex';
    }

    // ── Autocomplete / Búsqueda de Pacientes en Examen Clínico ──
    function mostrarListaPacientes(prefix) {
        const dd = document.getElementById('dropdown-' + prefix + '-pacientes');
        if (dd) dd.style.display = 'block';
        filtrarPacientesModal(prefix);
    }

    function filtrarPacientesModal(prefix) {
        const searchInput = document.getElementById('search-' + prefix + '-paciente');
        const query = (searchInput ? searchInput.value : '').toLowerCase().trim();
        const dd = document.getElementById('dropdown-' + prefix + '-pacientes');
        if (!dd) return;
        dd.style.display = 'block';

        const items = dd.querySelectorAll('.patient-opt-item');
        let count = 0;
        items.forEach(function (item) {
            const nombre = (item.getAttribute('data-nombre') || '').toLowerCase();
            const doc = (item.getAttribute('data-doc') || '').toLowerCase();
            if (!query || nombre.indexOf(query) !== -1 || doc.indexOf(query) !== -1) {
                item.style.display = 'flex';
                count++;
            } else {
                item.style.display = 'none';
            }
        });

        const noRes = document.getElementById('no-patients-' + prefix);
        if (noRes) {
            noRes.style.display = count === 0 ? 'block' : 'none';
        }
    }

    function seleccionarPacienteModal(prefix, id, nombre, doc) {
        const hiddenInput = document.getElementById('hidden-' + prefix + '-userid') 
                         || document.getElementById(prefix + '-userid');
        const searchInput = document.getElementById('search-' + prefix + '-paciente');
        const clearBtn = document.getElementById('clear-' + prefix + '-paciente');
        const dd = document.getElementById('dropdown-' + prefix + '-pacientes');
        const badge = document.getElementById('badge-' + prefix + '-paciente');
        const badgeName = document.getElementById('badge-name-' + prefix);

        if (hiddenInput) hiddenInput.value = id;
        if (searchInput) searchInput.value = nombre;
        if (clearBtn) clearBtn.style.display = 'block';
        if (dd) dd.style.display = 'none';
        if (badge && badgeName) {
            badgeName.textContent = doc ? nombre + ' (Doc: ' + doc + ')' : nombre;
            badge.style.display = 'block';
        }

        // TAREA 5: Si estamos en "Nueva Fórmula", autollenar datos de refracción desde el último examen
        if (prefix === 'nuevo-formula' && id) {
            cargarRefraccionUltimoExamen(id);
        }
    }

    function limpiarPacienteSeleccionado(prefix) {
        const hiddenInput = document.getElementById('hidden-' + prefix + '-userid') 
                         || document.getElementById(prefix + '-userid');
        const searchInput = document.getElementById('search-' + prefix + '-paciente');
        const clearBtn = document.getElementById('clear-' + prefix + '-paciente');
        const badge = document.getElementById('badge-' + prefix + '-paciente');
        const dd = document.getElementById('dropdown-' + prefix + '-pacientes');

        if (hiddenInput) hiddenInput.value = '';
        if (searchInput) {
            searchInput.value = '';
            searchInput.focus();
        }
        if (clearBtn) clearBtn.style.display = 'none';
        if (badge) badge.style.display = 'none';
        if (dd) {
            dd.style.display = 'block';
            filtrarPacientesModal(prefix);
        }

        if (prefix === 'nuevo-formula') {
            const aviso = document.getElementById('aviso-auto-refraccion');
            if (aviso) aviso.style.display = 'none';
        }
    }

    function cargarRefraccionUltimoExamen(userId) {
        fetch('/Optometra/GetUltimoExamenPorPaciente?userId=' + encodeURIComponent(userId))
            .then(function(res) { return res.json(); })
            .then(function(data) {
                const aviso = document.getElementById('aviso-auto-refraccion');
                if (data && data.encontrado && data.examen) {
                    const ex = data.examen;
                    const esferaOD = document.getElementById('new-formula-esferaod');
                    const cilOD = document.getElementById('new-formula-cilindrood');
                    const ejeOD = document.getElementById('new-formula-ejeod');
                    const esferaOI = document.getElementById('new-formula-esferaoi');
                    const cilOI = document.getElementById('new-formula-cilindrooi');
                    const ejeOI = document.getElementById('new-formula-ejeoi');

                    if (esferaOD && ex.esferaOD) esferaOD.value = ex.esferaOD;
                    if (cilOD && ex.cilindroOD) cilOD.value = ex.cilindroOD;
                    if (ejeOD && ex.ejeOD) ejeOD.value = ex.ejeOD;
                    if (esferaOI && ex.esferaOI) esferaOI.value = ex.esferaOI;
                    if (cilOI && ex.cilindroOI) cilOI.value = ex.cilindroOI;
                    if (ejeOI && ex.ejeOI) ejeOI.value = ex.ejeOI;

                    if (aviso) {
                        const avisoText = document.getElementById('aviso-auto-refraccion-text');
                        if (avisoText) {
                            avisoText.textContent = 'Datos de refracción precargados desde el último examen visual (' + ex.fecha + '). Son totalmente editables.';
                        } else {
                            aviso.textContent = 'Datos de refracción precargados desde el último examen visual (' + ex.fecha + '). Son totalmente editables.';
                        }
                        aviso.style.display = 'flex';
                    }
                } else {
                    if (aviso) aviso.style.display = 'none';
                }
            })
            .catch(function(err) {
                console.error('Error al consultar el último examen:', err);
            });
    }

    function abrirModalNuevaFormula() {
        limpiarPacienteSeleccionado('nuevo-formula');
        const f = document.getElementById('form-nueva-formula');
        if (f) f.reset();
        const aviso = document.getElementById('aviso-auto-refraccion');
        if (aviso) aviso.style.display = 'none';
        document.getElementById('modal-nueva-formula').style.display = 'flex';
    }

    function validateNuevoExamenForm(form) {
        const hiddenUserId = document.getElementById('hidden-nuevo-examen-userid');
        if (!hiddenUserId || !hiddenUserId.value) {
            if (typeof alertify !== 'undefined') {
                alertify.error('Por favor busque y seleccione un paciente.');
            } else {
                alert('Por favor busque y seleccione un paciente.');
            }
            const input = document.getElementById('search-nuevo-examen-paciente');
            if (input) input.focus();
            return false;
        }
        const res = document.getElementById('new-examen-resultado');
        const diag = document.getElementById('new-examen-diagnostico');
        if (res && diag) {
            res.value = diag.value || 'Examen Clínico Visual';
        }
        return true;
    }

    function validateEditExamenForm(form) {
        const hiddenUserId = document.getElementById('edit-examen-userid');
        if (!hiddenUserId || !hiddenUserId.value) {
            if (typeof alertify !== 'undefined') {
                alertify.error('Por favor busque y seleccione un paciente.');
            } else {
                alert('Por favor busque y seleccione un paciente.');
            }
            const input = document.getElementById('search-edit-examen-paciente');
            if (input) input.focus();
            return false;
        }
        const res = document.getElementById('edit-examen-resultado');
        const diag = document.getElementById('edit-examen-diagnostico');
        if (res && diag) {
            res.value = diag.value || 'Examen Clínico Visual';
        }
        return true;
    }

    function validateNuevoFormulaForm(form) {
        const hiddenUserId = document.getElementById('hidden-nuevo-formula-userid');
        if (!hiddenUserId || !hiddenUserId.value) {
            if (typeof alertify !== 'undefined') {
                alertify.error('Por favor busque y seleccione un paciente.');
            } else {
                alert('Por favor busque y seleccione un paciente.');
            }
            const input = document.getElementById('search-nuevo-formula-paciente');
            if (input) input.focus();
            return false;
        }
        return true;
    }

    function validateEditFormulaForm(form) {
        const hiddenUserId = document.getElementById('edit-formula-userid');
        if (!hiddenUserId || !hiddenUserId.value) {
            if (typeof alertify !== 'undefined') {
                alertify.error('Por favor busque y seleccione un paciente.');
            } else {
                alert('Por favor busque y seleccione un paciente.');
            }
            const input = document.getElementById('search-edit-formula-paciente');
            if (input) input.focus();
            return false;
        }
        return true;
    }

    // Cerrar dropdowns de pacientes al hacer clic fuera
    document.addEventListener('click', function (e) {
        if (!e.target.closest('#wrapper-nuevo-examen-paciente')) {
            const d1 = document.getElementById('dropdown-nuevo-examen-pacientes');
            if (d1) d1.style.display = 'none';
        }
        if (!e.target.closest('#wrapper-edit-examen-paciente')) {
            const d2 = document.getElementById('dropdown-edit-examen-pacientes');
            if (d2) d2.style.display = 'none';
        }
        if (!e.target.closest('#wrapper-nuevo-formula-paciente')) {
            const d3 = document.getElementById('dropdown-nuevo-formula-pacientes');
            if (d3) d3.style.display = 'none';
        }
        if (!e.target.closest('#wrapper-edit-formula-paciente')) {
            const d4 = document.getElementById('dropdown-edit-formula-pacientes');
            if (d4) d4.style.display = 'none';
        }
    });

    function abrirModalVerFormula(btn) {
        document.getElementById('ver-formula-id').textContent = btn.getAttribute('data-id');
        document.getElementById('ver-formula-paciente').textContent = btn.getAttribute('data-paciente');
        document.getElementById('ver-formula-documento').textContent = btn.getAttribute('data-documento');
        document.getElementById('ver-formula-fecha').textContent = btn.getAttribute('data-fecha');
        document.getElementById('ver-formula-tipolente').textContent = btn.getAttribute('data-tipolente');
        document.getElementById('ver-formula-esferaod').textContent = btn.getAttribute('data-esferaod');
        document.getElementById('ver-formula-cilindrood').textContent = btn.getAttribute('data-cilindrood');
        document.getElementById('ver-formula-ejeod').textContent = btn.getAttribute('data-ejeod');
        document.getElementById('ver-formula-esferaoi').textContent = btn.getAttribute('data-esferaoi');
        document.getElementById('ver-formula-cilindrooi').textContent = btn.getAttribute('data-cilindrooi');
        document.getElementById('ver-formula-ejeoi').textContent = btn.getAttribute('data-ejeoi');
        document.getElementById('ver-formula-distanciapupilar').textContent = btn.getAttribute('data-distanciapupilar');
        document.getElementById('ver-formula-validez').textContent = btn.getAttribute('data-estado');
        document.getElementById('ver-formula-observaciones').textContent = btn.getAttribute('data-observaciones');
        document.getElementById('ver-formula-optometra').textContent = btn.getAttribute('data-optometra');
        document.getElementById('ver-formula-registroopto').textContent = btn.getAttribute('data-registroopto');
        const dlLink = document.getElementById('btn-descargar-receta-pdf');
        if (dlLink) {
            dlLink.href = '/Optometra/DownloadRecetaPdf/' + btn.getAttribute('data-id');
        }
        document.getElementById('modal-ver-formula').style.display = 'flex';
    }

    function imprimirReceta() {
        const modal = document.getElementById('modal-ver-formula');
        if (modal) modal.style.display = 'flex';
        window.print();
    }

    function descargarRecetaPDF() {
        const rawId = (document.getElementById('ver-formula-id')?.textContent || '').trim();
        if (!rawId) {
            if (typeof alertify !== 'undefined') {
                alertify.error('No se pudo identificar la fórmula para descargar.');
            }
            return;
        }

        const id = rawId.replace(/^FOR-/i, '').replace(/^#/, '').trim();
        if (typeof alertify !== 'undefined') {
            alertify.message('Descargando archivo PDF desde el servidor...');
        }

        // Descarga directa desde el backend con cabecera Content-Disposition (igual que Facturas)
        window.location.href = '/Optometra/DownloadRecetaPdf/' + encodeURIComponent(id);
    }

    function abrirModalEditarFormula(btn) {
        document.getElementById('edit-formula-id').value = btn.getAttribute('data-id');
        const userId = btn.getAttribute('data-userid');
        const pacienteNombre = btn.getAttribute('data-paciente') || '';
        const pacienteDoc = btn.getAttribute('data-documento') || '';
        seleccionarPacienteModal('edit-formula', userId, pacienteNombre, pacienteDoc);
        document.getElementById('edit-formula-fecha').value = btn.getAttribute('data-fecha');
        document.getElementById('edit-formula-esferaod').value = btn.getAttribute('data-esferaod');
        document.getElementById('edit-formula-cilindrood').value = btn.getAttribute('data-cilindrood');
        document.getElementById('edit-formula-ejeod').value = btn.getAttribute('data-ejeod');
        document.getElementById('edit-formula-esferaoi').value = btn.getAttribute('data-esferaoi');
        document.getElementById('edit-formula-cilindrooi').value = btn.getAttribute('data-cilindrooi');
        document.getElementById('edit-formula-ejeoi').value = btn.getAttribute('data-ejeoi');
        document.getElementById('edit-formula-observaciones').value = btn.getAttribute('data-observaciones');
        document.getElementById('edit-formula-tipolente').value = btn.getAttribute('data-tipolente');
        document.getElementById('edit-formula-distanciapupilar').value = btn.getAttribute('data-distanciapupilar');
        document.getElementById('modal-editar-formula').style.display = 'flex';
    }

    function abrirModalCrearPedidoFormula(btn) {
        const formulaId = btn.getAttribute('data-formulaid');
        const paciente = btn.getAttribute('data-paciente') || 'N/A';
        const tipolente = btn.getAttribute('data-tipolente') || 'N/A';
        const fecha = btn.getAttribute('data-fecha') || 'N/A';

        document.getElementById('pedido-formula-id').value = formulaId;
        document.getElementById('pedido-formula-paciente').textContent = paciente;
        document.getElementById('pedido-formula-tipolente').textContent = tipolente;
        document.getElementById('pedido-formula-fecha').textContent = fecha;

        document.getElementById('pedido-formula-productid').value = '';
        document.getElementById('pedido-formula-cantidad').value = '1';
        document.getElementById('pedido-formula-notas').value = '';
        document.getElementById('pedido-formula-stock-hint').textContent = '';

        const errBox = document.getElementById('pedido-formula-error');
        if (errBox) {
            errBox.style.display = 'none';
            document.getElementById('pedido-formula-error-msg').textContent = '';
        }

        const modal = document.getElementById('modal-crear-pedido-formula');
        if (modal) modal.style.display = 'flex';
    }

    function cerrarModalCrearPedidoFormula() {
        const modal = document.getElementById('modal-crear-pedido-formula');
        if (modal) modal.style.display = 'none';
    }

    function actualizarMaxCantidadStock() {
        const sel = document.getElementById('pedido-formula-productid');
        const hint = document.getElementById('pedido-formula-stock-hint');
        const selectedOpt = sel.options[sel.selectedIndex];
        if (selectedOpt && selectedOpt.value) {
            const stock = selectedOpt.getAttribute('data-stock');
            hint.textContent = `Stock disponible: ${stock} unidad(es)`;
        } else {
            hint.textContent = '';
        }
    }

    async function enviarCrearPedidoFormula(e) {
        e.preventDefault();
        const form = document.getElementById('form-crear-pedido-formula');
        const submitBtn = document.getElementById('btn-submit-pedido-formula');
        const errBox = document.getElementById('pedido-formula-error');
        const errMsg = document.getElementById('pedido-formula-error-msg');

        errBox.style.display = 'none';
        errMsg.textContent = '';

        const formulaId = document.getElementById('pedido-formula-id').value;
        const productId = document.getElementById('pedido-formula-productid').value;
        const cantidad = parseInt(document.getElementById('pedido-formula-cantidad').value, 10);

        if (!productId) {
            errBox.style.display = 'flex';
            errMsg.textContent = 'Debe seleccionar un producto del catálogo.';
            return;
        }

        if (isNaN(cantidad) || cantidad < 1) {
            errBox.style.display = 'flex';
            errMsg.textContent = 'La cantidad debe ser mayor o igual a 1.';
            return;
        }

        const prevBtnHtml = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span>Procesando...</span>';

        try {
            const formData = new FormData(form);
            const response = await fetch('/Optometra/CrearPedidoDesdeFormula', {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            const result = await response.json();

            if (result.success) {
                cerrarModalCrearPedidoFormula();
                const numPedido = result.numeroPedido ? '#' + result.numeroPedido : (result.message?.match(/#?[A-Za-z0-9\-]+/)?.[0] || 'Pedido creado');
                
                // Notificación de éxito
                if (typeof alertify !== 'undefined') {
                    alertify.success(result.message || 'Pedido generado con éxito.');
                } else {
                    showToast(result.message || 'Pedido generado con éxito.', 'success');
                }

                // Reemplazar botón en la tabla por badge con número de pedido
                const btnGenerar = document.getElementById(`btn-generar-pedido-${formulaId}`);
                if (btnGenerar) {
                    const badge = document.createElement('span');
                    badge.className = 'badge-status-solid';
                    badge.style.cssText = 'background-color: var(--purple-100); color: var(--purple-700); border: 1px solid var(--purple-300); display: inline-flex; align-items: center; gap: 0.3rem; padding: 0.35rem 0.6rem; font-size: 0.8rem; font-weight: 600; border-radius: 9999px;';
                    badge.title = `Pedido de venta generado: ${numPedido}`;
                    badge.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="icon-sm"><path d="M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4Z"/><path d="M3 6h18"/><path d="M16 10a4 4 0 0 1-8 0"/></svg> <span>${numPedido}</span>`;
                    btnGenerar.parentNode.replaceChild(badge, btnGenerar);
                }
            } else {
                // Error devuelto por el servidor (ej: stock insuficiente o ya existe pedido)
                errBox.style.display = 'flex';
                errMsg.textContent = result.message || 'Error al generar el pedido.';
            }
        } catch (err) {
            console.error(err);
            errBox.style.display = 'flex';
            errMsg.textContent = 'Ocurrió un error inesperado de comunicación con el servidor.';
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerHTML = prevBtnHtml;
        }
    }

    function showToast(msg, type = 'success') {
        let container = document.getElementById('inv-toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'inv-toast-container';
            container.style.cssText = 'position: fixed; bottom: 20px; right: 20px; z-index: 99999; display: flex; flex-direction: column; gap: 10px; pointer-events: none;';
            document.body.appendChild(container);
        }
        const bg = type === 'success' ? '#059669' : (type === 'warning' ? '#d97706' : '#dc2626');
        const toast = document.createElement('div');
        toast.style.cssText = `pointer-events: auto; padding: 12px 18px; border-radius: 8px; color: white; font-weight: 500; font-size: 0.875rem; box-shadow: 0 10px 25px -5px rgba(0,0,0,0.3); transition: all 0.25s ease; opacity: 0; transform: translateY(12px); display: flex; align-items: center; gap: 10px; background: ${bg};`;
        toast.innerHTML = `<span>${msg}</span>`;
        container.appendChild(toast);
        setTimeout(() => {
            toast.style.opacity = '1';
            toast.style.transform = 'translateY(0)';
        }, 10);
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(12px)';
            setTimeout(() => toast.remove(), 250);
        }, 4000);
    }

    function confirmDeactivatePaciente(id, nombre) {
        if (typeof alertify !== 'undefined') {
            alertify.confirm(
                'Desactivar Paciente',
                '¿Está seguro de desactivar a <strong>' + nombre + '</strong>? Su estado cambiará a Inactivo.',
                function () { document.getElementById('form-delete-paciente-' + id).submit(); },
                function () { alertify.message('Eliminación cancelada'); }
            ).set('labels', { ok: 'Aceptar', cancel: 'Cancelar' });
        } else if (confirm('¿Está seguro de desactivar a ' + nombre + '?')) {
            document.getElementById('form-delete-paciente-' + id).submit();
        }
    }

    function confirmDeleteAppointment(id) {
        if (typeof alertify !== 'undefined') {
            alertify.confirm(
                'Eliminar Cita',
                '¿Está seguro de eliminar esta cita? Esta acción no se puede deshacer.',
                function () { document.getElementById('form-delete-appointment-' + id).submit(); },
                function () { alertify.message('Eliminación cancelada'); }
            ).set('labels', { ok: 'Aceptar', cancel: 'Cancelar' });
        } else if (confirm('¿Está seguro de eliminar esta cita?')) {
            document.getElementById('form-delete-appointment-' + id).submit();
        }
    }

    function confirmDeleteHistorial(id) {
        if (typeof alertify !== 'undefined') {
            alertify.confirm(
                'Eliminar Registro Clínico',
                '¿Está seguro de eliminar este registro del historial clínico?',
                function () { document.getElementById('form-delete-historial-' + id).submit(); },
                function () { alertify.message('Eliminación cancelada'); }
            ).set('labels', { ok: 'Aceptar', cancel: 'Cancelar' });
        } else if (confirm('¿Está seguro de eliminar este registro?')) {
            document.getElementById('form-delete-historial-' + id).submit();
        }
    }

    function confirmDeleteExamen(id) {
        if (typeof alertify !== 'undefined') {
            alertify.confirm(
                'Eliminar Examen Visual',
                '¿Está seguro de eliminar este examen visual?',
                function () { document.getElementById('form-delete-examen-' + id).submit(); },
                function () { alertify.message('Eliminación cancelada'); }
            ).set('labels', { ok: 'Aceptar', cancel: 'Cancelar' });
        } else if (confirm('¿Está seguro de eliminar este examen?')) {
            document.getElementById('form-delete-examen-' + id).submit();
        }
    }

    function confirmDeleteFormula(id) {
        if (typeof alertify !== 'undefined') {
            alertify.confirm(
                'Eliminar Fórmula Óptica',
                '¿Está seguro de eliminar esta fórmula óptica? Esta acción no se puede deshacer.',
                function () { document.getElementById('form-delete-formula-' + id).submit(); },
                function () { alertify.message('Eliminación cancelada'); }
            ).set('labels', { ok: 'Aceptar', cancel: 'Cancelar' });
        } else if (confirm('¿Está seguro de eliminar esta fórmula?')) {
            document.getElementById('form-delete-formula-' + id).submit();
        }
    }

    function validateCitaForm(form) {
        const input = form.querySelector('input[type="datetime-local"]');
        if (!input) return true;
        const datetimeVal = input.value;
        if (!datetimeVal) return true;
        
        const dt = new Date(datetimeVal);
        if (dt.getDay() === 0) {
            if (typeof alertify !== 'undefined') {
                alertify.error("Las citas solo pueden agendarse de lunes a sábado.");
            } else {
                alert("Las citas solo pueden agendarse de lunes a sábado.");
            }
            return false;
        }
        
        const hours = dt.getHours();
        const minutes = dt.getMinutes();
        const decimalHour = hours + minutes / 60.0;
        if (decimalHour < 8.0 || (decimalHour + 1.0) > 18.0) {
            if (typeof alertify !== 'undefined') {
                alertify.error("El horario de citas es de 8:00 a.m. a 6:00 p.m.");
            } else {
                alert("El horario de citas es de 8:00 a.m. a 6:00 p.m.");
            }
            return false;
        }
        return true;
    }

    function mostrarResumenPeriodo(e) {
            if (e) e.preventDefault();
            const desde = document.getElementById('rep-desde').value;
            const hasta = document.getElementById('rep-hasta').value;
            const allCitas = window.appointmentsData || [];

            let filtradas = allCitas;
            if (desde) {
                filtradas = filtradas.filter(c => c.fechaStr >= desde);
            }
            if (hasta) {
                filtradas = filtradas.filter(c => c.fechaStr <= hasta);
            }

            const conteos = {};
            filtradas.forEach(c => {
                const est = (c.estado || 'Sin estado').toLowerCase();
                const capitalizado = est.charAt(0).toUpperCase() + est.slice(1);
                conteos[capitalizado] = (conteos[capitalizado] || 0) + 1;
            });

            const itemsCont = document.getElementById('items-resumen-reporte');
            const titulo = document.getElementById('titulo-resumen-reporte');
            const card = document.getElementById('card-resumen-reporte');
            const btnLimpiar = document.getElementById('btn-limpiar-rep');

            let tit = 'Resumen de citas';
            if (desde && hasta) tit += ` (${desde} al ${hasta})`;
            else if (desde) tit += ` (Desde ${desde})`;
            else if (hasta) tit += ` (Hasta ${hasta})`;
            else tit += ' (Período completo)';
            titulo.textContent = `${tit} — Total: ${filtradas.length} cita(s)`;

            const keys = Object.keys(conteos);
            if (keys.length === 0) {
                itemsCont.innerHTML = '<p style="color: var(--gray-500); font-style: italic; margin: 0;">No hay citas en el período seleccionado.</p>';
            } else {
                itemsCont.innerHTML = keys.map(k => `
                    <div style="background: white; border: 1px solid var(--purple-200); border-radius: 0.375rem; padding: 0.75rem 1.25rem; text-align: center; min-width: 110px;">
                        <div style="font-size: 1.5rem; font-weight: 700; color: var(--purple-700);">${conteos[k]}</div>
                        <div style="font-size: 0.8rem; color: var(--gray-600);">${k}</div>
                    </div>
                `).join('');
            }

            card.style.display = 'block';
            if (btnLimpiar) btnLimpiar.style.display = 'inline-block';
        }

        function limpiarResumenPeriodo() {
            document.getElementById('rep-desde').value = '';
            document.getElementById('rep-hasta').value = '';
            document.getElementById('card-resumen-reporte').style.display = 'none';
            document.getElementById('btn-limpiar-rep').style.display = 'none';
        }