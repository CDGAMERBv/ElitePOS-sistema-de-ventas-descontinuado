using ElitePOS.Services;
using ElitePOS.Shared.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace ElitePOS.Client.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ISesionService _sesionService;

        public CustomAuthenticationStateProvider(ISesionService sesionService)
        {
            _sesionService = sesionService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // ULTRA-ROBUSTO: Leer directamente desde localStorage sin esperas
                Console.WriteLine("🔍 AuthenticationState: Verificando sesión...");
                
                // Verificar estado actual del SesionService
                var usuario = _sesionService.UsuarioActual;
                
                // Si no hay sesión en memoria, intentar cargarla directamente
                if (usuario?.estaAutenticado != true || string.IsNullOrEmpty(usuario.usuarioId))
                {
                    Console.WriteLine("📥 AuthenticationState: Cargando sesión desde localStorage...");
                    await _sesionService.CargarSesionDesdeStorage();
                    usuario = _sesionService.UsuarioActual;
                }

                if (usuario?.estaAutenticado == true && !string.IsNullOrEmpty(usuario.usuarioId))
                {
                    // Usuario autenticado - crear ClaimsPrincipal al instante
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuario.usuarioId),
                        new Claim(ClaimTypes.Name, usuario.nombre),
                        new Claim(ClaimTypes.Email, usuario.correo ?? ""),
                        new Claim(ClaimTypes.Role, usuario.rol),
                        new Claim("EmpresaId", usuario.empresaId ?? "")
                    };

                    var identity = new ClaimsIdentity(claims, "custom");
                    var principal = new ClaimsPrincipal(identity);

                    Console.WriteLine($"✅ AuthenticationState: Usuario autenticado INSTANTÁNEO - {usuario.nombre} ({usuario.rol})");
                    return new AuthenticationState(principal);
                }
                else
                {
                    // Usuario no autenticado
                    Console.WriteLine("❌ AuthenticationState: Usuario no autenticado - sesión vacía");
                    return new AuthenticationState(new ClaimsPrincipal());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en GetAuthenticationStateAsync: {ex.Message}");
                return new AuthenticationState(new ClaimsPrincipal());
            }
        }

        public void NotificarCambioSesion()
        {
            // Notificar que el estado de autenticación ha cambiado
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}


