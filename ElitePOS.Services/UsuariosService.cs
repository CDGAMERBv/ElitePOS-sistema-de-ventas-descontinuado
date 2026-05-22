using ElitePOS.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElitePOS.Services
{
    public class UsuariosService : IUsuariosService
    {
        private readonly HttpClient _http;
        private const string FIREBASE_URL = "https://firestore.googleapis.com/v1/projects/TU_FIREBASE_PROJECT_ID/databases/(default)/documents/Usuarios";

        public UsuariosService(HttpClient http)
        {
            _http = http;
        }

        // ═══════════════════════════════════════════════════════════
        // SEGURIDAD: HASHING DE CONTRASEÑAS CON BCRYPT
        // ═══════════════════════════════════════════════════════════

        private static string HashearContrasena(string contrasenaPlana)
        {
            return BCrypt.Net.BCrypt.HashPassword(contrasenaPlana, workFactor: 11);
        }

        private static bool VerificarContrasena(string contrasenaPlana, string hashAlmacenado)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(contrasenaPlana, hashAlmacenado);
            }
            catch
            {
                return false;
            }
        }

        private static bool EsHashBCrypt(string valor)
        {
            return !string.IsNullOrEmpty(valor) && valor.StartsWith("$2");
        }

        public async Task<List<UsuarioModel>> ObtenerUsuarios()
        {
            try
            {
                var response = await _http.GetFromJsonAsync<FirebaseListResponse>(FIREBASE_URL);
                var usuarios = new List<UsuarioModel>();

                if (response?.Documents != null)
                {
                    foreach (var doc in response.Documents)
                    {
                        var usuario = new UsuarioModel
                        {
                            id = doc.Name.Split('/').Last(),
                            empresaId = doc.Fields.empresaId?.StringValue ?? "empresa-demo",
                            nombreCompleto = (doc.Fields.nombreCompleto?.StringValue ?? doc.Fields.nombre?.StringValue) ?? "",
                            nombreUsuario = doc.Fields.nombreUsuario?.StringValue ?? "",
                            correo = doc.Fields.correo?.StringValue ?? "",
                            contrasena = "",
                            rol = doc.Fields.rol?.StringValue ?? "Cajero",
                            activo = doc.Fields.activo?.BooleanValue ?? true
                        };
                        usuarios.Add(usuario);
                    }
                }

                Console.WriteLine($"✅ Usuarios obtenidos: {usuarios.Count}");
                return usuarios;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener usuarios: {ex.Message}");
                return new List<UsuarioModel>();
            }
        }

        public async Task<UsuarioModel?> ObtenerUsuarioPorId(string id)
        {
            try
            {
                var response = await _http.GetFromJsonAsync<FirebaseDocumentResponse>($"{FIREBASE_URL}/{id}");
                
                if (response?.Fields != null)
                {
                    return new UsuarioModel
                    {
                        id = id,
                        empresaId = response.Fields.empresaId?.StringValue ?? "empresa-demo",
                        nombreCompleto = (response.Fields.nombreCompleto?.StringValue ?? response.Fields.nombre?.StringValue) ?? "",
                        nombreUsuario = response.Fields.nombreUsuario?.StringValue ?? "",
                        correo = response.Fields.correo?.StringValue ?? "",
                        contrasena = "",
                        rol = response.Fields.rol?.StringValue ?? "Cajero",
                        activo = response.Fields.activo?.BooleanValue ?? true
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener usuario {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> GuardarUsuario(UsuarioModel usuario)
        {
            try
            {
                // 🔒 HASHEAR CONTRASEÑA: Solo si no está ya hasheada
                var contrasenaParaGuardar = usuario.contrasena;
                if (!string.IsNullOrEmpty(contrasenaParaGuardar) && !EsHashBCrypt(contrasenaParaGuardar))
                {
                    contrasenaParaGuardar = HashearContrasena(contrasenaParaGuardar);
                    Console.WriteLine($"🔒 Contraseña hasheada con BCrypt para: {usuario.nombreUsuario}");
                }

                var firebaseData = new
                {
                    fields = new
                    {
                        empresaId = new { stringValue = usuario.empresaId },
                        nombreCompleto = new { stringValue = usuario.nombreCompleto },
                        nombreUsuario = new { stringValue = usuario.nombreUsuario },
                        correo = new { stringValue = usuario.correo },
                        contrasena = new { stringValue = contrasenaParaGuardar },
                        rol = new { stringValue = usuario.rol },
                        activo = new { booleanValue = usuario.activo }
                    }
                };

                string url;
                HttpResponseMessage response;

                if (string.IsNullOrEmpty(usuario.id))
                {
                    usuario.id = Guid.NewGuid().ToString();
                    url = $"{FIREBASE_URL}?documentId={usuario.id}";
                    response = await _http.PostAsJsonAsync(url, firebaseData);
                }
                else
                {
                    url = $"{FIREBASE_URL}/{usuario.id}";
                    response = await _http.PatchAsJsonAsync(url, firebaseData);
                }

                Console.WriteLine($"💾 Usuario guardado: {usuario.nombreCompleto}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al guardar usuario: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EliminarUsuario(string id)
        {
            try
            {
                var response = await _http.DeleteAsync($"{FIREBASE_URL}/{id}");
                Console.WriteLine($"🗑️ Usuario eliminado: {id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al eliminar usuario: {ex.Message}");
                return false;
            }
        }

        public async Task<UsuarioModel?> AutenticarUsuario(string nombreUsuario, string contrasena)
        {
            try
            {
                // Primero asegurar que exista el usuario admin
                await AsegurarUsuarioAdmin();

                // Obtener documentos RAW con hash incluido para verificar
                var httpResponse = await _http.GetAsync(FIREBASE_URL);
                
                if (!httpResponse.IsSuccessStatusCode)
                {
                    var statusCode = (int)httpResponse.StatusCode;
                    var errorMsg = $"Error de conexión con el servidor (HTTP {statusCode}). Verifique su conexión o cuota de red.";
                    Console.WriteLine($"❌ Error de red/cuota al autenticar: {statusCode}");
                    throw new Exception(errorMsg); // Lanzar excepción para que Login.razor la muestre
                }

                var response = await httpResponse.Content.ReadFromJsonAsync<FirebaseListResponse>();
                if (response?.Documents == null) return null;

                foreach (var doc in response.Documents)
                {
                    var nombreUsr = doc.Fields.nombreUsuario?.StringValue ?? "";
                    var hashAlmacenado = doc.Fields.contrasena?.StringValue ?? "";
                    var activo = doc.Fields.activo?.BooleanValue ?? true;

                    if (nombreUsr != nombreUsuario || !activo) continue;

                    bool contrasenaValida;

                    if (EsHashBCrypt(hashAlmacenado))
                    {
                        // 🔒 Verificar contra hash BCrypt
                        contrasenaValida = VerificarContrasena(contrasena, hashAlmacenado);
                    }
                    else
                    {
                        // ⚠️ MIGRACIÓN: Contraseña legacy en texto plano
                        contrasenaValida = hashAlmacenado == contrasena;

                        if (contrasenaValida)
                        {
                            // Migrar automáticamente a BCrypt
                            Console.WriteLine($"🔄 Migrando contraseña de {nombreUsr} a BCrypt...");
                            var id = doc.Name.Split('/').Last();
                            var nuevoHash = HashearContrasena(contrasena);
                            await ActualizarHashContrasena(id, nuevoHash);
                            Console.WriteLine($"✅ Contraseña de {nombreUsr} migrada a BCrypt");
                        }
                    }

                    if (contrasenaValida)
                    {
                        var usuario = new UsuarioModel
                        {
                            id = doc.Name.Split('/').Last(),
                            empresaId = doc.Fields.empresaId?.StringValue ?? "empresa-demo",
                            nombreCompleto = (doc.Fields.nombreCompleto?.StringValue ?? doc.Fields.nombre?.StringValue) ?? "",
                            nombreUsuario = nombreUsr,
                            correo = doc.Fields.correo?.StringValue ?? "",
                            contrasena = "",
                            rol = doc.Fields.rol?.StringValue ?? "Cajero",
                            activo = activo
                        };
                        Console.WriteLine($"✅ Usuario autenticado: {usuario.nombreUsuario} ({usuario.rol})");
                        return usuario;
                    }
                }

                Console.WriteLine($"❌ Autenticación fallida para: {nombreUsuario}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en autenticación: {ex.Message}");
                throw; // Propagar excepción a la UI
            }
        }

        private async Task ActualizarHashContrasena(string userId, string nuevoHash)
        {
            try
            {
                var patchData = new
                {
                    fields = new
                    {
                        contrasena = new { stringValue = nuevoHash }
                    }
                };
                var url = $"{FIREBASE_URL}/{userId}?updateMask.fieldPaths=contrasena";
                await _http.PatchAsJsonAsync(url, patchData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error migrando hash: {ex.Message}");
            }
        }

        public async Task AsegurarUsuarioAdmin()
        {
            try
            {
                var httpResponse = await _http.GetAsync(FIREBASE_URL);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorMsg = $"Error de red o Servidor (HTTP {(int)httpResponse.StatusCode}). Posible cuota excedida (429).";
                    Console.WriteLine($"❌ DETENIDO AsegurarUsuarioAdmin: {errorMsg}");
                    // Lanzar la excepción detiene la ejecución. 
                    // BAJO NINGÚN MOTIVO intentará crear el admin o hacer writes si falló la lectura.
                    throw new Exception(errorMsg);
                }

                var response = await httpResponse.Content.ReadFromJsonAsync<FirebaseListResponse>();
                
                var adminExistente = false;
                if (response?.Documents != null)
                {
                    adminExistente = response.Documents.Any(doc => 
                        (doc.Fields?.nombreUsuario?.StringValue ?? "") == "admin");
                }

                if (!adminExistente)
                {
                    // Crear usuario admin predeterminado con contraseña hasheada
                    var adminUser = new UsuarioModel
                    {
                        id = Guid.NewGuid().ToString(),
                        empresaId = "elitepos-demo",
                        nombreCompleto = "Administrador",
                        nombreUsuario = "admin",
                        correo = "admin@elitepos.com",
                        contrasena = "admin123",
                        rol = "Administrador",
                        activo = true
                    };

                    // GuardarUsuario hashea automáticamente
                    var guardado = await GuardarUsuario(adminUser);
                    if (guardado)
                    {
                        Console.WriteLine("✅ Usuario admin creado con contraseña hasheada (BCrypt)");
                    }
                    else
                    {
                        Console.WriteLine("❌ Error al crear usuario admin predeterminado");
                    }
                }
                else
                {
                    Console.WriteLine("✅ Usuario admin ya existe");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al asegurar usuario admin: {ex.Message}");
                throw; // Propagar la excepción hacia AutenticarUsuario
            }
        }

        private class FirebaseListResponse
        {
            public List<FirebaseDocumentResponse> Documents { get; set; } = new();
        }

        private class FirebaseDocumentResponse
        {
            public string Name { get; set; } = "";
            public UsuarioFields Fields { get; set; } = new();
        }

        private class UsuarioFields
        {
            public FirebaseStringValue? empresaId { get; set; }
            [JsonIgnore] public FirebaseStringValue? EmpresaId { get; set; }
            public FirebaseStringValue? nombreCompleto { get; set; }
            [JsonIgnore] public FirebaseStringValue? NombreCompleto { get; set; }
            public FirebaseStringValue? nombreUsuario { get; set; }
            [JsonIgnore] public FirebaseStringValue? NombreUsuario { get; set; }
            [JsonIgnore] public FirebaseStringValue? Nombre { get; set; }
            public FirebaseStringValue? nombre { get; set; }
            public FirebaseStringValue? correo { get; set; }
            [JsonIgnore] public FirebaseStringValue? Correo { get; set; }
            public FirebaseStringValue? contrasena { get; set; }
            [JsonIgnore] public FirebaseStringValue? Contrasena { get; set; }
            public FirebaseStringValue? rol { get; set; }
            [JsonIgnore] public FirebaseStringValue? Rol { get; set; }
            public FirebaseBooleanValue? activo { get; set; }
            [JsonIgnore] public FirebaseBooleanValue? Activo { get; set; }
        }

        private class FirebaseStringValue
        {
            public string StringValue { get; set; } = "";
        }

        private class FirebaseBooleanValue
        {
            public bool BooleanValue { get; set; }
        }
    }
}


