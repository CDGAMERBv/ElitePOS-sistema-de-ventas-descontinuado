using ElitePOS.Server.Services;
using ElitePOS.Services;
using ElitePOS.Shared.Models;
using Microsoft.AspNetCore.ResponseCompression;
using QuestPDF.Infrastructure;

// Configuración de licencia de QuestPDF (Requerido para .NET 8+)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Controladores y Swagger
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Configuración de CORS para el Bot de Node.js
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNodeBot", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 3. HttpClient para servicios que consumen APIs externas (como Gemini)
builder.Services.AddScoped(sp => new HttpClient());

// 4. Registro de Servicios compartidos en el Servidor (Firestore)
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
builder.Services.AddScoped<IImportacionExcelService, ImportacionExcelService>();
builder.Services.AddScoped<IValidacionDocumentosService, ValidacionDocumentosService>();
builder.Services.AddScoped<IComprobanteSunatService, ComprobanteSunatService>();
builder.Services.AddScoped<INumeracionService, NumeracionService>();
builder.Services.AddScoped<IActualizacionTiempoRealService, ActualizacionTiempoRealService>();
builder.Services.AddScoped<IFacturacionService, FacturacionService>();

// 5. Servicios de Servidor
builder.Services.AddScoped<IAsistenteIAService, AsistenteIAService>();
builder.Services.AddScoped<IPdfImpresionService, PdfImpresionService>();
builder.Services.AddSingleton<ISesionService, ServerSesionService>();
builder.Services.AddSingleton<IWhatsAppQRService, WhatsAppQRService>();

// 6. KardexQueueService: DEBE ser Singleton para que el SemaphoreSlim persista entre peticiones
//    Si fuera Scoped, se crearía un semáforo nuevo en cada request → sin protección.
builder.Services.AddScoped<IKardexQueueService, KardexQueueService>();

var app = builder.Build();

// 6. Pipeline de HTTP
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();

// 7. Hosting de Blazor WASM
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowNodeBot");

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
