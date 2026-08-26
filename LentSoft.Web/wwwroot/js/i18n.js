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
    window.getCurrentLang = getCurrentLang;
    window.applyTranslations = applyTranslations;
    window.setLanguage = setLanguage;
    window.toggleLangDropdown = toggleLangDropdown;

    // ── Inicialización al cargar la página ──
    document.addEventListener('DOMContentLoaded', function () {
        var lang = getCurrentLang();
        applyTranslations(lang);
    });

})();
