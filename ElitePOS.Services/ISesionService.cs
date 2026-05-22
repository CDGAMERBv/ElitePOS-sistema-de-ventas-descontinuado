using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface ISesionService
    {
        SesionUsuarioModel UsuarioActual { get; }
        SesionUsuarioModel usuarioActual { get; } // Alias para compatibilidad
        event Action? OnSesionCambiada;

        Task IniciarSesion(string usuarioId, string nombre, string correo, string rol, string empresaId);
        void CerrarSesion();
        bool TieneAcceso(string modulo);
        Task CargarSesionDesdeStorage();
        bool EsAdmin();
        // SetAuthenticationStateProvider movido a la implementación para evitar dependencia de Blazor en la interfaz compartida
    }
}


