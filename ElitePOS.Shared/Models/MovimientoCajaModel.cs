using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class MovimientoCajaModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo";

        [JsonPropertyName("fecha")]
        public DateTime fecha { get; set; }

        [JsonPropertyName("tipo")]
        public string tipo { get; set; } = string.Empty; // "Apertura", "Cierre", "Ingreso", "Egreso"

        [JsonPropertyName("monto")]
        public decimal monto { get; set; }

        [JsonPropertyName("montoSistema")]
        public decimal montoSistema { get; set; }

        [JsonPropertyName("diferencia")]
        public decimal diferencia { get; set; }

        [JsonPropertyName("usuarioId")]
        public string usuarioId { get; set; } = string.Empty;

        [JsonPropertyName("nombreUsuario")]
        public string nombreUsuario { get; set; } = string.Empty;

        [JsonPropertyName("observaciones")]
        public string observaciones { get; set; } = string.Empty;

        [JsonPropertyName("cajaAbierta")]
        public bool cajaAbierta { get; set; } = true;

        [JsonPropertyName("tipoMovimiento")]
        public string tipoMovimiento { get; set; } = string.Empty;

        // Propiedades de compatibilidad (Alias)
        [JsonIgnore] public string Id { get => id; set => id = value; }
        [JsonIgnore] public string EmpresaId { get => empresaId; set => empresaId = value; }
        [JsonIgnore] public DateTime Fecha { get => fecha; set => fecha = value; }
        [JsonIgnore] public string Tipo { get => tipo; set => tipo = value; }
        [JsonIgnore] public decimal Monto { get => monto; set => monto = value; }
        [JsonIgnore] public decimal MontoSistema { get => montoSistema; set => montoSistema = value; }
        [JsonIgnore] public decimal Diferencia { get => diferencia; set => diferencia = value; }
        [JsonIgnore] public bool CajaAbierta { get => cajaAbierta; set => cajaAbierta = value; }
        [JsonIgnore] public string UsuarioId { get => usuarioId; set => usuarioId = value; }
        [JsonIgnore] public string NombreUsuario { get => nombreUsuario; set => nombreUsuario = value; }
    }
}
