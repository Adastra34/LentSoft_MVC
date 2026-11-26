// Menú hamburguesa del navbar público (visible solo en ≤1024px, ver responsive.css)
(function () {
  'use strict';
  var nav = document.getElementById('lm-navbar');
  var btn = document.getElementById('lm-nav-toggle');
  if (!nav || !btn) return;

  function setOpen(open) {
    nav.classList.toggle('lm-open', open);
    btn.setAttribute('aria-expanded', open ? 'true' : 'false');
    btn.setAttribute('aria-label', open ? 'Cerrar menú' : 'Abrir menú');
  }

  btn.addEventListener('click', function (e) {
    e.stopPropagation();
    setOpen(!nav.classList.contains('lm-open'));
  });

  // Cerrar al navegar por un enlace del menú o con Escape
  nav.addEventListener('click', function (e) {
    var a = e.target.closest && e.target.closest('.lm-nav-links a');
    if (a) setOpen(false);
  });
  document.addEventListener('keydown', function (e) { if (e.key === 'Escape') setOpen(false); });

  // Cerrar al hacer clic fuera del navbar cuando esté abierto
  document.addEventListener('click', function (e) {
    if (!nav.classList.contains('lm-open')) return;
    if (!nav.contains(e.target)) {
      setOpen(false);
    }
  });

  // Al volver a escritorio, reiniciar estado
  window.addEventListener('resize', function () { if (window.innerWidth > 1024) setOpen(false); });
})();
