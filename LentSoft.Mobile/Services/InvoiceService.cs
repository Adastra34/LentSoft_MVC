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
        // Las facturas inician vacías y se cargan desde el API o al emitir una compra
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

    public void Clear()
    {
        _invoices.Clear();
        InvoicesChanged?.Invoke(this, EventArgs.Empty);
    }
}
