# Plan de Pruebas del Módulo de Optometría (M06) — LentSoft

## 1. Información General y Alcance
- **Sistema:** LentSoft — Plataforma Óptica y Optometría Integral (ASP.NET Core MVC 9, EF Core, SQL Server / SQLite)
- **Módulo:** M06 — Optometría y Salud Visual
- **Referencia Normativa:** ERS IEEE 830 v1.0 (LentSoft)
- **Objetivo:** Verificar la integridad funcional, validaciones clínicas, reglas de negocio, seguridad de acceso y rendimiento de los flujos de gestión de pacientes, citas, exámenes visuales y prescripción de fórmulas ópticas.

---

## 2. Requerimientos Cubiertos (Matriz de Trazabilidad)

| Requerimiento | Descripción según ERS IEEE 830 |
| :--- | :--- |
| **RF-M06-01** | **Gestión de Pacientes e Historial Clínico**: Registro seguro, edición con control de duplicados, desactivación lógica, registro de consultas clínicas y visualización de línea de tiempo consolidada. |
| **RF-M06-02** | **Gestión de Citas de Optometría**: Agendamiento en horario laboral (lunes a sábado 8:00 a 18:00, 60 min), validación de fecha futura, disponibilidad de agenda sin cruces, reprogramación y ciclo de vida de estados. |
| **RF-M06-03** | **Realización y Registro de Exámenes Visuales**: Registro de refracción (esfera, cilindro, eje), validación de fecha (solo pasadas y hoy), auto-generación de registro en historia clínica y consulta para precarga. |
| **RF-M06-04** | **Emisión y Gestión de Fórmulas Ópticas**: Prescripción con refracción y Adición (OD/OI), distancia pupilar, vigencia de 12 meses, generación de PDF de receta e integración con órdenes de venta. |
| **RNF-M06-01** | **Seguridad y Control de Acceso por Rol**: Restricción exclusiva al rol `optometra` en `/Optometra`, rechazo de accesos no autorizados y aislamiento estricto de historias clínicas entre pacientes. |
| **RNF-M06-02** | **Rendimiento y Experiencia de Usuario (UX)**: Fluidez en la actualización de estados de citas y respuesta de interfaz (documentación del estado actual y oportunidades de mejora asíncrona). |

---

## 3. Matriz de Casos de Prueba

