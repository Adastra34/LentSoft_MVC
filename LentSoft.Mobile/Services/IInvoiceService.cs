using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public interface IInvoiceService
{
    IReadOnlyList<InvoiceDto> Invoices { get; }
    event EventHandler? InvoicesChanged;

    void AddInvoice(InvoiceDto invoice);
    void SyncInvoices(IEnumerable<InvoiceDto> apiInvoices);
}
