using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class GastoModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo";

        [JsonPropertyName("fechaRegistro")]
        public DateTime fechaRegistro { get; set; } = DateTime.Now;

        [JsonPropertyName("categoria")]
        public string categoria { get; set; } = string.Empty;

        [JsonPropertyName("concepto")]
        public string concepto { get; set; } = string.Empty;

        [JsonPropertyName("monto")]
        public decimal monto { get; set; }

        [JsonPropertyName("usuarioId")]
        public string usuarioId { get; set; } = string.Empty;

        [JsonPropertyName("nombreUsuario")]
        public string nombreUsuario { get; set; } = string.Empty;

        [JsonPropertyName("observaciones")]
        public string observaciones { get; set; } = string.Empty;

        [JsonPropertyName("anulado")]
        public bool anulado { get; set; } = false;

        // Propiedades de compatibilidad (Alias)
        [JsonIgnore] public string Id { get => id; set => id = value; }
        [JsonIgnore] public string EmpresaId { get => empresaId; set => empresaId = value; }
        [JsonIgnore] public DateTime FechaRegistro { get => fechaRegistro; set => fechaRegistro = value; }
        [JsonIgnore] public string Categoria { get => categoria; set => categoria = value; }
        [JsonIgnore] public string Concepto { get => concepto; set => concepto = value; }
        [JsonIgnore] public decimal Monto { get => monto; set => monto = value; }
        [JsonIgnore] public string UsuarioId { get => usuarioId; set => usuarioId = value; }
        [JsonIgnore] public string NombreUsuario { get => nombreUsuario; set => nombreUsuario = value; }
        [JsonIgnore] public string Observaciones { get => observaciones; set => observaciones = value; }
        [JsonIgnore] public bool Anulado { get => anulado; set => anulado = value; }
    }
}
