using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class TarjetaPagoDTO
{
    public int? VentaId { get; set; }
    public string NumeroTarjeta { get; set; } = string.Empty;
    public string NombreTitular { get; set; } = string.Empty;
    public string FechaExpiracion { get; set; } = string.Empty; // MM/YY
    public string Cvv { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Responsable { get; set; } = "Ventas";
}

public class ResultadoTransaccionDTO
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public TransaccionPago? Transaccion { get; set; }
    public PagoVenta? PagoRegistrado { get; set; }
}

public interface IPasarelaPagoService
{
    Task<ResultadoTransaccionDTO> ProcesarPagoTarjetaAsync(TarjetaPagoDTO dto);
    bool ValidarAlgoritmoLuhn(string numeroTarjeta);
    string DetectarMarcaTarjeta(string numeroTarjeta);
}
