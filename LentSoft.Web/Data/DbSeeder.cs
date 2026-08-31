using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Data;

public static class DbSeeder
{
    public static void Seed(LentSoftDbContext context)
    {
        // 0. Convertir precios de productos y órdenes existentes a COP si están en formato viejo (< 10000)
        var oldProducts = context.Products.Where(p => p.Precio < 10000).ToList();
        foreach (var p in oldProducts)
        {
            p.Precio *= 1000;
            if (p.PrecioDescuento.HasValue) p.PrecioDescuento *= 1000;
        }

        var oldOrders = context.Orders.Where(o => o.Total < 10000).ToList();
        foreach (var o in oldOrders)
        {
            o.Total *= 1000;
        }

        var oldOrderItems = context.OrderItems.Where(oi => oi.PrecioUnitario < 10000).ToList();
        foreach (var oi in oldOrderItems)
        {
            oi.PrecioUnitario *= 1000;
        }
        var prodsToInit = context.Products.Where(p => p.PorcentajeIva <= 0).ToList();
        foreach (var p in prodsToInit)
        {
            p.PorcentajeIva = p.Nombre.Contains("Líquido", StringComparison.OrdinalIgnoreCase) ? 5.00m : 19.00m;
        }

        if (oldProducts.Any() || oldOrders.Any() || oldOrderItems.Any() || prodsToInit.Any())
        {
            context.SaveChanges();
        }

        // 1. Actualizar perfil del optómetra (Tarea 6)
        var optometra = context.Users.FirstOrDefault(u => u.Email == "optometra@lentsoft.com");
        if (optometra != null)
        {
            optometra.Nombre = "Ana";
            optometra.Apellido = "Gómez Torres";
            optometra.RegistroMedico = "RM-COL-12345";
            optometra.Universidad = "Universidad Nacional de Colombia";
            optometra.EspecialidadDetalle = "Optometría Clínica y Contactología";
            optometra.AniosExperiencia = 8;
            context.SaveChanges();
        }
        // 2. Sembrar pacientes (Tarea 1 y Tarea 7)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("user123");

        var patients = new[]
        {
            new User
            {
                Nombre = "Valentina", Apellido = "Rodríguez", TipoDocumento = "CC", NumeroDocumento = "1023456789",
                Email = "valentina.rodriguez@example.com", Telefono = "3001234567", PasswordHash = passwordHash, Role = "usuario",
                FechaNacimiento = new DateTime(1995, 4, 12), Genero = "Femenino", Direccion = "Calle 100 #15-30, Bogotá", EPS = "Sanitas", EstadoPaciente = "Activo",
                FechaRegistro = DateTime.UtcNow.AddDays(-30)
            },
            new User
            {
                Nombre = "Santiago", Apellido = "Morales", TipoDocumento = "CC", NumeroDocumento = "1098765432",
                Email = "santiago.morales@example.com", Telefono = "3109876543", PasswordHash = passwordHash, Role = "usuario",
                FechaNacimiento = new DateTime(1988, 11, 23), Genero = "Masculino", Direccion = "Carrera 7 #45-12, Bogotá", EPS = "Sura", EstadoPaciente = "Activo",
                FechaRegistro = DateTime.UtcNow.AddDays(-25)
            },
            new User
            {
                Nombre = "Sofía", Apellido = "Herrera", TipoDocumento = "TI", NumeroDocumento = "10234561",
                Email = "sofia.herrera@example.com", Telefono = "3154567890", PasswordHash = passwordHash, Role = "usuario",
                FechaNacimiento = new DateTime(2010, 8, 5), Genero = "Femenino", Direccion = "Calle 80 #68-40, Bogotá", EPS = "Compensar", EstadoPaciente = "Activo",
                FechaRegistro = DateTime.UtcNow.AddDays(-20)
            },
            new User
            {
                Nombre = "Andrés", Apellido = "Díaz", TipoDocumento = "CC", NumeroDocumento = "79456123",
                Email = "andres.diaz@example.com", Telefono = "3207894561", PasswordHash = passwordHash, Role = "usuario",
                FechaNacimiento = new DateTime(1975, 1, 15), Genero = "Masculino", Direccion = "Carrera 15 #104-50, Bogotá", EPS = "Salud Total", EstadoPaciente = "Activo",
                FechaRegistro = DateTime.UtcNow.AddDays(-15)
            },
            new User
            {
                Nombre = "María", Apellido = "López", TipoDocumento = "CE", NumeroDocumento = "E-123456",
                Email = "maria.lopez@example.com", Telefono = "3004561237", PasswordHash = passwordHash, Role = "usuario",
                FechaNacimiento = new DateTime(1992, 6, 30), Genero = "Femenino", Direccion = "Calle 127 #19-80, Bogotá", EPS = "Sanitas", EstadoPaciente = "Inactivo",
                FechaRegistro = DateTime.UtcNow.AddDays(-40)
            },
            new User
            {
                Nombre = "Camilo", Apellido = "Vargas", TipoDocumento = "CC", NumeroDocumento = "1054321678",
                Email = "camilo.vargas@example.com", Telefono = "3123456789", PasswordHash = passwordHash, Role = "usuario",
                FechaNacimiento = new DateTime(2001, 9, 18), Genero = "Masculino", Direccion = "Avenida Suba #115-20, Bogotá", EPS = "Famisanar", EstadoPaciente = "Activo",
                FechaRegistro = DateTime.UtcNow.AddDays(-10)
            },
            new User
            {
                Nombre = "Daniela", Apellido = "Pinzón", TipoDocumento = "CC", NumeroDocumento = "1012345678",
                Email = "daniela.pinzon@example.com", Telefono = "3186543210", PasswordHash = passwordHash, Role = "usuario",
                FechaNacimiento = new DateTime(1998, 3, 4), Genero = "Femenino", Direccion = "Calle 140 #9-15, Bogotá", EPS = "Sura", EstadoPaciente = "Activo",
                FechaRegistro = DateTime.UtcNow.AddDays(-5)
            }
        };

        foreach (var p in patients)
        {
            if (!context.Users.Any(u => u.Email == p.Email))
            {
                context.Users.Add(p);
            }
        }
        context.SaveChanges();

        // Obtener IDs de pacientes sembrados
        var optoId = context.Users.First(u => u.Email == "optometra@lentsoft.com").Id;
        var pValentina = context.Users.First(u => u.Email == "valentina.rodriguez@example.com");
        var pSantiago = context.Users.First(u => u.Email == "santiago.morales@example.com");
        var pSofia = context.Users.First(u => u.Email == "sofia.herrera@example.com");
        var pAndres = context.Users.First(u => u.Email == "andres.diaz@example.com");
        var pMaria = context.Users.First(u => u.Email == "maria.lopez@example.com");
        var pCamilo = context.Users.First(u => u.Email == "camilo.vargas@example.com");
        var pDaniela = context.Users.First(u => u.Email == "daniela.pinzon@example.com");

        // 3. Sembrar Citas
        if (context.Appointments.Count() <= 2)
        {
            var now = DateTime.UtcNow;
            var optId = context.Users.FirstOrDefault(u => u.Role == "optometra")?.Id;
            var appointments = new[]
            {
                // Citas en horario laboral (L-S, 8-18h), sin solapamiento (≥60 min entre sí)
                new Appointment
                {
                    UserId = pValentina.Id, Servicio = "Examen visual completo", FechaHora = now.Date.AddDays(1).AddHours(9), Estado = "confirmada", Notas = "Paciente refiere fatiga ocular", FechaCreacion = now, OptometraId = optId
                },
                new Appointment
                {
                    UserId = pSantiago.Id, Servicio = "Control de lentes", FechaHora = now.Date.AddDays(1).AddHours(10).AddMinutes(30), Estado = "pendiente", Notas = "Ajuste de montura progresiva", FechaCreacion = now, OptometraId = optId
                },
                new Appointment
                {
                    UserId = pSofia.Id, Servicio = "Primera consulta", FechaHora = now.Date.AddDays(1).AddHours(12), Estado = "en proceso", Notas = "Examen de agudeza para colegio", FechaCreacion = now, OptometraId = optId
                },
                new Appointment
                {
                    UserId = pAndres.Id, Servicio = "Seguimiento glaucoma", FechaHora = now.Date.AddDays(1).AddHours(13).AddMinutes(30), Estado = "atendida", Notas = "Medición de presión intraocular", FechaCreacion = now, OptometraId = optId
                },
                new Appointment
                {
                    UserId = pMaria.Id, Servicio = "Adaptación lentes contacto", FechaHora = now.Date.AddDays(2).AddHours(9), Estado = "cancelada", Notas = "No pudo asistir por trabajo", FechaCreacion = now, OptometraId = optId
                },
                new Appointment
                {
                    // Corregido: era 2am (fuera de horario). Ahora es 10am del día siguiente.
                    UserId = pCamilo.Id, Servicio = "Ajuste de lentes", FechaHora = now.Date.AddDays(2).AddHours(10), Estado = "pendiente", Notas = "Traer montura rota", FechaCreacion = now, OptometraId = optId
                },
                new Appointment
                {
                    UserId = pDaniela.Id, Servicio = "Examen visual completo", FechaHora = now.Date.AddDays(2).AddHours(11).AddMinutes(30), Estado = "confirmada", Notas = "Chequeo anual", FechaCreacion = now, OptometraId = optId
                }
            };
            context.Appointments.AddRange(appointments);
            context.SaveChanges();
        }

        // 4. Sembrar Historial Clínico
        if (!context.HistorialesClinicos.Any())
        {
            var historiales = new[]
            {
                new HistorialClinico
                {
                    UserId = pValentina.Id, OptometraId = optoId, Fecha = DateTime.UtcNow.AddDays(-30),
                    Diagnostico = "Miopía moderada OD/OI con astigmatismo",
                    Tratamiento = "Fórmula correctora con lentes de alto índice y filtro antirreflejo azul. Uso permanente para lectura y pantallas.",
                    Antecedentes = "Madre con miopía alta. Paciente reporta dolor de cabeza frecuente al final del día.",
                    ExamenesRealizados = "Agudeza visual snellen, Queratometría, Refracción manifiesta, Examen de fondo de ojo con oftalmoscopio directo.",
                    Observaciones = "Programar control en un año.", Estado = "Activo"
                },
                new HistorialClinico
                {
                    UserId = pSantiago.Id, OptometraId = optoId, Fecha = DateTime.UtcNow.AddDays(-25),
                    Diagnostico = "Hipermetropía leve bilateral y presbicia",
                    Tratamiento = "Lentes progresivos monofocales. Uso para tareas de visión cercana y pantallas.",
                    Antecedentes = "Sin antecedentes familiares de importancia. No refiere cirugías oculares.",
                    ExamenesRealizados = "Test de Ishihara, Retinoscopía, Afaquia/Pseudoafaquia test.",
                    Observaciones = "Monitorear adaptación a lentes progresivos.", Estado = "Activo"
                },
                new HistorialClinico
                {
                    UserId = pAndres.Id, OptometraId = optoId, Fecha = DateTime.UtcNow.AddDays(-1),
                    Diagnostico = "Glaucoma de ángulo abierto incipiente",
                    Tratamiento = "Remisión a oftalmología para tratamiento con gotas hipotensoras. Monitoreo semestral.",
                    Antecedentes = "Abuelo paterno con glaucoma y ceguera.",
                    ExamenesRealizados = "Tonometría de aplanamientos (presión intraocular elevada: 21 mmHg OD), Oftalmoscopía (excavación papilar 0.6).",
                    Observaciones = "Urgente valoración por especialista en glaucoma.", Estado = "Activo"
                },
                new HistorialClinico
                {
                    UserId = pMaria.Id, OptometraId = optoId, Fecha = DateTime.UtcNow.AddDays(-40),
                    Diagnostico = "Ojo seco moderado bilateral",
                    Tratamiento = "Lágrimas artificiales sin preservantes cada 4 horas. Compresas tibias por la noche.",
                    Antecedentes = "Uso prolongado de lentes de contacto y pantallas (>8 horas diarias).",
                    ExamenesRealizados = "Test de Schirmer, Tiempo de ruptura de película lagrimal (TBUT) reducido (6 segundos).",
                    Observaciones = "Suspender temporalmente el uso de lentes de contacto blandos.", Estado = "Activo"
                }
            };
            context.HistorialesClinicos.AddRange(historiales);
            context.SaveChanges();
        }

        // 5. Sembrar Exámenes Visuales
        if (!context.ExamenesVisuales.Any())
        {
            var examenes = new[]
            {
                new ExamenVisual
                {
                    UserId = pValentina.Id, OptometraId = optoId, Fecha = DateTime.UtcNow.AddDays(-30),
                    TipoExamen = "Agudeza Visual y Refracción",
                    OjoDerecho = "20/200 sc / 20/20 cc", OjoIzquierdo = "20/150 sc / 20/20 cc",
                    Resultado = "Miopía y astigmatismo mixto",
                    TonometriaOD = "14 mmHg", TonometriaOI = "13 mmHg",
                    EsferaOD = "-3.25", CilindroOD = "-0.75", EjeOD = "180", AdicionOD = "0.00",
                    EsferaOI = "-3.00", CilindroOI = "-1.00", EjeOI = "175", AdicionOI = "0.00",
                    SegmentoAnterior = "Córnea clara, conjuntiva normal, cámara anterior profunda y limpia, iris regular.",
                    SegmentoPosterior = "Papila de bordes netos, coloración normal, excavación 0.3. Relación vaso-vaso adecuada.",
                    Diagnostico = "Miopía miópica moderada en ambos ojos con astigmatismo regular.",
                    Tratamiento = "Lentes monofocales con corrección total.",
                    Observaciones = "Paciente cooperadora en el examen."
                },
                new ExamenVisual
                {
                    UserId = pSantiago.Id, OptometraId = optoId, Fecha = DateTime.UtcNow.AddDays(-25),
                    TipoExamen = "Examen de control presbicia",
                    OjoDerecho = "20/30 sc / 20/20 cc", OjoIzquierdo = "20/30 sc / 20/20 cc",
                    Resultado = "Presbicia estable",
                    TonometriaOD = "15 mmHg", TonometriaOI = "16 mmHg",
                    EsferaOD = "+1.00", CilindroOD = "0.00", EjeOD = "0", AdicionOD = "+2.00",
                    EsferaOI = "+1.00", CilindroOI = "0.00", EjeOI = "0", AdicionOI = "+2.00",
                    SegmentoAnterior = "Cristalino con esclerosis incipiente compatible con edad.",
                    SegmentoPosterior = "Normal, mácula sana sin lesiones.",
                    Diagnostico = "Presbicia y leve hipermetropía binocular.",
                    Tratamiento = "Adición de +2.00 para lectura cercana.",
                    Observaciones = "Adaptación a progresivos recomendada."
                }
            };
            context.ExamenesVisuales.AddRange(examenes);
            context.SaveChanges();
        }

        // 6. Sembrar Fórmulas Ópticas
        if (!context.FormulasOpticas.Any())
        {
            var formulas = new[]
            {
                new FormulaOptica
                {
                    UserId = pValentina.Id, OptometraId = optoId, Fecha = DateTime.UtcNow.AddDays(-30),
                    EsferaOD = "-3.25", CilindroOD = "-0.75", EjeOD = "180",
                    EsferaOI = "-3.00", CilindroOI = "-1.00", EjeOI = "175",
                    Observaciones = "Filtro Blue Protect para uso en computador.",
                    TipoLente = "Monofocal - Anti reflejo", DistanciaPupilar = "62"
                },
                new FormulaOptica
                {
                    UserId = pSantiago.Id, OptometraId = optoId, Fecha = DateTime.UtcNow.AddDays(-25),
                    EsferaOD = "+1.00", CilindroOD = "0.00", EjeOD = "0",
                    EsferaOI = "+1.00", CilindroOI = "0.00", EjeOI = "0",
                    Observaciones = "Adición +2.00 para lectura. Usar lentes progresivos.",
                    TipoLente = "Progresivo", DistanciaPupilar = "64"
                },
                new FormulaOptica
                {
                    UserId = pMaria.Id, OptometraId = optoId, Fecha = DateTime.UtcNow.AddYears(-2),
                    EsferaOD = "-1.00", CilindroOD = "-0.50", EjeOD = "90",
                    EsferaOI = "-1.25", CilindroOI = "-0.25", EjeOI = "85",
                    Observaciones = "Uso intermitente.",
                    TipoLente = "Monofocal", DistanciaPupilar = "60"
                }
            };
            context.FormulasOpticas.AddRange(formulas);
            context.SaveChanges();
        }
    }
}
