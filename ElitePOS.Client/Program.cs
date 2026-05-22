using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ElitePOS.Client;
using ElitePOS.Client.Services;
using ElitePOS.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. HttpClient Base
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// 2. Servicios de UI (Locales)
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddSingleton<ISesionService, SesionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardStateService, DashboardStateService>();
builder.Services.AddScoped<PuntoVentaStateService>();
builder.Services.AddScoped<GestionStateService>();

// 3. Servicios de Lógica / Datos (Shared Services via REST)
builder.Services.AddScoped<IInventarioService, FirestoreService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IClientesService, ClientesService>();
builder.Services.AddScoped<IConfiguracionImpresionService, ConfiguracionImpresionService>();
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<IComprasService, ComprasService>();
builder.Services.AddScoped<IProformasService, ProformasService>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped<IGastosService, GastosService>();
builder.Services.AddScoped<IAbonosService, AbonosService>();
builder.Services.AddScoped<ILogsService, LogsService>();
builder.Services.AddScoped<IKardexService, KardexService>();
builder.Services.AddScoped<IConfiguracionEmpresaService, ConfiguracionEmpresaService>();
builder.Services.AddScoped<IConfiguracionSunatService, ConfiguracionSunatService>();
builder.Services.AddScoped<IConfiguracionAlmacenesService, ConfiguracionAlmacenesService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IPlanLimitesService, PlanLimitesService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IImportacionExcelService, ImportacionExcelService>();
builder.Services.AddScoped<IValidacionDocumentosService, ValidacionDocumentosService>();
builder.Services.AddScoped<IComprobanteSunatService, ComprobanteSunatService>();
builder.Services.AddScoped<INumeracionService, NumeracionService>();
builder.Services.AddScoped<IActualizacionTiempoRealService, ActualizacionTiempoRealService>();
builder.Services.AddScoped<ISincronizacionOfflineService, SincronizacionOfflineService>();
builder.Services.AddScoped<IFacturacionService, FacturacionService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();

// 4. Kardex Queue (Consumido por VentasService)
builder.Services.AddScoped<IKardexQueueService, KardexQueueClientService>();

builder.Services.AddScoped<IAsistenteIAService, AsistenteIAServiceProxy>();

// 4. Seguridad
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();


