using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IUsuariosService
    {
        Task<List<UsuarioModel>> ObtenerUsuarios();
        Task<UsuarioModel?> ObtenerUsuarioPorId(string id);
        Task<bool> GuardarUsuario(UsuarioModel usuario);
        Task<bool> EliminarUsuario(string id);
        Task<UsuarioModel?> AutenticarUsuario(string nombreUsuario, string contrasena);
        Task AsegurarUsuarioAdmin();
    }
}


