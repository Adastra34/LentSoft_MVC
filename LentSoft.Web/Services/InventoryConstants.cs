namespace LentSoft.Web.Services;

public static class InventoryConstants
{
    public static class MovementTypes
    {
        public const string Entrada = "Entrada";
        public const string Salida = "Salida";
        public const string Alta = "Alta";
        public const string Baja = "Baja";
        public const string Reactivacion = "Reactivación";
        public const string Edicion = "Edición";
        public const string Ajuste = "Ajuste";
    }

    public static class MovementMotives
    {
        public const string Compra = "Compra";
        public const string Venta = "Venta";
        public const string Merma = "Merma";
        public const string Dano = "Daño";
        public const string Devolucion = "Devolución";
        public const string Garantia = "Garantía";
        public const string AjustePorConteo = "Ajuste por conteo";
        public const string Otro = "Otro";
    }

    public static class SupplierOrderStates
    {
        public const string Pendiente = "pendiente";
        public const string Parcial = "parcial";
        public const string Recibido = "recibido";
        public const string Cancelado = "cancelado";
    }
}
