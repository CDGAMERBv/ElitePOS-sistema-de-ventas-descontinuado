using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

using ElitePOS.Shared.Models;
using ElitePOS.Services;
using ElitePOS.Client.Services;

namespace ElitePOS.Client.Pages
{
    public partial class Inventario : ComponentBase, IDisposable
    {
        [Inject] private IInventarioService InventarioService { get; set; } = default!;
        [Inject] private GestionStateService GestionState { get; set; } = default!;
        [Inject] private ISesionService SesionService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private List<ProductoModel> productos = new();
        private List<ProductoModel> productosFiltrados => FiltrarLista();
        private List<string> categorias => productos.Select(p => p.categoria).Distinct().OrderBy(c => c).ToList();

        private string textoBusqueda = "";
        private string categoriaSeleccionada = "Todas";
        private string estadoSeleccionado = "Todos";
        private bool cargando = true;

        // Variables para KPIs reales
        private int totalProducts => productos.Count;
        private decimal totalInventoryValue => productos.Sum(p => p.precioVenta * p.stock);
        private int lowStockAlerts => productos.Count(p => p.stock <= p.stockMinimo);

        // Variables para modales nativos (MudDialog inline)
        private bool mostrarModalEditar = false;
        private bool mostrarModalEliminar = false;
        private bool mostrarModalKardex = false;
        private bool mostrarModalAgregar = false;
        private bool cargandoKardex = false;
        
        private ProductoModel productoEditando = new();
        private ProductoModel productoEliminando = new();
        private ProductoModel productoKardex = new();
        private ProductoModel nuevoProducto = new();


        protected override async Task OnInitializedAsync()
        {
            GestionState.OnChange += HandleStateChange;
            await Cargar();
        }

        private void HandleStateChange() => InvokeAsync(() => { productos = GestionState.ProductosCache; StateHasChanged(); });

        public void Dispose()
        {
            GestionState.OnChange -= HandleStateChange;
        }

        private async Task Cargar()
        {
            try 
            {
                cargando = true;
                productos = await GestionState.GetProductosAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar productos: {ex.Message}");
            }
            finally 
            { 
                cargando = false; 
                StateHasChanged();
            }
        }

        private List<ProductoModel> FiltrarLista()
        {
            var query = productos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(textoBusqueda))
            {
                var search = textoBusqueda.ToLower();
                query = query.Where(p => 
                    (p.nombre?.ToLower().Contains(search) ?? false) ||
                    (p.codigoBarras?.ToLower().Contains(search) ?? false) ||
                    (p.categoria?.ToLower().Contains(search) ?? false));
            }

            if (categoriaSeleccionada != "Todas" && !string.IsNullOrEmpty(categoriaSeleccionada))
            {
                query = query.Where(p => p.categoria == categoriaSeleccionada);
            }

            if (estadoSeleccionado != "Todos")
            {
                if (estadoSeleccionado == "En Stock") query = query.Where(p => p.stock > p.stockMinimo);
                else if (estadoSeleccionado == "Stock Bajo") query = query.Where(p => p.stock > 0 && p.stock <= p.stockMinimo);
                else if (estadoSeleccionado == "Agotado") query = query.Where(p => p.stock <= 0);
            }

            return query.ToList();
        }

        private void OnBusquedaInput(ChangeEventArgs e)
        {
            textoBusqueda = e.Value?.ToString() ?? "";
            StateHasChanged();
        }

        // LÓGICA DE MODALES
        private void AbrirModalAgregarProducto()
        {
            nuevoProducto = new ProductoModel();
            mostrarModalAgregar = true;
        }

        private void CerrarModalAgregar() => mostrarModalAgregar = false;

        private async Task GuardarNuevoProducto()
        {
            if (string.IsNullOrEmpty(nuevoProducto.nombre)) return;
            
            var result = await InventarioService.AgregarProducto(nuevoProducto);
            if (result)
            {
                await NotificationService.ShowSuccess("Producto agregado exitosamente");
                CerrarModalAgregar();
                await Cargar();
            }
        }

        private void AbrirModalEditar(ProductoModel producto)
        {
            productoEditando = new ProductoModel
            {
                id = producto.id,
                nombre = producto.nombre,
                categoria = producto.categoria,
                stock = producto.stock,
                stockMinimo = producto.stockMinimo,
                precioVenta = producto.precioVenta,
                precioCompra = producto.precioCompra,
                codigoBarras = producto.codigoBarras,
                empresaId = producto.empresaId,
                imagenUrl = producto.imagenUrl,
                unidadMedida = producto.unidadMedida
            };
            mostrarModalEditar = true;
        }

        private void CerrarModalEditar() => mostrarModalEditar = false;

        private async Task GuardarEdicion()
        {
            var result = await InventarioService.ActualizarProducto(productoEditando);
            if (result)
            {
                await NotificationService.ShowSuccess("Producto actualizado exitosamente");
                CerrarModalEditar();
                await Cargar();
            }
        }

        private void AbrirModalEliminar(ProductoModel producto)
        {
            productoEliminando = producto;
            mostrarModalEliminar = true;
        }

        private void CerrarModalEliminar() => mostrarModalEliminar = false;

        private async Task EliminarProducto()
        {
            var result = await InventarioService.EliminarProducto(productoEliminando.id);
            if (result)
            {
                await NotificationService.ShowSuccess("Producto eliminado");
                CerrarModalEliminar();
                await Cargar();
            }
        }

        private async Task AbrirModalKardex(ProductoModel producto)
        {
            productoKardex = producto;
            mostrarModalKardex = true;
            cargandoKardex = true;
            StateHasChanged();
            
            try 
            {
                await GestionState.RefreshProductoKardexAsync(producto.id, null, null);
            }
            finally 
            {
                cargandoKardex = false;
                StateHasChanged();
            }
        }

        private void CerrarModalKardex() => mostrarModalKardex = false;
    }
}
