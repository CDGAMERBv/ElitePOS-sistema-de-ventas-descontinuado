using ElitePOS.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace ElitePOS.Services
{
    public class ClientesService : IClientesService
    {
        private readonly HttpClient _httpClient;
        private const string FirebaseUrl = "https://firestore.googleapis.com/v1/projects/TU_FIREBASE_PROJECT_ID/databases/(default)/documents/clientes";

        public ClientesService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<ClienteModel>> ObtenerClientes()
        {
            try
            {
                Console.WriteLine("🔄 Obteniendo clientes desde Firebase...");

                var response = await _httpClient.GetFromJsonAsync<FirestoreListResponse>(FirebaseUrl);

                if (response?.Documents == null || !response.Documents.Any())
                {
                    Console.WriteLine("⚠️ No se encontraron clientes");
                    return new List<ClienteModel>();
                }

                var clientes = response.Documents.Select(doc => new ClienteModel
                {
                    id = doc.Name.Split('/').Last(),
                    dniRuc = doc.Fields.dniRuc?.StringValue ?? string.Empty,
                    nombre = doc.Fields.nombre?.StringValue ?? string.Empty,
                    telefono = doc.Fields.telefono?.StringValue ?? string.Empty,
                    direccion = doc.Fields.direccion?.StringValue ?? string.Empty,
                    fotoUrl = doc.Fields.fotoUrl?.StringValue ?? string.Empty
                }).ToList();

                Console.WriteLine($"✅ {clientes.Count} clientes obtenidos");
                return clientes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener clientes: {ex.Message}");
                return new List<ClienteModel>();
            }
        }

        public async Task<ClienteModel?> ObtenerClientePorId(string id)
        {
            try
            {
                var url = $"{FirebaseUrl}/{id}";
                var response = await _httpClient.GetFromJsonAsync<FirestoreDocument>(url);

                if (response?.Fields == null)
                    return null;

                return new ClienteModel
                {
                    id = id,
                    dniRuc = response.Fields.dniRuc?.StringValue ?? string.Empty,
                    nombre = response.Fields.nombre?.StringValue ?? string.Empty,
                    telefono = response.Fields.telefono?.StringValue ?? string.Empty,
                    direccion = response.Fields.direccion?.StringValue ?? string.Empty,
                    fotoUrl = response.Fields.fotoUrl?.StringValue ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener cliente: {ex.Message}");
                return null;
            }
        }

        public async Task<IEnumerable<ClienteModel>> BuscarClientes(string busqueda)
        {
            try
            {
                var todosClientes = await ObtenerClientes();

                if (string.IsNullOrWhiteSpace(busqueda))
                    return new List<ClienteModel>();

                var busquedaLower = busqueda.ToLower();
                return todosClientes.Where(c =>
                    c.nombre.ToLower().Contains(busquedaLower) ||
                    c.dniRuc.Contains(busqueda)
                ).Take(5).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al buscar clientes: {ex.Message}");
                return new List<ClienteModel>();
            }
        }

        public async Task<ClienteModel?> ObtenerClientePorDniRuc(string dniRuc)
        {
            try
            {
                var todos = await ObtenerClientes();
                return todos.FirstOrDefault(c => c.dniRuc == dniRuc);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener por DNI/RUC: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> AgregarCliente(ClienteModel cliente)
        {
            try
            {
                Console.WriteLine($"🔄 Agregando cliente: {cliente.nombre}");

                var firestoreDoc = new
                {
                    fields = new
                    {
                        dniRuc = new { stringValue = cliente.dniRuc },
                        nombre = new { stringValue = cliente.nombre },
                        telefono = new { stringValue = cliente.telefono },
                        direccion = new { stringValue = cliente.direccion },
                        fotoUrl = new { stringValue = cliente.fotoUrl }
                    }
                };

                var url = $"{FirebaseUrl}?documentId={cliente.id}";
                var response = await _httpClient.PostAsJsonAsync(url, firestoreDoc);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Cliente agregado: {cliente.nombre}");
                    return true;
                }

                Console.WriteLine($"❌ Error al agregar cliente: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al agregar cliente: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EditarCliente(ClienteModel cliente)
        {
            try
            {
                Console.WriteLine($"🔄 Editando cliente: {cliente.nombre}");

                var firestoreDoc = new
                {
                    fields = new
                    {
                        dniRuc = new { stringValue = cliente.dniRuc },
                        nombre = new { stringValue = cliente.nombre },
                        telefono = new { stringValue = cliente.telefono },
                        direccion = new { stringValue = cliente.direccion },
                        fotoUrl = new { stringValue = cliente.fotoUrl }
                    }
                };

                var url = $"{FirebaseUrl}/{cliente.id}?updateMask.fieldPaths=dniRuc&updateMask.fieldPaths=nombre&updateMask.fieldPaths=telefono&updateMask.fieldPaths=direccion&updateMask.fieldPaths=fotoUrl";
                var response = await _httpClient.PatchAsJsonAsync(url, firestoreDoc);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Cliente editado: {cliente.nombre}");
                    return true;
                }

                Console.WriteLine($"❌ Error al editar cliente: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al editar cliente: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EliminarCliente(string id)
        {
            try
            {
                Console.WriteLine($"🔄 Eliminando cliente: {id}");

                var url = $"{FirebaseUrl}/{id}";
                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Cliente eliminado");
                    return true;
                }

                Console.WriteLine($"❌ Error al eliminar cliente: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al eliminar cliente: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ActualizarDeudaCliente(string clienteId, decimal cambioDeuda)
        {
            await Task.CompletedTask;
            return true;
        }

        private class FirestoreListResponse
        {
            public List<FirestoreDocument> Documents { get; set; } = new();
        }

        private class FirestoreDocument
        {
            public string Name { get; set; } = string.Empty;
            public FirestoreFields Fields { get; set; } = new();
        }

        private class FirestoreFields
        {
            public FirestoreValue? dniRuc { get; set; }
            public FirestoreValue? nombre { get; set; }
            public FirestoreValue? telefono { get; set; }
            public FirestoreValue? direccion { get; set; }
            public FirestoreValue? fotoUrl { get; set; }
        }

        private class FirestoreValue
        {
            public string? StringValue { get; set; }
        }
    }
}
