using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class ClienteModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo";

        [JsonPropertyName("dniRuc")]
        public string dniRuc { get; set; } = string.Empty;

        [JsonPropertyName("nombre")]
        public string nombre { get; set; } = string.Empty;

        [JsonPropertyName("telefono")]
        public string telefono { get; set; } = string.Empty;

        [JsonPropertyName("direccion")]
        public string direccion { get; set; } = string.Empty;

        [JsonPropertyName("fotoUrl")]
        public string fotoUrl { get; set; } = string.Empty;

        [JsonPropertyName("deuda")]
        public decimal deuda { get; set; } = 0;

        [JsonPropertyName("anulado")]
        public bool anulado { get; set; } = false;

        [JsonIgnore] public string Id { get => id; set => id = value; }
        [JsonIgnore] public string EmpresaId { get => empresaId; set => empresaId = value; }
        [JsonIgnore] public string Nombre { get => nombre; set => nombre = value; }
        [JsonIgnore] public string DniRuc { get => dniRuc; set => dniRuc = value; }
        [JsonIgnore] public decimal Deuda { get => deuda; set => deuda = value; }
        [JsonIgnore] public bool Anulado { get => anulado; set => anulado = value; }
    }
}
