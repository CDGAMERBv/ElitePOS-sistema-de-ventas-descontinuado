using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IClientesService
    {
        Task<IEnumerable<ClienteModel>> ObtenerClientes();
        Task<ClienteModel?> ObtenerClientePorId(string id);
        Task<IEnumerable<ClienteModel>> BuscarClientes(string busqueda);
        Task<ClienteModel?> ObtenerClientePorDniRuc(string dniRuc); // ⭐ NUEVO
        Task<bool> AgregarCliente(ClienteModel cliente);
        Task<bool> EditarCliente(ClienteModel cliente);
        Task<bool> EliminarCliente(string id);
        Task<bool> ActualizarDeudaCliente(string clienteId, decimal cambioDeuda); // ⭐ NUEVO: Para actualizar deuda
    }
}


