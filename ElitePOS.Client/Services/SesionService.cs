using ElitePOS.Services;
using ElitePOS.Shared.Models;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;

namespace ElitePOS.Client.Services
{
    public class SesionService : ISesionService
    {
        private SesionUsuarioModel _usuarioActual = new();
        private readonly IJSRuntime? _jsRuntime;
        private AuthenticationStateProvider? _authenticationStateProvider;

        public SesionUsuarioModel UsuarioActual => _usuarioActual;
        public SesionUsuarioModel usuarioActual => _usuarioActual; // Alias

        public event Action? OnSesionCambiada;

        public SesionService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            // Ya no iniciamos sesión automática - se requiere login
        }

        public void SetAuthenticationStateProvider(AuthenticationStateProvider authenticationStateProvider)
        {
            _authenticationStateProvider = authenticationStateProvider;
        }

        // previously had compatibility methods for initial splash; removed

        public async Task IniciarSesion(string usuarioId, string nombre, string correo, string rol, string empresaId)
        {
            _usuarioActual = new SesionUsuarioModel
            {
                usuarioId = usuarioId,
                nombre = nombre,
                correo = correo,
                rol = rol?.Trim()?.ToUpper() ?? string.Empty,
                empresaId = empresaId,
                estaAutenticado = true
            };

            // Guardar en localStorage
            if (_jsRuntime != null)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "sesion_usuario", System.Text.Json.JsonSerializer.Serialize(_usuarioActual));
            }

            Console.WriteLine($"✅ Sesión iniciada: {nombre} ({rol?.Trim().ToUpper()}) - EsAdmin: {EsAdmin()}");
            OnSesionCambiada?.Invoke();

            // Notificar al AuthenticationStateProvider
            if (_authenticationStateProvider is CustomAuthenticationStateProvider customProvider)
            {
                customProvider.NotificarCambioSesion();
            }
        }

        public void CerrarSesion()
        {
            _usuarioActual = new SesionUsuarioModel();

            // Limpiar localStorage
            if (_jsRuntime != null)
            {
                try
                {
                    _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "sesion_usuario");
                }
                catch { /* Ignorar errores */ }
            }

            Console.WriteLine("🚪 Sesión cerrada");
            OnSesionCambiada?.Invoke();

            // Notificar al AuthenticationStateProvider
            if (_authenticationStateProvider is CustomAuthenticationStateProvider customProvider)
            {
                customProvider.NotificarCambioSesion();
            }
        }

        public async Task CargarSesionDesdeStorage()
        {
            if (_jsRuntime == null) return;

            try
            {
                var sesionJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "sesion_usuario");

                if (!string.IsNullOrEmpty(sesionJson))
                {
                    var sesion = System.Text.Json.JsonSerializer.Deserialize<SesionUsuarioModel>(sesionJson);
                    if (sesion != null && sesion.estaAutenticado)
                    {
                        // Limpiar rol de espacios
                        sesion.rol = sesion.rol?.Trim()?.ToUpper() ?? string.Empty;
                        _usuarioActual = sesion;
                        Console.WriteLine($"✅ Sesión recuperada: {sesion.nombre} ({sesion.rol}) - EsAdmin: {EsAdmin()}");
                        OnSesionCambiada?.Invoke();

                        // Notificar al AuthenticationStateProvider
                        if (_authenticationStateProvider is CustomAuthenticationStateProvider customProvider)
                        {
                            customProvider.NotificarCambioSesion();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error al cargar sesión: {ex.Message}");
            }
        }

        public bool EsAdmin()
        {
            return UsuarioActual?.rol?.Trim().Equals("ADMINISTRADOR", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public bool TieneAcceso(string modulo)
        {
            if (!_usuarioActual.estaAutenticado)
                return false;

            // ADMIN tiene acceso a todo
            if (EsAdmin())
                return true;

            // CAJERO solo tiene acceso limitado
            var modulosPermitidosCajero = new[]
            {
                "ventas",
                "compras",
                "inventario",
                "historial-ventas",
                "historial-compras",
                "historial-proformas",
                "clientes",
                "cuentas-por-cobrar" // Permitir acceso a cuentas por cobrar para cajeros también
            };

            return modulosPermitidosCajero.Contains(modulo?.Trim().ToLower());
        }
    }
}


