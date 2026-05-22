using ElitePOS.Services;
using ElitePOS.Shared.Models;

namespace ElitePOS.Server.Services
{
    // Esta es una versión para el servidor que no usa el navegador
    public class ServerSesionService : ISesionService
    {
        public SesionUsuarioModel UsuarioActual { get; } = new SesionUsuarioModel 
        { 
            EmpresaId = "SISTEMA",
            Nombre = "SISTEMA",
            Rol = "ADMINISTRADOR",
            EstaAutenticado = true
        };

        public SesionUsuarioModel? usuarioActual => UsuarioActual;

        public event Action? OnSesionCambiada { add { } remove { } }

        public Task IniciarSesion(string usuarioId, string nombre, string correo, string rol, string empresaId) => Task.CompletedTask;
        
        public void CerrarSesion() { }

        public bool TieneAcceso(string modulo) => true;

        public Task CargarSesionDesdeStorage() => Task.CompletedTask;

        public bool EsAdmin() => true;
    }
}
