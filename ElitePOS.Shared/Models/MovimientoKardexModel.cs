using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models;

public class MovimientoKardexModel
{
    [JsonPropertyName("id")]
    public string id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("productoId")]
    public string productoId { get; set; } = string.Empty;

    [JsonPropertyName("productoNombre")]
    public string productoNombre { get; set; } = string.Empty;

    [JsonPropertyName("fecha")]
    public DateTime fecha { get; set; } = DateTime.Now;

    [JsonPropertyName("concepto")]
    public string concepto { get; set; } = string.Empty; // "Venta", "Compra", "Ajuste Manual", "Devolución"

    [JsonPropertyName("tipoMovimiento")]
    public string tipoMovimiento { get; set; } = string.Empty; // "Entrada" o "Salida"

    [JsonPropertyName("tipo")]
    public string tipo => tipoMovimiento;

    [JsonPropertyName("motivo")]
    public string motivo => concepto;

    [JsonPropertyName("cantidad")]
    public decimal cantidad { get; set; }

    [JsonPropertyName("saldoAnterior")]
    public decimal saldoAnterior { get; set; }

    [JsonPropertyName("saldoActual")]
    public decimal saldoActual { get; set; }

    [JsonPropertyName("stockResultante")]
    public decimal stockResultante => saldoActual;

    [JsonPropertyName("usuarioId")]
    public string usuarioId { get; set; } = string.Empty;

    [JsonPropertyName("usuarioNombre")]
    public string usuarioNombre { get; set; } = string.Empty;

    [JsonPropertyName("documentoReferencia")]
    public string? documentoReferencia { get; set; } // ID de venta, compra, etc.

    [JsonPropertyName("almacenId")]
    public string? almacenId { get; set; }

    [JsonPropertyName("almacenNombre")]
    public string? almacenNombre { get; set; }

    [JsonPropertyName("observaciones")]
    public string? observaciones { get; set; }

    // Propiedades de compatibilidad (Alias)
    [JsonIgnore] public string Id { get => id; set => id = value; }
    [JsonIgnore] public string ProductoId { get => productoId; set => productoId = value; }
    [JsonIgnore] public string ProductoNombre { get => productoNombre; set => productoNombre = value; }
    [JsonIgnore] public DateTime Fecha { get => fecha; set => fecha = value; }
    [JsonIgnore] public string Concepto { get => concepto; set => concepto = value; }
    [JsonIgnore] public string TipoMovimiento { get => tipoMovimiento; set => tipoMovimiento = value; }
    [JsonIgnore] public decimal Cantidad { get => cantidad; set => cantidad = value; }
    [JsonIgnore] public decimal SaldoAnterior { get => saldoAnterior; set => saldoAnterior = value; }
    [JsonIgnore] public decimal SaldoActual { get => saldoActual; set => saldoActual = value; }
}
