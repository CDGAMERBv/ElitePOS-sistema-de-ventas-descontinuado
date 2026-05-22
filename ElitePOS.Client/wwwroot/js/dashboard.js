// JavaScript para gráficos del Dashboard (Ventas Mensuales y Productos Top)

// 1. Gráfico de Líneas: Ventas Mensuales
function inicializarGraficoVentas(ventasMensuales) {
    console.log('📊 Inicializando gráfico mensual:', ventasMensuales);
    
    const ctx = document.getElementById('ventasChart');
    if (!ctx) return;

    // Destruir anterior
    if (window.ventasChartInstance) {
        window.ventasChartInstance.destroy();
    }

    // Datos
    const labels = ventasMensuales.map(v => v.mes);
    const data = ventasMensuales.map(v => v.total);

    // Crear
    window.ventasChartInstance = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Ventas Mensuales',
                data: data,
                borderColor: '#3b82f6',
                backgroundColor: 'rgba(59, 130, 246, 0.1)',
                borderWidth: 3,
                pointBackgroundColor: '#ffffff',
                pointBorderColor: '#3b82f6',
                pointBorderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 6,
                fill: true,
                tension: 0.4 // Curva suave
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: 'rgba(30, 41, 59, 0.9)',
                    padding: 12,
                    callbacks: {
                        label: function(context) {
                            return 'Total: S/ ' + context.parsed.y.toLocaleString('es-PE', { minimumFractionDigits: 2 });
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: { color: '#f1f5f9', drawBorder: false },
                    ticks: {
                        callback: function(value) { return 'S/ ' + value; },
                        color: '#64748b',
                        font: { size: 11 }
                    }
                },
                x: {
                    grid: { display: false },
                    ticks: { color: '#64748b', font: { size: 11 } }
                }
            },
            animation: false // Desactivar animación inicial para carga rápida
        }
    });
}

// 2. Gráfico Circular (Doughnut): Productos Más Vendidos
function inicializarGraficoProductos(productosTop) {
    console.log('🍩 Inicializando gráfico productos:', productosTop);
    
    const ctx = document.getElementById('productosChart');
    if (!ctx) return;

    if (window.productosChartInstance) {
        window.productosChartInstance.destroy();
    }

    const labels = productosTop.map(p => p.nombre);
    const data = productosTop.map(p => p.cantidad);
    
    // Paleta de colores moderna
    const backgroundColors = [
        '#3b82f6', // Blue
        '#10b981', // Green
        '#f59e0b', // Amber
        '#ec4899', // Pink
        '#8b5cf6'  // Violet
    ];

    window.productosChartInstance = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: backgroundColors,
                borderWidth: 0,
                hoverOffset: 4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '70%', // Dona más fina
            plugins: {
                legend: {
                    position: 'right',
                    labels: {
                        usePointStyle: true,
                        boxWidth: 8,
                        font: { size: 11, family: "'Inter', sans-serif" },
                        color: '#475569'
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(30, 41, 59, 0.9)',
                    callbacks: {
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.parsed;
                            return `${label}: ${value} unid.`;
                        }
                    }
                }
            },
            animation: false
        }
    });
}