using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models;

public class ConfiguracionEmpresaModel
{
    [JsonPropertyName("id")]
    public string id { get; set; } = "configuracion-principal";

    [JsonPropertyName("empresaid")]
    public string empresaId { get; set; } = "empresa-demo";

    [JsonPropertyName("logourl")]
    public string? logoUrl { get; set; }

    [JsonIgnore] public string? LogoUrl { get => logoUrl; set => logoUrl = value; }

    [JsonPropertyName("ruc")]
    public string? ruc { get; set; }

    [JsonPropertyName("razonsocial")]
    public string razonSocial { get; set; } = string.Empty;

    [JsonPropertyName("nombrecomercial")]
    public string nombreComercial { get; set; } = string.Empty;

    [JsonPropertyName("direccion")]
    public string? direccion { get; set; }

    [JsonPropertyName("distrito")]
    public string? distrito { get; set; }

    [JsonPropertyName("provincia")]
    public string? provincia { get; set; }

    [JsonPropertyName("departamento")]
    public string? departamento { get; set; }

    [JsonPropertyName("telefono")]
    public string? telefono { get; set; }

    [JsonPropertyName("correo")]
    public string? correo { get; set; }

    [JsonPropertyName("moneda")]
    public string moneda { get; set; } = "S/";

    [JsonPropertyName("simbolomoneda")]
    public string simboloMoneda { get; set; } = "S/";

    [JsonPropertyName("afectacionigvdefecto")]
    public string afectacionIgvDefecto { get; set; } = "10";

    [JsonPropertyName("permitirventasinstock")]
    public bool permitirVentaSinStock { get; set; } = false;

    [JsonPropertyName("clienteanonimodefecto")]
    public string clienteAnonimoDefecto { get; set; } = "PÚBLICO EN GENERAL";

    [JsonPropertyName("cuentasbancarias")]
    public string? cuentasBancarias { get; set; }

    [JsonPropertyName("resolucionautorizacion")]
    public string? resolucionAutorizacion { get; set; }

    [JsonPropertyName("urlconsulta")]
    public string? urlConsulta { get; set; }

    [JsonPropertyName("planactual")]
    public string planActual { get; set; } = "infinity";

    [JsonPropertyName("fechavencimiento")]
    public DateTime fechaVencimiento { get; set; } = DateTime.Now.AddDays(15);

    [JsonPropertyName("esprueba")]
    public bool esPrueba { get; set; } = true;

    [JsonPropertyName("fechacreacion")]
    public DateTime fechaCreacion { get; set; } = DateTime.Now;

    [JsonPropertyName("activo")]
    public bool activo { get; set; } = true;

    [JsonPropertyName("modulocajachicaactivo")]
    public bool moduloCajaChicaActivo { get; set; } = false;

    [JsonPropertyName("asistenteiaactivado")]
    public bool asistenteIaActivado { get; set; } = false;

    [JsonPropertyName("integraciones")]
    public IntegracionesModel integraciones { get; set; } = new();

    // Propiedades de compatibilidad (Alias)
    [JsonIgnore] public bool ModuloCajaChicaActivo { get => moduloCajaChicaActivo; set => moduloCajaChicaActivo = value; }
    [JsonIgnore] public bool AsistenteIAActivado { get => asistenteIaActivado; set => asistenteIaActivado = value; }
    [JsonIgnore] public string? Id { get => id; set => id = value; }
    [JsonIgnore] public string? Ruc { get => ruc; set => ruc = value; }
    [JsonIgnore] public string? RazonSocial { get => razonSocial; set => razonSocial = value; }
    [JsonIgnore] public string? NombreComercial { get => nombreComercial; set => nombreComercial = value; }
    [JsonIgnore] public string? Direccion { get => direccion; set => direccion = value; }
    [JsonIgnore] public string? Distrito { get => distrito; set => distrito = value; }
    [JsonIgnore] public string? Provincia { get => provincia; set => provincia = value; }
    [JsonIgnore] public string? Departamento { get => departamento; set => departamento = value; }
    [JsonIgnore] public string? Telefono { get => telefono; set => telefono = value; }
    [JsonIgnore] public string? Correo { get => correo; set => correo = value; }
    [JsonIgnore] public string? CuentasBancarias { get => cuentasBancarias; set => cuentasBancarias = value; }
    [JsonIgnore] public string? ResolucionAutorizacion { get => resolucionAutorizacion; set => resolucionAutorizacion = value; }
    [JsonIgnore] public string? UrlConsulta { get => urlConsulta; set => urlConsulta = value; }
    [JsonIgnore] public string EmpresaId { get => empresaId; set => empresaId = value; }
    [JsonIgnore] public IntegracionesModel Integraciones { get => integraciones; set => integraciones = value; }
}

public class IntegracionesModel
{
    [JsonPropertyName("whatsapp")]
    public WhatsAppConfigModel whatsapp { get; set; } = new();

    // Propiedades de compatibilidad (Alias)
    [JsonIgnore] public WhatsAppConfigModel WhatsApp { get => whatsapp; set => whatsapp = value; }
}

public class WhatsAppConfigModel
{
    [JsonPropertyName("isEnabled")]
    public bool isEnabled { get; set; } = false;

    [JsonPropertyName("serviceUrl")]
    public string serviceUrl { get; set; } = "http://localhost:3000";

    [JsonPropertyName("instanceId")]
    public string instanceId { get; set; } = "empresa_default";

    [JsonPropertyName("botPrompt")]
    public string botPrompt { get; set; } = "Eres un asistente amable de ElitePOS. Responde dudas sobre productos y precios.";

    [JsonPropertyName("promptPersonalizado")]
    public string promptPersonalizado { get; set; } = string.Empty;

    [JsonIgnore] public bool IsEnabled { get => isEnabled; set => isEnabled = value; }
    [JsonIgnore] public bool IsBotEnabled { get => isEnabled; set => isEnabled = value; }
    [JsonIgnore] public string InstanceId { get => instanceId; set => instanceId = value; }
    [JsonIgnore] public string ServiceUrl { get => serviceUrl; set => serviceUrl = value; }
    [JsonIgnore] public string BotPrompt { get => botPrompt; set => botPrompt = value; }
    [JsonIgnore] public string PromptPersonalizado { get => promptPersonalizado; set => promptPersonalizado = value; }
}
