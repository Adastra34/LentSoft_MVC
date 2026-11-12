using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class ArPreviewContractTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly ProductService _service;
    private readonly string _repoRoot;

    public ArPreviewContractTests()
    {
        _repoRoot = FindRepoRoot();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _service = new ProductService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static string FindRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "LentSoft.sln")))
            {
                return current;
            }
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        throw new DirectoryNotFoundException("No se encontró la raíz del repositorio que contiene LentSoft.sln");
    }

    [Fact]
    public void MediaPipe_ArchivosLocalesExistenYTienenTamanoValido()
    {
        // Arrange
        var libDir = Path.Combine(_repoRoot, "LentSoft.Web", "wwwroot", "lib", "mediapipe");
        var bundlePath = Path.Combine(libDir, "vision_bundle.mjs");
        var wasmPath = Path.Combine(libDir, "wasm", "vision_wasm_internal.wasm");
        var wasmNosimdPath = Path.Combine(libDir, "wasm", "vision_wasm_nosimd_internal.wasm");
        var taskPath = Path.Combine(libDir, "models", "face_landmarker.task");

        const long unMegaByte = 1024 * 1024;

        // Act & Assert
        Assert.True(File.Exists(bundlePath), $"Falta {bundlePath}");
        Assert.True(new FileInfo(bundlePath).Length > 0, $"{bundlePath} está vacío");

        Assert.True(File.Exists(wasmPath), $"Falta {wasmPath}");
        Assert.True(new FileInfo(wasmPath).Length > unMegaByte, $"{wasmPath} debe tener un tamaño > 1 MB");

        Assert.True(File.Exists(wasmNosimdPath), $"Falta {wasmNosimdPath}");
        Assert.True(new FileInfo(wasmNosimdPath).Length > unMegaByte, $"{wasmNosimdPath} debe tener un tamaño > 1 MB");

        Assert.True(File.Exists(taskPath), $"Falta {taskPath}");
        Assert.True(new FileInfo(taskPath).Length > unMegaByte, $"{taskPath} debe tener un tamaño > 1 MB");
    }

    [Fact]
    public void MuestraMontura_NoContieneLangEnScriptInline()
    {
        // Arrange
        var viewPath = Path.Combine(_repoRoot, "LentSoft.Web", "Views", "Product", "MuestraMontura.cshtml");
        Assert.True(File.Exists(viewPath), $"No existe la vista {viewPath}");

        var content = File.ReadAllText(viewPath);

        // Extraer todo el código dentro de etiquetas <script>...</script>
        var scriptRegex = new Regex(@"<script\b[^>]*>([\s\S]*?)</script>", RegexOptions.IgnoreCase);
        var matches = scriptRegex.Matches(content);

        Assert.NotEmpty(matches);

        // Act & Assert: Ningún bloque de script inline debe contener la variable o identificador 'lang'
        foreach (Match match in matches)
        {
            var scriptBody = match.Groups[1].Value;
            var langMatch = Regex.Match(scriptBody, @"\blang\b", RegexOptions.IgnoreCase);
            Assert.False(langMatch.Success, $"La vista MuestraMontura.cshtml contiene 'lang' en un <script> inline: '{langMatch.Value}'");
        }
    }

    [Fact]
    public void MuestraMonturaJs_NoContieneReferenciasSueltasALang()
    {
        // Arrange
        var jsPath = Path.Combine(_repoRoot, "LentSoft.Web", "wwwroot", "js", "muestra-montura.js");
        Assert.True(File.Exists(jsPath), $"No existe el archivo {jsPath}");

        var lines = File.ReadAllLines(jsPath);

        // Verificar que getLang esté definido al inicio
        var allText = string.Join("\n", lines);
        Assert.Matches(@"const\s+getLang\s*=\s*\(\)\s*=>", allText);

        // Act & Assert: Toda coincidencia de \blang\b debe ser dentro de la definición de getLang o
        // asignaciones/comparaciones explícitas de getLang()
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (Regex.IsMatch(line, @"\blang\b"))
            {
                bool isGetLangDef = line.Contains("getLang") || line.Contains("getCurrentLang");
                bool isLangAssignment = line.Contains("const lang = getLang()") || line.Contains("let lang = getLang()");
                bool isLangComparison = line.Contains("lang ===") || line.Contains("lang !==") || line.Contains("lang ==");
                bool isTranslateUsage = line.Contains("translate(") && line.Contains("lang");
                bool isPassingToTitleOrError = line.Contains("showCameraError") || line.Contains("resolvedTitle");

                bool isValidUsage = isGetLangDef || isLangAssignment || isLangComparison || isTranslateUsage || isPassingToTitleOrError;

                Assert.True(isValidUsage, $"Línea {i + 1} de muestra-montura.js tiene una referencia suelta a 'lang': {line.Trim()}");
            }
        }
    }

    [Fact]
    public void DbSeeder_Y_DbContext_OverlaysCorrespondenAArchivosReales()
    {
        // Arrange
        var seederPath = Path.Combine(_repoRoot, "LentSoft.Web", "Data", "DbSeeder.cs");
        var dbContextPath = Path.Combine(_repoRoot, "LentSoft.Web", "Data", "LentSoftDbContext.cs");
        var wwwrootPath = Path.Combine(_repoRoot, "LentSoft.Web", "wwwroot");

        Assert.True(File.Exists(seederPath), $"Falta {seederPath}");
        Assert.True(File.Exists(dbContextPath), $"Falta {dbContextPath}");

        var seederText = File.ReadAllText(seederPath);
        var dbContextText = File.ReadAllText(dbContextPath);

        var overlayRegex = new Regex(@"/img/overlays/[a-zA-Z0-9_\-\.]+\.(png|svg|jpg|jpeg)", RegexOptions.IgnoreCase);

        var seederMatches = overlayRegex.Matches(seederText).Select(m => m.Value).Distinct().ToList();
        var dbContextMatches = overlayRegex.Matches(dbContextText).Select(m => m.Value).Distinct().ToList();

        Assert.NotEmpty(seederMatches);
        Assert.NotEmpty(dbContextMatches);

        var allOverlays = seederMatches.Union(dbContextMatches).ToList();

        // Act & Assert
        foreach (var overlayUrl in allOverlays)
        {
            var relativePath = overlayUrl.TrimStart('/');
            var physicalPath = Path.Combine(wwwrootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(physicalPath), $"El overlay '{overlayUrl}' referenciado en código no existe en disco: {physicalPath}");
        }
    }

    [Fact]
    public async Task GetGafasAsync_IncluyeVariantesDeCategoria()
    {
        // Arrange - Limpiar productos previos desactivando temporalmente foreign keys en sqlite
        _context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");
        _context.Products.RemoveRange(_context.Products);
        await _context.SaveChangesAsync();
        _context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");

        var p1 = new Product { Nombre = "Montura Elegante", Categoria = "Monturas", Precio = 500000m, Activo = true, Stock = 10 };
        var p2 = new Product { Nombre = "Lentes de Sol Aviator", Categoria = "lentes-sol", Precio = 600000m, Activo = true, Stock = 10 };
        var p3 = new Product { Nombre = "Gafas de Sol Sport", Categoria = "gafas-sol", Precio = 700000m, Activo = true, Stock = 10 };
        var p4 = new Product { Nombre = "Lentes de Contacto Ultra", Categoria = "lentes-contacto", Precio = 200000m, Activo = true, Stock = 10 };
        var p5 = new Product { Nombre = "Accesorios Limpieza", Categoria = "accesorios", Precio = 50000m, Activo = true, Stock = 10 };

        _context.Products.AddRange(p1, p2, p3, p4, p5);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetGafasAsync();

        // Assert: Devuelve exactamente los 3 primeros productos y excluye lentes-contacto y accesorios
        Assert.Equal(3, result.Count);
        Assert.Contains(result, p => p.Id == p1.Id);
        Assert.Contains(result, p => p.Id == p2.Id);
        Assert.Contains(result, p => p.Id == p3.Id);
        Assert.DoesNotContain(result, p => p.Id == p4.Id);
        Assert.DoesNotContain(result, p => p.Id == p5.Id);
    }

    [Fact]
    public async Task GetGafasAsync_ExcluyeInactivos()
    {
        // Arrange - Limpiar productos previos desactivando temporalmente foreign keys en sqlite
        _context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");
        _context.Products.RemoveRange(_context.Products);
        await _context.SaveChangesAsync();
        _context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");

        var activeProduct = new Product { Nombre = "Montura Activa", Categoria = "monturas", Precio = 500000m, Activo = true, Stock = 10 };
        var inactiveProduct = new Product { Nombre = "Montura Desactivada", Categoria = "monturas", Precio = 400000m, Activo = false, Stock = 10 };

        _context.Products.AddRange(activeProduct, inactiveProduct);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetGafasAsync();

        // Assert
        Assert.Contains(result, p => p.Id == activeProduct.Id);
        Assert.DoesNotContain(result, p => p.Id == inactiveProduct.Id);
    }
}
