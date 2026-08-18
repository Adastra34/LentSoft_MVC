/**
 * morgana-faq.js
 * Base de conocimiento de Morgana — Bot FAQ de LentSoft
 * Matching simple por palabras clave (sin IA externa).
 */

const MORGANA_FAQ = [
  {
    pregunta: "¿Qué es LentSoft?",
    palabrasClave: ["lentsoft", "que es", "optica", "tienda"],
    respuesta: "¡LentSoft es tu óptica digital! 🏪 Aquí puedes comprar gafas formuladas, gafas de sol y lentes de contacto, además de probar monturas virtualmente con tu cámara y agendar citas con nuestros optómetras — todo desde un solo lugar."
  },
  {
    pregunta: "¿Cómo busco productos en la Tienda?",
    palabrasClave: ["busco", "buscar", "filtro", "categoria", "tienda", "productos"],
    respuesta: "En la sección <strong>Tienda</strong> encontrarás pestañas para filtrar por categoría: <em>Todos</em>, <em>Gafas</em> y <em>Lentes de Contacto</em>. También puedes usar los filtros laterales de <strong>marca</strong> y <strong>precio</strong> para afinar tu búsqueda. 🔍"
  },
  {
    pregunta: "¿Qué es la sección 'Más Vendidos'?",
    palabrasClave: ["mas vendidos", "vendidos", "populares", "destacados", "bestseller"],
    respuesta: "La sección <strong>Más Vendidos</strong> muestra los productos más populares y destacados de la tienda. ¡Es un excelente punto de partida si no sabes por dónde empezar! ⭐"
  },
  {
    pregunta: "¿Cómo agrego un producto a favoritos?",
    palabrasClave: ["favoritos", "favorito", "corazon", "guardar", "wishlist"],
    respuesta: "En cada tarjeta de producto verás un ícono de ❤️ corazón en la esquina. Al hacer clic en él, el producto se guarda en tu lista de <strong>Favoritos</strong>. Necesitas tener sesión iniciada para que los favoritos se conserven."
  },
  {
    pregunta: "¿Necesito una cuenta para comprar?",
    palabrasClave: ["cuenta", "sesion", "login", "necesito", "registrar", "comprar sin"],
    respuesta: "Puedes <strong>navegar y ver productos</strong> sin necesidad de una cuenta. Sin embargo, para agregar productos al carrito, guardar favoritos o agendar citas, necesitarás <strong>iniciar sesión</strong> o registrarte. ¡Es rápido y gratuito! 😊"
  },
  {
    pregunta: "¿Cómo me registro en LentSoft?",
    palabrasClave: ["registro", "registrar", "crear cuenta", "nueva cuenta", "sign up"],
    respuesta: "Para registrarte haz clic en <strong>Registrarse</strong> en la barra de navegación. Necesitarás ingresar: tipo y número de documento, nombre, apellido, teléfono, correo electrónico y una contraseña. ¡Listo, en segundos tienes tu cuenta! 📝"
  },
  {
    pregunta: "Olvidé mi contraseña, ¿qué hago?",
    palabrasClave: ["olvide", "contraseña", "password", "recuperar", "restablecer"],
    respuesta: "No te preocupes 😊 En la página de inicio de sesión encontrarás el enlace <strong>\"¿Olvidaste tu contraseña?\"</strong>. Al hacer clic, recibirás un correo electrónico con un enlace temporal para restablecerla. Revisa también tu carpeta de spam."
  },
  {
    pregunta: "¿Qué es 'Muestra de Montura'?",
    palabrasClave: ["muestra", "montura", "virtual", "prueba", "camara", "ver gafas"],
    respuesta: "¡Es nuestra función estrella! ✨ La <strong>Muestra de Montura</strong> es una prueba virtual que usa tu cámara web para mostrarte cómo lucirían diferentes gafas en tu rostro <em>antes de comprarlas</em>. Ideal para encontrar el estilo perfecto para ti."
  },
  {
    pregunta: "¿Cómo activo la cámara para probarme las gafas?",
    palabrasClave: ["activar camara", "activar", "permiso camara", "encender camara", "como uso camara"],
    respuesta: "Dentro de la sección <strong>Muestra de Montura</strong>, haz clic en el botón <strong>\"Activar Cámara\"</strong>. Tu navegador te pedirá permiso para acceder a la cámara — acéptalo para continuar. Una vez activada, verás tu imagen en tiempo real con las gafas superpuestas. 📷"
  },
  {
    pregunta: "¿Puedo cambiar de montura mientras uso la cámara?",
    palabrasClave: ["cambiar montura", "otra montura", "diferentes gafas", "seleccionar montura"],
    respuesta: "¡Sí! 🎉 Mientras tienes la cámara activa, puedes seleccionar cualquier otra montura de la galería que aparece en el panel lateral. El cambio es instantáneo y <strong>no necesitas recargar la página</strong>. ¡Pruébate todas las que quieras!"
  },
  {
    pregunta: "¿Cómo agrego productos al carrito?",
    palabrasClave: ["carrito", "agregar", "anadir", "comprar", "add to cart"],
    respuesta: "Puedes agregar productos al carrito desde tres lugares: 🛒<br>• El botón <strong>\"Añadir al Carrito\"</strong> en la tarjeta de la Tienda.<br>• La página de <strong>detalle del producto</strong>.<br>• La sección de <strong>Muestra de Montura</strong> después de probarte las gafas."
  },
  {
    pregunta: "¿Cómo pago mi pedido?",
    palabrasClave: ["pago", "pagar", "checkout", "pedido", "compra", "factura"],
    respuesta: "El proceso es sencillo 💳:<br>1. Ve a tu <strong>Carrito</strong> y revisa los productos.<br>2. Haz clic en <strong>\"Proceder al pago\"</strong>.<br>3. Confirma tu pedido.<br><br>⚠️ <em>Ten en cuenta que LentSoft es un entorno académico/de pruebas — no se procesan pagos reales.</em>"
  },
  {
    pregunta: "¿Qué pasa si no tengo sesión y quiero pagar?",
    palabrasClave: ["sin sesion", "no iniciado", "pagar sin cuenta", "redirige", "iniciar sesion pagar"],
    respuesta: "Si intentas proceder al pago sin haber iniciado sesión, el sistema te redirigirá automáticamente a la página de <strong>inicio de sesión</strong>. Una vez que ingreses con tu cuenta, volverás directamente al carrito o al proceso de pago. 🔄"
  },
  {
    pregunta: "Soy optómetra o vendedor, ¿dónde inicio sesión?",
    palabrasClave: ["optometra", "vendedor", "admin", "empleado", "rol", "staff"],
    respuesta: "El acceso es unificado 👨‍⚕️ Tanto optómetras como vendedores usan el <strong>mismo formulario de inicio de sesión</strong> que los clientes. Una vez autenticado, el sistema detecta tu rol y te redirige automáticamente a tu <strong>dashboard correspondiente</strong>."
  },
  {
    pregunta: "¿Cómo agendo una cita con un optómetra?",
    palabrasClave: ["cita", "agendar", "agenda", "optometra", "consulta", "turno"],
    respuesta: "Puedes agendar tu cita desde el <strong>Dashboard de Usuario</strong> en la sección de citas. Allí podrás escoger un horario disponible con nuestros optómetras. Recuerda que necesitas tener sesión iniciada. 📅"
  },
  {
    pregunta: "¿Cómo actualizo mis datos personales?",
    palabrasClave: ["actualizar", "datos", "perfil", "editar", "informacion personal", "cambiar nombre"],
    respuesta: "Para editar tu información personal, ingresa a tu <strong>Dashboard de Usuario</strong> (accesible desde el menú de usuario en la barra de navegación). Allí encontrarás una sección de <strong>Perfil</strong> donde puedes actualizar tu nombre, teléfono, correo y otros datos. ✏️"
  },
  {
    pregunta: "¿Puedo ver mis pedidos anteriores?",
    palabrasClave: ["pedidos", "historial", "anteriores", "compras", "facturas", "ordenes"],
    respuesta: "¡Claro! 📋 En tu <strong>Dashboard de Usuario</strong> encontrarás la sección de <strong>Pedidos</strong> o <strong>Facturas</strong> donde podrás revisar todo el historial de tus compras, con los detalles de cada pedido."
  }
];

/**
 * Normaliza un texto: minúsculas y sin acentos.
 * @param {string} text
 * @returns {string}
 */
function morganaNomalizar(text) {
  return text
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "");
}

/**
 * Busca la respuesta más relevante dado un texto del usuario.
 * Retorna el objeto FAQ o null si no hay match.
 * @param {string} userText
 * @returns {Object|null}
 */
function morganaBuscarRespuesta(userText) {
  const normalizado = morganaNomalizar(userText);
  let mejorMatch = null;
  let mejorPuntaje = 0;

  for (const item of MORGANA_FAQ) {
    let puntaje = 0;
    for (const kw of item.palabrasClave) {
      if (normalizado.includes(morganaNomalizar(kw))) {
        puntaje++;
      }
    }
    if (puntaje > mejorPuntaje) {
      mejorPuntaje = puntaje;
      mejorMatch = item;
    }
  }

  return mejorPuntaje > 0 ? mejorMatch : null;
}

// Exportar al objeto global window para asegurar accesibilidad desde otros scripts
window.MORGANA_FAQ = MORGANA_FAQ;
window.morganaBuscarRespuesta = morganaBuscarRespuesta;
