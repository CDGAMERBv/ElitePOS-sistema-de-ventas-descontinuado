using ElitePOS.Shared.Models;

namespace ElitePOS.Services;

public interface IPlanLimitesService
{
    /// <summary>
    /// Obtiene los límites según el plan actual de la empresa
    /// </summary>
    Task<LimitesPlanModel> ObtenerLimitesPlan();

    /// <summary>
    /// Verifica si se puede agregar un nuevo producto
    /// </summary>
    Task<(bool Permitido, string Mensaje)> PuedeAgregarProducto();

    /// <summary>
    /// Verifica si se puede agregar un nuevo usuario
    /// </summary>
    Task<(bool Permitido, string Mensaje)> PuedeAgregarUsuario();

    /// <summary>
    /// Verifica si se puede registrar una venta este mes
    /// </summary>
    Task<(bool Permitido, string Mensaje)> PuedeRegistrarVenta();

    /// <summary>
    /// Verifica si se puede agregar un nuevo almacén
    /// </summary>
    Task<(bool Permitido, string Mensaje)> PuedeAgregarAlmacen();

    /// <summary>
    /// Verifica si el plan actual está vencido
    /// </summary>
    Task<(bool Vencido, DateTime? FechaVencimiento)> VerificarVencimiento();

    /// <summary>
    /// Verifica si la empresa está en período de prueba
    /// </summary>
    Task<bool> EstaEnPrueba();
}


