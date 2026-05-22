// ═══════════════════════════════════════════════════════
// SPLASH SCREEN - OCULTAR CUANDO BLAZOR CARGUE
// ═══════════════════════════════════════════════════════

// Ocultar splash screen cuando Blazor esté listo
window.addEventListener('load', function() {
    console.log('🚀 Blazor cargado - ocultando splash screen');
    
    // Verificar si hay una sesión guardada para mostrar animación inicial
    const mostrarCargaInicial = localStorage.getItem('mostrar_carga_inicial');
    
    setTimeout(() => {
        const loadingContainer = document.querySelector('.loading-container');
        if (loadingContainer) {
            loadingContainer.style.opacity = '0';
            loadingContainer.style.transition = 'opacity 0.5s ease-out';
            
            setTimeout(() => {
                loadingContainer.style.display = 'none';
            }, 500);
        }
        
        // Limpiar flag de animación inicial
        if (mostrarCargaInicial === '1') {
            localStorage.removeItem('mostrar_carga_inicial');
        }
    }, mostrarCargaInicial === '1' ? 2000 : 800); // Animación más larga si es primer login
});

// ═══════════════════════════════════════════════════════
// IMPRESIÓN AUTOMÁTICA Y SILENCIOSA DE TICKETS
// ═══════════════════════════════════════════════════════

// Función para impresión automática sin diálogos
function imprimirTicketSilencioso(datos) {
    console.log('🖨️ Iniciando impresión automática del ticket...');
    
    try {
        // Crear contenido del ticket
        let ticketContent = generarContenidoTicket(datos);
        
        // Crear una ventana oculta para impresión
        const printWindow = window.open('', '_blank', 'width=600,height=400');
        
        if (printWindow) {
            printWindow.document.write(`
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Ticket ${datos.numeroComprobante}</title>
                    <style>
                        body { 
                            font-family: 'Courier New', monospace; 
                            font-size: 12px; 
                            margin: 0; 
                            padding: 10px; 
                            width: 280px;
                        }
                        .header { 
                            text-align: center; 
                            border-bottom: 2px dashed #000; 
                            padding-bottom: 10px; 
                            margin-bottom: 10px; 
                        }
                        .item { 
                            margin: 5px 0; 
                            display: flex; 
                            justify-content: space-between; 
                        }
                        .total { 
                            border-top: 2px dashed #000; 
                            padding-top: 10px; 
                            margin-top: 10px; 
                            font-weight: bold; 
                        }
                        .footer { 
                            text-align: center; 
                            margin-top: 20px; 
                            font-size: 10px; 
                        }
                    </style>
                </head>
                <body>
                    ${ticketContent}
                </body>
                </html>
            `);
            
            // Esperar a que el contenido se cargue
            printWindow.document.close();
            
            // Imprimir directamente sin mostrar diálogos
            printWindow.print();
            printWindow.close();
            
            console.log('✅ Ticket enviado automáticamente a impresora');
        } else {
            console.error('❌ No se pudo abrir la ventana de impresión');
        }
    } catch (error) {
        console.error('❌ Error en impresión automática:', error);
    }
}

// Generar contenido del ticket
function generarContenidoTicket(datos) {
    let items = datos.items || [];
    let fecha = datos.fecha || new Date().toLocaleString();
    
    let contenido = `
        <div class="header">
            <strong>ELITE POS</strong><br>
            Ticket: ${datos.numeroComprobante}<br>
            Fecha: ${fecha}<br>
            Cliente: ${datos.cliente}<br>
            Pago: ${datos.pago}
        </div>
    `;
    
    // Agregar items
    items.forEach(item => {
        contenido += `
            <div class="item">
                <span>${item.cantidad} x ${item.nombre}</span>
                <span>S/ ${item.subtotal}</span>
            </div>
        `;
    });
    
    // Agregar total
    contenido += `
        <div class="total">
            <div style="display: flex; justify-content: space-between;">
                <span>TOTAL:</span>
                <span>S/ ${datos.total}</span>
            </div>
        </div>
        <div class="footer">
            Gracias por su compra<br>
            Elite POS - Sistema de Gestión
        </div>
    `;
    
    return contenido;
}