| ID | Requerimiento | Nombre del Caso | Precondición | Pasos Principales | Datos de Entrada | Resultado Esperado | Tipo | Estado |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **CP-M06-01** | RF-M06-01 | Creación de paciente con contraseña aleatoria y token | Optómetra autenticado | 1. Enviar formulario `CreatePaciente`.<br>2. Validar BD y servicio de correo. | Nombre, correo, doc nuevo | Paciente creado con hash seguro $\ne$ `"user123"` y enlace de reset enviado por correo. | Automatizada | Pasó |
| **CP-M06-02** | RF-M06-01 | Edición de paciente con correo duplicado | Existen paciente A y paciente B | 1. Editar paciente A con correo del paciente B.<br>2. Enviar `EditPaciente`. | `Email = pacienteB@test.com` | Rechazo con mensaje: *"El correo ya está registrado."*. Datos no modificados. | Automatizada | Pasó |
| **CP-M06-03** | RF-M06-01 | Edición de paciente con documento duplicado | Existen paciente A y paciente B | 1. Editar paciente A con documento de paciente B.<br>2. Enviar `EditPaciente`. | `NumeroDocumento = docPacienteB` | Rechazo con mensaje: *"El número de documento ya está registrado."*. Datos no modificados. | Automatizada | Pasó |
| **CP-M06-04** | RF-M06-01 | Edición de datos propios del paciente conservando correo y doc | Paciente registrado | 1. Editar teléfono/nombre conservando mismo correo y doc.<br>2. Enviar `EditPaciente`. | Teléfono actualizado, mismo email y doc | Éxito con mensaje *"Datos del paciente actualizados exitosamente."* y cambios guardados. | Automatizada | Pasó |
| **CP-M06-05** | RF-M06-01 | Eliminación lógica de paciente | Paciente activo en BD | 1. Invocar `DeletePaciente(id)`.<br>2. Consultar paciente en BD. | `id = paciente.Id` | `Activo == false`, `EstadoPaciente == "Inactivo"` y mensaje de éxito. | Automatizada | Pasó |
| **CP-M06-06** | RF-M06-01 | Registro de entrada en Historial Clínico | Paciente registrado | 1. Enviar formulario `CreateHistorial`.<br>2. Consultar BD. | Diagnóstico, tratamiento, refracción | Registro guardado con diagnóstico, tratamiento y campos de refracción asociados. | Automatizada | Pasó |
| **CP-M06-07** | RF-M06-01 | Eliminación lógica de entrada de Historial Clínico | Historial activo en BD | 1. Invocar `DeleteHistorial(id)`.<br>2. Consultar BD. | `id = historial.Id` | `Activo == false` y mensaje de confirmación de eliminación. | Automatizada | Pasó |
| **CP-M06-08** | RF-M06-01 | Visualización de Línea de Tiempo del paciente | Paciente con historiales, exámenes y fórmulas | 1. Invocar `HistorialCompleto(pacienteId)`.<br>2. Validar `Index` y vista parcial. | `pacienteId` | Carga `LineaDeTiempo` ordenada descendentemente por fecha con los tres tipos de eventos. | Automatizada | Pasó |
| **CP-M06-09** | RF-M06-02 | Validación horario laboral - Rechazo de cita en Domingo | Fecha cae en día Domingo | 1. Evaluar `EsHorarioLaboral(fechaDomingo)`. | Fecha Domingo a las 10:00 AM | Retorna `false`. Las citas no se pueden agendar los domingos. | Automatizada | Pasó |
| **CP-M06-10** | RF-M06-02 | Validación horario laboral - Rechazo a las 17:30 (cierre 18:00) | Duración de cita = 60 min | 1. Evaluar `EsHorarioLaboral(17:30)`. | 17:30 (finaliza 18:30) | Retorna `false`. Excede la hora límite de las 18:00. | Automatizada | Pasó |
| **CP-M06-11** | RF-M06-02 | Validación horario laboral - Aceptación a las 08:00 exactas | Apertura de clínica | 1. Evaluar `EsHorarioLaboral(08:00)`. | Lunes a Sábado 08:00 AM | Retorna `true`. La cita cabe exactamente en el horario de atención. | Automatizada | Pasó |
| **CP-M06-12** | RF-M06-02 | Rechazo de cita con fecha en el pasado | Fecha y hora anterior a la actual | 1. Enviar `CreateAppointment` con fecha pasada. | `FechaHora = ayer 10:00 AM` | Rechazo con mensaje: *"La fecha y hora de la cita debe ser futura."*. Cita no guardada. | Automatizada | Pasó |
| **CP-M06-13** | RF-M06-02 | Agendamiento de cita en fecha futura válida | Horario laboral disponible | 1. Enviar `CreateAppointment` con fecha futura. | Próxima semana hábil 10:00 AM | Cita creada exitosamente con estado `"pendiente"`. | Automatizada | Pasó |
| **CP-M06-14** | RF-M06-02 | Rechazo por cruce de horario con otra cita del optómetra | Optómetra ya tiene cita a las 10:00 AM | 1. Intentar agendar otra cita para el mismo optómetra a las 10:00 AM. | Misma `FechaHora` y `OptometraId` | Rechazo con mensaje de falta de disponibilidad del optómetra. | Automatizada | Pasó |
| **CP-M06-15** | RF-M06-02 | Cancelación/Eliminación y reutilización de horario | Cita agendada a las 14:00 | 1. Eliminar cita con `DeleteAppointment`.<br>2. Agendar nueva cita en ese mismo horario. | Mismo horario previamente ocupado | `DeleteAppointment` libera la agenda y la nueva cita se programa con éxito. | Automatizada | Pasó |
| **CP-M06-16** | RF-M06-02 | Reprogramación con cruce en segundo intento | Cita A (10:00) y Cita B (11:00) | 1. Intentar mover Cita B a las 10:00 ocupada por Cita A. | `model.FechaHora = 10:00 AM` | Rechazo de edición por cruce de horario. Cita B conserva su horario original. | Automatizada | Pasó |
| **CP-M06-17** | RF-M06-02 | Actualización a estados válidos | Cita existente | 1. Invocar `UpdateAppointmentStatus` con `confirmada`, `completada`, `cancelada`. | `nuevoEstado = "completada"` | Estado actualizado exitosamente en base de datos y normalizado a minúsculas. | Automatizada | Pasó |
| **CP-M06-18** | RF-M06-02 | Rechazo de estado inexistente `"atendida"` | Cita existente | 1. Invocar `UpdateAppointmentStatus` con `"atendida"`. | `nuevoEstado = "atendida"` | Rechazo con mensaje: *"Estado no válido. Los valores permitidos son: pendiente, confirmada, completada, cancelada."*. | Automatizada | Pasó |
| **CP-M06-19** | RF-M06-03 | Registro de examen con fecha pasada válida | Examen realizado previamente | 1. Enviar `CreateExamen` con fecha de hace 5 días. | `Fecha = DateTime.Today.AddDays(-5)` | Examen registrado exitosamente. | Automatizada | Pasó |
| **CP-M06-20** | RF-M06-03 | Rechazo de examen con fecha futura | Fecha posterior a hoy | 1. Enviar `CreateExamen` con fecha de mañana. | `Fecha = DateTime.Today.AddDays(1)` | Rechazo con mensaje: *"La fecha del examen no puede ser una fecha futura."*. | Automatizada | Pasó |
| **CP-M06-21** | RF-M06-03 | Generación automática de entrada en historial desde examen | Examen visual válido | 1. Registrar examen con refracción completa.<br>2. Consultar `HistorialesClinicos`. | Refracción OD/OI y diagnóstico | Se crea automáticamente un `HistorialClinico` con los 6 campos de refracción y diagnóstico. | Automatizada | Pasó |
| **CP-M06-22** | RF-M06-03 | Edición de examen visual con fecha pasada | Examen registrado | 1. Modificar refracción y fecha pasada con `EditExamen`. | Fecha pasada, refracción actualizada | Registro actualizado con éxito en base de datos. | Automatizada | Pasó |
| **CP-M06-23** | RF-M06-03 | Edición de examen visual rechazando fecha futura | Examen registrado | 1. Intentar actualizar examen con fecha de mañana. | `Fecha = DateTime.Today.AddDays(2)` | Rechazo con mensaje de error; registro original sin cambios en BD. | Automatizada | Pasó |
| **CP-M06-24** | RF-M06-03 | Consulta de último examen para precarga | Paciente con exámenes previos | 1. Llamar `GetUltimoExamenPorPaciente(userId)`. | `userId = paciente.Id` | Retorna JSON con `encontrado: true` y los datos del examen más reciente. | Automatizada | Pasó |
| **CP-M06-25** | RF-M06-04 | Creación de fórmula óptica con Adición (OD y OI) | Paciente registrado | 1. Enviar `CreateFormula` con `AdicionOD` y `AdicionOI`. | `AdicionOD = "+1.50"`, `AdicionOI = "+1.75"` | Fórmula guardada con los campos de adición persistidos. | Automatizada | Pasó |
| **CP-M06-26** | RF-M06-04 | Creación de fórmula óptica sin adición (monofocal) | Paciente registrado | 1. Enviar `CreateFormula` con `AdicionOD` y `AdicionOI` nulos. | `AdicionOD = null`, `AdicionOI = null` | Fórmula guardada correctamente con valores nulos sin errores de validación. | Automatizada | Pasó |
| **CP-M06-27** | RF-M06-04 | Creación de fórmula óptica sin distancia pupilar | Campo opcional | 1. Enviar `CreateFormula` con `DistanciaPupilar = null`. | `DistanciaPupilar = null` | Fórmula creada exitosamente. | Automatizada | Pasó |
| **CP-M06-28** | RF-M06-04 | Cálculo de vigencia de fórmula (12 meses) | Fórmulas con distintas fechas | 1. Evaluar propiedad `Estado` en fórmula de hace 6 meses vs hace 13 meses. | Fecha hace 6 meses / Fecha hace 13 meses | Retorna `"Vigente"` para $\le 12$ meses y `"Vencida"` para $> 12$ meses. | Automatizada | Pasó |
| **CP-M06-29** | RF-M06-04 | Edición de fórmula actualizando refracción y adición | Fórmula existente | 1. Enviar `EditFormula` actualizando `AdicionOD` y `AdicionOI`. | `AdicionOD = "+2.00"`, `AdicionOI = "+2.25"` | Cambios persistidos correctamente en la base de datos. | Automatizada | Pasó |
| **CP-M06-30** | RF-M06-04 | Generación y descarga de receta en PDF con adición | Fórmula con adición | 1. Invocar `PdfRecetaService.GenerateRecetaPdf(formula)`. | Entidad `FormulaOptica` completa | Retorna arreglo binario válido con cabecera `%PDF-` y tabla con columna de adición. | Automatizada | Pasó |
| **CP-M06-31** | RF-M06-04 | Generación de pedido de venta desde fórmula vigente | Fórmula vigente con stock | 1. Invocar `CrearPedidoDesdeFormula`. | `formulaId`, `productId`, `cantidad` | Pedido creado con estado `"Pendiente"` y vinculado a la fórmula. | Automatizada | Pasó |
| **CP-M06-32** | RF-M06-04 | Rechazo de generación de pedido desde fórmula vencida | Fórmula con $> 12$ meses | 1. Invocar `CrearPedidoDesdeFormula`. | `formulaId` de fórmula vencida | Rechazo con mensaje: *"La fórmula está vencida; no se puede generar el pedido."*. | Automatizada | Pasó |
| **CP-M06-33** | RNF-M06-01 | Restricción de acceso a `/Optometra` sin sesión o rol usuario | Usuario no optómetra | 1. Inspeccionar atributo `[Authorize(Roles = "optometra")]` en `OptometraController`. | Request sin rol optómetra | Acceso bloqueado por el middleware de autorización de ASP.NET Core. | Automatizada | Pasó |
| **CP-M06-34** | RNF-M06-01 | Restricción de acceso a `/Optometra` con rol ventas | Usuario con rol `ventas` | 1. Inspeccionar atributo `[Authorize]` en `OptometraController`. | Rol `ventas` | Acceso denegado; el controlador exige explícitamente rol `optometra`. | Automatizada | Pasó |
| **CP-M06-35** | RNF-M06-01 | Aislamiento de datos clínicos entre pacientes | Pacientes A y B con historiales y fórmulas | 1. Consultar datos del paciente A en portal usuario. | `UserId = pacienteA.Id` | La consulta solo retorna registros de paciente A; cero registros de paciente B. | Automatizada | Pasó |
| **CP-M06-36** | RNF-M06-02 | Actualización ágil de citas sin recarga completa | Interfaz web de citas | 1. Cambiar estado de cita en frontend.<br>2. Verificar recarga de página. | Cambio de estado de cita en UI | **Pendiente de implementar:** Hoy las acciones ejecutan `RedirectToAction` provocando recarga completa de página. Requiere migración a endpoints AJAX/Fetch para cumplimiento pleno. | Manual | Pendiente de implementar |

