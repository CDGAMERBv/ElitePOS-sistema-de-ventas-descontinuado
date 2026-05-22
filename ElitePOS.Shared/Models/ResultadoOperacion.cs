using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class ResultadoOperacion
    {
        [JsonPropertyName("exitoso")]
        public bool exitoso { get; set; }

        [JsonPropertyName("mensajeError")]
        public string? mensajeError { get; set; }

        [JsonPropertyName("codigoHttp")]
        public int? codigoHttp { get; set; }

        [JsonPropertyName("detalleError")]
        public string? detalleError { get; set; }

        public static ResultadoOperacion Exito()
        {
            return new ResultadoOperacion { exitoso = true };
        }

        public static ResultadoOperacion Error(string mensaje, int? codigoHttp = null, string? detalle = null)
        {
            return new ResultadoOperacion
            {
                exitoso = false,
                mensajeError = mensaje,
                codigoHttp = codigoHttp,
                detalleError = detalle
            };
        }
    }
}
