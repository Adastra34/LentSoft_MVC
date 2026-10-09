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
    }

    public static class SupplierOrderStates
    {
        public const string Pendiente = "pendiente";
        public const string Recibido = "recibido";
        public const string Cancelado = "cancelado";
    }
}