---

## 4. Registro de Defectos Encontrados

Durante la ejecución de las pruebas y la auditoría del módulo de optometría no se detectaron fallos funcionales ni defectos bloqueantes en el código corregido. Todos los 35 casos automatizados se ejecutan con resultado **Pasó** (0 fallos).

---

## 5. Hallazgos Abiertos y Decisiones Pendientes

### Hallazgo 1 (RNF-M06-02): Calendario y gestión de citas sin recarga completa
- **Requerimiento asociado:** RNF-M06-02 (Rendimiento, Usabilidad y Experiencia de Usuario).
- **Estado:** Pendiente de implementar.
- **Detalle técnico:** En `OptometraController`, las acciones `CreateAppointment`, `EditAppointment`, `DeleteAppointment` y `UpdateAppointmentStatus` finalizan con `RedirectToAction("Index", new { section = "citas" })`. Esto provoca una petición HTTP completa y recarga de página por cada cambio de estado o acción sobre citas, en lugar de una actualización parcial asíncrona reactiva (AJAX/Fetch).
- **Decisión pendiente:** Definir si para la siguiente entrega se refactorizan las llamadas en `optometra.js` para consumir respuestas JSON (habilitando el soporte AJAX ya presente en otros endpoints) y manipular el DOM sin recargas, o si se mantiene la navegación síncrona estándar con `RedirectToAction` y mensajes `TempData`.

