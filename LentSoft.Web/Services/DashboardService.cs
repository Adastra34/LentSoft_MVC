using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;

namespace LentSoft.Web.Services;

/// <summary>
/// Dashboard service — orquesta métricas, resúmenes y consultas paginadas del panel de administración
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly LentSoftDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly IProductService _productService;
    private readonly InventorySettings _inventorySettings;
    private readonly IMemoryCache? _cache;

    public DashboardService(
        LentSoftDbContext context,
        IInvoiceService invoiceService,
        IProductService productService,
        IOptions<InventorySettings> inventoryOptions,
        IMemoryCache? cache = null)
    {
        _context = context;
        _invoiceService = invoiceService;
        _productService = productService;
        _inventorySettings = inventoryOptions?.Value ?? new InventorySettings();
        _cache = cache;
    }

    public async Task<DashboardStats> GetAdminStatsAsync()
    {
        return new DashboardStats
        {
            TotalUsuarios = await _context.Users.AsNoTracking().CountAsync(u => u.Role == "usuario"),
            TotalProductos = await _context.Products.AsNoTracking().CountAsync(p => p.Activo),
            TotalPedidos = await _context.Orders.AsNoTracking().CountAsync(o => o.Estado != "cancelado"),
            VentasTotales = await _context.Orders.AsNoTracking()
                .Where(o => o.Estado != "cancelado")
                .SumAsync(o => o.Total)
        };
    }

    public async Task<DashboardAdminViewModel> GetDashboardAdminViewModelAsync(DashboardAdminQueryFilter filter)
    {
        var now = DateTime.UtcNow;
        var inicioMes = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var inicioMesAnterior = inicioMes.AddMonths(-1);

        // ── Stats del mes actual (con caché opcional en memoria) ──
        decimal ventasMes, ventasMesAnterior;
        int pedidosActivos, clientesTotales, productosEnStock;

        var cacheKey = "admin_dashboard_stats_kpi";
        if (_cache != null && _cache.TryGetValue(cacheKey, out (decimal vm, decimal vma, int pa, int ct, int ps) cachedStats))
        {
            ventasMes = cachedStats.vm;
            ventasMesAnterior = cachedStats.vma;
            pedidosActivos = cachedStats.pa;
            clientesTotales = cachedStats.ct;
            productosEnStock = cachedStats.ps;
        }
        else
        {
            ventasMes = await _context.Orders.AsNoTracking()
                .Where(o => o.Activo && o.Estado != "cancelado" && o.FechaPedido >= inicioMes)
                .SumAsync(o => (decimal?)o.Total) ?? 0;

            ventasMesAnterior = await _context.Orders.AsNoTracking()
                .Where(o => o.Activo && o.Estado != "cancelado" && o.FechaPedido >= inicioMesAnterior && o.FechaPedido < inicioMes)
                .SumAsync(o => (decimal?)o.Total) ?? 0;

            pedidosActivos = await _context.Orders.AsNoTracking()
                .Where(o => o.Activo)
                .CountAsync(o => o.Estado == "pendiente" || o.Estado == "procesando" || o.Estado == "enviado");

            clientesTotales = await _context.Users.AsNoTracking().CountAsync(u => u.Role == "usuario" && u.Activo);

            productosEnStock = await _context.Products.AsNoTracking().CountAsync(p => p.Activo && p.Stock > 0);

            _cache?.Set(cacheKey, (ventasMes, ventasMesAnterior, pedidosActivos, clientesTotales, productosEnStock), TimeSpan.FromMinutes(2));
        }

        var pedidosActivosAnterior = Math.Max(1, pedidosActivos - 1);
        var clientesAnterior = Math.Max(1, clientesTotales - 1);
        var productosAnterior = Math.Max(1, productosEnStock);

        // ── Pedidos Recientes ──
        var pedidosRecientes = await _context.Orders.AsNoTracking()
            .Where(o => o.Activo)
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.FechaPedido)
            .Take(_inventorySettings.RecentItemsLimit)
            .ToListAsync();

        // Fallback para searchTerm genérico
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            if (filter.Section == "inventario" && filter.Subtab == "productos" && string.IsNullOrWhiteSpace(filter.ProdSearch)) filter.ProdSearch = filter.SearchTerm;
            if (filter.Section == "inventario" && filter.Subtab == "historial" && string.IsNullOrWhiteSpace(filter.MovSearch)) filter.MovSearch = filter.SearchTerm;
            if (filter.Section == "inventario" && filter.Subtab == "pedidos" && string.IsNullOrWhiteSpace(filter.PedidosProvSearch)) filter.PedidosProvSearch = filter.SearchTerm;
        }

        if (filter.Section == "inventario")
        {
            await _productService.AsignarSkusConsecutivosFaltantesAsync();
        }

        // ── Productos (Paginados y Filtrados en BD) ──
        var prodQuery = _context.Products.AsNoTracking().Include(p => p.Supplier).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.ProdSearch))
        {
            var s = filter.ProdSearch.Trim().ToLower();
            prodQuery = prodQuery.Where(p => p.Nombre.ToLower().Contains(s)
                || (p.Sku != null && p.Sku.ToLower().Contains(s))
                || (p.Marca != null && p.Marca.ToLower().Contains(s))
                || (p.Categoria != null && p.Categoria.ToLower().Contains(s))
                || (p.Ubicacion != null && p.Ubicacion.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(filter.ProdEstado))
        {
            var est = filter.ProdEstado.Trim().ToLower();
            if (est == "activo") prodQuery = prodQuery.Where(p => p.Activo);
            else if (est == "inactivo") prodQuery = prodQuery.Where(p => !p.Activo);
        }

        if (!string.IsNullOrWhiteSpace(filter.ProdCategoria))
        {
            var cat = filter.ProdCategoria.Trim().ToLower();
            if (cat == "lentes-sol")
                prodQuery = prodQuery.Where(p => p.Categoria != null && (p.Categoria.ToLower().Contains("sol") || p.Categoria.ToLower().Contains("lentes-sol")));
            else if (cat == "monturas")
                prodQuery = prodQuery.Where(p => p.Categoria != null && p.Categoria.ToLower().Contains("montura"));
            else if (cat == "lentes-contacto")
                prodQuery = prodQuery.Where(p => p.Categoria != null && (p.Categoria.ToLower().Contains("contacto") || p.Categoria.ToLower().Contains("lentes-contacto")));
            else if (cat == "lentes-graduados")
                prodQuery = prodQuery.Where(p => p.Categoria != null && (p.Categoria.ToLower().Contains("graduado") || p.Categoria.ToLower().Contains("lentes-graduados")));
            else if (cat == "accesorios")
                prodQuery = prodQuery.Where(p => p.Categoria != null && p.Categoria.ToLower().Contains("accesorio"));
            else
                prodQuery = prodQuery.Where(p => p.Categoria != null && p.Categoria.ToLower().Contains(cat));
        }

        if (!string.IsNullOrWhiteSpace(filter.ProdProveedor))
        {
            var supp = filter.ProdProveedor.Trim();
            prodQuery = prodQuery.Where(p => p.SupplierId == supp || (p.Supplier != null && p.Supplier.Nombre.ToLower().Contains(supp.ToLower())));
        }

        if (filter.ProdStockBajo)
        {
            prodQuery = prodQuery.Where(p => p.Stock <= (p.StockMinimo > 0 ? p.StockMinimo : _inventorySettings.LowStockThreshold));
        }

        prodQuery = (filter.ProdSortBy?.ToLower(), filter.ProdSortDir?.ToLower()) switch
        {
            ("sku", "desc") => prodQuery.OrderByDescending(p => p.Sku),
            ("sku", _) => prodQuery.OrderBy(p => p.Sku),
            ("categoria", "desc") => prodQuery.OrderByDescending(p => p.Categoria),
            ("categoria", _) => prodQuery.OrderBy(p => p.Categoria),
            ("marca", "desc") => prodQuery.OrderByDescending(p => p.Marca),
            ("marca", _) => prodQuery.OrderBy(p => p.Marca),
            ("proveedor", "desc") => prodQuery.OrderByDescending(p => p.Supplier != null ? p.Supplier.Nombre : ""),
            ("proveedor", _) => prodQuery.OrderBy(p => p.Supplier != null ? p.Supplier.Nombre : ""),
            ("precio", "desc") => prodQuery.OrderByDescending(p => (double)p.Precio),
            ("precio", _) => prodQuery.OrderBy(p => (double)p.Precio),
            ("stock", "desc") => prodQuery.OrderByDescending(p => p.Stock),
            ("stock", _) => prodQuery.OrderBy(p => p.Stock),
            ("estado", "desc") => prodQuery.OrderByDescending(p => p.Activo),
            ("estado", _) => prodQuery.OrderBy(p => p.Activo),
            (_, "desc") => prodQuery.OrderByDescending(p => p.Nombre),
            _ => prodQuery.OrderBy(p => p.Nombre)
        };

        var prodTotalCount = await prodQuery.CountAsync();
        var prodPageSize = filter.ProdPageSize > 0 ? filter.ProdPageSize : _inventorySettings.DefaultPageSize;
        var productos = await prodQuery
            .Skip((filter.ProdPage - 1) * prodPageSize)
            .Take(prodPageSize)
            .ToListAsync();

        var todosLosProductos = await _context.Products.AsNoTracking().Include(p => p.Supplier).OrderBy(p => p.Nombre).ToListAsync();

        // ── Clientes (Paginados y Filtrados) ──
        var clientesPage = Math.Max(1, filter.ClientesPage);
        var clientesPageSize = Math.Clamp(filter.ClientesPageSize, 1, 100);
        var trabajadoresPage = Math.Max(1, filter.TrabajadoresPage);
        var trabajadoresPageSize = Math.Clamp(filter.TrabajadoresPageSize, 1, 100);

        var clientesQuery = _context.Users.AsNoTracking().Where(u => u.Role == "usuario");
        if (!filter.IncludeInactive) clientesQuery = clientesQuery.Where(u => u.Activo);

        if (!string.IsNullOrWhiteSpace(filter.ClientesSearch))
        {
            var term = filter.ClientesSearch.Trim().ToLower();
            clientesQuery = clientesQuery.Where(u => u.Nombre.ToLower().Contains(term) || u.Apellido.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }
        else if (filter.Section == "usuarios" && filter.Subtab == "clientes" && !string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            clientesQuery = clientesQuery.Where(u => u.Nombre.ToLower().Contains(term) || u.Apellido.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
            filter.ClientesSearch = filter.SearchTerm;
        }

        var clientesTotalCount = await clientesQuery.CountAsync();
        var clientesList = await clientesQuery
            .OrderBy(u => u.Nombre)
            .Skip((clientesPage - 1) * clientesPageSize)
            .Take(clientesPageSize)
            .ToListAsync();

        // ── Trabajadores (Paginados y Filtrados) ──
        var trabajadoresQuery = _context.Employees.AsNoTracking().AsQueryable();
        if (!filter.IncludeInactive) trabajadoresQuery = trabajadoresQuery.Where(e => e.Activo);

        if (!string.IsNullOrWhiteSpace(filter.TrabajadoresSearch))
        {
            var term = filter.TrabajadoresSearch.Trim().ToLower();
            trabajadoresQuery = trabajadoresQuery.Where(e => e.Nombre.ToLower().Contains(term) || e.Email.ToLower().Contains(term) || e.Puesto.ToLower().Contains(term));
        }
        else if (filter.Section == "usuarios" && filter.Subtab == "trabajadores" && !string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            trabajadoresQuery = trabajadoresQuery.Where(e => e.Nombre.ToLower().Contains(term) || e.Email.ToLower().Contains(term) || e.Puesto.ToLower().Contains(term));
            filter.TrabajadoresSearch = filter.SearchTerm;
        }

        var trabajadoresTotalCount = await trabajadoresQuery.CountAsync();
        var rawTrabajadores = await trabajadoresQuery
            .OrderBy(e => e.Nombre)
            .Skip((trabajadoresPage - 1) * trabajadoresPageSize)
            .Take(trabajadoresPageSize)
            .ToListAsync();

        var empEmails = rawTrabajadores
            .Select(e => e.Email)
            .Where(email => !string.IsNullOrEmpty(email))
            .Distinct()
            .ToList();

        var userByEmail = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var pedidosCountByUserId = new Dictionary<int, int>();

        if (empEmails.Count > 0)
        {
            var matchedUsers = await _context.Users
                .AsNoTracking()
                .Where(u => empEmails.Contains(u.Email))
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();

            foreach (var u in matchedUsers)
            {
                userByEmail.TryAdd(u.Email, u.Id);
            }

            var matchedUserIds = matchedUsers.Select(u => u.Id).Distinct().ToList();
            if (matchedUserIds.Count > 0)
            {
                var pedidosCounts = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.Activo && matchedUserIds.Contains(o.UserId))
                    .GroupBy(o => o.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToListAsync();

                foreach (var pc in pedidosCounts)
                {
                    pedidosCountByUserId[pc.UserId] = pc.Count;
                }
            }
        }

        var trabajadoresList = new List<TrabajadorItemViewModel>();
        foreach (var emp in rawTrabajadores)
        {
            int pedidosCount = 0;
            if (!string.IsNullOrEmpty(emp.Email) && userByEmail.TryGetValue(emp.Email, out var userId))
            {
                pedidosCountByUserId.TryGetValue(userId, out pedidosCount);
            }

            trabajadoresList.Add(new TrabajadorItemViewModel
            {
                Id = emp.Id,
                Nombre = emp.Nombre,
                Email = emp.Email,
                Telefono = emp.Telefono,
                Puesto = emp.Puesto,
                Departamento = emp.Departamento,
                Salario = emp.Salario,
                Rol = string.IsNullOrWhiteSpace(emp.Rol) ? "Trabajador" : emp.Rol,
                Activo = emp.Activo,
                PedidosCount = pedidosCount
            });
        }

        var ventas = await _context.Orders.AsNoTracking()
            .Where(o => o.Activo)
            .Include(o => o.User)
            .OrderByDescending(o => o.FechaPedido)
            .ToListAsync();

        var citas = await _context.Appointments.AsNoTracking()
            .Where(a => a.Activo)
            .Include(a => a.User)
            .Include(a => a.Optometra)
            .OrderByDescending(a => a.FechaHora)
            .ToListAsync();

        var (facturasList, facturasTotalCount) = await _invoiceService.GetAllAsync(filter.SearchTerm, filter.Page, filter.PageSize);
        var pedidosDisponibles = await _invoiceService.GetOrdersAvailableForInvoicingAsync();

        // ── Inventario: Resumen ──
        var valorTotalInventario = await _context.Products.AsNoTracking()
            .Where(p => p.Activo)
            .SumAsync(p => p.Stock * (p.CostoCompra ?? p.Precio));

        var totalProductosActivos = await _context.Products.AsNoTracking()
            .CountAsync(p => p.Activo);

        var totalPedidosPendientesProv = await _context.SupplierOrders.AsNoTracking()
            .CountAsync(s => s.Activo && s.Estado == "pendiente");

        var productosAgotadosCount = await _context.Products.AsNoTracking().CountAsync(p => p.Stock == 0);
        var productosPorAgotarseCount = await _context.Products.AsNoTracking().CountAsync(p => p.Stock > 0 && p.Stock <= _inventorySettings.LowStockThreshold);

        var productosAgotados = await _context.Products.AsNoTracking()
            .Include(p => p.Supplier)
            .Where(p => p.Stock == 0)
            .OrderBy(p => p.Nombre)
            .Take(_inventorySettings.TopProductsLimit)
            .ToListAsync();

        var productosPorAgotarse = await _context.Products.AsNoTracking()
            .Include(p => p.Supplier)
            .Where(p => p.Stock > 0 && p.Stock <= _inventorySettings.LowStockThreshold)
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Nombre)
            .Take(_inventorySettings.TopProductsLimit)
            .ToListAsync();

        var haceDiasThreshold = now.AddDays(-_inventorySettings.InactiveDaysThreshold);
        var productosConMovimiento = await _context.InventoryMovements.AsNoTracking()
            .Where(m => m.Fecha >= haceDiasThreshold)
            .Select(m => m.ProductId)
            .Distinct()
            .ToListAsync();

        var productosSinMovimiento60 = await _context.Products.AsNoTracking()
            .Include(p => p.Supplier)
            .Where(p => p.Activo && p.Stock > 0 && !productosConMovimiento.Contains(p.Id))
            .OrderBy(p => p.Nombre)
            .Take(_inventorySettings.TopProductsLimit)
            .ToListAsync();

        // ── Productos más vendidos (Optimizado sin N+1) ──
        var topItems = await _context.OrderItems.AsNoTracking()
            .Include(oi => oi.Product)
            .Where(oi => oi.Order != null && oi.Order.Activo && oi.Order.Estado != "cancelado" && oi.Product != null)
            .GroupBy(oi => new { oi.ProductId, oi.Product!.Nombre, oi.Product.Categoria, oi.Product.ImagenUrl, oi.Product.Stock, oi.Product.Activo })
            .Select(g => new TopProductoVendidoViewModel
            {
                ProductId = g.Key.ProductId,
                Nombre = g.Key.Nombre,
                Categoria = g.Key.Categoria,
                ImagenUrl = g.Key.ImagenUrl,
                StockActual = g.Key.Stock,
                Activo = g.Key.Activo,
                CantidadVendida = g.Sum(x => x.Cantidad),
                TotalRecaudado = g.Sum(x => x.Cantidad * x.PrecioUnitario)
            })
            .ToListAsync();

        var salesOrders = await _context.SalesOrders.AsNoTracking()
            .Where(so => so.Activo && so.Estado != "cancelado")
            .ToListAsync();

        if (salesOrders.Count > 0)
        {
            var salesProductNames = salesOrders.Select(so => so.ProductoNombre.Trim().ToLower()).Distinct().ToList();
            var matchedProdsList = await _context.Products.AsNoTracking()
                .Where(p => salesProductNames.Contains(p.Nombre.ToLower()))
                .ToListAsync();
            var matchedProdsMap = matchedProdsList.ToDictionary(p => p.Nombre.ToLower(), p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var so in salesOrders)
            {
                var existing = topItems.FirstOrDefault(t => t.Nombre.Equals(so.ProductoNombre, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.CantidadVendida += so.Cantidad;
                    existing.TotalRecaudado += so.Total > 0 ? so.Total : (so.Cantidad * so.PrecioUnitario);
                }
                else
                {
                    matchedProdsMap.TryGetValue(so.ProductoNombre.Trim(), out var matchedProd);
                    topItems.Add(new TopProductoVendidoViewModel
                    {
                        ProductId = matchedProd?.Id ?? 0,
                        Nombre = so.ProductoNombre,
                        Categoria = matchedProd?.Categoria ?? "general",
                        ImagenUrl = matchedProd?.ImagenUrl,
                        StockActual = matchedProd?.Stock ?? 0,
                        Activo = matchedProd?.Activo ?? true,
                        CantidadVendida = so.Cantidad,
                        TotalRecaudado = so.Total > 0 ? so.Total : (so.Cantidad * so.PrecioUnitario)
                    });
                }
            }
        }

        var productosMasVendidos = topItems
            .OrderByDescending(x => x.CantidadVendida)
            .ThenByDescending(x => x.TotalRecaudado)
            .Take(_inventorySettings.TopProductsLimit)
            .ToList();

        // ── Gráfico de evolución de ventas e inventario (últimos 6 meses) ──
        var fechaInicioEvolucion = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var pedidosHistoricos = await _context.Orders.AsNoTracking()
            .Where(o => o.Activo && o.Estado != "cancelado" && o.FechaPedido >= fechaInicioEvolucion)
            .ToListAsync();

        var movimientosHistoricos = await _context.InventoryMovements.AsNoTracking()
            .Where(m => m.Fecha >= fechaInicioEvolucion)
            .ToListAsync();

        var evolucionVentas = new List<ChartDataPoint>();
        for (int i = 5; i >= 0; i--)
        {
            var targetDate = now.AddMonths(-i);
            var mesNombre = targetDate.ToString("MMM yyyy", new CultureInfo("es-CO"));
            var pedidosMes = pedidosHistoricos.Where(o => o.FechaPedido.Year == targetDate.Year && o.FechaPedido.Month == targetDate.Month).ToList();
            var movimientosMes = movimientosHistoricos.Where(m => m.Fecha.Year == targetDate.Year && m.Fecha.Month == targetDate.Month).ToList();

            var entradas = movimientosMes.Where(m => m.Tipo != null && m.Tipo.Equals("Entrada", StringComparison.OrdinalIgnoreCase)).Sum(m => m.Cantidad);
            var salidas = movimientosMes.Where(m => m.Tipo != null && !m.Tipo.Equals("Entrada", StringComparison.OrdinalIgnoreCase)).Sum(m => m.Cantidad);

            evolucionVentas.Add(new ChartDataPoint
            {
                Periodo = char.ToUpper(mesNombre[0]) + mesNombre.Substring(1),
                TotalVentas = pedidosMes.Sum(o => o.Total),
                CantidadPedidos = pedidosMes.Count,
                EntradasStock = entradas,
                SalidasStock = salidas
            });
        }

        // ── Historial de Movimientos (Paginado y Filtrado en BD) ──
        var movQuery = _context.InventoryMovements.AsNoTracking().Include(m => m.Product).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.MovSearch))
        {
            var s = filter.MovSearch.Trim().ToLower();
            movQuery = movQuery.Where(m => (m.NombreProducto != null && m.NombreProducto.ToLower().Contains(s))
                || (m.Product != null && m.Product.Nombre.ToLower().Contains(s))
                || (m.Responsable != null && m.Responsable.ToLower().Contains(s))
                || m.Id.ToString().Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(filter.MovTipo))
        {
            var t = filter.MovTipo.Trim().ToLower();
            movQuery = movQuery.Where(m => m.Tipo != null && m.Tipo.ToLower() == t);
        }
        if (filter.MovProductId.HasValue && filter.MovProductId.Value > 0)
        {
            movQuery = movQuery.Where(m => m.ProductId == filter.MovProductId.Value);
        }
        if (filter.MovFechaInicio.HasValue)
        {
            var fInicioUtc = DateTime.SpecifyKind(filter.MovFechaInicio.Value.Date, DateTimeKind.Utc);
            movQuery = movQuery.Where(m => m.Fecha >= fInicioUtc);
        }
        if (filter.MovFechaFin.HasValue)
        {
            var fFinUtc = DateTime.SpecifyKind(filter.MovFechaFin.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            movQuery = movQuery.Where(m => m.Fecha <= fFinUtc);
        }
        if (!string.IsNullOrWhiteSpace(filter.MovMotivo))
        {
            var mot = filter.MovMotivo.Trim().ToLower();
            movQuery = movQuery.Where(m => m.Motivo != null && m.Motivo.ToLower() == mot);
        }
        movQuery = (filter.MovSort == "asc")
            ? movQuery.OrderBy(m => m.Fecha)
            : movQuery.OrderByDescending(m => m.Fecha);

        var movTotalCount = await movQuery.CountAsync();
        var movPageSize = filter.MovPageSize > 0 ? filter.MovPageSize : _inventorySettings.DefaultPageSize;
        var historialMovimientos = await movQuery
            .Skip((filter.MovPage - 1) * movPageSize)
            .Take(movPageSize)
            .ToListAsync();

        // ── Pedidos a Proveedores (Paginados y Filtrados en BD) ──
        var pedidosProvQuery = _context.SupplierOrders.AsNoTracking()
            .Include(o => o.Supplier)
            .Include(o => o.Product)
            .Where(o => o.Activo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.PedidosProvSearch))
        {
            var s = filter.PedidosProvSearch.Trim().ToLower();
            pedidosProvQuery = pedidosProvQuery.Where(o => (o.NumeroPedido != null && o.NumeroPedido.ToLower().Contains(s))
                || (o.Supplier != null && o.Supplier.Nombre.ToLower().Contains(s))
                || (o.Product != null && o.Product.Nombre.ToLower().Contains(s)));
        }
        if (!string.IsNullOrWhiteSpace(filter.PedidosProvEstado))
        {
            var est = filter.PedidosProvEstado.Trim().ToLower();
            pedidosProvQuery = pedidosProvQuery.Where(o => o.Estado != null && o.Estado.ToLower() == est);
        }
        pedidosProvQuery = pedidosProvQuery.OrderByDescending(o => o.Fecha);

        var pedidosProvTotalCount = await pedidosProvQuery.CountAsync();
        var pedidosProvPageSize = filter.PedidosProvPageSize > 0 ? filter.PedidosProvPageSize : _inventorySettings.DefaultPageSize;
        var pedidosProveedores = await pedidosProvQuery
            .Skip((filter.PedidosProvPage - 1) * pedidosProvPageSize)
            .Take(pedidosProvPageSize)
            .ToListAsync();

        var proveedores = await _context.Suppliers.AsNoTracking().Where(s => s.Activo).OrderBy(s => s.Nombre).ToListAsync();
        var pedidosVentas = await _context.SalesOrders.AsNoTracking().Where(s => s.Activo).OrderByDescending(s => s.Fecha).ToListAsync();

        return new DashboardAdminViewModel
        {
            VentasDelMes = ventasMes,
            VentasDelMesAnterior = ventasMesAnterior,
            PedidosActivos = pedidosActivos,
            PedidosActivosAnterior = pedidosActivosAnterior,
            ClientesTotales = clientesTotales,
            ClientesTotalesAnterior = clientesAnterior,
            ProductosEnStock = productosEnStock,
            ProductosEnStockAnterior = productosAnterior,

            PedidosRecientes = pedidosRecientes,

            Productos = productos,
            TodosLosProductos = todosLosProductos,
            ProdSearchTerm = filter.ProdSearch,
            ProdEstado = filter.ProdEstado,
            ProdCategoria = filter.ProdCategoria,
            ProdProveedor = filter.ProdProveedor,
            ProdStockBajo = filter.ProdStockBajo,
            ProdSortBy = filter.ProdSortBy,
            ProdSortDir = filter.ProdSortDir,
            ProdPage = filter.ProdPage,
            ProdPageSize = prodPageSize,
            ProdTotalCount = prodTotalCount,

            Proveedores = proveedores,

            HistorialMovimientos = historialMovimientos,
            MovSearchTerm = filter.MovSearch,
            MovTipo = filter.MovTipo,
            MovProductId = filter.MovProductId,
            MovFechaInicio = filter.MovFechaInicio,
            MovFechaFin = filter.MovFechaFin,
            MovMotivo = filter.MovMotivo,
            MovSort = filter.MovSort,
            MovPage = filter.MovPage,
            MovPageSize = movPageSize,
            MovTotalCount = movTotalCount,

            PedidosVentas = pedidosVentas,

            PedidosProveedores = pedidosProveedores,
            PedidosProvSearchTerm = filter.PedidosProvSearch,
            PedidosProvEstado = filter.PedidosProvEstado,
            PedidosProvPage = filter.PedidosProvPage,
            PedidosProvPageSize = pedidosProvPageSize,
            PedidosProvTotalCount = pedidosProvTotalCount,

            ValorTotalInventario = valorTotalInventario,
            TotalProductosActivos = totalProductosActivos,
            TotalPedidosPendientesProveedor = totalPedidosPendientesProv,
            ProductosSinMovimiento60Dias = productosSinMovimiento60,
            EvolucionVentas = evolucionVentas,
            ProductosMasVendidos = productosMasVendidos,
            ProductosAgotados = productosAgotados,
            ProductosAgotadosCount = productosAgotadosCount,
            ProductosPorAgotarse = productosPorAgotarse,
            ProductosPorAgotarseCount = productosPorAgotarseCount,

            Ventas = ventas,
            Citas = citas,

            Clientes = clientesList,
            TodosLosClientes = await _context.Users.AsNoTracking().Where(u => u.Role == "usuario" && u.Activo).OrderBy(u => u.Nombre).ToListAsync(),
            ClientesSearchTerm = filter.ClientesSearch,
            ClientesPage = clientesPage,
            ClientesPageSize = clientesPageSize,
            ClientesTotalCount = clientesTotalCount,

            Trabajadores = trabajadoresList,
            TrabajadoresSearchTerm = filter.TrabajadoresSearch,
            TrabajadoresPage = trabajadoresPage,
            TrabajadoresPageSize = trabajadoresPageSize,
            TrabajadoresTotalCount = trabajadoresTotalCount,

            Facturas = facturasList,
            FacturasSearchTerm = filter.SearchTerm,
            FacturasPage = filter.Page,
            FacturasPageSize = filter.PageSize,
            FacturasTotalCount = facturasTotalCount,
            PedidosDisponibles = pedidosDisponibles,

            ActiveSection = filter.Section,
            ActiveSubTab = filter.Subtab
        };
    }
}
