using ElitePOS.Shared.Models;
using System.Net.Http.Json;

namespace ElitePOS.Services
{
    public class PlanService : IPlanService
    {
        private readonly HttpClient _http;
        private const string FIREBASE_URL = "https://firestore.googleapis.com/v1/projects/TU_FIREBASE_PROJECT_ID/databases/(default)/documents/Plan";
        private const string DOCUMENTO_ID = "plan-actual";

        public PlanService(HttpClient http)
        {
            _http = http;
        }

        public async Task<PlanModel> ObtenerPlanActual()
        {
            try
            {
                var response = await _http.GetFromJsonAsync<FirebaseDocumentResponse>($"{FIREBASE_URL}/{DOCUMENTO_ID}");
                
                if (response?.Fields != null)
                {
                    var f = response.Fields;
                    DateTime fechaVenc = DateTime.Now.AddMonths(1);
                    var rawFecha = f.fechaVencimiento?.StringValue ?? f.FechaVencimiento?.StringValue;
                    
                    if (!string.IsNullOrEmpty(rawFecha) && DateTime.TryParse(rawFecha, out var fecha))
                    {
                        fechaVenc = fecha;
                    }

                    return new PlanModel
                    {
                        id = DOCUMENTO_ID,
                        nombrePlan = f.nombrePlan?.StringValue ?? (f.NombrePlan?.StringValue ?? "Plan Gratuito"),
                        fechaVencimiento = fechaVenc,
                        limiteProductos = int.TryParse(f.limiteProductos?.IntegerValue ?? f.LimiteProductos?.IntegerValue, out var lp) ? lp : 100,
                        productosUsados = int.TryParse(f.productosUsados?.IntegerValue ?? f.ProductosUsados?.IntegerValue, out var pu) ? pu : 0,
                        limiteVentas = int.TryParse(f.limiteVentas?.IntegerValue ?? f.LimiteVentas?.IntegerValue, out var lv) ? lv : 500,
                        ventasUsadas = int.TryParse(f.ventasUsadas?.IntegerValue ?? f.VentasUsadas?.IntegerValue, out var vu) ? vu : 0,
                        limiteUsuarios = int.TryParse(f.limiteUsuarios?.IntegerValue ?? f.LimiteUsuarios?.IntegerValue, out var lu) ? lu : 2,
                        usuariosUsados = int.TryParse(f.usuariosUsados?.IntegerValue ?? f.UsuariosUsados?.IntegerValue, out var uu) ? uu : 1
                    };
                }

                return new PlanModel { id = DOCUMENTO_ID };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener plan: {ex.Message}");
                return new PlanModel { id = DOCUMENTO_ID };
            }
        }

        public async Task<bool> ActualizarContadores()
        {
            try
            {
                var plan = await ObtenerPlanActual();
                var firebaseData = new
                {
                    fields = new
                    {
                        productosUsados = new { integerValue = plan.productosUsados.ToString() },
                        ventasUsadas = new { integerValue = plan.ventasUsadas.ToString() },
                        usuariosUsados = new { integerValue = plan.usuariosUsados.ToString() }
                    }
                };

                var response = await _http.PatchAsJsonAsync($"{FIREBASE_URL}/{DOCUMENTO_ID}", firebaseData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al actualizar contadores: {ex.Message}");
                return false;
            }
        }

        private class FirebaseDocumentResponse { public PlanFields Fields { get; set; } = new(); }
        private class PlanFields {
            public FirebaseStringValue? nombrePlan { get; set; }
            public FirebaseStringValue? NombrePlan { get; set; }
            public FirebaseStringValue? fechaVencimiento { get; set; }
            public FirebaseStringValue? FechaVencimiento { get; set; }
            public FirebaseIntegerValue? limiteProductos { get; set; }
            public FirebaseIntegerValue? LimiteProductos { get; set; }
            public FirebaseIntegerValue? productosUsados { get; set; }
            public FirebaseIntegerValue? ProductosUsados { get; set; }
            public FirebaseIntegerValue? limiteVentas { get; set; }
            public FirebaseIntegerValue? LimiteVentas { get; set; }
            public FirebaseIntegerValue? ventasUsadas { get; set; }
            public FirebaseIntegerValue? VentasUsadas { get; set; }
            public FirebaseIntegerValue? limiteUsuarios { get; set; }
            public FirebaseIntegerValue? LimiteUsuarios { get; set; }
            public FirebaseIntegerValue? usuariosUsados { get; set; }
            public FirebaseIntegerValue? UsuariosUsados { get; set; }
        }
        private class FirebaseStringValue { public string StringValue { get; set; } = ""; }
        private class FirebaseIntegerValue { public string IntegerValue { get; set; } = "0"; }
    }
}
