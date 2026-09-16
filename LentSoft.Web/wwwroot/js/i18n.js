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
        //  VENDEDOR (SALES DASHBOARD)
        // ════════════════════════════════════════════
        'sales.portal': { es: 'Portal de Ventas', en: 'Sales Portal' },
        'sales.management': { es: 'Gestión Comercial', en: 'Commercial Management' },
        'sales.nav.general': { es: '📊 General', en: '📊 General' },
        'sales.nav.sales': { es: '💳 Ventas', en: '💳 Sales' },
        'sales.nav.invoices': { es: '🧾 Facturas', en: '🧾 Invoices' },
        'sales.nav.inventory': { es: '📦 Inventarios', en: '📦 Inventory' },
        'sales.nav.profile': { es: '⚙️ Mi Perfil', en: '⚙️ My Profile' },

        'sales.general.title': { es: 'Resumen Comercial', en: 'Commercial Summary' },
        'sales.general.monthly': { es: 'Ventas del Mes', en: 'Monthly Sales' },
        'sales.general.activeorders': { es: 'Pedidos Activos', en: 'Active Orders' },
        'sales.general.clients': { es: 'Clientes Atendidos', en: 'Served Clients' },
        'sales.general.averageticket': { es: 'Ticket Promedio', en: 'Average Ticket' },
        'sales.general.recentsales': { es: 'Últimas Ventas Registradas', en: 'Recent Sales Registered' },

        'sales.table.order': { es: 'Pedido', en: 'Order' },
        'sales.table.client': { es: 'Cliente', en: 'Client' },
        'sales.table.date': { es: 'Fecha', en: 'Date' },
        'sales.table.total': { es: 'Total', en: 'Total' },
        'sales.table.status': { es: 'Estado', en: 'Status' },
        'sales.table.currentstatus': { es: 'Estado Actual', en: 'Current Status' },
        'sales.table.action': { es: 'Acción', en: 'Action' },
        'sales.table.actions': { es: 'Acciones', en: 'Actions' },

        'sales.management.title': { es: 'Gestión de Ventas', en: 'Sales Management' },
        'sales.btn.new': { es: '+ Registrar Nueva Venta', en: '+ Register New Sale' },
        'sales.search.placeholder': { es: 'Buscar venta o cliente...', en: 'Search sale or client...' },
        'sales.btn.update': { es: 'Actualizar', en: 'Update' },
        'sales.status.pending': { es: 'Pendiente', en: 'Pending' },
        'sales.status.processing': { es: 'Procesando', en: 'Processing' },
        'sales.status.shipped': { es: 'Enviado', en: 'Shipped' },
        'sales.status.delivered': { es: 'Entregado', en: 'Delivered' },
        'sales.status.cancelled': { es: 'Cancelado', en: 'Cancelled' },

        'sales.invoices.title': { es: 'Facturas', en: 'Invoices' },
        'sales.invoices.subtitle': { es: 'Gestiona tu negocio desde un solo lugar', en: 'Manage your business from one place' },
        'sales.invoices.mgmt': { es: 'Gestión de Facturas', en: 'Invoice Management' },
        'sales.invoices.new': { es: '+ Nueva Factura', en: '+ New Invoice' },
        'sales.invoices.show': { es: 'Mostrar', en: 'Show' },
        'sales.invoices.records': { es: 'registros', en: 'records' },
        'sales.invoices.searchlabel': { es: 'Buscar:', en: 'Search:' },
        'sales.invoices.searchplaceholder': { es: 'Buscar por número, cliente', en: 'Search by number, client' },
        'sales.invoices.filter': { es: 'Filtrar', en: 'Filter' },
        'sales.invoices.number': { es: 'Número', en: 'Number' },
        'sales.invoices.paid': { es: 'Pagada', en: 'Paid' },
        'sales.invoices.delete': { es: '🗑️ Eliminar', en: '🗑️ Delete' },
        'sales.invoices.pdf': { es: '📄 PDF', en: '📄 PDF' },
        'sales.invoices.empty': { es: 'No se encontraron facturas registradas o que coincidan con la búsqueda.', en: 'No registered invoices found matching your search.' },
        'sales.invoices.showing': { es: 'Mostrando registros del', en: 'Showing records from' },
        'sales.invoices.to': { es: 'al', en: 'to' },
        'sales.invoices.of': { es: 'de un total de', en: 'of a total of' },
        'sales.invoices.prev': { es: '« Anterior', en: '« Previous' },
        'sales.invoices.next': { es: 'Siguiente »', en: 'Next »' },

        'sales.inventory.title': { es: 'Consulta de Inventario (Solo Lectura)', en: 'Inventory Consultation (Read Only)' },
        'sales.inventory.searchplaceholder': { es: 'Buscar producto en inventario...', en: 'Search product in inventory...' },
        'sales.inventory.product': { es: 'Producto', en: 'Product' },
        'sales.inventory.category': { es: 'Categoría', en: 'Category' },
        'sales.inventory.brand': { es: 'Marca', en: 'Brand' },
        'sales.inventory.price': { es: 'Precio', en: 'Price' },
        'sales.inventory.stock': { es: 'Stock Disponible', en: 'Available Stock' },
        'sales.inventory.available': { es: 'Disponible', en: 'Available' },
        'sales.inventory.soldout': { es: 'Agotado/Inactivo', en: 'Sold Out/Inactive' },

        'sales.profile.title': { es: 'Perfil Ejecutivo de Ventas', en: 'Sales Executive Profile' },
        'sales.profile.advisor': { es: 'Asesor Comercial / Ventas', en: 'Sales Advisor / Commercial' },
        'sales.profile.email': { es: 'CORREO ELECTRÓNICO', en: 'EMAIL ADDRESS' },
        'sales.profile.phone': { es: 'TELÉFONO', en: 'PHONE NUMBER' },
        'sales.profile.nophone': { es: 'No registrado', en: 'Not registered' },
        'sales.profile.role': { es: 'ROL DE ACCESO', en: 'ACCESS ROLE' },

        'sales.modal.invoice.title': { es: 'Factura Electrónica de Venta', en: 'Electronic Sales Invoice' },
        'sales.modal.invoice.resolution': { es: 'Resolución DIAN Nº 18764028920000 | Rango: FAC-0001 a FAC-5000', en: 'DIAN Resolution No. 18764028920000 | Range: FAC-0001 to FAC-5000' },
        'sales.modal.invoice.sec1': { es: '📋 DATOS DEL PEDIDO', en: '📋 ORDER DATA' },
        'sales.modal.invoice.orderassoc': { es: 'Pedido Asociado *', en: 'Associated Order *' },
        'sales.modal.invoice.selectorder': { es: '-- Seleccionar Pedido --', en: '-- Select Order --' },
        'sales.modal.invoice.sec2': { es: '📑 DATOS DE FACTURACIÓN (NÚMERO AUTO-GENERADO)', en: '📑 BILLING DATA (AUTO-GENERATED NUMBER)' },
        'sales.modal.invoice.status': { es: 'Estado *', en: 'Status *' },
        'sales.modal.invoice.paymentmethod': { es: 'Método de Pago *', en: 'Payment Method *' },
        'sales.modal.invoice.cash': { es: 'Efectivo', en: 'Cash' },
        'sales.modal.invoice.card': { es: 'Tarjeta Débito/Crédito', en: 'Debit/Credit Card' },
        'sales.modal.invoice.transfer': { es: 'Transferencia / PSE', en: 'Bank Transfer / PSE' },
        'sales.modal.invoice.sec3': { es: '💰 VALORES (IVA 19% — ART. 468 E.T.)', en: '💰 VALUES (VAT 19% — ART. 468 E.T.)' },
        'sales.modal.invoice.subtotal': { es: 'Subtotal', en: 'Subtotal' },
        'sales.modal.invoice.tax': { es: 'IVA 19%', en: 'VAT 19%' },
        'sales.modal.invoice.total': { es: 'TOTAL', en: 'TOTAL' },
        'sales.modal.invoice.autocalc': { es: 'Auto-calculado', en: 'Auto-calculated' },
        'sales.modal.invoice.warning': { es: 'Al crear esta factura se generará un CUFE (Código Único de Factura Electrónica) y estará disponible para descarga en formato PDF conforme a la Resolución DIAN.', en: 'Creating this invoice will generate a CUFE (Unique Electronic Invoice Code) and will be available for PDF download according to DIAN Resolution.' },
        'sales.modal.invoice.cancel': { es: 'Cancelar', en: 'Cancel' },
        'sales.modal.invoice.emit': { es: '📄 Emitir Factura Electrónica', en: '📄 Issue Electronic Invoice' },

        // ════════════════════════════════════════════
        //  MODAL REGISTRAR VENTA
        // ════════════════════════════════════════════
        'modal.sale.title': { es: 'Registrar Nueva Venta', en: 'Register New Sale' },
        'modal.sale.sec.client': { es: '👤 DATOS DEL CLIENTE', en: '👤 CLIENT DATA' },
        'modal.sale.search.name': { es: '🔍 Buscar por Nombre (Clientes)', en: '🔍 Search by Name (Clients)' },
        'modal.sale.search.name.placeholder': { es: '-- Buscar por Nombre --', en: '-- Search by Name --' },
        'modal.sale.search.doc': { es: '🪪 Buscar por C.C. / Documento (Clientes)', en: '🪪 Search by ID / Document (Clients)' },
        'modal.sale.search.doc.placeholder': { es: '-- Buscar por C.C. --', en: '-- Search by ID --' },
        'modal.sale.firstname': { es: 'Nombre *', en: 'First Name *' },
        'modal.sale.lastname': { es: 'Apellido *', en: 'Last Name *' },
        'modal.sale.docnum': { es: 'N° Cédula / Documento *', en: 'ID / Document No. *' },
        'modal.sale.phone': { es: 'Teléfono *', en: 'Phone *' },
        'modal.sale.address': { es: 'Dirección *', en: 'Address *' },
        'modal.sale.sec.products': { es: '📦 AGREGAR PRODUCTOS A LA VENTA', en: '📦 ADD PRODUCTS TO SALE' },
        'modal.sale.selectproduct': { es: 'Seleccionar Producto', en: 'Select Product' },
        'modal.sale.selectproduct.placeholder': { es: '-- Seleccionar Producto --', en: '-- Select Product --' },
        'modal.sale.qty': { es: 'Cantidad', en: 'Quantity' },
        'modal.sale.additem': { es: '+ Agregar Producto', en: '+ Add Product' },
        'modal.sale.items.title': { es: '🛒 Productos en la Venta', en: '🛒 Products in Sale' },
        'modal.sale.noitems': { es: 'No hay productos agregados a esta venta.', en: 'No products added to this sale.' },
        'modal.sale.sec.summary': { es: '💳 RESUMEN Y PAGO', en: '💳 SUMMARY AND PAYMENT' },
        'modal.sale.paymentmethod': { es: 'Método de Pago *', en: 'Payment Method *' },
        'modal.sale.confirm': { es: '✓ Confirmar Venta', en: '✓ Confirm Sale' },

        // ════════════════════════════════════════════
        //  ADMIN DASHBOARD
        // ════════════════════════════════════════════
        'admin.portal': { es: 'Admin Dashboard', en: 'Admin Dashboard' },
        'admin.management': { es: 'Gestión Integral LentSoft', en: 'Comprehensive LentSoft Management' },
        'admin.nav.general': { es: '📊 General', en: '📊 General' },
        'admin.nav.inventory': { es: '📦 Inventario', en: '📦 Inventory' },
        'admin.nav.sales': { es: '💳 Ventas', en: '💳 Sales' },
        'admin.nav.appointments': { es: '📅 Citas Médicas', en: '📅 Eye Appointments' },
        'admin.nav.users': { es: '👥 Usuarios', en: '👥 Users' },
        'admin.nav.invoices': { es: '🧾 Facturas', en: '🧾 Invoices' },

        'admin.general.title': { es: 'Resumen General', en: 'General Overview' },
        'admin.general.monthly': { es: 'Ventas del Mes', en: 'Monthly Sales' },
        'admin.general.activeorders': { es: 'Pedidos Activos', en: 'Active Orders' },
        'admin.general.totalclients': { es: 'Clientes Totales', en: 'Total Clients' },
        'admin.general.instock': { es: 'Productos en Stock', en: 'Products in Stock' },
        'admin.general.recentorders': { es: 'Pedidos Recientes', en: 'Recent Orders' },
        'admin.general.searchorder': { es: 'Buscar pedido...', en: 'Search order...' },

        'admin.inv.title': { es: 'Gestión de Inventario', en: 'Inventory Management' },
        'admin.inv.newproduct': { es: '+ Nuevo Producto', en: '+ New Product' },
        'admin.inv.newsupplier': { es: '+ Nuevo Proveedor', en: '+ New Supplier' },
        'admin.inv.newmovement': { es: '+ Nuevo Movimiento', en: '+ New Movement' },
        'admin.inv.tab.products': { es: 'Productos', en: 'Products' },
        'admin.inv.tab.suppliers': { es: 'Proveedores', en: 'Suppliers' },
        'admin.inv.tab.history': { es: 'Historial de Movimientos', en: 'Movement History' },
        'admin.inv.searchproduct': { es: 'Buscar producto...', en: 'Search product...' },
        'admin.inv.searchsupplier': { es: 'Buscar proveedor...', en: 'Search supplier...' },
        'admin.inv.searchhistory': { es: 'Buscar en historial...', en: 'Search in history...' },
        'admin.inv.active': { es: 'Activo', en: 'Active' },
        'admin.inv.inactive': { es: 'Inactivo', en: 'Inactive' },
        'admin.inv.edit': { es: '✏️ Editar', en: '✏️ Edit' },
        'admin.inv.delete': { es: 'Eliminar', en: 'Delete' },
        'admin.inv.company': { es: 'Empresa', en: 'Company' },
        'admin.inv.producttype': { es: 'Tipo Productos', en: 'Product Type' },
        'admin.inv.phone': { es: 'Teléfono', en: 'Phone' },
        'admin.inv.email': { es: 'Email', en: 'Email' },
        'admin.inv.type': { es: 'Tipo', en: 'Type' },
        'admin.inv.qty': { es: 'Cantidad', en: 'Quantity' },
        'admin.inv.date': { es: 'Fecha', en: 'Date' },
        'admin.inv.responsible': { es: 'Responsable', en: 'Responsible' },
        'admin.inv.inflow': { es: 'ENTRADA', en: 'INFLOW' },
        'admin.inv.outflow': { es: 'SALIDA', en: 'OUTFLOW' },

        'admin.sales.title': { es: 'Listado de Ventas', en: 'Sales List' },
        'admin.sales.search': { es: 'Buscar venta o cliente...', en: 'Search sale or client...' },
        'admin.sales.save': { es: 'Guardar', en: 'Save' },

        'admin.citas.title': { es: 'Gestión de Citas Médicas', en: 'Eye Appointments Management' },
        'admin.citas.new': { es: '+ Nueva Cita', en: '+ New Appointment' },
        'admin.citas.search': { es: 'Buscar cita o paciente...', en: 'Search appointment or patient...' },
        'admin.citas.patient': { es: 'Paciente', en: 'Patient' },
        'admin.citas.service': { es: 'Servicio', en: 'Service' },
        'admin.citas.datetime': { es: 'Fecha y Hora', en: 'Date & Time' },
        'admin.citas.confirmed': { es: 'Confirmada', en: 'Confirmed' },
        'admin.citas.completed': { es: 'Completada', en: 'Completed' },

        'admin.users.title': { es: 'Gestión de Usuarios', en: 'User Management' },
        'admin.users.subtitle': { es: 'Administra los clientes y empleados del sistema', en: 'Manage system clients and employees' },
        'admin.users.tab.clients': { es: '👥 Clientes', en: '👥 Clients' },
        'admin.users.tab.employees': { es: '👔 Trabajadores (Empleados)', en: '👔 Workers (Employees)' },
        'admin.users.regclients': { es: 'Clientes Registrados', en: 'Registered Clients' },
        'admin.users.newuser': { es: '+ Nuevo Usuario', en: '+ New User' },
        'admin.users.newemployee': { es: '+ Nuevo Trabajador', en: '+ New Worker' },
        'admin.users.showinactive': { es: 'Mostrar inactivos', en: 'Show inactive' },
        'admin.users.searchclients': { es: 'Buscar por nombre o email', en: 'Search by name or email' },
        'admin.users.reactivate': { es: '🔄 Reactivar', en: '🔄 Reactivate' },
        'admin.users.regdate': { es: 'Fecha Registro', en: 'Registration Date' },
        'admin.users.orderscount': { es: 'Pedidos', en: 'Orders' },
        'admin.users.role.admin': { es: 'Administrador', en: 'Administrator' },
        'admin.users.role.seller': { es: 'Vendedor', en: 'Salesperson' },
        'admin.users.role.worker': { es: 'Trabajador', en: 'Worker' },
        'admin.users.role.readonly': { es: 'Solo Lectura', en: 'Read Only' },
        'admin.users.noresults': { es: 'No se encontraron clientes registrados o que coincidan con la búsqueda.', en: 'No registered clients found matching your search.' },
        'admin.users.noemployees': { es: 'No se encontraron trabajadores registrados o que coincidan con la búsqueda.', en: 'No registered workers found matching your search.' },

        // Modals Admin
        'admin.modal.product.new': { es: 'Nuevo Producto', en: 'New Product' },
        'admin.modal.product.edit': { es: '✏️ Editar Producto', en: '✏️ Edit Product' },
        'admin.modal.product.name': { es: 'Nombre del Producto *', en: 'Product Name *' },
        'admin.modal.product.price': { es: 'Precio Regular *', en: 'Regular Price *' },
        'admin.modal.product.discountprice': { es: 'Precio Descuento (Opcional)', en: 'Discount Price (Optional)' },
        'admin.modal.product.category': { es: 'Categoría *', en: 'Category *' },
        'admin.modal.product.brand': { es: 'Marca', en: 'Brand' },
        'admin.modal.product.stock': { es: 'Stock Inicial *', en: 'Initial Stock *' },
        'admin.modal.product.imgurl': { es: 'URL de la Imagen', en: 'Image URL' },
        'admin.modal.product.description': { es: 'Descripción', en: 'Description' },
        'admin.modal.product.save': { es: 'Guardar Producto', en: 'Save Product' },
        'admin.modal.product.update': { es: 'Actualizar Producto', en: 'Update Product' },

        'admin.modal.cita.title': { es: 'Programar Cita', en: 'Schedule Appointment' },
        'admin.modal.cita.selectpatient': { es: 'Seleccionar Paciente', en: 'Select Patient' },
        'admin.modal.cita.service': { es: 'Servicio', en: 'Service' },
        'admin.modal.cita.datetime': { es: 'Fecha y Hora', en: 'Date and Time' },
        'admin.modal.cita.notes': { es: 'Notas Adicionales', en: 'Additional Notes' },
        'admin.modal.cita.create': { es: 'Crear Cita', en: 'Create Appointment' },

        'admin.modal.client.new': { es: '+ Nuevo Cliente', en: '+ New Client' },
        'admin.modal.client.edit': { es: '✏️ Editar Cliente', en: '✏️ Edit Client' },
        'admin.modal.client.doctype': { es: 'Tipo Doc.', en: 'Doc. Type' },
        'admin.modal.client.docnum': { es: 'Núm. Documento', en: 'Doc. Number' },
        'admin.modal.client.password': { es: 'Contraseña (Opcional, defecto: user123)', en: 'Password (Optional, default: user123)' },
        'admin.modal.client.save': { es: 'Guardar Cliente', en: 'Save Client' },
        'admin.modal.client.update': { es: 'Actualizar Cliente', en: 'Update Client' },

        'admin.modal.worker.new': { es: '+ Nuevo Trabajador', en: '+ New Worker' },
        'admin.modal.worker.edit': { es: '✏️ Editar Trabajador', en: '✏️ Edit Worker' },
        'admin.modal.worker.fullname': { es: 'Nombre Completo *', en: 'Full Name *' },
        'admin.modal.worker.position': { es: 'Puesto', en: 'Position' },
        'admin.modal.worker.department': { es: 'Departamento', en: 'Department' },
        'admin.modal.worker.salary': { es: 'Salario', en: 'Salary' },
        'admin.modal.worker.role': { es: 'Rol *', en: 'Role *' },
        'admin.modal.worker.save': { es: 'Guardar Trabajador', en: 'Save Worker' },
        'admin.modal.worker.update': { es: 'Actualizar Trabajador', en: 'Update Worker' },

        'admin.modal.supplier.new': { es: '+ Nuevo Proveedor', en: '+ New Supplier' },
        'admin.modal.supplier.edit': { es: '✏️ Editar Proveedor', en: '✏️ Edit Supplier' },
        'admin.modal.supplier.code': { es: 'Código de Proveedor (Opcional, ej: PROV001)', en: 'Supplier Code (Optional, e.g. PROV001)' },
        'admin.modal.supplier.company': { es: 'Empresa / Proveedor *', en: 'Company / Supplier *' },
        'admin.modal.supplier.producttype': { es: 'Tipo de Productos *', en: 'Product Type *' },
        'admin.modal.supplier.phone': { es: 'Teléfono *', en: 'Phone *' },
        'admin.modal.supplier.email': { es: 'Correo Electrónico *', en: 'Email Address *' },
        'admin.modal.supplier.save': { es: 'Guardar Proveedor', en: 'Save Supplier' },
        'admin.modal.supplier.update': { es: 'Actualizar Proveedor', en: 'Update Supplier' },

        'admin.modal.movement.new': { es: '+ Registrar Movimiento de Inventario', en: '+ Register Inventory Movement' },
        'admin.modal.movement.product': { es: 'Producto *', en: 'Product *' },
        'admin.modal.movement.selectproduct': { es: '-- Seleccionar Producto --', en: '-- Select Product --' },
        'admin.modal.movement.type': { es: 'Tipo de Movimiento *', en: 'Movement Type *' },
        'admin.modal.movement.in': { es: 'Entrada (Sumar a Stock)', en: 'Inflow (Add to Stock)' },
        'admin.modal.movement.out': { es: 'Salida (Restar de Stock)', en: 'Outflow (Deduct from Stock)' },
        'admin.modal.movement.qty': { es: 'Cantidad *', en: 'Quantity *' },
        'admin.modal.movement.resp': { es: 'Responsable (Opcional)', en: 'Responsible Person (Optional)' },
        'admin.modal.movement.save': { es: 'Guardar Movimiento', en: 'Save Movement' },

        // ════════════════════════════════════════════
        //  CONFIRMACIÓN DE COMPRA
        // ════════════════════════════════════════════
        'confirmation.title': { es: 'Resumen Oficial de Transacción', en: 'Official Transaction Summary' },
        'confirmation.expired': { es: 'Enlace Expirado o Inválido', en: 'Link Expired or Invalid' },
        'confirmation.home': { es: 'Ir al Inicio', en: 'Go to Home' },
        'confirmation.thankyou': { es: '¡Gracias por tu confianza!', en: 'Thank you for your trust!' },
        'confirmation.registered': { es: 'Tu venta se encuentra registrada y procesada en el sistema.', en: 'Your sale is registered and processed in the system.' },
        'confirmation.clientinfo': { es: 'Datos del Cliente', en: 'Client Details' },
        'confirmation.orderinfo': { es: 'Datos del Pedido', en: 'Order Details' },
        'confirmation.product': { es: 'Producto', en: 'Product' },
        'confirmation.qty': { es: 'Cant', en: 'Qty' },
        'confirmation.unitprice': { es: 'Precio U.', en: 'Unit Price' },
        'confirmation.subtotal': { es: 'Subtotal', en: 'Subtotal' },
        'confirmation.discount': { es: 'Descuento', en: 'Discount' },
        'confirmation.totalpaid': { es: 'Total Pagado:', en: 'Total Paid:' },
        'confirmation.paymentmethod': { es: 'Método de pago:', en: 'Payment method:' },
        'confirmation.salestatus': { es: 'Estado de la venta:', en: 'Sale status:' },

        // ════════════════════════════════════════════
        //  TIENDA / SHOP CATEGORIES
        // ════════════════════════════════════════════
        'shop.cat.sun': { es: 'Gafas de Sol', en: 'Sunglasses' },
        'shop.cat.contact': { es: 'Lentes de Contacto', en: 'Contact Lenses' },
        'shop.cat.grad': { es: 'Lentes Graduados', en: 'Prescription Glasses' },
        'shop.cat.frames': { es: 'Monturas', en: 'Frames' },
        'shop.cat.accessories': { es: 'Accesorios', en: 'Accessories' },

        // ════════════════════════════════════════════
        //  PRODUCT DETAILS & SPECIFICATIONS
        // ════════════════════════════════════════════
        'product.description': { es: 'Descripción', en: 'Description' },
        'product.reviews': { es: 'reseñas', en: 'reviews' },
        'product.review': { es: 'reseña', en: 'review' },
        'product.soldout.temp': { es: 'Agotado temporalmente', en: 'Temporarily Out of Stock' },
        'product.frametryon': { es: '👓 MUESTRA DE MONTURA', en: '👓 FRAME SHOWCASE' },
        'product.benefit.warranty.title': { es: 'Garantía incluida', en: 'Warranty included' },
        'product.benefit.warranty.desc': { es: 'Cobertura de 12 meses ante defectos de fábrica en todas tus monturas.', en: '12-month coverage against factory defects on all your frames.' },
        'product.benefit.shipping.title': { es: 'Envío gratis', en: 'Free shipping' },
        'product.benefit.shipping.desc': { es: 'Recibe tu pedido a domicilio en un plazo de 3 a 5 días hábiles sin costo.', en: 'Get your order delivered to your door within 3 to 5 business days at no cost.' },
        'product.benefit.quality.title': { es: 'Calidad garantizada', en: 'Guaranteed quality' },
        'product.benefit.quality.desc': { es: 'Materiales duraderos, cómodos y avalados por profesionales de la salud visual.', en: 'Durable, comfortable materials endorsed by eye health professionals.' },
        'product.specs.title': { es: 'Detalles de la Montura', en: 'Frame Details' },
        'product.specs.material': { es: 'Material', en: 'Material' },
        'product.specs.category': { es: 'Categoría', en: 'Category' },
        'product.specs.color': { es: 'Color', en: 'Color' },
        'product.specs.protection': { es: 'Protección', en: 'Protection' },
        'product.specs.style': { es: 'Estilo', en: 'Style' },
        'product.specs.size': { es: 'Tamaño (Montura)', en: 'Size (Frame)' },
        'product.fav.add': { es: 'Agregar a favoritos', en: 'Add to favorites' },
        'product.fav.remove': { es: 'Quitar de favoritos', en: 'Remove from favorites' },
        'product.fav.login': { es: 'Inicia sesión para guardar favoritos', en: 'Sign in to save favorites' },
        'product.fav.added': { es: '❤️ Añadido a favoritos', en: '❤️ Added to favorites' },
        'product.fav.removed': { es: '🤍 Quitado de favoritos', en: '🤍 Removed from favorites' },

        // ════════════════════════════════════════════
        //  MUESTRA DE MONTURA (AR / TRY-ON)
        // ════════════════════════════════════════════
        'montura.title': { es: 'Catálogo y Vista de Marcos', en: 'Frame Catalog & Virtual Try-On' },
        'montura.subtitle': { es: 'Prueba virtual con realidad aumentada - Encuentra el marco perfecto para ti', en: 'Virtual try-on with augmented reality - Find the perfect frame for you' },
        'montura.preview.title': { es: 'Vista Previa Interactiva', en: 'Interactive Preview' },
        'montura.preview.instant': { es: 'Prueba virtual instantánea', en: 'Instant virtual try-on' },
        'montura.preview.desc': { es: 'Activa tu cámara web para ver cómo lucen los marcos en tu rostro.', en: 'Turn on your webcam to see how frames look on your face.' },
        'montura.camera.start': { es: 'Activar Cámara 📸', en: 'Turn On Camera 📸' },
        'montura.camera.stop': { es: 'Apagar Cámara', en: 'Turn Off Camera' },
        'montura.camera.errortitle': { es: 'Error de Acceso a la Cámara', en: 'Camera Access Error' },
        'montura.camera.errordesc': { es: 'No se pudo acceder a la cámara. Asegúrate de dar los permisos correspondientes.', en: 'Could not access camera. Please make sure to grant the necessary permissions.' },
        'montura.camera.retry': { es: 'Reintentar', en: 'Retry' },
        'montura.selected.badge': { es: 'Marco Seleccionado', en: 'Selected Frame' },
        'montura.selected.style': { es: 'Estilo:', en: 'Style:' },
        'montura.selected.size': { es: 'Medidas:', en: 'Measurements:' },
        'montura.gallery.title': { es: 'Galería de Marcos', en: 'Frame Gallery' },
        'montura.loading': { es: 'Cargando...', en: 'Loading...' },

        // ════════════════════════════════════════════
        //  CHECKOUT & CONFIRMATION
        // ════════════════════════════════════════════
        'checkout.title': { es: 'Finalizar Compra (Pago Simulado)', en: 'Checkout (Simulated Payment)' },
        'checkout.paymentmethod': { es: 'Método de Pago Simulado', en: 'Simulated Payment Method' },
        'checkout.cardnumber': { es: 'Número de Tarjeta', en: 'Card Number' },
        'checkout.cardholder': { es: 'Nombre en la Tarjeta', en: 'Cardholder Name' },
        'checkout.expiration': { es: 'Fecha de Expiración', en: 'Expiration Date' },
        'checkout.cvv': { es: 'CVV', en: 'CVV' },
        'checkout.address': { es: 'Dirección de Envío', en: 'Shipping Address' },
        'checkout.address.placeholder': { es: 'Ingresa tu dirección completa de entrega', en: 'Enter your full shipping address' },
        'checkout.secure.title': { es: 'Entorno Seguro de Simulación:', en: 'Simulated Secure Environment:' },
        'checkout.secure.desc': { es: 'Las transacciones bancarias son simuladas internamente. Ningún dato real de tarjeta de crédito será enviado a procesadores externos ni validado contra instituciones bancarias.', en: 'Bank transactions are simulated internally. No real credit card information is sent to external processors or validated with banks.' },
        'checkout.submit': { es: 'Confirmar Pago 💳', en: 'Confirm Payment 💳' },
        'checkout.ordersummary': { es: 'Resumen del Pedido', en: 'Order Summary' },
        'checkout.success.title': { es: '¡Pago simulado exitosamente!', en: 'Payment Simulated Successfully!' },
        'checkout.success.desc': { es: 'Tu pedido ha sido procesado de forma simulada. A continuación se detallan los datos de tu orden de compra.', en: 'Your simulated order has been processed. The details of your purchase order are shown below.' },
        'checkout.order.number': { es: 'Número de Orden', en: 'Order Number' },
        'checkout.order.date': { es: 'Fecha del pedido', en: 'Order Date' },
        'checkout.order.status': { es: 'Estado', en: 'Status' },
        'checkout.order.items': { es: 'Artículos Adquiridos', en: 'Purchased Items' },
        'checkout.order.total': { es: 'Total Pagado', en: 'Total Paid' },
        'checkout.order.back': { es: 'Volver a la Tienda 👓', en: 'Back to Shop 👓' },

        // ════════════════════════════════════════════
        //  USUARIO (USER DASHBOARD)
        // ════════════════════════════════════════════
        'user.portal': { es: 'Mi Cuenta', en: 'My Account' },
        'user.nav.profile': { es: '👤 Mi Perfil', en: '👤 My Profile' },
        'user.nav.medical': { es: '📑 Historial Médico', en: '📑 Medical History' },
        'user.nav.orders': { es: '📦 Mis Pedidos', en: '📦 My Orders' },
        'user.nav.appointments': { es: '🗓️ Mis Citas', en: '🗓️ My Appointments' },
        'user.nav.favorites': { es: '❤️ Mis Favoritos', en: '❤️ My Favorites' },
        'user.nav.settings': { es: '⚙️ Configuración', en: '⚙️ Settings' },

        'user.profile.title': { es: 'Información Personal', en: 'Personal Information' },
        'user.profile.fullname': { es: 'Nombre Completo', en: 'Full Name' },
        'user.profile.email': { es: 'Correo Electrónico (Solo Lectura)', en: 'Email Address (Read Only)' },
        'user.profile.phone': { es: 'Teléfono', en: 'Phone Number' },
        'user.profile.update': { es: 'Actualizar Perfil', en: 'Update Profile' },

        'user.medical.title': { es: '📑 Historial Médico de Citas', en: '📑 Medical Appointment History' },
        'user.medical.empty': { es: 'No tienes registros de historial médico o fórmulas ópticas registradas.', en: 'You have no medical history or optical prescription records registered.' },
        'user.medical.attendedby': { es: 'Atendido por:', en: 'Attended by:' },
        'user.medical.completed': { es: '✓ Completada', en: '✓ Completed' },
        'user.medical.diagnosis': { es: '👁️ Diagnóstico', en: '👁️ Diagnosis' },
        'user.medical.prescription': { es: 'Fórmula Prescrita', en: 'Prescribed Formula' },
        'user.medical.od': { es: 'Ojo Derecho (OD)', en: 'Right Eye (OD)' },
        'user.medical.oi': { es: 'Ojo Izquierdo (OI)', en: 'Left Eye (OS)' },
        'user.medical.sphere': { es: 'Esfera:', en: 'Sphere:' },
        'user.medical.cylinder': { es: 'Cilindro:', en: 'Cylinder:' },
        'user.medical.axis': { es: 'Eje:', en: 'Axis:' },
        'user.medical.dp': { es: 'Distancia Pupilar (DP):', en: 'Pupillary Distance (PD):' },
        'user.medical.observations': { es: 'Observaciones y Recomendaciones', en: 'Observations and Recommendations' },

        'user.orders.title': { es: 'Historial de Pedidos', en: 'Order History' },
        'user.orders.empty': { es: 'Aún no has realizado ningún pedido.', en: "You haven't placed any orders yet." },
        'user.orders.goshop': { es: 'Ir a la Tienda', en: 'Go to Shop' },
        'user.orders.orderprefix': { es: 'Pedido', en: 'Order' },
        'user.orders.placedon': { es: 'Realizado el:', en: 'Placed on:' },
        'user.orders.total': { es: 'Total:', en: 'Total:' },
        'user.orders.viewdetail': { es: '👁️ Ver Detalle', en: '👁️ View Details' },
        'user.orders.tracking': { es: 'Seguimiento de Entrega', en: 'Delivery Tracking' },
        'user.orders.products': { es: 'Productos del Pedido', en: 'Order Products' },
        'user.orders.product': { es: 'Producto', en: 'Product' },
        'user.orders.qty': { es: 'Cantidad', en: 'Quantity' },
        'user.orders.unitprice': { es: 'Precio Unit.', en: 'Unit Price' },
        'user.orders.subtotal': { es: 'Subtotal', en: 'Subtotal' },
        'user.orders.nodetail': { es: 'No hay detalle de productos disponible para este pedido.', en: 'No product details available for this order.' },
        'user.orders.status.pending': { es: 'Pendiente', en: 'Pending' },
        'user.orders.status.processing': { es: 'Procesando', en: 'Processing' },
        'user.orders.status.shipped': { es: 'Enviado', en: 'Shipped' },
        'user.orders.status.delivered': { es: 'Entregado', en: 'Delivered' },
        'user.orders.status.cancelled': { es: '✖ Cancelado', en: '✖ Cancelled' },

        'user.citas.title': { es: 'Mis Citas Optométricas', en: 'My Optometric Appointments' },
        'user.citas.new': { es: '+ Agendar Nueva Cita', en: '+ Schedule New Appointment' },
        'user.citas.show': { es: 'Mostrar', en: 'Show' },
        'user.citas.records': { es: 'registros', en: 'records' },
        'user.citas.search': { es: 'Buscar:', en: 'Search:' },
        'user.citas.searchplaceholder': { es: 'Buscar por servicio o estado...', en: 'Search by service or status...' },
        'user.citas.col.service': { es: 'Servicio', en: 'Service' },
        'user.citas.col.optometrist': { es: 'Optómetra', en: 'Optometrist' },
        'user.citas.col.datetime': { es: 'Fecha y Hora', en: 'Date & Time' },
        'user.citas.col.status': { es: 'Estado', en: 'Status' },
        'user.citas.col.actions': { es: 'Acciones', en: 'Actions' },
        'user.citas.empty': { es: 'No tienes citas programadas actualmente.', en: 'You currently have no scheduled appointments.' },
        'user.citas.cancel': { es: 'Cancelar', en: 'Cancel' },
        'user.citas.reschedule': { es: 'Reprogramar', en: 'Reschedule' },
        'user.citas.alreadyrescheduled': { es: 'Ya reprogramada', en: 'Already rescheduled' },
        'user.citas.showing': { es: 'Mostrando registros del', en: 'Showing records from' },
        'user.citas.to': { es: 'al', en: 'to' },
        'user.citas.of': { es: 'de un total de', en: 'of a total of' },

        'user.fav.title': { es: '❤️ Mis Favoritos', en: '❤️ My Favorites' },
        'user.fav.empty': { es: 'Aún no tienes productos en tu lista de favoritos.', en: "You don't have any products in your favorites list yet." },
        'user.fav.explore': { es: 'Explorar la Tienda', en: 'Explore the Shop' },
        'user.fav.add': { es: '🛒 Añadir', en: '🛒 Add' },
        'user.fav.remove': { es: 'Quitar de Favoritos', en: 'Remove from Favorites' },

        'user.config.title': { es: 'Configuración de Seguridad', en: 'Security Settings' },
        'user.config.currentpwd': { es: 'Contraseña Actual', en: 'Current Password' },
        'user.config.newpwd': { es: 'Nueva Contraseña', en: 'New Password' },
        'user.config.newpwdplaceholder': { es: 'Ej: Pass1234!', en: 'E.g.: Pass1234!' },
        'user.config.pwdhint': { es: '🔒 Mín. 8 caract. (Mayúscula, minúscula, número y especial).', en: '🔒 Min. 8 chars. (Uppercase, lowercase, number and special).' },
        'user.config.confirmpwd': { es: 'Confirmar Nueva Contraseña', en: 'Confirm New Password' },
        'user.config.submit': { es: 'Cambiar Contraseña', en: 'Change Password' },

        'user.modal.agenda.title': { es: '+ Agendar Nueva Cita', en: '+ Schedule New Appointment' },
        'user.modal.agenda.service': { es: 'Servicio *', en: 'Service *' },
        'user.modal.agenda.selectservice': { es: '-- Selecciona un servicio --', en: '-- Select a service --' },
        'user.modal.agenda.datetime': { es: 'Fecha y Hora *', en: 'Date & Time *' },
        'user.modal.agenda.notes': { es: 'Notas adicionales (Opcional)', en: 'Additional Notes (Optional)' },
        'user.modal.agenda.notesplaceholder': { es: 'Ej: Presento dolor de cabeza o molestia en ojo derecho...', en: 'E.g.: I have a headache or discomfort in right eye...' },
        'user.modal.agenda.cancel': { es: 'Cancelar', en: 'Cancel' },
        'user.modal.agenda.confirm': { es: 'Confirmar Cita', en: 'Confirm Appointment' },

        'user.modal.reprog.title': { es: '🗓️ Reprogramar Cita', en: '🗓️ Reschedule Appointment' },
        'user.modal.reprog.service': { es: 'Servicio', en: 'Service' },
        'user.modal.reprog.newdatetime': { es: 'Nueva Fecha y Hora *', en: 'New Date & Time *' },
        'user.modal.reprog.cancel': { es: 'Cancelar', en: 'Cancel' },
        'user.modal.reprog.save': { es: 'Guardar Cambio', en: 'Save Changes' },

        // Servicios comunes
        'user.service.exam': { es: 'Examen visual completo', en: 'Complete eye exam' },
        'user.service.control': { es: 'Control de lentes', en: 'Lens check' },
        'user.service.first': { es: 'Primera consulta', en: 'First consultation' },
        'user.service.contact': { es: 'Adaptación de lentes de contacto', en: 'Contact lens fitting' },
        'user.service.adjustment': { es: 'Ajuste y mantenimiento de lentes', en: 'Glasses adjustment and maintenance' },

        // ════════════════════════════════════════════
        //  LANGUAGE SWITCHER
        // ════════════════════════════════════════════
        'lang.change': { es: 'Cambiar idioma', en: 'Change language' },
    };

    // ── Diccionario Dinámico de Productos y Especificaciones ──
    const productDictionary = {
        // Servicios Ópticos y Citas
        'Examen visual completo': { es: 'Examen visual completo', en: 'Complete eye exam' },
        'Examen de vista completo': { es: 'Examen de vista completo', en: 'Complete eye exam' },
        'Control de lentes': { es: 'Control de lentes', en: 'Lens check' },
        'Primera consulta': { es: 'Primera consulta', en: 'First consultation' },
        'Adaptación de lentes de contacto': { es: 'Adaptación de lentes de contacto', en: 'Contact lens fitting' },
        'Ajuste y mantenimiento de lentes': { es: 'Ajuste y mantenimiento de lentes', en: 'Glasses adjustment and maintenance' },
        'Por asignar': { es: 'Por asignar', en: 'To be assigned' },
        'Ya reprogramada': { es: 'Ya reprogramada', en: 'Already rescheduled' },

        // Estados de Citas y Pedidos
        'Pendiente': { es: 'Pendiente', en: 'Pending' },
        'Confirmada': { es: 'Confirmada', en: 'Confirmed' },
        'Completada': { es: 'Completada', en: 'Completed' },
        'Cancelada': { es: 'Cancelada', en: 'Cancelled' },
        'Cancelado': { es: 'Cancelado', en: 'Cancelled' },
        'Procesando': { es: 'Procesando', en: 'Processing' },
        'Enviado': { es: 'Enviado', en: 'Shipped' },
        'Entregado': { es: 'Entregado', en: 'Delivered' },
        'pendiente': { es: 'Pendiente', en: 'Pending' },
        'confirmada': { es: 'Confirmada', en: 'Confirmed' },
        'completada': { es: 'Completada', en: 'Completed' },
        'cancelada': { es: 'Cancelada', en: 'Cancelled' },
        'cancelado': { es: 'Cancelado', en: 'Cancelled' },
        'procesando': { es: 'Procesando', en: 'Processing' },
        'enviado': { es: 'Enviado', en: 'Shipped' },
        'entregado': { es: 'Entregado', en: 'Delivered' },
        'Pagada': { es: 'Pagada', en: 'Paid' },
        'pagada': { es: 'Pagada', en: 'Paid' },
        'Efectivo': { es: 'Efectivo', en: 'Cash' },
        'Tarjeta Débito/Crédito': { es: 'Tarjeta Débito/Crédito', en: 'Debit/Credit Card' },
        'Transferencia / PSE': { es: 'Transferencia / PSE', en: 'Transfer / PSE' },

        // Nombres de Productos
        'Lentes Ray-Ban Aviator': { es: 'Lentes Ray-Ban Aviator', en: 'Ray-Ban Aviator Glasses' },
        'Lentes de Contacto Acuvue': { es: 'Lentes de Contacto Acuvue', en: 'Acuvue Contact Lenses' },
        'Montura Oakley Sport': { es: 'Montura Oakley Sport', en: 'Oakley Sport Frame' },
        'Lentes Graduados Classic': { es: 'Lentes Graduados Classic', en: 'Classic Prescription Glasses' },
        'Estuche Premium': { es: 'Estuche Premium', en: 'Premium Case' },
        'Líquido Limpiador': { es: 'Líquido Limpiador', en: 'Cleaning Solution' },

        // Descripciones de Productos
        'Lentes de sol clásicos estilo aviador': { es: 'Lentes de sol clásicos estilo aviador', en: 'Classic aviator style sunglasses' },
        'Lentes de contacto mensuales': { es: 'Lentes de contacto mensuales', en: 'Monthly contact lenses' },
        'Montura deportiva ultraligera': { es: 'Montura deportiva ultraligera', en: 'Ultralight sport frame' },
        'Lentes graduados con diseño clásico': { es: 'Lentes graduados con diseño clásico', en: 'Prescription glasses with classic design' },
        'Estuche rígido para lentes': { es: 'Estuche rígido para lentes', en: 'Hard case for glasses' },
        'Solución limpiadora para lentes 360ml': { es: 'Solución limpiadora para lentes 360ml', en: 'Cleaning solution for lenses 360ml' },
        'Descubre la calidad y el diseño excepcional de este producto seleccionado por LentSoft para tu cuidado visual.': {
            es: 'Descubre la calidad y el diseño excepcional de este producto seleccionado por LentSoft para tu cuidado visual.',
            en: 'Discover the exceptional quality and design of this product selected by LentSoft for your eye care.'
        },
        'Excelente diseño y materiales de alta calidad, seleccionados para garantizar tu bienestar visual.': {
            es: 'Excelente diseño y materiales de alta calidad, seleccionados para garantizar tu bienestar visual.',
            en: 'Excellent design and high quality materials, selected to ensure your visual well-being.'
        },

        // Materiales
        'Metal': { es: 'Metal', en: 'Metal' },
        'Acetato': { es: 'Acetato', en: 'Acetate' },
        'O-Matter (Plástico)': { es: 'O-Matter (Plástico)', en: 'O-Matter (Plastic)' },
        'Plástico': { es: 'Plástico', en: 'Plastic' },
        'Titanio': { es: 'Titanio', en: 'Titanium' },
        'Policarbonato': { es: 'Policarbonato', en: 'Polycarbonate' },
        'Silicona': { es: 'Silicona', en: 'Silicone' },
        'Aluminio': { es: 'Aluminio', en: 'Aluminum' },
        'TR-90': { es: 'TR-90', en: 'TR-90' },

        // Colores
        'Negro / Verde G-15': { es: 'Negro / Verde G-15', en: 'Black / G-15 Green' },
        'Negro Mate': { es: 'Negro Mate', en: 'Matte Black' },
        'Carey': { es: 'Carey', en: 'Tortoise' },
        'Negro': { es: 'Negro', en: 'Black' },
        'Dorado': { es: 'Dorado', en: 'Gold' },
        'Plateado': { es: 'Plateado', en: 'Silver' },
        'Azul': { es: 'Azul', en: 'Blue' },
        'Rojo': { es: 'Rojo', en: 'Red' },
        'Transparente': { es: 'Transparente', en: 'Transparent' },
        'Gris': { es: 'Gris', en: 'Gray' },
        'Marrón': { es: 'Marrón', en: 'Brown' },
        'Rosa': { es: 'Rosa', en: 'Pink' },

        // Protecciones
        'UV400': { es: 'UV400', en: 'UV400' },
        'Filtro UV': { es: 'Filtro UV', en: 'UV Filter' },
        'Antirreflejo / Luz Azul': { es: 'Antirreflejo / Luz Azul', en: 'Anti-reflective / Blue Light' },
        'Luz Azul': { es: 'Luz Azul', en: 'Blue Light' },
        'Antirreflejo': { es: 'Antirreflejo', en: 'Anti-reflective' },
        'Polarizado': { es: 'Polarizado', en: 'Polarized' },
        '100% UV': { es: '100% UV', en: '100% UV' },

        // Estilos
        'Aviador': { es: 'Aviador', en: 'Aviator' },
        'Deportivo': { es: 'Deportivo', en: 'Sport' },
        'Wayfarer': { es: 'Wayfarer', en: 'Wayfarer' },
        'Clásico': { es: 'Clásico', en: 'Classic' },
        'Redondo': { es: 'Redondo', en: 'Round' },
        'Cuadrado': { es: 'Cuadrado', en: 'Square' },
        'Cat Eye': { es: 'Cat Eye', en: 'Cat Eye' },
        'Ojo de Gato': { es: 'Ojo de Gato', en: 'Cat Eye' },
        'Rectangular': { es: 'Rectangular', en: 'Rectangular' },
        'Ovalado': { es: 'Ovalado', en: 'Oval' },
        'Geométrico': { es: 'Geométrico', en: 'Geometric' },

        // Categorías
        'lentes-sol': { es: 'Gafas de Sol', en: 'Sunglasses' },
        'lentes sol': { es: 'Gafas de Sol', en: 'Sunglasses' },
        'Gafas de Sol': { es: 'Gafas de Sol', en: 'Sunglasses' },
        'Lentes de Sol': { es: 'Lentes de Sol', en: 'Sunglasses' },
        'lentes-contacto': { es: 'Lentes de Contacto', en: 'Contact Lenses' },
        'lentes contacto': { es: 'Lentes de Contacto', en: 'Contact Lenses' },
        'Lentes de Contacto': { es: 'Lentes de Contacto', en: 'Contact Lenses' },
        'lentes-graduados': { es: 'Lentes Graduados', en: 'Prescription Glasses' },
        'lentes graduados': { es: 'Lentes Graduados', en: 'Prescription Glasses' },
        'Lentes Graduados': { es: 'Lentes Graduados', en: 'Prescription Glasses' },
        'monturas': { es: 'Monturas', en: 'Frames' },
        'Monturas': { es: 'Monturas', en: 'Frames' },
        'accesorios': { es: 'Accesorios', en: 'Accessories' },
        'Accesorios': { es: 'Accesorios', en: 'Accessories' },

        // Valores por defecto / Genéricos
        'No especificado': { es: 'No especificado', en: 'Not specified' },
        'Estándar': { es: 'Estándar', en: 'Standard' },

        // Beneficios y UI de detalles
        'Garantía incluida': { es: 'Garantía incluida', en: 'Warranty included' },
        'Cobertura de 12 meses ante defectos de fábrica en todas tus monturas.': {
            es: 'Cobertura de 12 meses ante defectos de fábrica en todas tus monturas.',
            en: '12-month coverage against factory defects on all your frames.'
        },
        'Envío gratis': { es: 'Envío gratis', en: 'Free shipping' },
        'Recibe tu pedido a domicilio en un plazo de 3 a 5 días hábiles sin costo.': {
            es: 'Recibe tu pedido a domicilio en un plazo de 3 a 5 días hábiles sin costo.',
            en: 'Get your order delivered to your door within 3 to 5 business days at no cost.'
        },
        'Calidad garantizada': { es: 'Calidad garantizada', en: 'Guaranteed quality' },
        'Materiales duraderos, cómodos y avalados por profesionales de la salud visual.': {
            es: 'Materiales duraderos, cómodos y avalados por profesionales de la salud visual.',
            en: 'Durable, comfortable materials endorsed by eye health professionals.'
        },
        'Detalles de la Montura': { es: 'Detalles de la Montura', en: 'Frame Details' },
        'MUESTRA DE MONTURA': { es: 'MUESTRA DE MONTURA', en: 'FRAME SHOWCASE' },
        '👓 MUESTRA DE MONTURA': { es: '👓 MUESTRA DE MONTURA', en: '👓 FRAME SHOWCASE' },
        'Añadir al Carrito': { es: 'Añadir al Carrito', en: 'Add to Cart' },
        '🛒 Añadir al Carrito': { es: '🛒 Añadir al Carrito', en: '🛒 Add to Cart' },
        '🛒 Agregar al Carrito': { es: '🛒 Agregar al Carrito', en: '🛒 Add to Cart' },
        'Agregar al Carrito': { es: 'Agregar al Carrito', en: 'Add to Cart' },
        'Agotado': { es: 'Agotado', en: 'Sold Out' },
        'Agotado temporalmente': { es: 'Agotado temporalmente', en: 'Temporarily Out of Stock' },
        'Descripción': { es: 'Descripción', en: 'Description' },
        'Medidas': { es: 'Medidas', en: 'Measurements' },
        'Tamaño (Montura)': { es: 'Tamaño (Montura)', en: 'Size (Frame)' },
        'Top ventas': { es: 'Top ventas', en: 'Top Sellers' },
        'Productos Más Vendidos': { es: 'Productos Más Vendidos', en: 'Best Selling Products' },
        'Productos con Descuento': { es: 'Productos con Descuento', en: 'Discounted Products' },
        'Ofertas': { es: 'Ofertas', en: 'Offers' },
        '🔥 Ofertas': { es: '🔥 Ofertas', en: '🔥 Offers' }
    };

    /**
     * Traducir texto de producto o especificación según el idioma
     */
    function translateProductText(text, lang) {
        if (!text || typeof text !== 'string') return text;
        var clean = text.trim();
        if (!clean) return text;
        
        // 1. Búsqueda directa exacta
        if (productDictionary[clean] && productDictionary[clean][lang]) {
            return productDictionary[clean][lang];
        }

        // 2. Búsqueda insensible a mayúsculas
        var lower = clean.toLowerCase();
        for (var key in productDictionary) {
            if (key.toLowerCase() === lower) {
                return productDictionary[key][lang] || text;
            }
        }

        // 3. Normalización con guiones (ej. "lentes-sol" -> "lentes sol")
        var normalizedHyphen = clean.replace(/-/g, ' ').toLowerCase();
        for (var key in productDictionary) {
            if (key.toLowerCase() === normalizedHyphen) {
                return productDictionary[key][lang] || text;
            }
        }

        // 4. Búsqueda en el diccionario general por valor en español o inglés
        for (var tKey in translations) {
            var item = translations[tKey];
            if (item.es && item.es.trim().toLowerCase() === lower) {
                return item[lang] || text;
            }
            if (item.en && item.en.trim().toLowerCase() === lower) {
                return item[lang] || text;
            }
        }

        // 5. Si no se encuentra, retornar el texto original
        return text;
    }

    // ── Funciones principales ──

    /**
     * Obtener el idioma actual desde localStorage (por defecto español)
     */
    function getCurrentLang() {
        return localStorage.getItem('lentsoft_lang') || 'es';
    }

    /**
     * Escaneo automático de nodos del DOM para traducir textos dinámicos o no etiquetados
     */
    function scanAndTranslateDynamicDOM(lang) {
        var selectors = 'h1, h2, h3, h4, h5, h6, p, span, a, strong, b, td, th, label, summary, li, button';
        var elements = document.querySelectorAll(selectors);
        
        elements.forEach(function (el) {
            // Ignorar elementos del switcher de idioma
            if (el.closest('#language-switcher')) return;

            // Si tiene data-i18n procesado estándar, omitir
            if (el.hasAttribute('data-i18n') && translations[el.getAttribute('data-i18n')]) return;

            // Revisar texto del elemento si no contiene muchos hijos complejos
            if (el.children.length === 0 || (el.children.length === 1 && (el.children[0].tagName === 'SPAN' || el.children[0].tagName === 'STRONG' || el.children[0].tagName === 'EM'))) {
                var currentText = el.textContent.trim();
                if (!currentText || currentText.length > 250) return;

                var orig = el.getAttribute('data-orig-text');
                if (!orig) {
                    // Detectar si el texto actual coincide con alguna clave de producto, spec o UI
                    var lower = currentText.toLowerCase();
                    var found = false;

                    for (var pKey in productDictionary) {
                        if (pKey.toLowerCase() === lower || productDictionary[pKey].es.toLowerCase() === lower || productDictionary[pKey].en.toLowerCase() === lower) {
                            orig = productDictionary[pKey].es; // Normalizar original a español
                            el.setAttribute('data-orig-text', orig);
                            found = true;
                            break;
                        }
                    }

                    if (!found) {
                        for (var tKey in translations) {
                            var item = translations[tKey];
                            if ((item.es && item.es.toLowerCase() === lower) || (item.en && item.en.toLowerCase() === lower)) {
                                orig = item.es;
                                el.setAttribute('data-orig-text', orig);
                                found = true;
                                break;
                            }
                        }
                    }
                }

                if (orig) {
                    var translated = translateProductText(orig, lang);
                    if (translated && el.textContent.trim() !== translated) {
                        el.textContent = translated;
                    }
                }
            }
        });
    }

    /**
     * Aplicar traducciones a todos los elementos con data-i18n y atributos de producto
     */
    function applyTranslations(lang) {
        // 1. Traducir contenido de texto directo por clave
        document.querySelectorAll('[data-i18n]').forEach(function (el) {
            var key = el.getAttribute('data-i18n');
            if (translations[key] && translations[key][lang]) {
                el.textContent = translations[key][lang];
            }
        });

        // 2. Traducir placeholders de inputs
        document.querySelectorAll('[data-i18n-placeholder]').forEach(function (el) {
            var key = el.getAttribute('data-i18n-placeholder');
            if (translations[key] && translations[key][lang]) {
                el.placeholder = translations[key][lang];
            }
        });

        // 3. Traducir atributos title
        document.querySelectorAll('[data-i18n-title]').forEach(function (el) {
            var key = el.getAttribute('data-i18n-title');
            if (translations[key] && translations[key][lang]) {
                el.title = translations[key][lang];
            }
        });

        // 4. Traducir Nombres de Productos dinámicos
        document.querySelectorAll('[data-i18n-product-name]').forEach(function (el) {
            var orig = el.getAttribute('data-orig-text');
            if (!orig) {
                orig = el.getAttribute('data-i18n-product-name') || el.textContent.trim();
                el.setAttribute('data-orig-text', orig);
            }
            el.textContent = translateProductText(orig, lang);
        });

        // 5. Traducir Descripciones de Productos dinámicas
        document.querySelectorAll('[data-i18n-product-desc]').forEach(function (el) {
            var orig = el.getAttribute('data-orig-text');
            if (!orig) {
                orig = el.getAttribute('data-i18n-product-desc') || el.textContent.trim();
                el.setAttribute('data-orig-text', orig);
            }
            var translated = translateProductText(orig, lang);
            var truncate = el.getAttribute('data-truncate');
            if (truncate) {
                var limit = parseInt(truncate, 10);
                if (translated.length > limit) {
                    translated = translated.substring(0, limit) + '…';
                }
            }
            el.textContent = translated;
        });

        // 6. Traducir Valores de Especificaciones (Material, Estilo, Color, Protección, etc.)
        document.querySelectorAll('[data-i18n-spec-val]').forEach(function (el) {
            var orig = el.getAttribute('data-orig-text');
            if (!orig) {
                orig = el.getAttribute('data-i18n-spec-val') || el.textContent.trim();
                el.setAttribute('data-orig-text', orig);
            }
            el.textContent = translateProductText(orig, lang);
        });

        // 7. Auto-escaneo inteligente de elementos en la vista
        scanAndTranslateDynamicDOM(lang);

        // Actualizar el atributo lang del HTML
        document.documentElement.lang = lang === 'es' ? 'es' : 'en';

        // Actualizar indicador visual del idioma seleccionado
        updateLangIndicator(lang);

        // Notificar a componentes interactivos del cambio de idioma
        window.dispatchEvent(new CustomEvent('languageChanged', { detail: { lang: lang } }));
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
    window.getCurrentLang = getCurrentLang;
    window.applyTranslations = applyTranslations;
    window.setLanguage = setLanguage;
    window.toggleLangDropdown = toggleLangDropdown;
    window.translateProductText = translateProductText;
    window.productDictionary = productDictionary;

    // ── Inicialización al cargar la página ──
    function initI18n() {
        var lang = getCurrentLang();
        applyTranslations(lang);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initI18n);
    } else {
        initI18n();
    }
    window.addEventListener('load', initI18n);

})();