// ═════════════════════════════════════════════════════════
// BUSCADOR INSTANTÁNEO DE PRODUCTOS CON INDEXACIÓN
// ═══════════════════════════════════════════════════════════

let productosIndexados = [];
let indiceBusqueda = {};

// Función para indexar productos para búsqueda instantánea
function indexarProductos(productos) {
    console.log('🔍 Indexando productos para búsqueda instantánea...');
    
    productosIndexados = productos || [];
    indiceBusqueda = {};
    
    // Crear índice invertido para búsqueda rápida
    productosIndexados.forEach((producto, index) => {
        // Indexar por nombre (sin acentos y en minúsculas)
        const nombreNormalizado = normalizarTexto(producto.Nombre);
        const codigoNormalizado = normalizarTexto(producto.CodigoBarras);
        const categoriaNormalizada = normalizarTexto(producto.Categoria);
        
        // Agregar al índice
        if (!indiceBusqueda[nombreNormalizado]) {
            indiceBusqueda[nombreNormalizado] = [];
        }
        indiceBusqueda[nombreNormalizado].push({ producto, index, tipo: 'nombre' });
        
        if (!indiceBusqueda[codigoNormalizado]) {
            indiceBusqueda[codigoNormalizado] = [];
        }
        indiceBusqueda[codigoNormalizado].push({ producto, index, tipo: 'codigo' });
        
        if (!indiceBusqueda[categoriaNormalizada]) {
            indiceBusqueda[categoriaNormalizada] = [];
        }
        indiceBusqueda[categoriaNormalizada].push({ producto, index, tipo: 'categoria' });
    });
    
    console.log(`✅ ${productosIndexados.length} productos indexados para búsqueda instantánea`);
}

// Función de búsqueda instantánea
function buscarProductosInstantaneo(termino) {
    if (!termino || termino.length < 2) {
        return productosIndexados;
    }
    
    const terminoNormalizado = normalizarTexto(termino);
    const resultados = new Set();
    
    // Buscar en el índice
    if (indiceBusqueda[terminoNormalizado]) {
        indiceBusqueda[terminoNormalizado].forEach(item => {
            resultados.add(item.producto);
        });
    }
    
    // Buscar coincidencias parciales
    Object.keys(indiceBusqueda).forEach(key => {
        if (key.includes(terminoNormalizado)) {
            indiceBusqueda[key].forEach(item => {
                resultados.add(item.producto);
            });
        }
    });
    
    console.log(`🔍 Búsqueda "${termino}": ${resultados.size} resultados encontrados`);
    return Array.from(resultados);
}

// Función para normalizar texto (quitar acentos y convertir a minúsculas)
function normalizarTexto(texto) {
    if (!texto) return '';
    
    return texto
        .toLowerCase()
        .normalize('NFD')
        .replace(/[áàâä]/g, 'a')
        .replace(/[éèêë]/g, 'e')
        .replace(/[íìîï]/g, 'i')
        .replace(/[óòôö]/g, 'o')
        .replace(/[úùûü]/g, 'u')
        .replace(/[ñ]/g, 'n')
        .replace(/[ç]/g, 'c')
        .replace(/[^a-z0-9\s]/g, '')
        .trim();
}

// ═════════════════════════════════════════════════════════
// UTILIDADES GLOBALES
// ═════════════════════════════════════════════════════════

// ═════════════════════════════════════════════════════════
// EXPORTACIÓN CSV / EXCEL DESDE BLAZOR
// ═════════════════════════════════════════════════════════

/**
 * Descarga un archivo CSV con BOM UTF-8 (compatible con Excel).
 * @param {string} csvContent - Contenido CSV generado en C#.
 * @param {string} nombreArchivo - Nombre del archivo sin extensión.
 */
function descargarArchivoCSV(csvContent, nombreArchivo) {
    try {
        // BOM UTF-8 para que Excel reconozca acentos correctamente
        var BOM = '\uFEFF';
        var blob = new Blob([BOM + csvContent], { type: 'text/csv;charset=utf-8;' });
        var url = URL.createObjectURL(blob);
        var link = document.createElement('a');
        link.href = url;
        link.download = (nombreArchivo || 'export') + '.csv';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
        console.log('✅ Archivo CSV descargado: ' + link.download);
    } catch (error) {
        console.error('❌ Error al descargar CSV:', error);
    }
}