### Hallazgo 2 (Seguridad / ERS): Discrepancia en permisos de acceso clínico entre ERS y Código
- **Requerimiento asociado:** RF-M06-01 / RNF-M06-01 (Matriz de Control de Acceso).
- **Estado:** Decisión de diseño pendiente.
- **Detalle técnico:** La especificación de requerimientos ERS IEEE 830 v1.0 estipula que los expedientes clínicos y datos de optometría deben ser accesibles para el personal asistencial (`optometra`) y para el perfil `admin` (para labores de supervisión, auditoría médica y gestión operativa). No obstante, el código actual de `OptometraController` restringe el acceso de forma rígida a:
  ```csharp
  [Authorize(Roles = "optometra")]
  public class OptometraController : Controller
  ```
  Esto bloquea por completo el acceso a usuarios con rol `admin` a la ruta `/Optometra`, obligándolos a interactuar únicamente a través del panel general.
- **Decisión pendiente:** Decidir si se amplía la anotación del controlador a `[Authorize(Roles = "optometra,admin")]` permitiendo al administrador auditar la vista clínica, o si se debe construir una vista de solo lectura específica dentro del módulo `/Dashboard/Admin` para auditorías clínicas.

---

## 6. Matriz de Ejecución Manual (Entorno con Base de Datos y Servicios Reales)

