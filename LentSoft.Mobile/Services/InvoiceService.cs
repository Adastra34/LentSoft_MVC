using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public class InvoiceService : IInvoiceService
{
    private static InvoiceService? _instance;
    public static InvoiceService Instance => _instance ??= new InvoiceService();

    private readonly List<InvoiceDto> _invoices = new();

    public IReadOnlyList<InvoiceDto> Invoices => _invoices.AsReadOnly();

    public event EventHandler? InvoicesChanged;

    public InvoiceService()
    {
        _instance = this;

        // Initial sample invoice for demonstration
        _invoices.Add(new InvoiceDto
        {
            Id = 1,
            NumeroFactura = "FAC-2026-8819",
            Subtotal = 588235m,
            Impuestos = 111765m,
            Total = 700000m,
            Estado = "Pagada",
            EstadoRaw = "pagada",
            FechaEmision = DateTime.Now.AddDays(-2),
            FechaPago = DateTime.Now.AddDays(-2),
            MetodoPago = "Tarjeta Visa **** 4532",
            ItemsSummary = "1x Lentes Ray-Ban Aviator"
        });
    }

    public void AddInvoice(InvoiceDto invoice)
    {
        if (invoice == null) return;
        _invoices.Insert(0, invoice);
        InvoicesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SyncInvoices(IEnumerable<InvoiceDto> apiInvoices)
    {
        if (apiInvoices == null) return;

        foreach (var inv in apiInvoices)
        {
            if (!_invoices.Any(i => i.NumeroFactura == inv.NumeroFactura || i.Id == inv.Id))
            {
                _invoices.Add(inv);
            }
        }

        InvoicesChanged?.Invoke(this, EventArgs.Empty);
    }
}
