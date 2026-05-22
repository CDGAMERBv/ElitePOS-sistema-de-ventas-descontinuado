using ElitePOS.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElitePOS.Services
{
    public class ConfiguracionAlmacenesService : IConfiguracionAlmacenesService
    {
        private readonly HttpClient _http;
        private const string FIREBASE_URL = "https://firestore.googleapis.com/v1/projects/TU_FIREBASE_PROJECT_ID/databases/(default)/documents/ConfiguracionAlmacenes";
        private const string DOCUMENTO_ID = "config-almacenes";

        public ConfiguracionAlmacenesService(HttpClient http)
        {
            _http = http;
        }

        public async Task<ConfiguracionAlmacenesModel> ObtenerConfiguracion()
        {
            try
            {
                var response = await _http.GetFromJsonAsync<FirebaseDocumentResponse>($"{FIREBASE_URL}/{DOCUMENTO_ID}");
                
                if (response?.Fields != null)
                {
                    var listaAlmacenes = new List<AlmacenModel>();
                    var values = response.Fields.almacenes?.ArrayValue?.Values ?? response.Fields.Almacenes?.ArrayValue?.Values;
                    
                    if (values != null)
                    {
                        foreach (var value in values)
                        {
                            if (value.MapValue?.Fields != null)
                            {
                                var f = value.MapValue.Fields;
                                listaAlmacenes.Add(new AlmacenModel
                                {
                                    id = f.id?.StringValue ?? (f.Id?.StringValue ?? ""),
                                    codigo = f.codigo?.StringValue ?? (f.Codigo?.StringValue ?? ""),
                                    nombre = f.nombre?.StringValue ?? (f.Nombre?.StringValue ?? ""),
                                    ubicacion = f.ubicacion?.StringValue ?? (f.Ubicacion?.StringValue ?? ""),
                                    activo = f.activo?.BooleanValue ?? (f.Activo?.BooleanValue ?? true),
                                    IsLocked = true
                                });
                            }
                        }
                    }

                    return new ConfiguracionAlmacenesModel
                    {
                        id = DOCUMENTO_ID,
                        notificarStockMinimo = response.Fields.notificarStockMinimo?.BooleanValue ?? (response.Fields.NotificarStockMinimo?.BooleanValue ?? true),
                        almacenes = listaAlmacenes
                    };
                }

                return new ConfiguracionAlmacenesModel { id = DOCUMENTO_ID };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener configuración de almacenes: {ex.Message}");
                return new ConfiguracionAlmacenesModel { id = DOCUMENTO_ID };
            }
        }

        public async Task<bool> GuardarConfiguracion(ConfiguracionAlmacenesModel config)
        {
            try
            {
                var almacenesArray = config.almacenes.Select(a => new
                {
                    mapValue = new
                    {
                        fields = new
                        {
                            id = new { stringValue = a.id },
                            codigo = new { stringValue = a.codigo },
                            nombre = new { stringValue = a.nombre },
                            ubicacion = new { stringValue = a.ubicacion },
                            activo = new { booleanValue = a.activo }
                        }
                    }
                }).ToList();

                var firebaseData = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["notificarStockMinimo"] = new { booleanValue = config.notificarStockMinimo },
                        ["almacenes"] = new { arrayValue = new { values = almacenesArray } }
                    }
                };

                const string updateMask = "?updateMask.fieldPaths=notificarStockMinimo&updateMask.fieldPaths=almacenes";
                var response = await _http.PatchAsJsonAsync($"{FIREBASE_URL}/{DOCUMENTO_ID}{updateMask}", firebaseData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al guardar configuración: {ex.Message}");
                return false;
            }
        }

        private class FirebaseDocumentResponse { public ConfigFields Fields { get; set; } = new(); }
        private class ConfigFields {
            public FirebaseBooleanValue? notificarStockMinimo { get; set; }
            public FirebaseBooleanValue? NotificarStockMinimo { get; set; }
            public FirebaseArrayValue? almacenes { get; set; }
            public FirebaseArrayValue? Almacenes { get; set; }
        }
        private class FirebaseArrayValue { public ArrayValue? ArrayValue { get; set; } }
        private class ArrayValue { public List<MapValueWrapper> Values { get; set; } = new(); }
        private class MapValueWrapper { public MapValue? MapValue { get; set; } }
        private class MapValue { public AlmacenFields? Fields { get; set; } }
        private class AlmacenFields {
            public FirebaseStringValue? id { get; set; }
            public FirebaseStringValue? Id { get; set; }
            public FirebaseStringValue? codigo { get; set; }
            public FirebaseStringValue? Codigo { get; set; }
            public FirebaseStringValue? nombre { get; set; }
            public FirebaseStringValue? Nombre { get; set; }
            public FirebaseStringValue? ubicacion { get; set; }
            public FirebaseStringValue? Ubicacion { get; set; }
            public FirebaseBooleanValue? activo { get; set; }
            public FirebaseBooleanValue? Activo { get; set; }
        }
        private class FirebaseStringValue { public string StringValue { get; set; } = ""; }
        private class FirebaseBooleanValue { public bool BooleanValue { get; set; } }
    }
}