Esta sección lista las pruebas de aceptación y verificación en vivo que dependen de infraestructura real (SQL Server, SMTP/correo, visualizador PDF y navegador). Las columnas **Resultado Real** y **Evidencia (Captura / Log)** están preparadas en blanco para su diligenciamiento durante la prueba manual:

| ID | Caso de Prueba Manual | Precondición | Pasos de Verificación | Resultado Esperado | Resultado Real | Evidencia (Captura / Log) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **CPM-01** | Guardado y visualización de Adición OD/OI en UI | BD con migración aplicada, optómetra logueado | 1. Ir a sección Fórmulas.<br>2. Abrir modal "Nueva Fórmula".<br>3. Digitar `+1.50` en Adición OD y `+1.75` en Adición OI.<br>4. Guardar y verificar en tabla y modal Editar. | Los campos Adición OD y OI se muestran en el formulario, se persisten en base de datos y se recargan correctamente en el modal de edición. | | |
| **CPM-02** | Descarga y renderizado de PDF de receta con Adición | Fórmula con Adición creada | 1. En la tabla de fórmulas, clic en botón "Receta" (ojo).<br>2. Clic en "Descargar PDF".<br>3. Abrir el archivo descargado en visor PDF. | El PDF se descarga con el nombre `Receta_[Paciente]_[Id].pdf`. La tabla de corrección visual contiene la columna `Adición (ADD)` con los valores formulados. | | |
| **CPM-03** | Verificación de Procedimiento Almacenado vs Fallback en Línea de Tiempo | Paciente con historial en SQL Server | 1. En la sección Historial, hacer clic en "Línea de tiempo" de un paciente.<br>2. Revisar logs de la aplicación / Profiler de SQL Server.<br>3. Comprobar si ejecutó `sp_HistorialCompletoPaciente` o cayó a LINQ. | Si el SP existe en la BD SQL Server, se ejecuta sin excepciones. Si no existe o falla, el fallback LINQ toma el control transparentemente y la vista muestra la cronología. | | |
| **CPM-04** | Envío de correo real para fijar contraseña de nuevo paciente | Servidor SMTP / EmailService configurado | 1. Ir a sección Pacientes.<br>2. Crear nuevo paciente con un correo real accesible.<br>3. Abrir bandeja de entrada del correo.<br>4. Clic en el enlace recibido y fijar nueva contraseña. | Se recibe el correo con el asunto de recuperación/definición, el enlace redirige a `/Auth/ResetPassword?token=...` y permite ingresar una nueva clave sin conocer la clave temporal. | | |
| **CPM-05** | Control de acceso: Intento de acceso a `/Optometra` con rol `usuario` | Sesión iniciada como paciente (rol `usuario`) | 1. Iniciar sesión como paciente (`user@lentsoft.com`).<br>2. Escribir manualmente en la barra de URL: `http://localhost:.../Optometra`.<br>3. Presionar Enter. | El sistema deniega el acceso y redirige a la página de acceso denegado (`/Auth/AccessDenied` o error 403 HTTP). | | |
| **CPM-06** | Control de acceso: Intento de acceso a `/Optometra` con rol `ventas` | Sesión iniciada con usuario de ventas | 1. Iniciar sesión con cuenta de rol `ventas`.<br>2. Navegar directamente a `http://localhost:.../Optometra`.<br>3. Presionar Enter. | El sistema deniega el acceso y redirige a `/Auth/AccessDenied`. El personal de ventas no puede ingresar a la interfaz clínica. | | |
| **CPM-07** | Control de acceso: Intento de acceso a `/Optometra` sin sesión activa | Navegador en modo incógnito / sin cookies de sesión | 1. Abrir ventana de incógnito.<br>2. Intentar ingresar directamente a `http://localhost:.../Optometra`.<br>3. Presionar Enter. | El sistema redirige automáticamente al formulario de autenticación: `/Auth/Login?ReturnUrl=%2FOptometra`. | | |


