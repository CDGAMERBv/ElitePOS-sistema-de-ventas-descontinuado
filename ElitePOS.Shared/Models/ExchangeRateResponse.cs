using System.Collections.Generic;

namespace ElitePOS.Shared.Models
{
    // Modelo sencillo para deserializar la respuesta de exchangerate.host
    public class ExchangeRateResponse
    {
        // El código en Reportes.razor accede a `result.rates`, por eso usamos el nombre en minúsculas.
        public Dictionary<string, decimal>? rates { get; set; }
    }
}
