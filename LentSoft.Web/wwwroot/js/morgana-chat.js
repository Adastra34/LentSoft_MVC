/**
 * morgana-chat.js
 * Lógica del widget de chat flotante "Morgana" — LentSoft FAQ Bot
 * Sin dependencias externas. Todo en el cliente.
 */

(function () {
  'use strict';

  // ── Estado ──────────────────────────────────────────────────────────────────
  let isOpen = false;
  let hasGreeted = false;
  let conversationHistory = [];

  // ── Crear estructura HTML del widget ────────────────────────────────────────
  function crearWidget() {
    const wrapper = document.createElement('div');
    wrapper.id = 'morgana-widget';

    // Panel HTML (sin SVG problemático, sólo iconos simples de texto/stroke)
    wrapper.innerHTML = `
      <!-- Panel de chat -->
      <div id="morgana-panel" class="morgana-panel" role="dialog" aria-label="Chat Morgana" aria-hidden="true">
        <!-- Header -->
        <div class="morgana-header">
          <div class="morgana-header-info">
            <div class="morgana-avatar-sm">M</div>
            <div>
              <div class="morgana-header-name">Morgana</div>
              <div class="morgana-header-sub">Asistente virtual de LentSoft</div>
            </div>
          </div>
          <button id="morgana-close-btn" class="morgana-close-btn" aria-label="Cerrar chat">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        </div>

        <!-- Historial de mensajes -->
        <div id="morgana-messages" class="morgana-messages" aria-live="polite"></div>

        <!-- Título de preguntas predeterminadas -->
        <div class="morgana-chips-header">Preguntas Predeterminadas</div>

        <!-- Chips de preguntas sugeridas (como lista seleccionable) -->
        <div id="morgana-chips" class="morgana-chips"></div>

        <!-- Input de texto libre -->
        <div class="morgana-input-bar">
          <input type="text" id="morgana-input" class="morgana-input" placeholder="Escribe tu mensaje…" autocomplete="off" />
          <button id="morgana-send-btn" class="morgana-send-btn" aria-label="Enviar mensaje">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <line x1="22" y1="2" x2="11" y2="13"></line>
              <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
            </svg>
          </button>
        </div>
      </div>

      <!-- Botón flotante (el SVG del stickman se agrega vía JS más abajo) -->
      <button id="morgana-toggle-btn" class="morgana-toggle-btn" aria-label="Abrir chat de ayuda Morgana" title="\u00bfNecesitas ayuda?">
        <span id="morgana-icon-chat" class="morgana-btn-icon"></span>
        <span id="morgana-icon-m" class="morgana-btn-icon morgana-btn-icon--hidden">M</span>
        <span class="morgana-notif-dot" id="morgana-notif-dot"></span>
      </button>
    `;

    document.body.appendChild(wrapper);

    // Inyectar el SVG del stickman usando createElementNS para evitar
    // problemas de parseo HTML con xmlns dentro de innerHTML
    var NS = 'http://www.w3.org/2000/svg';
    var svg = document.createElementNS(NS, 'svg');
    svg.setAttribute('viewBox', '0 0 100 100');
    svg.setAttribute('width', '34');
    svg.setAttribute('height', '34');
    svg.setAttribute('aria-hidden', 'true');

    var head = document.createElementNS(NS, 'circle');
    head.setAttribute('cx', '50'); head.setAttribute('cy', '22'); head.setAttribute('r', '12'); head.setAttribute('fill', '#FFFFFF');
    svg.appendChild(head);

    var lines = [
      ['50','34','50','68'],   // cuerpo
      ['50','44','30','58'],   // brazo izq
      ['50','44','70','58'],   // brazo der
      ['50','68','34','90'],   // pierna izq
      ['50','68','66','90'],   // pierna der
    ];
    lines.forEach(function(pts) {
      var l = document.createElementNS(NS, 'line');
      l.setAttribute('x1', pts[0]); l.setAttribute('y1', pts[1]);
      l.setAttribute('x2', pts[2]); l.setAttribute('y2', pts[3]);
      l.setAttribute('stroke', '#FFFFFF');
      l.setAttribute('stroke-width', '6');
      l.setAttribute('stroke-linecap', 'round');
      svg.appendChild(l);
    });

    var iconSpan = document.getElementById('morgana-icon-chat');
    if (iconSpan) iconSpan.appendChild(svg);
  }

  // ── Renderizar chips de preguntas sugeridas ──────────────────────────────────
  function renderizarChips() {
    const chipsContainer = document.getElementById('morgana-chips');
    if (!chipsContainer || !window.MORGANA_FAQ) return;

    chipsContainer.innerHTML = '';
    MORGANA_FAQ.forEach(function (item, index) {
      const chip = document.createElement('button');
      chip.className = 'morgana-chip';
      chip.textContent = item.pregunta;
      chip.setAttribute('data-index', index);
      chip.setAttribute('aria-label', 'Preguntar: ' + item.pregunta);
      chip.addEventListener('click', function () {
        manejarChipClick(item);
      });
      chipsContainer.appendChild(chip);
    });
  }

  // ── Agregar mensaje al historial ─────────────────────────────────────────────
  function agregarMensaje(texto, tipo) {
    const messagesEl = document.getElementById('morgana-messages');
    if (!messagesEl) return;

    const burbuja = document.createElement('div');
    burbuja.className = 'morgana-bubble morgana-bubble--' + tipo;

    if (tipo === 'bot') {
      const avatar = document.createElement('div');
      avatar.className = 'morgana-bubble-avatar';
      avatar.textContent = 'M';
      burbuja.appendChild(avatar);
    }

    const contenido = document.createElement('div');
    contenido.className = 'morgana-bubble-content';
    contenido.innerHTML = texto; // Permite HTML en respuestas del bot
    burbuja.appendChild(contenido);

    messagesEl.appendChild(burbuja);
    scrollAlFinal();
  }

  // ── Indicador de escritura (typing) ──────────────────────────────────────────
  function mostrarTyping() {
    const messagesEl = document.getElementById('morgana-messages');
    if (!messagesEl) return;

    const typing = document.createElement('div');
    typing.id = 'morgana-typing';
    typing.className = 'morgana-bubble morgana-bubble--bot';
    typing.innerHTML = `
      <div class="morgana-bubble-avatar">M</div>
      <div class="morgana-bubble-content morgana-typing-dots">
        <span></span><span></span><span></span>
      </div>
    `;
    messagesEl.appendChild(typing);
    scrollAlFinal();
  }

  function ocultarTyping() {
    const typing = document.getElementById('morgana-typing');
    if (typing) typing.remove();
  }

  // ── Scroll automático ────────────────────────────────────────────────────────
  function scrollAlFinal() {
    const messagesEl = document.getElementById('morgana-messages');
    if (messagesEl) {
      messagesEl.scrollTop = messagesEl.scrollHeight;
    }
  }

  // ── Obtener respuesta local (fallback FAQ) ──────────────────────────────────
  function obtenerRespuestaLocal(textoPregunta) {
    if (window.morganaBuscarRespuesta) {
      const resultado = morganaBuscarRespuesta(textoPregunta);
      return resultado
        ? resultado.respuesta
        : 'No tengo una respuesta para eso todavía 🤔 pero puedes explorar la <strong>Tienda</strong> o escribirnos por soporte.';
    }
    return 'Lo siento, el módulo de respuestas no está disponible.';
  }

  // ── Procesar consulta del usuario (IA con fallback a FAQ) ───────────────────
  function procesarConsulta(textoPregunta, textoBurbuja) {
    // Mostrar pregunta del usuario
    agregarMensaje(textoBurbuja || textoPregunta, 'user');

    // Deshabilitar interacciones mientras procesa
    const chipsContainer = document.getElementById('morgana-chips');
    const inputEl = document.getElementById('morgana-input');
    const sendBtn = document.getElementById('morgana-send-btn');
    if (chipsContainer) chipsContainer.classList.add('morgana-chips--disabled');
    if (inputEl) inputEl.disabled = true;
    if (sendBtn) sendBtn.disabled = true;

    mostrarTyping();

    // Llamar al backend de IA
    fetch('/Chat/Ask', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        mensaje: textoPregunta,
        historial: conversationHistory
      })
    })
    .then(function (res) {
      if (!res.ok) throw new Error('HTTP ' + res.status);
      return res.json();
    })
    .then(function (data) {
      ocultarTyping();
      var respuesta = data.respuesta || obtenerRespuestaLocal(textoPregunta);
      agregarMensaje(respuesta, 'bot');

      // Actualizar historial en memoria
      conversationHistory.push({ role: 'user', content: textoPregunta });
      conversationHistory.push({ role: 'assistant', content: respuesta });

      // Mantener máximo 20 mensajes (los más recientes)
      if (conversationHistory.length > 20) {
        conversationHistory = conversationHistory.slice(-20);
      }
    })
    .catch(function () {
      ocultarTyping();
      // Fallback a FAQ local si la API falla
      var respuestaLocal = obtenerRespuestaLocal(textoPregunta);
      agregarMensaje(respuestaLocal, 'bot');
    })
    .finally(function () {
      // Rehabilitar interacciones
      if (chipsContainer) chipsContainer.classList.remove('morgana-chips--disabled');
      if (inputEl) { inputEl.disabled = false; inputEl.focus(); }
      if (sendBtn) sendBtn.disabled = false;
    });
  }

  // ── Manejadores de eventos ────────────────────────────────────────────────────
  function manejarChipClick(item) {
    procesarConsulta(item.pregunta, item.pregunta);
  }

  function manejarEnvio() {
    const input = document.getElementById('morgana-input');
    if (!input) return;
    const texto = input.value.trim();
    if (!texto) return;
    input.value = '';
    procesarConsulta(texto, texto);
  }

  // ── Abrir / Cerrar panel ─────────────────────────────────────────────────────
  function abrirPanel() {
    const panel = document.getElementById('morgana-panel');
    const iconChat = document.getElementById('morgana-icon-chat');
    const iconM = document.getElementById('morgana-icon-m');
    const notifDot = document.getElementById('morgana-notif-dot');
    const btn = document.getElementById('morgana-toggle-btn');

    if (!panel) return;
    isOpen = true;
    panel.classList.add('morgana-panel--open');
    panel.setAttribute('aria-hidden', 'false');
    btn && btn.setAttribute('aria-label', 'Cerrar chat de ayuda Morgana');
    iconChat && iconChat.classList.add('morgana-btn-icon--hidden');
    iconM && iconM.classList.remove('morgana-btn-icon--hidden');
    notifDot && notifDot.classList.add('morgana-notif-dot--hidden');

    // Mensaje de bienvenida (solo la primera vez)
    if (!hasGreeted) {
      hasGreeted = true;
      setTimeout(function () {
        agregarMensaje(
          '¡Hola! Soy Morgana 👋 Puedo ayudarte a resolver dudas sobre la tienda. Elige una pregunta o escribe la tuya:',
          'bot'
        );
      }, 250);
    }

    // Foco en input
    setTimeout(function () {
      const input = document.getElementById('morgana-input');
      if (input) input.focus();
    }, 350);
  }

  function cerrarPanel() {
    const panel = document.getElementById('morgana-panel');
    const iconChat = document.getElementById('morgana-icon-chat');
    const iconM = document.getElementById('morgana-icon-m');
    const btn = document.getElementById('morgana-toggle-btn');

    if (!panel) return;
    isOpen = false;
    panel.classList.remove('morgana-panel--open');
    panel.setAttribute('aria-hidden', 'true');
    btn && btn.setAttribute('aria-label', 'Abrir chat de ayuda Morgana');
    iconChat && iconChat.classList.remove('morgana-btn-icon--hidden');
    iconM && iconM.classList.add('morgana-btn-icon--hidden');
  }

  // ── Inicialización ───────────────────────────────────────────────────────────
  function inicializar() {
    crearWidget();
    renderizarChips();

    // Toggle botón flotante
    const toggleBtn = document.getElementById('morgana-toggle-btn');
    if (toggleBtn) {
      toggleBtn.addEventListener('click', function () {
        isOpen ? cerrarPanel() : abrirPanel();
      });
    }

    // Botón cerrar dentro del panel
    const closeBtn = document.getElementById('morgana-close-btn');
    if (closeBtn) {
      closeBtn.addEventListener('click', cerrarPanel);
    }

    // Botón enviar
    const sendBtn = document.getElementById('morgana-send-btn');
    if (sendBtn) {
      sendBtn.addEventListener('click', manejarEnvio);
    }

    // Enter en el input
    const input = document.getElementById('morgana-input');
    if (input) {
      input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') manejarEnvio();
      });
    }

    // Cerrar con Escape
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && isOpen) cerrarPanel();
    });
  }

  // Esperar al DOM
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', inicializar);
  } else {
    inicializar();
  }
})();
