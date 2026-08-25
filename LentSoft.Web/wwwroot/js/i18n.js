// ════════════════════════════════════════════════════════════════════════════
// i18n.js — Sistema de Internacionalización (Español / Inglés)
// ════════════════════════════════════════════════════════════════════════════
// Este script maneja el cambio de idioma en el lado del cliente.
// Usa atributos data-i18n="clave" en el HTML y un diccionario de traducciones.
// La preferencia se guarda en localStorage para persistir entre sesiones.
// ════════════════════════════════════════════════════════════════════════════

(function () {
    'use strict';

    // ── Diccionario de traducciones ──
    const translations = {

        // ════════════════════════════════════════════
        //  NAVBAR
        // ════════════════════════════════════════════
        'nav.home': { es: 'Inicio', en: 'Home' },
        'nav.shop': { es: 'Tienda', en: 'Shop' },
        'nav.about': { es: 'Nosotros', en: 'About Us' },
        'nav.search': { es: 'Buscar gafas, lentes...', en: 'Search glasses, lenses...' },
        'nav.cart': { es: 'Carrito de compras', en: 'Shopping cart' },
        'nav.dashboard.admin': { es: 'Dashboard Admin', en: 'Admin Dashboard' },
        'nav.dashboard.optometria': { es: 'Dashboard Optometría', en: 'Optometry Dashboard' },
        'nav.dashboard.ventas': { es: 'Dashboard Ventas', en: 'Sales Dashboard' },
        'nav.dashboard.cuenta': { es: 'Mi Cuenta', en: 'My Account' },
        'nav.greeting': { es: 'Hola,', en: 'Hello,' },
        'nav.logout': { es: 'Cerrar Sesión', en: 'Log Out' },
        'nav.login': { es: '👤 Ingresar', en: '👤 Sign In' },

        // ════════════════════════════════════════════
        //  FOOTER
        // ════════════════════════════════════════════
        'footer.rights': { es: 'Todos los derechos reservados.', en: 'All rights reserved.' },
        'footer.platform': { es: 'Plataforma E-commerce Óptico', en: 'Optical E-commerce Platform' },

        // ════════════════════════════════════════════
        //  HOME — HERO
        // ════════════════════════════════════════════
        'home.hero.badge': { es: 'Oferta especial', en: 'Special offer' },
        'home.hero.title': { es: 'GAFAS FORMULADAS', en: 'PRESCRIPTION GLASSES' },
        'home.hero.cta': { es: 'Ver ofertas →', en: 'View offers →' },
        'home.hero.services': { es: 'Servicios Ópticos', en: 'Optical Services' },
        'home.hero.exam': { es: 'PROGRAMA TU EXAMEN VISUAL PARA TI Y TU FAMILIA', en: 'SCHEDULE YOUR EYE EXAM FOR YOU AND YOUR FAMILY' },
        'home.hero.appointment': { es: '📅 Agendar cita ahora', en: '📅 Book appointment now' },
        'home.hero.frames': { es: '👓 Muestra de montura', en: '👓 Frame showcase' },
        'home.hero.personalized': { es: 'Atención personalizada', en: 'Personalized attention' },
        'home.hero.modern': { es: 'Equipos modernos', en: 'Modern equipment' },
        'home.hero.certified': { es: 'Profesionales certificados', en: 'Certified professionals' },

        // ════════════════════════════════════════════
        //  HOME — BANNER REGISTRO
        // ════════════════════════════════════════════
        'home.banner.new': { es: '¿Nuevo en LentSoft?', en: 'New to LentSoft?' },
        'home.banner.noaccount': { es: 'Si no tienes cuenta, ', en: "If you don't have an account, " },
        'home.banner.register': { es: 'regístrate aquí', en: 'register here' },
        'home.banner.benefits': { es: 'Accede a descuentos exclusivos, historial de citas y mucho más', en: 'Access exclusive discounts, appointment history and much more' },
        'home.banner.signup': { es: '➕ Registrarse →', en: '➕ Sign Up →' },

        // ════════════════════════════════════════════
        //  HOME — TECNOLOGÍA
        // ════════════════════════════════════════════
        'home.tech.badge': { es: 'TECNOLOGÍA INNOVADORA', en: 'INNOVATIVE TECHNOLOGY' },
        'home.tech.title': { es: 'Previsualización de Marcos con Realidad Aumentada', en: 'Frame Preview with Augmented Reality' },
        'home.tech.desc': { es: 'Utiliza nuestra tecnología de realidad aumentada para probarte monturas desde la comodidad de tu hogar. Sube tu foto o activa tu cámara y visualiza cómo lucen nuestros marcos en tu rostro antes de comprar.', en: 'Use our augmented reality technology to try on frames from the comfort of your home. Upload your photo or turn on your camera and see how our frames look on your face before buying.' },
        'home.tech.soon': { es: 'Prueba virtual disponible próximamente', en: 'Virtual try-on coming soon' },
        'home.tech.camera': { es: 'Activa tu cámara para probarte monturas al instante', en: 'Activate your camera to try on frames instantly' },

        // ════════════════════════════════════════════
        //  HOME — CATEGORÍAS
        // ════════════════════════════════════════════
        'home.cat.badge': { es: 'Explora', en: 'Explore' },
        'home.cat.title': { es: 'Categorías Populares', en: 'Popular Categories' },
        'home.cat.sun': { es: 'Lentes de Sol', en: 'Sunglasses' },
        'home.cat.sun.desc': { es: 'Protección y estilo para tus ojos', en: 'Protection and style for your eyes' },
        'home.cat.grad': { es: 'Lentes Graduados', en: 'Prescription Lenses' },
        'home.cat.grad.desc': { es: 'Claridad y comodidad garantizada', en: 'Guaranteed clarity and comfort' },
        'home.cat.contact': { es: 'Lentes de Contacto', en: 'Contact Lenses' },
        'home.cat.contact.desc': { es: 'Libertad y visión perfecta', en: 'Freedom and perfect vision' },
        'home.cat.viewmore': { es: 'Ver Más', en: 'View More' },

        // ════════════════════════════════════════════
        //  HOME — PRODUCTOS MÁS VENDIDOS
        // ════════════════════════════════════════════
        'home.best.badge': { es: 'Top ventas', en: 'Top sellers' },
        'home.best.title': { es: 'Productos Más Vendidos', en: 'Best Selling Products' },
        'home.best.viewall': { es: 'Ver todos →', en: 'View all →' },
        'home.best.details': { es: 'Ver Detalles', en: 'View Details' },

        // ════════════════════════════════════════════
        //  HOME — PRODUCTOS CON DESCUENTO
        // ════════════════════════════════════════════
        'home.disc.badge': { es: '🔥 Ofertas', en: '🔥 Deals' },
        'home.disc.title': { es: 'Productos con Descuento', en: 'Discounted Products' },
        'home.disc.buy': { es: 'Comprar Ahora', en: 'Buy Now' },

        // ════════════════════════════════════════════
        //  HOME — LENTES DE CONTACTO
        // ════════════════════════════════════════════
        'home.contact.badge': { es: '👁️ Especialidad', en: '👁️ Specialty' },
        'home.contact.title': { es: 'Lentes de Contacto', en: 'Contact Lenses' },
        'home.contact.soon': { es: 'Próximamente — Lentes de contacto de las mejores marcas', en: 'Coming soon — Contact lenses from the best brands' },
        'home.contact.catalog': { es: 'Estamos preparando un catálogo completo para ti.', en: 'We are preparing a complete catalog for you.' },

        // ════════════════════════════════════════════
        //  ABOUT (NOSOTROS)
        // ════════════════════════════════════════════
        'about.title': { es: 'Sobre LentSoft', en: 'About LentSoft' },
        'about.subtitle': { es: 'Líderes en soluciones ópticas con más de 10 años de experiencia', en: 'Leaders in optical solutions with over 10 years of experience' },
        'about.mission': { es: 'Nuestra Misión', en: 'Our Mission' },
        'about.mission.text': { es: 'Proporcionar soluciones ópticas de la más alta calidad, combinando tecnología avanzada con un servicio personalizado para mejorar la calidad de vida de nuestros clientes a través de una visión clara y cómoda.', en: 'Provide the highest quality optical solutions, combining advanced technology with personalized service to improve the quality of life of our clients through clear and comfortable vision.' },
        'about.vision': { es: 'Nuestra Visión', en: 'Our Vision' },
        'about.vision.text': { es: 'Ser la plataforma óptica líder en Latinoamérica, reconocida por nuestra innovación, calidad excepcional y compromiso con la satisfacción del cliente, transformando la forma en que las personas cuidan de su salud visual.', en: 'Be the leading optical platform in Latin America, recognized for our innovation, exceptional quality and commitment to customer satisfaction, transforming the way people take care of their eye health.' },
        'about.values': { es: 'Nuestros Valores', en: 'Our Values' },
        'about.val.quality': { es: 'Calidad', en: 'Quality' },
        'about.val.quality.desc': { es: 'Productos certificados de las mejores marcas', en: 'Certified products from the best brands' },
        'about.val.innovation': { es: 'Innovación', en: 'Innovation' },
        'about.val.innovation.desc': { es: 'Tecnología de punta en salud visual', en: 'Cutting-edge technology in eye health' },
        'about.val.commitment': { es: 'Compromiso', en: 'Commitment' },
        'about.val.commitment.desc': { es: 'Dedicados a tu bienestar visual', en: 'Dedicated to your visual well-being' },
        'about.val.trust': { es: 'Confianza', en: 'Trust' },
        'about.val.trust.desc': { es: 'Tu salud visual es nuestra prioridad', en: 'Your eye health is our priority' },
        'about.team': { es: 'Nuestro Equipo', en: 'Our Team' },
        'about.team.optometrist': { es: 'Optometrista Principal', en: 'Lead Optometrist' },
        'about.team.contact.spec': { es: 'Especialista en Lentes de Contacto', en: 'Contact Lens Specialist' },
        'about.team.sales': { es: 'Asesor de Ventas', en: 'Sales Advisor' },
        'about.team.exp15': { es: '15 años de experiencia', en: '15 years of experience' },
        'about.team.exp12': { es: '12 años de experiencia', en: '12 years of experience' },
        'about.team.exp8': { es: '8 años de experiencia', en: '8 years of experience' },
        'about.cta.title': { es: '¿Tienes Preguntas?', en: 'Have Questions?' },
        'about.cta.desc': { es: 'Estamos aquí para ayudarte. Contáctanos hoy mismo.', en: 'We are here to help you. Contact us today.' },
        'about.cta.btn': { es: 'Contáctanos', en: 'Contact Us' },

        // ════════════════════════════════════════════
        //  AUTH — LOGIN
        // ════════════════════════════════════════════
        'auth.login.subtitle': { es: 'Tu salud visual en las mejores manos.', en: 'Your eye health in the best hands.' },
        'auth.login.title': { es: 'INICIO DE SESIÓN', en: 'SIGN IN' },
        'auth.login.email': { es: 'Correo electrónico', en: 'Email address' },
        'auth.login.password': { es: 'Contraseña', en: 'Password' },
        'auth.login.forgot': { es: '¿Olvidaste tu contraseña?', en: 'Forgot your password?' },
        'auth.login.submit': { es: 'Iniciar sesión', en: 'Sign in' },
        'auth.login.create': { es: 'Crear cuenta', en: 'Create account' },

        // ════════════════════════════════════════════
        //  AUTH — REGISTER
        // ════════════════════════════════════════════
        'auth.reg.title': { es: 'Crear cuenta', en: 'Create Account' },
        'auth.reg.subtitle': { es: 'Únete a LentSoft y accede a todos los beneficios', en: 'Join LentSoft and access all the benefits' },
        'auth.reg.section.id': { es: 'DOCUMENTO DE IDENTIDAD', en: 'IDENTITY DOCUMENT' },
        'auth.reg.doctype': { es: 'Tipo de documento', en: 'Document type' },
        'auth.reg.docnum': { es: 'Número de documento', en: 'Document number' },
        'auth.reg.section.personal': { es: 'DATOS PERSONALES', en: 'PERSONAL DATA' },
        'auth.reg.name': { es: 'Nombre', en: 'First name' },
        'auth.reg.lastname': { es: 'Apellido', en: 'Last name' },
        'auth.reg.section.contact': { es: 'INFORMACIÓN DE CONTACTO', en: 'CONTACT INFORMATION' },
        'auth.reg.phone': { es: 'Número de teléfono', en: 'Phone number' },
        'auth.reg.email': { es: 'Correo electrónico', en: 'Email address' },
        'auth.reg.section.password': { es: 'CONTRASEÑA', en: 'PASSWORD' },
        'auth.reg.password': { es: 'Contraseña', en: 'Password' },
        'auth.reg.confirm': { es: 'Confirmar contraseña', en: 'Confirm password' },
        'auth.reg.hint': { es: '🔒 Mín. 8 caract. (Incluye mayúscula, minúscula, número y símbolo especial).', en: '🔒 Min. 8 chars. (Includes uppercase, lowercase, number and special symbol).' },
        'auth.reg.required': { es: '* Campos obligatorios', en: '* Required fields' },
        'auth.reg.submit': { es: '👤 Crear cuenta', en: '👤 Create account' },
        'auth.reg.hasaccount': { es: '¿Ya tienes cuenta?', en: 'Already have an account?' },
        'auth.reg.signin': { es: 'Iniciar sesión', en: 'Sign in' },
        'auth.reg.opt.cc': { es: 'Cédula de ciudadanía (CC)', en: 'Citizenship ID (CC)' },
        'auth.reg.opt.ce': { es: 'Cédula de extranjería (CE)', en: 'Foreign ID (CE)' },
        'auth.reg.opt.ti': { es: 'Tarjeta de identidad (TI)', en: 'Identity Card (TI)' },
        'auth.reg.opt.passport': { es: 'Pasaporte', en: 'Passport' },

        // ════════════════════════════════════════════
        //  TIENDA (SHOP)
        // ════════════════════════════════════════════
        'shop.bestsellers': { es: 'Más Vendidos', en: 'Best Sellers' },
        'shop.allproducts': { es: 'Todos los Productos', en: 'All Products' },
        'shop.all': { es: 'Todos', en: 'All' },
        'shop.allbrands': { es: 'Todas las marcas', en: 'All brands' },
        'shop.allprices': { es: 'Todos los precios', en: 'All prices' },
        'shop.lessthan': { es: 'Menos de COP $500.000', en: 'Less than COP $500,000' },
        'shop.range': { es: 'COP $500.000 - COP $1.500.000', en: 'COP $500,000 - COP $1,500,000' },
        'shop.morethan': { es: 'Más de COP $1.500.000', en: 'More than COP $1,500,000' },
        'shop.clear': { es: '✕ Limpiar', en: '✕ Clear' },
        'shop.noproducts': { es: 'No se encontraron productos', en: 'No products found' },
        'shop.changefilters': { es: 'Intenta cambiar los filtros de búsqueda.', en: 'Try changing the search filters.' },
        'shop.viewall': { es: 'Ver todos los productos', en: 'View all products' },

        // ════════════════════════════════════════════
        //  PRODUCT CARD
        // ════════════════════════════════════════════
        'product.addcart': { es: '🛒 Añadir al Carrito', en: '🛒 Add to Cart' },
        'product.soldout': { es: 'Agotado', en: 'Sold Out' },

        // ════════════════════════════════════════════
        //  CART (CARRITO)
        // ════════════════════════════════════════════
        'cart.title': { es: 'Tu Carrito de Compras', en: 'Your Shopping Cart' },
        'cart.empty': { es: 'Tu carrito está vacío', en: 'Your cart is empty' },
        'cart.empty.desc': { es: 'Parece que aún no has agregado productos. Explora nuestro catálogo y encuentra lo que necesitas.', en: "It looks like you haven't added any products yet. Browse our catalog and find what you need." },
        'cart.goshop': { es: 'Ir a la Tienda', en: 'Go to Shop' },
        'cart.product': { es: 'Producto', en: 'Product' },
        'cart.price': { es: 'Precio', en: 'Price' },
        'cart.qty': { es: 'Cantidad', en: 'Quantity' },
        'cart.subtotal': { es: 'Subtotal', en: 'Subtotal' },
        'cart.summary': { es: 'Resumen de Compra', en: 'Order Summary' },
        'cart.products': { es: 'Productos', en: 'Products' },
        'cart.shipping': { es: 'Envío', en: 'Shipping' },
        'cart.free': { es: 'Gratis', en: 'Free' },
        'cart.total': { es: 'Total General', en: 'Grand Total' },
        'cart.checkout': { es: 'Proceder al pago 💳', en: 'Proceed to checkout 💳' },
        'cart.continue': { es: '← Continuar Comprando', en: '← Continue Shopping' },
        'cart.brand': { es: 'Marca:', en: 'Brand:' },

        // ════════════════════════════════════════════
        //  LANGUAGE SWITCHER
        // ════════════════════════════════════════════
        'lang.change': { es: 'Cambiar idioma', en: 'Change language' },
    };

    // ── Funciones principales ──

    /**
     * Obtener el idioma actual desde localStorage (por defecto español)
     */
    function getCurrentLang() {
        return localStorage.getItem('lentsoft_lang') || 'es';
    }

    /**
     * Aplicar traducciones a todos los elementos con data-i18n
     */
    function applyTranslations(lang) {
        // Traducir contenido de texto
        document.querySelectorAll('[data-i18n]').forEach(function (el) {
            var key = el.getAttribute('data-i18n');
            if (translations[key] && translations[key][lang]) {
                el.textContent = translations[key][lang];
            }
        });

        // Traducir placeholders de inputs
        document.querySelectorAll('[data-i18n-placeholder]').forEach(function (el) {
            var key = el.getAttribute('data-i18n-placeholder');
            if (translations[key] && translations[key][lang]) {
                el.placeholder = translations[key][lang];
            }
        });

        // Traducir atributos title
        document.querySelectorAll('[data-i18n-title]').forEach(function (el) {
            var key = el.getAttribute('data-i18n-title');
            if (translations[key] && translations[key][lang]) {
                el.title = translations[key][lang];
            }
        });

        // Actualizar el atributo lang del HTML
        document.documentElement.lang = lang === 'es' ? 'es' : 'en';

        // Actualizar indicador visual del idioma seleccionado
        updateLangIndicator(lang);
    }

    /**
     * Cambiar idioma y guardar preferencia
     */
    function setLanguage(lang) {
        localStorage.setItem('lentsoft_lang', lang);
        applyTranslations(lang);
        closeLangDropdown();
    }

    /**
     * Toggle dropdown del selector de idioma
     */
    function toggleLangDropdown() {
        var dropdown = document.getElementById('lang-dropdown');
        if (dropdown) {
            var isVisible = dropdown.style.display === 'block';
            dropdown.style.display = isVisible ? 'none' : 'block';
        }
    }

    /**
     * Cerrar dropdown
     */
    function closeLangDropdown() {
        var dropdown = document.getElementById('lang-dropdown');
        if (dropdown) {
            dropdown.style.display = 'none';
        }
    }

    /**
     * Actualizar indicador visual (radio buttons del dropdown)
     */
    function updateLangIndicator(lang) {
        var radios = document.querySelectorAll('.lang-radio');
        radios.forEach(function (radio) {
            if (radio.getAttribute('data-lang') === lang) {
                radio.style.background = '#f97316';
                radio.style.borderColor = '#f97316';
            } else {
                radio.style.background = 'white';
                radio.style.borderColor = '#d1d5db';
            }
        });

        // Actualizar texto del botón indicador
        var indicator = document.getElementById('lang-current');
        if (indicator) {
            indicator.textContent = lang.toUpperCase();
        }
    }

    // ── Cerrar dropdown al hacer clic fuera ──
    document.addEventListener('click', function (e) {
        var switcher = document.getElementById('language-switcher');
        if (switcher && !switcher.contains(e.target)) {
            closeLangDropdown();
        }
    });

    // ── Exponer funciones globalmente ──
    window.setLanguage = setLanguage;
    window.toggleLangDropdown = toggleLangDropdown;

    // ── Inicialización al cargar la página ──
    document.addEventListener('DOMContentLoaded', function () {
        var lang = getCurrentLang();
        applyTranslations(lang);
    });

})();