/**
 * Descarga un archivo Excel (.xlsx) real usando SheetJS (si está cargado),
 * o genera CSV como fallback.
 * @param {string} jsonData - JSON serializado con los datos.
 * @param {string} nombreArchivo - Nombre del archivo sin extensión.
 */
function exportarAExcel(jsonData, nombreArchivo) {
    try {
        var datos = JSON.parse(jsonData);
        if (!datos || datos.length === 0) {
            console.warn('⚠️ No hay datos para exportar');
            return;
        }

        // Fallback a CSV si SheetJS no está cargado
        var headers = Object.keys(datos[0]);
        var csvRows = [headers.join(',')];

        datos.forEach(function(row) {
            var values = headers.map(function(h) {
                var val = (row[h] !== null && row[h] !== undefined) ? String(row[h]) : '';
                // Escapar comillas y envolver en comillas si tiene coma o salto de línea
                if (val.includes(',') || val.includes('"') || val.includes('\n')) {
                    val = '"' + val.replace(/"/g, '""') + '"';
                }
                return val;
            });
            csvRows.push(values.join(','));
        });

        var csvContent = csvRows.join('\n');
        descargarArchivoCSV(csvContent, nombreArchivo);
    } catch (error) {
        console.error('❌ Error al exportar a Excel:', error);
    }
}

/**
 * Descarga un archivo desde Base64 (Útil para Excel generado en Backend/Blazor).
 * @param {string} filename - Nombre del archivo con extensión.
 * @param {string} base64Content - Contenido en Base64.
 */
window.saveAsFile = (filename, base64Content) => {
    const link = document.createElement('a');
    link.download = filename;
    link.href = 'data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,' + base64Content;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

window.clickElement = (id) => {
    const el = document.getElementById(id);
    if (el) {
        el.click();
    } else {
        console.error('❌ Elemento no encontrado para clic:', id);
    }
};

// ═════════════════════════════════════════════════════════
// RECONOCIMIENTO DE VOZ (IA HANDS-FREE)
// ═════════════════════════════════════════════════════════

window.startVoiceRecognition = function(dotNetHelper) {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    
    if (!SpeechRecognition) {
        console.error("❌ El navegador no soporta Reconocimiento de Voz.");
        if (dotNetHelper) dotNetHelper.invokeMethodAsync('OnVoiceError', "No soportado");
        return;
    }

    const recognition = new SpeechRecognition();
    recognition.lang = 'es-ES';
    recognition.interimResults = false;
    recognition.maxAlternatives = 1;

    recognition.onstart = function() {
        console.log("🎙️ Micrófono activado - Escuchando...");
    };

    recognition.onresult = function(event) {
        const transcript = event.results[0][0].transcript;
        console.log("🗣️ Transcripción: " + transcript);
        if (dotNetHelper) dotNetHelper.invokeMethodAsync('OnVoiceResult', transcript);
    };

    recognition.onerror = function(event) {
        console.error("❌ Error en reconocimiento:", event.error);
        if (dotNetHelper) dotNetHelper.invokeMethodAsync('OnVoiceError', event.error);
    };

    recognition.onend = function() {
        console.log("🔇 Micrófono desactivado.");
        if (dotNetHelper) dotNetHelper.invokeMethodAsync('OnVoiceEnd');
    };

    try {
        recognition.start();
    } catch (e) {
        console.error("❌ Error al iniciar reconocimiento:", e);
        if (dotNetHelper) dotNetHelper.invokeMethodAsync('OnVoiceError', e.message);
    }
};

// Hacer funciones globales para uso en Blazor
window.imprimirTicketSilencioso = imprimirTicketSilencioso;
window.indexarProductos = indexarProductos;
window.buscarProductosInstantaneo = buscarProductosInstantaneo;
window.descargarArchivoCSV = descargarArchivoCSV;
window.exportarAExcel = exportarAExcel;
