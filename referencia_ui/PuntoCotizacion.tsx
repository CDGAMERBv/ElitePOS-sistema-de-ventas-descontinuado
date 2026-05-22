import React, { useState } from 'react';
import { useApp } from '../context/AppContext';
import type { Product, SavedQuote, Client } from '../context/AppContext';
import { numeroALetras } from '../utils/numberToLetters';
import { PrintPreview } from './PrintPreview';
import { 
  Search, 
  User, 
  Trash2, 
  Plus, 
  Minus, 
  FileText,
  CheckCircle2, 
  AlertCircle,
  HelpCircle,
  X,
  Printer,
  Eye,
  EyeOff,
  DollarSign
} from 'lucide-react';
import { ModalPortal } from './ModalPortal';

interface CartItem {
  product: Product;
  cantidad: number;
  precioFinal: number; // Editable price
}

interface ClientInfo {
  nombre: string;
  documento: string;
  correo: string;
  direccion: string;
  telefono?: string;
}

export const PuntoCotizacion: React.FC = () => {
  const { products, config, correlativo, incrementCorrelativo, saveQuote, clients, addClient, updateClient } = useApp();

  const getTodayDateString = () => {
    const d = new Date();
    return d.toISOString().split('T')[0];
  };

  const getFutureDateString = (days: number) => {
    const d = new Date();
    d.setDate(d.getDate() + days);
    return d.toISOString().split('T')[0];
  };

  // Component States
  const [cart, setCart] = useState<CartItem[]>([]);
  const [client, setClient] = useState<ClientInfo>({
    nombre: '',
    documento: '',
    correo: '',
    direccion: '',
    telefono: ''
  });
  
  // New proforma parameters
  const [fechaEmision, setFechaEmision] = useState(getTodayDateString());
  const [fechaVencimiento, setFechaVencimiento] = useState(getFutureDateString(15));
  const [condicionPago, setCondicionPago] = useState('CONTADO');
  const [usuarioEmisor, setUsuarioEmisor] = useState('');

  // POS Layout / Metadata parameters
  const [tipoComprobante, setTipoComprobante] = useState('PROFORMA');
  const [serie, setSerie] = useState('PF01');
  const [descuentoGlobal, setDescuentoGlobal] = useState<number | string>(0);
  const [placaVehiculo, setPlacaVehiculo] = useState('');
  const [ordenCompra, setOrdenCompra] = useState('');
  const [guiaRemision, setGuiaRemision] = useState('');
  const [observaciones, setObservaciones] = useState('');
  
  // UI Tab modal states
  const [isExtraModalOpen, setIsExtraModalOpen] = useState(false);
  const [activeExtraTab, setActiveExtraTab] = useState<'placa' | 'compra' | 'guia' | 'obs' | 'pago'>('placa');
  
  // Tax setting: Read from isIgvActive with explicit fallback logic
  // If isIgvActive is explicitly false, DO NOT apply tax
  // If isIgvActive is true OR undefined/missing, apply tax
  const applyTax = (() => {
    // First priority: isIgvActive if explicitly defined
    if (config.isIgvActive !== undefined) {
      return config.isIgvActive === true;
    }
    // Second priority: aplicaIgv if defined
    if (config.aplicaIgv !== undefined) {
      return config.aplicaIgv === true;
    }
    // Default: apply tax
    return true;
  })();

  // Catalog sidebar visibility toggle (Default false per user request)
  const [showCatalog, setShowCatalog] = useState(false);
  const [showProductSuggestions, setShowProductSuggestions] = useState(false);

  // Quick product search state
  const [quickProductSearch, setQuickProductSearch] = useState('');
  
  // Client and Document Autocomplete Suggestion States
  const [showClientSuggestions, setShowClientSuggestions] = useState(false);
  const [showDocSuggestions, setShowDocSuggestions] = useState(false);
  
  // Validation and Modal UI States
  const [errors, setErrors] = useState<{ [key: string]: string }>({});
  const [isSuccessOpen, setIsSuccessOpen] = useState(false);
  const [isPreviewOpen, setIsPreviewOpen] = useState(false);
  const [isPreviewMode, setIsPreviewMode] = useState(false);
  const [generatedQuoteDetails, setGeneratedQuoteDetails] = useState<Omit<SavedQuote, 'id' | 'savedAt' | 'status'> | null>(null);
  const [isGenerating, setIsGenerating] = useState(false);
  
  const [isSearchingMainDoc, setIsSearchingMainDoc] = useState(false);


  // Tipo de Cambio (Exchange Rate) States
  const [tipoCambio, setTipoCambio] = useState<number | null>(null);
  const [isExchangeRateModalOpen, setIsExchangeRateModalOpen] = useState(false);
  
  const [toast, setToast] = useState<{ show: boolean; message: string; type: 'success' | 'error' }>({
    show: false,
    message: '',
    type: 'success'
  });

  const triggerToast = (message: string, type: 'success' | 'error' = 'success') => {
    setToast({ show: true, message, type });
    setTimeout(() => {
      setToast(prev => ({ ...prev, show: false }));
    }, 3500);
  };

  // Add product to Cart
  const handleAddToCart = (product: Product) => {
    setCart(prevCart => {
      const existingIndex = prevCart.findIndex(item => item.product.sku === product.sku);
      
      if (existingIndex > -1) {
        // If already in cart, increment quantity
        const newCart = [...prevCart];
        newCart[existingIndex].cantidad += 1;
        triggerToast(`Cantidad de ${product.nombre} incrementada.`);
        return newCart;
      } else {
        // Add new cart item
        triggerToast(`${product.nombre} añadido a la cotización.`);
        return [...prevCart, { product, cantidad: 1, precioFinal: product.precioUnitario }];
      }
    });
  };

  // Remove product from Cart
  const handleRemoveFromCart = (sku: string, name: string) => {
    setCart(prevCart => prevCart.filter(item => item.product.sku !== sku));
    triggerToast(`${name} removido de la cotización.`);
  };

  // Update Quantity (Buttons or Manual Input)
  const handleQtyChange = (sku: string, qty: number | string) => {
    let parsedQty = typeof qty === 'string' ? parseInt(qty) : qty;
    if (isNaN(parsedQty) || parsedQty < 1) parsedQty = 1;
    
    setCart(prevCart => 
      prevCart.map(item => 
        item.product.sku === sku ? { ...item, cantidad: parsedQty } : item
      )
    );
  };

  // Update Price override in Cart
  const handlePriceChange = (sku: string, price: string) => {
    let parsedPrice = parseFloat(price);
    if (isNaN(parsedPrice) || parsedPrice < 0) parsedPrice = 0;
    
    setCart(prevCart => 
      prevCart.map(item => 
        item.product.sku === sku ? { ...item, precioFinal: parsedPrice } : item
      )
    );
  };

  // Quick SKU/Barcode/Name product adder
  const handleQuickProductAdd = (e: React.FormEvent) => {
    e.preventDefault();
    if (!quickProductSearch.trim()) return;
    
    // Exact case-insensitive match by SKU
    const productBySku = products.find(p => p.sku.toLowerCase() === quickProductSearch.trim().toLowerCase());
    
    if (productBySku) {
      handleAddToCart(productBySku);
      setQuickProductSearch('');
    } else {
      // Partial match by Name
      const productByName = products.find(p => p.nombre.toLowerCase().includes(quickProductSearch.trim().toLowerCase()));
      if (productByName) {
        handleAddToCart(productByName);
        setQuickProductSearch('');
      } else {
        triggerToast(`Producto "${quickProductSearch}" no encontrado`, 'error');
      }
    }
  };

  // Filter Catalog by search (Unified with quickProductSearch at bottom)
  const filteredCatalog = products.filter(product => {
    const query = quickProductSearch.toLowerCase();
    return (
      (product.sku || '').toLowerCase().includes(query) ||
      (product.nombre || '').toLowerCase().includes(query) ||
      (product.descripcion || '').toLowerCase().includes(query)
    );
  });



  const handleDocumentoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setClient(prev => ({ ...prev, documento: value }));
    if (errors.documento) setErrors(prev => ({ ...prev, documento: '' }));
    setShowDocSuggestions(value.trim().length > 0);

    const matched = clients.find(c => c.documento === value.trim());
    if (matched) {
      setClient({
        nombre: matched.nombreRazonSocial,
        documento: matched.documento,
        correo: matched.correo,
        direccion: matched.direccion,
        telefono: matched.telefono || ''
      });
      setErrors(prev => ({ ...prev, nombre: '', correo: '', documento: '' }));
      setShowDocSuggestions(false);
      triggerToast(`Cliente "${matched.nombreRazonSocial}" autocompletado.`);
    }
  };

  const handleMainDocumentBlur = async (e: React.FocusEvent<HTMLInputElement>) => {
    // Hide suggestions after a short delay so dropdown click handles first
    setTimeout(async () => {
      setShowDocSuggestions(false);

      const docValue = e.target.value.trim();
      if (docValue.length !== 8 && docValue.length !== 11) {
        return;
      }

      const matched = clients.find(c => c.documento === docValue);
      if (matched) {
        setClient({
          nombre: matched.nombreRazonSocial,
          documento: matched.documento,
          correo: matched.correo,
          direccion: matched.direccion,
          telefono: matched.telefono || ''
        });
        return;
      }

      if (!config.apisNetPeToken) {
        triggerToast('Configura el token de consulta en Configuración para búsqueda automática.', 'error');
        return;
      }

      setIsSearchingMainDoc(true);
      setErrors(prev => ({ ...prev, documento: '' }));

      const { lookupPeruDocument } = await import('../utils/peruApi');
      const result = await lookupPeruDocument(docValue, config.apisNetPeToken);
      setIsSearchingMainDoc(false);

      if (result.success) {
        // Auto register client in CRM local DB
        const newClientObj: Client = {
          id: `cli_${Date.now()}_${Math.random().toString(36).substr(2, 5)}`,
          documento: docValue,
          nombreRazonSocial: result.nombre,
          direccion: result.direccion || '',
          correo: '', // empty initially, user can modify on the main view
          telefono: '',
        };
        addClient(newClientObj);

        setClient({
          nombre: result.nombre,
          documento: docValue,
          direccion: result.direccion || '',
          correo: '',
          telefono: ''
        });
        setErrors(prev => ({ ...prev, nombre: '', documento: '', correo: '' }));
        triggerToast('¡Cliente encontrado y registrado automáticamente!', 'success');
      } else {
        triggerToast(result.error || 'No se pudo obtener información del documento.', 'error');
      }
    }, 200);
  };




  const handleApplyExchangeRate = (rate: number) => {
    if (isNaN(rate) || rate <= 0) {
      triggerToast('Ingresa un tipo de cambio válido.', 'error');
      return;
    }
    setTipoCambio(rate);
    setIsExchangeRateModalOpen(false);
    triggerToast(`Tipo de cambio S/ ${rate.toFixed(3)} aplicado a la cotización.`, 'success');
  };

  const handleClearExchangeRate = () => {
    setTipoCambio(null);
    triggerToast('Tipo de cambio referencial removido.', 'success');
  };

  const handleNombreChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setClient(prev => ({ ...prev, nombre: value }));
    if (errors.nombre) setErrors(prev => ({ ...prev, nombre: '' }));
    setShowClientSuggestions(value.trim().length > 0);

    const matched = clients.find(c => c.nombreRazonSocial === value.trim());
    if (matched) {
      setClient({
        nombre: matched.nombreRazonSocial,
        documento: matched.documento,
        correo: matched.correo,
        direccion: matched.direccion,
        telefono: matched.telefono || ''
      });
      setErrors(prev => ({ ...prev, nombre: '', correo: '', documento: '' }));
      setShowClientSuggestions(false);
      triggerToast(`Cliente "${matched.nombreRazonSocial}" autocompletado.`);
    }
  };

  const filteredClientSuggestions = clients.filter(c =>
    c.nombreRazonSocial.toLowerCase().includes(client.nombre.toLowerCase())
  );

  const filteredDocSuggestions = clients.filter(c =>
    c.documento.includes(client.documento)
  );

  const getValidatedClient = () => {
    const docTrimmed = client.documento.trim();
    const nombreTrimmed = client.nombre.trim();
    const matched = clients.find(c => c.documento.trim() === docTrimmed);
    const isRegistered = matched && matched.nombreRazonSocial.trim().toLowerCase() === nombreTrimmed.toLowerCase();

    let finalClient = { ...client };

    if (!isRegistered) {
      finalClient = {
        nombre: 'Clientes Varios',
        documento: '00000000',
        correo: '',
        direccion: '',
        telefono: ''
      };
    }
    return { finalClient, isRegistered };
  };



  // Cart math calculations
  const calculateSubtotal = () => {
    return cart.reduce((sum, item) => sum + (item.cantidad * item.precioFinal), 0);
  };

  const getConvertedTotal = () => {
    if (!tipoCambio) return null;
    const isPen = config.monedaPorDefecto === 'SOLES' || config.monedaPorDefecto === 'PEN' || config.monedaPorDefecto === '';
    if (isPen) {
      const converted = totalAmount / tipoCambio;
      return `$ ${converted.toFixed(2)}`;
    } else {
      const converted = totalAmount * tipoCambio;
      return `S/ ${converted.toFixed(2)}`;
    }
  };

  const rawSubtotal = calculateSubtotal();
  const parsedDiscount = parseFloat(String(descuentoGlobal)) || 0;
  const discountAmount = rawSubtotal * (parsedDiscount / 100);
  const subtotal = rawSubtotal - discountAmount;
  const taxRate = 0.18; // 18% IGV
  const taxAmount = applyTax ? subtotal * taxRate : 0;
  const totalAmount = subtotal + taxAmount;

  // Format Currency
  const formatCurrency = (val: number) => {
    const symbol = config.monedaPorDefecto === 'DOLARES' ? '$' : config.monedaPorDefecto === 'EUROS' ? '€' : 'S/';
    return `${symbol} ${val.toFixed(2)}`;
  };

  // Validation before generating quote
  const validateForm = (validatedClient: ClientInfo) => {
    const newErrors: { [key: string]: string } = {};

    if (cart.length === 0) {
      triggerToast('Debes añadir al menos un producto al carrito.', 'error');
      return false;
    }

    if (!validatedClient.nombre.trim()) {
      newErrors.nombre = 'El nombre/razón social es obligatorio';
    }

    if (!validatedClient.documento.trim()) {
      newErrors.documento = 'El documento/RUC es obligatorio';
    } else if (!/^\d{8,11}$/.test(validatedClient.documento.trim())) {
      newErrors.documento = 'El RUC/Documento debe contener estrictamente entre 8 y 11 dígitos numéricos';
    }

    // Strict CRM verification
    if (!newErrors.documento && !newErrors.nombre) {
      if (validatedClient.nombre === 'Clientes Varios' && validatedClient.documento === '00000000') {
        // Skip check for guest client fallback
      } else {
        const matched = clients.find(c => c.documento.trim() === validatedClient.documento.trim());
        if (!matched) {
          newErrors.nombre = 'Cliente no registrado';
          newErrors.documento = 'Debe registrar o seleccionar un cliente válido';
          triggerToast('Debe seleccionar un cliente registrado o registrar uno nuevo.', 'error');
        } else if (matched.nombreRazonSocial.trim().toLowerCase() !== validatedClient.nombre.trim().toLowerCase()) {
          newErrors.nombre = 'Nombre no coincide con el RUC registrado';
          newErrors.documento = 'RUC no coincide con el nombre registrado';
          triggerToast('Los datos ingresados no coinciden con el cliente registrado en el CRM.', 'error');
        }
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Generate Quote Action
  const handleGenerateQuote = (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (isGenerating) return;

    const { finalClient, isRegistered } = getValidatedClient();
    if (!isRegistered) {
      setClient(finalClient);
    }

    if (!validateForm(finalClient)) return;

    setIsGenerating(true);

    // Proforma unique correlativo code
    const proformaCode = `0001-${String(correlativo).padStart(7, '0')}`;

    // Convert date string from inputs (YYYY-MM-DD) to friendly format (DD/MM/YYYY)
    const friendlyDate = (ds: string) => {
      if (!ds) return '';
      const parts = ds.split('-');
      return `${parts[2]}/${parts[1]}/${parts[0]}`;
    };

    // Extract current formatted time for seller watermark
    const getFormattedTime = () => {
      const d = new Date();
      let hours = d.getHours();
      const minutes = d.getMinutes();
      const ampm = hours >= 12 ? 'PM' : 'AM';
      hours = hours % 12;
      hours = hours ? hours : 12; // the hour '0' should be '12'
      const minutesStr = minutes < 10 ? '0' + minutes : minutes;
      return `${hours}:${minutesStr} ${ampm}`;
    };

    const newQuote = {
      codigoCotizacion: proformaCode,
      fechaEmision: friendlyDate(fechaEmision),
      fechaVencimiento: friendlyDate(fechaVencimiento),
      condicionPago,
      usuarioEmisor,
      horaEmision: getFormattedTime(),
      empresaEmisora: {
        nombre: config.nombreEmpresa,
        ruc: config.ruc,
        direccion: config.direccion,
        telefono: config.telefono,
        email: config.email,
        logo: config.logo,
        actividad: config.actividadEmpresa,
        cuentas: config.cuentasBancarias,
        moneda: config.monedaPorDefecto
      },
      cliente: finalClient,
      items: cart.map(item => ({
        sku: item.product.sku,
        nombre: item.product.nombre,
        descripcion: item.product.descripcion,
        unidadMedida: item.product.unidadMedida || 'UNIDAD',
        cantidad: item.cantidad,
        precioUnitario: item.precioFinal,
        totalItem: item.cantidad * item.precioFinal,
        imagen: item.product.imagen
      })),
      finanzas: {
        subtotal,
        igv: taxAmount,
        total: totalAmount,
        aplicaIgv: applyTax,
        isIgvActive: applyTax,
        totalEnLetras: numeroALetras(totalAmount, config.monedaPorDefecto)
      },
      terminos: config.terminosPorDefecto,
      // POS fields
      tipoComprobante,
      serie,
      descuentoGlobal: parseFloat(String(descuentoGlobal)) || 0,
      placaVehiculo: placaVehiculo.trim() || undefined,
      ordenCompra: ordenCompra.trim() || undefined,
      guiaRemision: guiaRemision.trim() || undefined,
      observaciones: observaciones.trim() || undefined,
      tipoCambio: tipoCambio || undefined,
    };

    // Sync/update client details in the CRM if modified
    const matchedClient = clients.find(c => c.documento.trim() === client.documento.trim());
    if (matchedClient) {
      const emailChanged = (client.correo || '').trim() !== (matchedClient.correo || '').trim();
      const addressChanged = (client.direccion || '').trim() !== (matchedClient.direccion || '').trim();
      const phoneChanged = (client.telefono || '').trim() !== (matchedClient.telefono || '').trim();
      if (emailChanged || addressChanged || phoneChanged) {
        const updatedClientData: Client = {
          ...matchedClient,
          correo: (client.correo || '').trim(),
          direccion: (client.direccion || '').trim(),
          telefono: (client.telefono || '').trim()
        };
        updateClient(updatedClientData);
      }
    }



    // Persist details locally so success popup can read
    setGeneratedQuoteDetails(newQuote);

    // --- MÓDULO 5: Save to persistent quotes history ---
    saveQuote(newQuote);

    setIsSuccessOpen(true);
    triggerToast('¡Cotización generada y registrada con éxito!');
    
    // Increment global invoice sequence index
    incrementCorrelativo();

    // Release button lock after 1 second to avoid duplicate submittals
    setTimeout(() => {
      setIsGenerating(false);
    }, 1000);
  };

  const handleResetQuote = () => {
    setCart([]);
    setClient({ nombre: '', documento: '', correo: '', direccion: '', telefono: '' });
    setFechaEmision(getTodayDateString());
    setFechaVencimiento(getFutureDateString(15));
    setCondicionPago('CONTADO');
    setUsuarioEmisor('');
    setTipoComprobante('PROFORMA');
    setSerie('PF01');
    setDescuentoGlobal(0);
    setPlacaVehiculo('');
    setOrdenCompra('');
    setGuiaRemision('');
    setObservaciones('');
    setErrors({});
    setIsSuccessOpen(false);
    setGeneratedQuoteDetails(null);
    setTipoCambio(null);
  };

  return (
    <div className="pos-page-container" id="punto-cotizacion-hub">
      {/* 0. Top Header Title (Proforma Only) */}
      <div className="pos-top-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.15rem' }}>
        <h1 className="pos-main-title" style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--text-primary)', margin: 0, fontFamily: 'var(--font-title)' }}>
          Nueva venta
        </h1>
        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
          {tipoCambio && (
            <div 
              onClick={() => setIsExchangeRateModalOpen(true)}
              style={{
                background: 'var(--primary-glow)',
                border: '1px solid var(--primary)',
                color: 'var(--primary)',
                padding: '4px 10px',
                borderRadius: '4px',
                fontSize: '0.75rem',
                fontWeight: 600,
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: '4px'
              }}
              title="Tipo de Cambio Aplicado"
            >
              <span>TC: {tipoCambio.toFixed(3)}</span>
            </div>
          )}
          
          <button
            type="button"
            onClick={() => setIsExchangeRateModalOpen(true)}
            className="btn-pos-secondary"
            style={{ 
              height: '32px', 
              padding: '0 0.75rem', 
              fontSize: '0.75rem', 
              display: 'flex', 
              alignItems: 'center', 
              gap: '6px',
              borderRadius: '4px',
              cursor: 'pointer'
            }}
          >
            <DollarSign size={14} />
            <span>Tipo de cambio</span>
          </button>
          
          <button
            type="button"
            onClick={() => setShowCatalog(!showCatalog)}
            className="btn-pos-secondary"
            style={{ 
              height: '32px', 
              padding: '0 0.75rem', 
              fontSize: '0.75rem', 
              display: 'flex', 
              alignItems: 'center', 
              gap: '6px',
              borderRadius: '4px',
              cursor: 'pointer'
            }}
          >
            {showCatalog ? <EyeOff size={14} /> : <Eye size={14} />}
            <span>{showCatalog ? 'Ocultar catálogo' : 'Mostrar catálogo'}</span>
          </button>
        </div>
      </div>

      {/* 1. POS Top Input Grid (KeyFacil Style: 12-Column Double Row) */}
      <div className="pos-header-grid-keyfacil" id="pos-header-grid">
        {/* Cliente / Razón Social (Col-span 4 with custom premium suggestion list) */}
        <div className="pos-border-label-group col-span-4" style={{ position: 'relative' }}>
          <label htmlFor="nombre" style={{ marginBottom: '4px' }}>Cliente</label>
          <input
            type="text"
            id="nombre"
            name="nombre"
            placeholder="Buscar o ingresar cliente..."
            value={client.nombre}
            onChange={handleNombreChange}
            onFocus={() => setShowClientSuggestions(true)}
            onBlur={() => setTimeout(() => setShowClientSuggestions(false), 200)}
            style={{ borderColor: errors.nombre ? '#ef4444' : undefined }}
            autoComplete="off"
          />
          {showClientSuggestions && client.nombre.trim().length > 0 && filteredClientSuggestions.length > 0 && (
            <div className="pos-client-suggestions-dropdown" style={{
              position: 'absolute',
              top: '100%',
              left: 0,
              right: 0,
              background: 'var(--bg-card)',
              border: '1px solid var(--border-color)',
              borderRadius: '4px',
              boxShadow: 'var(--shadow-md)',
              zIndex: 100,
              maxHeight: '180px',
              overflowY: 'auto',
              marginTop: '2px'
            }}>
              {filteredClientSuggestions.map(c => (
                  <div
                    key={c.id}
                    onMouseDown={() => {
                      setClient({
                        nombre: c.nombreRazonSocial,
                        documento: c.documento,
                        correo: c.correo,
                        direccion: c.direccion,
                        telefono: c.telefono || ''
                      });
                      setErrors(prev => ({ ...prev, nombre: '', documento: '', correo: '' }));
                      setShowClientSuggestions(false);
                      triggerToast(`Cliente "${c.nombreRazonSocial}" seleccionado.`);
                    }}
                    style={{
                      padding: '8px 12px',
                      cursor: 'pointer',
                      fontSize: '0.82rem',
                      borderBottom: '1px solid var(--border-color)',
                      color: 'var(--text-primary)',
                      transition: 'background 0.2s'
                    }}
                    className="suggestion-item"
                  >
                    <div style={{ fontWeight: 600 }}>{c.nombreRazonSocial}</div>
                    <div style={{ fontSize: '0.72rem', color: 'var(--text-secondary)' }}>Doc: {c.documento}</div>
                  </div>
                ))}
            </div>
          )}
          {errors.nombre && (
            <span style={{ color: '#ef4444', fontSize: '9px', marginTop: '2px', position: 'absolute', bottom: '-12px', zIndex: 10 }}>
              {errors.nombre}
            </span>
          )}
        </div>

        {/* RUC / Identificación (Col-span 2 with custom premium suggestion list) */}
        <div className="pos-border-label-group col-span-2" style={{ position: 'relative' }}>
          <label htmlFor="documento">RUC / Nro. Documento</label>
          <input
            type="text"
            id="documento"
            name="documento"
            placeholder="Documento..."
            value={client.documento}
            onChange={handleDocumentoChange}
            onFocus={() => setShowDocSuggestions(true)}
            onBlur={handleMainDocumentBlur}
            style={{ borderColor: errors.documento ? '#ef4444' : undefined, paddingRight: isSearchingMainDoc ? '32px' : undefined }}
            autoComplete="off"
          />
          {isSearchingMainDoc && (
            <div 
              className="spinner" 
              style={{ 
                position: 'absolute', 
                right: '12px', 
                top: '55%', 
                transform: 'translateY(-50%)', 
                width: '14px', 
                height: '14px', 
                border: '2px solid var(--border-color)', 
                borderTopColor: 'var(--primary)',
                background: 'transparent',
                zIndex: 10
              }}
            ></div>
          )}
          {showDocSuggestions && client.documento.trim().length > 0 && filteredDocSuggestions.length > 0 && (
            <div className="pos-client-suggestions-dropdown" style={{
              position: 'absolute',
              top: '100%',
              left: 0,
              right: 0,
              background: 'var(--bg-card)',
              border: '1px solid var(--border-color)',
              borderRadius: '4px',
              boxShadow: 'var(--shadow-md)',
              zIndex: 100,
              maxHeight: '180px',
              overflowY: 'auto',
              marginTop: '2px'
            }}>
              {filteredDocSuggestions.map(c => (
                  <div
                    key={c.id}
                    onMouseDown={() => {
                      setClient({
                        nombre: c.nombreRazonSocial,
                        documento: c.documento,
                        correo: c.correo,
                        direccion: c.direccion,
                        telefono: c.telefono || ''
                      });
                      setErrors(prev => ({ ...prev, nombre: '', documento: '', correo: '' }));
                      setShowDocSuggestions(false);
                      triggerToast(`Cliente "${c.nombreRazonSocial}" seleccionado.`);
                    }}
                    style={{
                      padding: '8px 12px',
                      cursor: 'pointer',
                      fontSize: '0.82rem',
                      borderBottom: '1px solid var(--border-color)',
                      color: 'var(--text-primary)',
                      transition: 'background 0.2s'
                    }}
                    className="suggestion-item"
                  >
                    <div style={{ fontWeight: 600 }}>{c.nombreRazonSocial}</div>
                    <div style={{ fontSize: '0.72rem', color: 'var(--text-secondary)' }}>Doc: {c.documento}</div>
                  </div>
                ))}
            </div>
          )}
          {errors.documento && (
            <span style={{ color: '#ef4444', fontSize: '9px', marginTop: '2px', position: 'absolute', bottom: '-12px', zIndex: 10 }}>
              {errors.documento}
            </span>
          )}
        </div>

        {/* Fecha Emisión (Col-span 3) */}
        <div className="pos-border-label-group col-span-3">
          <label htmlFor="fechaEmision">Fecha de emisión</label>
          <input
            type="date"
            id="fechaEmision"
            value={fechaEmision}
            onChange={(e) => setFechaEmision(e.target.value)}
          />
        </div>

        {/* Fecha Vencimiento (Col-span 3) */}
        <div className="pos-border-label-group col-span-3">
          <label htmlFor="fechaVencimiento">Fecha de vcto</label>
          <input
            type="date"
            id="fechaVencimiento"
            value={fechaVencimiento}
            onChange={(e) => setFechaVencimiento(e.target.value)}
          />
        </div>

        {/* Tipo de Comprobante (Col-span 3 - Locked to PROFORMA) */}
        <div className="pos-border-label-group col-span-3">
          <label htmlFor="tipoComprobante">Tipo de comprobante</label>
          <input
            type="text"
            id="tipoComprobante"
            value="PROFORMA"
            disabled
            readOnly
          />
        </div>

        {/* Serie (Col-span 2) */}
        <div className="pos-border-label-group col-span-2">
          <label htmlFor="serie">Serie</label>
          <input
            type="text"
            id="serie"
            value={serie}
            disabled
            readOnly
          />
        </div>

        {/* Tipo de operación (Col-span 4) */}
        <div className="pos-border-label-group col-span-4">
          <label htmlFor="tipoOperacion">Tipo de operación</label>
          <input
            type="text"
            id="tipoOperacion"
            value="VENTA INTERNA"
            disabled
            readOnly
          />
        </div>

        {/* Dscto. global (%) (Col-span 3) */}
        <div className="pos-border-label-group col-span-3">
          <label htmlFor="descuentoGlobal">Dscto. global (%)</label>
          <input
            type="number"
            id="descuentoGlobal"
            min="0"
            max="100"
            step="any"
            placeholder="0"
            value={descuentoGlobal === 0 ? '' : descuentoGlobal}
            onChange={(e) => {
              const valStr = e.target.value;
              if (valStr === '') {
                setDescuentoGlobal(0);
                return;
              }
              const val = parseFloat(valStr);
              if (isNaN(val) || val < 0) {
                setDescuentoGlobal(0);
              } else if (val > 100) {
                setDescuentoGlobal(100);
              } else {
                setDescuentoGlobal(valStr);
              }
            }}
            onBlur={() => {
              const val = parseFloat(String(descuentoGlobal)) || 0;
              setDescuentoGlobal(val);
            }}
          />
        </div>


      </div>

      {/* 2. Secondary parameters tabs button row (High Contrast Bold Blue) */}
      <div className="pos-action-tabs">
        <button 
          type="button" 
          className={`pos-tab-btn ${placaVehiculo ? 'tab-active' : ''}`}
          onClick={() => {
            setActiveExtraTab('placa');
            setIsExtraModalOpen(true);
          }}
        >
          <span>PLACA</span> {placaVehiculo && <span className="pos-tab-badge">✓</span>}
        </button>

        <button 
          type="button" 
          className={`pos-tab-btn ${ordenCompra ? 'tab-active' : ''}`}
          onClick={() => {
            setActiveExtraTab('compra');
            setIsExtraModalOpen(true);
          }}
        >
          <span>O. COMPRA</span> {ordenCompra && <span className="pos-tab-badge">✓</span>}
        </button>

        <button 
          type="button" 
          className={`pos-tab-btn ${guiaRemision ? 'tab-active' : ''}`}
          onClick={() => {
            setActiveExtraTab('guia');
            setIsExtraModalOpen(true);
          }}
        >
          <span>G. REMISIÓN</span> {guiaRemision && <span className="pos-tab-badge">✓</span>}
        </button>

        <button 
          type="button" 
          className={`pos-tab-btn ${observaciones ? 'tab-active' : ''}`}
          onClick={() => {
            setActiveExtraTab('obs');
            setIsExtraModalOpen(true);
          }}
        >
          <span>OBSERVACIONES</span> {observaciones && <span className="pos-tab-badge">✓</span>}
        </button>

        <button 
          type="button" 
          className="pos-tab-btn"
          onClick={() => {
            setActiveExtraTab('pago');
            setIsExtraModalOpen(true);
          }}
        >
          <span>COND. PAGO: {condicionPago}</span>
        </button>


      </div>

      {/* 3. Main Split workspace columns */}
      <div className="pos-main-workspace">
        
        {/* Catalog Sidebar panel */}
        {showCatalog && (
          <div className="pos-catalog-column" id="catalog-sidebar">
            <div className="pos-column-title">
              <FileText size={16} style={{ color: 'var(--primary)' }} />
              <span>Catálogo</span>
              <span style={{ fontSize: '0.75rem', fontWeight: 'normal', marginLeft: 'auto', background: 'var(--border-color)', padding: '2px 6px', borderRadius: '4px', color: 'var(--text-secondary)' }}>
                {products.length} Items
              </span>
            </div>

            <div className="pos-scrollable" id="catalog-item-results">
              {filteredCatalog.length > 0 ? (
                filteredCatalog.map(product => (
                  <div 
                    key={product.sku}
                    className="catalog-item-row"
                    onClick={() => handleAddToCart(product)}
                    id={`catalog-row-${product.sku}`}
                    style={{ padding: '0.45rem 0.65rem', borderBottom: '1px solid var(--border-color)', cursor: 'pointer', transition: 'background 0.2s', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
                  >
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '2px', minWidth: 0, flex: 1 }}>
                      <span style={{ fontWeight: 600, fontSize: '0.82rem', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', color: 'var(--text-primary)' }}>{product.nombre}</span>
                      <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
                        <span className="sku-badge" style={{ fontSize: '9px', padding: '0px 4px' }}>{product.sku}</span>
                        <span style={{ fontSize: '0.78rem', fontWeight: 700, color: 'var(--primary)' }}>{formatCurrency(product.precioUnitario)}</span>
                      </div>
                    </div>
                    <button 
                      type="button" 
                      className="catalog-item-add-btn" 
                      style={{ width: '24px', height: '24px', borderRadius: '4px', background: 'var(--bg-primary)', border: '1px solid var(--border-color)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-secondary)', cursor: 'pointer' }}
                      onClick={(e) => {
                        e.stopPropagation();
                        handleAddToCart(product);
                      }}
                    >
                      <Plus size={12} />
                    </button>
                  </div>
                ))
              ) : (
                <div className="empty-state-container" style={{ padding: '1.5rem 0.5rem', textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '0.5rem' }}>
                  <HelpCircle size={28} className="empty-state-icon" style={{ opacity: 0.3 }} />
                  <span style={{ fontSize: '0.78rem', color: 'var(--text-muted)' }}>No hay productos coincidentes</span>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Cart Panel Column */}
        <div className="pos-cart-column" id="quote-creation-panel">
          <div className="pos-column-title">
            <User size={16} style={{ color: 'var(--primary)' }} />
            <span>Items de Cotización</span>
            {cart.length > 0 && (
              <span style={{ fontSize: '0.75rem', fontWeight: 'bold', marginLeft: 'auto', background: 'var(--primary)', color: 'white', padding: '2px 8px', borderRadius: '10px' }}>
                {cart.length} Artículos
              </span>
            )}
          </div>

          <div className="pos-scrollable" style={{ padding: 0 }}>
            {cart.length > 0 ? (
              <table className="premium-table pos-compact-table" id="cart-items-table" style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                  <tr>
                    <th style={{ textAlign: 'left', padding: '0.5rem' }}>Descripción / SKU</th>
                    <th style={{ width: '100px', textAlign: 'center' }}>Cantidad</th>
                    <th style={{ width: '110px', textAlign: 'right' }}>P. Unitario</th>
                    <th style={{ width: '100px', textAlign: 'right' }}>Total</th>
                    <th style={{ width: '40px', textAlign: 'center' }}></th>
                  </tr>
                </thead>
                <tbody>
                  {cart.map(item => (
                    <tr key={item.product.sku} id={`cart-row-${item.product.sku}`} style={{ borderBottom: '1px solid var(--border-color)' }}>
                      <td style={{ padding: '0.5rem' }}>
                        <div style={{ display: 'flex', flexDirection: 'column', gap: '2px' }}>
                          <span style={{ fontWeight: 600, fontSize: '0.8rem', color: 'var(--text-primary)' }}>{item.product.nombre}</span>
                          <div style={{ display: 'flex', gap: '4px', alignItems: 'center' }}>
                            <span className="sku-badge" style={{ fontSize: '8px', padding: '0px 3px' }}>
                              {item.product.sku}
                            </span>
                            <span style={{ fontSize: '8px', padding: '0px 3px', background: 'var(--bg-primary)', borderRadius: '3px', color: 'var(--text-secondary)' }}>
                              U.M: {item.product.unidadMedida || 'UNIDAD'}
                            </span>
                          </div>
                        </div>
                      </td>
                      
                      {/* Quantity controls */}
                      <td style={{ textAlign: 'center' }}>
                        <div className="qty-control-wrapper" id={`qty-control-${item.product.sku}`} style={{ display: 'inline-flex', alignItems: 'center', height: '28px', border: '1px solid var(--border-color)', borderRadius: '4px', overflow: 'hidden', background: 'var(--bg-secondary)' }}>
                          <button
                            type="button"
                            className="qty-control-btn"
                            onClick={() => handleQtyChange(item.product.sku, item.cantidad - 1)}
                            style={{ border: 'none', background: 'transparent', width: '22px', height: '100%', cursor: 'pointer', color: 'var(--text-secondary)' }}
                          >
                            <Minus size={10} />
                          </button>
                          <input
                            type="number"
                            className="qty-control-input"
                            value={item.cantidad}
                            onChange={(e) => handleQtyChange(item.product.sku, e.target.value)}
                            id={`qty-input-${item.product.sku}`}
                            min="1"
                            step="1"
                            style={{ borderLeft: '1px solid var(--border-color)', borderRight: '1px solid var(--border-color)', borderTop: 'none', borderBottom: 'none', background: 'transparent', width: '32px', height: '100%', textAlign: 'center', fontSize: '0.8rem', fontWeight: 'bold', color: 'var(--text-primary)', outline: 'none' }}
                          />
                          <button
                            type="button"
                            className="qty-control-btn"
                            onClick={() => handleQtyChange(item.product.sku, item.cantidad + 1)}
                            style={{ border: 'none', background: 'transparent', width: '22px', height: '100%', cursor: 'pointer', color: 'var(--text-secondary)' }}
                          >
                            <Plus size={10} />
                          </button>
                        </div>
                      </td>
                      
                      {/* Price Final Override Input */}
                      <td style={{ textAlign: 'right' }}>
                        <div className="price-override-wrapper" style={{ display: 'inline-flex', alignItems: 'center', height: '28px', border: '1px solid var(--border-color)', borderRadius: '4px', padding: '0 4px', background: 'var(--bg-secondary)', width: '90px' }}>
                          <span className="price-override-symbol" style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginRight: '2px' }}>
                            {config.monedaPorDefecto === 'DOLARES' ? '$' : config.monedaPorDefecto === 'EUROS' ? '€' : 'S/'}
                          </span>
                          <input
                            type="number"
                            step="0.01"
                            min="0"
                            className="price-override-input"
                            value={item.precioFinal}
                            onChange={(e) => handlePriceChange(item.product.sku, e.target.value)}
                            id={`price-input-${item.product.sku}`}
                            style={{ border: 'none', background: 'transparent', width: '100%', height: '100%', fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-primary)', textAlign: 'right', outline: 'none' }}
                          />
                        </div>
                      </td>
                      
                      {/* Row Subtotal */}
                      <td style={{ textAlign: 'right', fontWeight: 600, fontSize: '0.82rem', color: 'var(--text-primary)', paddingRight: '0.5rem' }}>
                        {formatCurrency(item.cantidad * item.precioFinal)}
                      </td>
                      
                      {/* Remove item button */}
                      <td style={{ textAlign: 'center' }}>
                        <button
                          type="button"
                           className="btn-icon-delete"
                          onClick={() => handleRemoveFromCart(item.product.sku, item.product.nombre)}
                          style={{ border: 'none', background: 'transparent', color: '#ef4444', cursor: 'pointer', padding: '4px', borderRadius: '4px' }}
                        >
                          <Trash2 size={13} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <div className="barcode-placeholder" id="empty-state-cart">
                <div className="barcode-graphic-wrapper">
                  <svg 
                    width="96" 
                    height="96" 
                    viewBox="0 0 96 96" 
                    fill="none" 
                    xmlns="http://www.w3.org/2000/svg"
                    style={{ color: '#64748B' }}
                  >
                    {/* Corners */}
                    <path d="M12 28V16C12 13.7909 13.7909 12 16 12H28" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
                    <path d="M68 12H80C82.2091 12 84 13.7909 84 16V28" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
                    <path d="M84 68V80C84 82.2091 82.2091 84 80 84H68" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
                    <path d="M28 84H16C13.7909 84 12 82.2091 12 80V68" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
                    
                    {/* Red laser line across (just like premium scanner) */}
                    <line x1="16" y1="48" x2="80" y2="48" stroke="#EF4444" strokeWidth="3" strokeDasharray="3 3" strokeLinecap="round" />
                    
                    {/* Barcode bars */}
                    <rect x="24" y="22" width="4" height="52" rx="1.5" fill="currentColor" />
                    <rect x="32" y="22" width="8" height="52" rx="1.5" fill="currentColor" />
                    <rect x="44" y="22" width="3" height="52" rx="1.5" fill="currentColor" />
                    <rect x="51" y="22" width="6" height="52" rx="1.5" fill="currentColor" />
                    <rect x="61" y="22" width="8" height="52" rx="1.5" fill="currentColor" />
                    <rect x="72" y="22" width="4" height="52" rx="1.5" fill="currentColor" />
                  </svg>
                </div>
                <div className="barcode-placeholder-text">
                  Escanea un producto con un lector de código de barras o búscalo manualmente
                </div>
              </div>
            )}
          </div>
        </div>

      </div>

      {/* 4. POS Bottom sticky bar containing quick product search, total panel, and action triggers */}
      <div className="pos-bottom-bar">
        
        {/* Left Side: SKU or barcode rapid input adder form with recommendations */}
        <form onSubmit={handleQuickProductAdd} style={{ display: 'block', flex: 1, maxWidth: '40%' }}>
          <div className="search-box-wrapper" style={{ width: '100%', minWidth: 'auto', position: 'relative' }}>
            <input
              type="text"
              className="search-input"
              style={{ height: '40px', paddingLeft: '2.25rem', fontSize: '0.85rem' }}
              placeholder="Escanea SKU o escribe nombre..."
              value={quickProductSearch}
              onChange={(e) => {
                setQuickProductSearch(e.target.value);
                setShowProductSuggestions(true);
              }}
              onFocus={() => setShowProductSuggestions(true)}
              onBlur={() => setTimeout(() => setShowProductSuggestions(false), 200)}
            />
            <Search className="search-icon" size={16} style={{ left: '0.85rem' }} />

            {/* Product Suggestions Autocomplete Dropdown */}
            {showProductSuggestions && quickProductSearch.trim().length > 0 && (
              (() => {
                const query = quickProductSearch.toLowerCase().trim();
                const matchedProducts = products.filter(p => 
                  p.nombre.toLowerCase().includes(query) || p.sku.toLowerCase().includes(query)
                );
                
                if (matchedProducts.length > 0) {
                  return (
                    <div className="pos-product-suggestions-dropdown" style={{
                      position: 'absolute',
                      bottom: '100%',
                      left: 0,
                      right: 0,
                      background: 'var(--bg-card)',
                      border: '1px solid var(--border-color)',
                      borderRadius: '4px',
                      boxShadow: 'var(--shadow-lg)',
                      zIndex: 1000,
                      maxHeight: '220px',
                      overflowY: 'auto',
                      marginBottom: '6px'
                    }}>
                      {matchedProducts.map(p => (
                        <div
                          key={p.sku}
                          onMouseDown={() => {
                            handleAddToCart(p);
                            setQuickProductSearch('');
                            setShowProductSuggestions(false);
                          }}
                          style={{
                            padding: '8px 12px',
                            cursor: 'pointer',
                            fontSize: '0.82rem',
                            borderBottom: '1px solid var(--border-color)',
                            color: 'var(--text-primary)',
                            transition: 'background 0.2s',
                            display: 'flex',
                            justifyContent: 'space-between',
                            alignItems: 'center'
                          }}
                          className="suggestion-item"
                        >
                          <div style={{ display: 'flex', flexDirection: 'column', gap: '2px', minWidth: 0, flex: 1 }}>
                            <div style={{ fontWeight: 600, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{p.nombre}</div>
                            <div style={{ fontSize: '0.72rem', color: 'var(--text-secondary)' }}>SKU: {p.sku}</div>
                          </div>
                          <div style={{ fontWeight: 700, color: 'var(--primary)', marginLeft: '10px' }}>
                            {formatCurrency(p.precioUnitario)}
                          </div>
                        </div>
                      ))}
                    </div>
                  );
                } else {
                  return (
                    <div style={{
                      position: 'absolute',
                      bottom: '100%',
                      left: 0,
                      right: 0,
                      background: 'var(--bg-card)',
                      border: '1px solid var(--border-color)',
                      borderRadius: '4px',
                      boxShadow: 'var(--shadow-lg)',
                      zIndex: 1000,
                      padding: '12px',
                      marginBottom: '6px',
                      textAlign: 'center',
                      fontSize: '0.8rem',
                      color: 'var(--text-secondary)'
                    }}>
                      Ningún producto coincide
                    </div>
                  );
                }
              })()
            )}
          </div>
        </form>

        {/* Right Side Group: Total Neto box and preview/process actions */}
        <div className="pos-bottom-right-group">
          {/* Middle Area: Corporate Total Highlights panel */}
          <div className="pos-total-panel" style={{ display: 'flex', flexDirection: 'column', height: 'auto', padding: '10px 14px', alignItems: 'flex-end', justifyContent: 'center' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: '8px' }}>
              {parseFloat(String(descuentoGlobal)) > 0 && (
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: '1px', marginRight: '0.5rem' }}>
                  <span style={{ fontSize: '0.62rem', color: 'var(--text-muted)', fontWeight: 700 }}>DSCTO. GLOBAL ({descuentoGlobal}%)</span>
                  <span style={{ fontSize: '0.78rem', color: '#ef4444', fontWeight: 'bold' }}>-{formatCurrency(rawSubtotal * ((parseFloat(String(descuentoGlobal)) || 0) / 100))}</span>
                </div>
              )}
              {parseFloat(String(descuentoGlobal)) > 0 && <div style={{ width: '1px', height: '24px', background: 'var(--border-color)', marginRight: '0.5rem' }}></div>}
              <span className="pos-total-label">TOTAL NETO</span>
              <span className="pos-total-value" id="summary-total" style={{ margin: 0 }}>{formatCurrency(totalAmount)}</span>
            </div>
            {tipoCambio && (
              <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--primary)', marginTop: '4px', borderTop: '1px solid var(--border-color)', width: '100%', paddingTop: '4px', textAlign: 'right' }}>
                Ref: TC {tipoCambio.toFixed(3)} | Eq: <span style={{ color: 'var(--text-primary)', fontWeight: 700 }}>{getConvertedTotal()}</span>
              </div>
            )}
          </div>

          {/* Right Side: Flat gray preview and process triggers */}
          <div className="pos-bottom-actions">
            <button
              type="button"
              className="btn-pos-secondary btn-pos-action"
              onClick={() => {
                const { finalClient, isRegistered } = getValidatedClient();
                if (!isRegistered) {
                  setClient(finalClient);
                }
                if (validateForm(finalClient)) {
                  const friendlyDate = (ds: string) => {
                    if (!ds) return '';
                    const parts = ds.split('-');
                    return `${parts[2]}/${parts[1]}/${parts[0]}`;
                  };
                  const getFormattedTime = () => {
                    const d = new Date();
                    let hours = d.getHours();
                    const minutes = d.getMinutes();
                    const ampm = hours >= 12 ? 'PM' : 'AM';
                    hours = hours % 12;
                    hours = hours ? hours : 12;
                    const minutesStr = minutes < 10 ? '0' + minutes : minutes;
                    return `${hours}:${minutesStr} ${ampm}`;
                  };
                  // Build temporary quote for preview
                  const tempQuote = {
                    codigoCotizacion: `0001-${String(correlativo).padStart(7, '0')}`,
                    fechaEmision: friendlyDate(fechaEmision),
                    fechaVencimiento: friendlyDate(fechaVencimiento),
                    condicionPago,
                    usuarioEmisor,
                    horaEmision: getFormattedTime(),
                    empresaEmisora: {
                      nombre: config.nombreEmpresa,
                      ruc: config.ruc,
                      direccion: config.direccion,
                      telefono: config.telefono,
                      email: config.email,
                      logo: config.logo,
                      actividad: config.actividadEmpresa,
                      cuentas: config.cuentasBancarias,
                      moneda: config.monedaPorDefecto
                    },
                    cliente: finalClient,
                    items: cart.map(item => ({
                      sku: item.product.sku,
                      nombre: item.product.nombre,
                      descripcion: item.product.descripcion,
                      unidadMedida: item.product.unidadMedida || 'UNIDAD',
                      cantidad: item.cantidad,
                      precioUnitario: item.precioFinal,
                      totalItem: item.cantidad * item.precioFinal,
                      imagen: item.product.imagen
                    })),
                    finanzas: {
                      subtotal,
                      igv: taxAmount,
                      total: totalAmount,
                      aplicaIgv: applyTax,
                      totalEnLetras: numeroALetras(totalAmount, config.monedaPorDefecto)
                    },
                    terminos: config.terminosPorDefecto,
                    tipoComprobante,
                    serie,
                    descuentoGlobal: parseFloat(String(descuentoGlobal)) || 0,
                    placaVehiculo: placaVehiculo.trim() || undefined,
                    ordenCompra: ordenCompra.trim() || undefined,
                    guiaRemision: guiaRemision.trim() || undefined,
                    observaciones: observaciones.trim() || undefined,
                    tipoCambio: tipoCambio || undefined,
                  };
                  setGeneratedQuoteDetails(tempQuote);
                  setIsPreviewMode(true);
                  setIsPreviewOpen(true);
                }
              }}
              disabled={cart.length === 0}
              id="btn-preview-quote"
            >
              VISTA PREVIA
            </button>
            <button
              type="button"
              className="btn-pos-primary btn-pos-action"
              onClick={() => handleGenerateQuote()}
              disabled={isGenerating || cart.length === 0}
              id="btn-generate-quote"
            >
              {isGenerating ? 'PROCESANDO...' : 'PROCESAR'}
            </button>
          </div>
        </div>
      </div>

      {/* UNIFIED AUXILIARY MODAL */}
      {isExtraModalOpen && (
        <ModalPortal>
          <div className="modal-overlay" id="extra-modal-overlay" onClick={() => setIsExtraModalOpen(false)}>
            <div className="modal-container" style={{ maxWidth: '500px' }} onClick={e => e.stopPropagation()}>
              <div className="modal-header">
                <h3 className="modal-title">
                  <span>Parámetros Adicionales</span>
                </h3>
                <button type="button" className="modal-close-btn" onClick={() => setIsExtraModalOpen(false)}>
                  <X size={20} />
                </button>
              </div>
              <div className="modal-body" style={{ minHeight: '320px' }}>
                {/* Modal Internal Tabs navigation */}
                <div className="pos-action-tabs" style={{ marginBottom: '1.25rem', borderBottom: '1px solid var(--border-color)', paddingBottom: '0.75rem', gap: '4px' }}>
                  <button 
                    type="button" 
                    className={`pos-tab-btn ${activeExtraTab === 'placa' ? 'tab-active' : ''}`}
                    onClick={() => setActiveExtraTab('placa')}
                    style={{ background: activeExtraTab === 'placa' ? 'var(--primary)' : 'var(--bg-primary)', color: activeExtraTab === 'placa' ? 'white' : 'var(--text-secondary)', border: '1px solid var(--border-color)', flex: 1, padding: '0.35rem' }}
                  >
                    PLACA
                  </button>
                  <button 
                    type="button" 
                    className={`pos-tab-btn ${activeExtraTab === 'compra' ? 'tab-active' : ''}`}
                    onClick={() => setActiveExtraTab('compra')}
                    style={{ background: activeExtraTab === 'compra' ? 'var(--primary)' : 'var(--bg-primary)', color: activeExtraTab === 'compra' ? 'white' : 'var(--text-secondary)', border: '1px solid var(--border-color)', flex: 1, padding: '0.35rem' }}
                  >
                    COMPRA
                  </button>
                  <button 
                    type="button" 
                    className={`pos-tab-btn ${activeExtraTab === 'guia' ? 'tab-active' : ''}`}
                    onClick={() => setActiveExtraTab('guia')}
                    style={{ background: activeExtraTab === 'guia' ? 'var(--primary)' : 'var(--bg-primary)', color: activeExtraTab === 'guia' ? 'white' : 'var(--text-secondary)', border: '1px solid var(--border-color)', flex: 1, padding: '0.35rem' }}
                  >
                    GUÍA
                  </button>
                  <button 
                    type="button" 
                    className={`pos-tab-btn ${activeExtraTab === 'obs' ? 'tab-active' : ''}`}
                    onClick={() => setActiveExtraTab('obs')}
                    style={{ background: activeExtraTab === 'obs' ? 'var(--primary)' : 'var(--bg-primary)', color: activeExtraTab === 'obs' ? 'white' : 'var(--text-secondary)', border: '1px solid var(--border-color)', flex: 1, padding: '0.35rem' }}
                  >
                    OBS
                  </button>
                  <button 
                    type="button" 
                    className={`pos-tab-btn ${activeExtraTab === 'pago' ? 'tab-active' : ''}`}
                    onClick={() => setActiveExtraTab('pago')}
                    style={{ background: activeExtraTab === 'pago' ? 'var(--primary)' : 'var(--bg-primary)', color: activeExtraTab === 'pago' ? 'white' : 'var(--text-secondary)', border: '1px solid var(--border-color)', flex: 1, padding: '0.35rem' }}
                  >
                    PAGO
                  </button>

                </div>

                {/* Form panels depending on selected active tab */}
                {activeExtraTab === 'placa' && (
                  <div className="form-group" style={{ animation: 'pageEnter 0.2s' }}>
                    <label className="form-label" style={{ fontSize: '0.8rem' }}>Placa de Vehículo</label>
                    <div className="input-container">
                      <input 
                        type="text" 
                        className="form-input no-icon" 
                        placeholder="Placa del Vehículo" 
                        value={placaVehiculo} 
                        onChange={e => setPlacaVehiculo(e.target.value.toUpperCase())}
                      />
                    </div>
                    <span className="helper-text">Registra la placa del vehículo para impresión en cabecera.</span>
                  </div>
                )}

                {activeExtraTab === 'compra' && (
                  <div className="form-group" style={{ animation: 'pageEnter 0.2s' }}>
                    <label className="form-label" style={{ fontSize: '0.8rem' }}>Orden de Compra (O/C)</label>
                    <div className="input-container">
                      <input 
                        type="text" 
                        className="form-input no-icon" 
                        placeholder="Orden de Compra" 
                        value={ordenCompra} 
                        onChange={e => setOrdenCompra(e.target.value)}
                      />
                    </div>
                    <span className="helper-text">Código de la Orden de Compra oficial de la empresa contratante.</span>
                  </div>
                )}

                {activeExtraTab === 'guia' && (
                  <div className="form-group" style={{ animation: 'pageEnter 0.2s' }}>
                    <label className="form-label" style={{ fontSize: '0.8rem' }}>Guía de Remisión</label>
                    <div className="input-container">
                      <input 
                        type="text" 
                        className="form-input no-icon" 
                        placeholder="Guía de Remisión" 
                        value={guiaRemision} 
                        onChange={e => setGuiaRemision(e.target.value)}
                      />
                    </div>
                    <span className="helper-text">Guía de remisión asociada a la mercadería.</span>
                  </div>
                )}

                {activeExtraTab === 'obs' && (
                  <div className="form-group" style={{ animation: 'pageEnter 0.2s' }}>
                    <label className="form-label" style={{ fontSize: '0.8rem' }}>Observaciones del Documento</label>
                    <textarea 
                      className="form-input no-icon" 
                      placeholder="Escribe aquí observaciones, condiciones de entrega especiales o comentarios generales..." 
                      value={observaciones} 
                      onChange={e => setObservaciones(e.target.value)}
                      rows={5}
                      style={{ minHeight: '120px' }}
                    />
                  </div>
                )}

                {activeExtraTab === 'pago' && (
                  <div className="form-group" style={{ animation: 'pageEnter 0.2s' }}>
                    <label className="form-label" style={{ fontSize: '0.8rem' }}>Condición de Pago</label>
                    <select
                      className="form-input no-icon"
                      style={{ height: '38px', cursor: 'pointer' }}
                      value={condicionPago}
                      onChange={e => setCondicionPago(e.target.value)}
                    >
                      <option value="CONTADO">CONTADO</option>
                      <option value="CRÉDITO 15 DÍAS">CRÉDITO 15 DÍAS</option>
                      <option value="CRÉDITO 30 DÍAS">CRÉDITO 30 DÍAS</option>
                      <option value="CRÉDITO 45 DÍAS">CRÉDITO 45 DÍAS</option>
                      <option value="CONTRA ENTREGA">CONTRA ENTREGA</option>
                    </select>
                  </div>
                )}


              </div>
              <div className="modal-footer" style={{ borderTop: '1px solid var(--border-color)', paddingTop: '0.75rem' }}>
                <button type="button" className="btn-primary" onClick={() => setIsExtraModalOpen(false)} style={{ width: '100%' }}>
                  Guardar y Cerrar
                </button>
              </div>
            </div>
          </div>
        </ModalPortal>
      )}

      {/* Tipo de Cambio Modal */}
      {isExchangeRateModalOpen && (
        <ModalPortal>
          <div className="modal-overlay" id="exchange-rate-modal-overlay" onClick={() => setIsExchangeRateModalOpen(false)}>
            <div className="modal-container" style={{ maxWidth: '450px' }} onClick={e => e.stopPropagation()}>
              <div className="modal-header">
                <h3 className="modal-title">
                  <span>Consulta de Tipo de Cambio</span>
                </h3>
                <button type="button" className="modal-close-btn" onClick={() => setIsExchangeRateModalOpen(false)}>
                  <X size={20} />
                </button>
              </div>
              <div className="modal-body" style={{ minHeight: '120px' }}>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                  
                  <div className="form-group">
                    <label className="form-label" style={{ fontSize: '0.8rem' }}>Ingresar Tipo de Cambio Manual</label>
                    <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                      <input
                        type="number"
                        className="form-input no-icon"
                        style={{ flex: 1, height: '38px', fontSize: '0.95rem' }}
                        step="0.001"
                        min="0.1"
                        placeholder="Ejem: 3.750"
                        defaultValue={tipoCambio || ''}
                        id="custom-exchange-rate-input"
                      />
                      <button
                        type="button"
                        onClick={() => {
                          const input = document.getElementById('custom-exchange-rate-input') as HTMLInputElement;
                          if (input) {
                            handleApplyExchangeRate(parseFloat(input.value));
                          }
                        }}
                        className="btn-primary"
                        style={{
                          height: '38px',
                          padding: '0 1.25rem',
                          borderRadius: 'var(--radius-md)',
                          fontSize: '0.9rem',
                          fontWeight: 600,
                          cursor: 'pointer'
                        }}
                      >
                        Aplicar
                      </button>
                    </div>
                  </div>

                  {tipoCambio && (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', borderTop: '1px solid var(--border-color)', paddingTop: '12px', marginTop: '4px' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ fontSize: '0.78rem', fontWeight: 600 }}>T.C. Aplicado actual:</span>
                        <span style={{ fontSize: '0.88rem', fontWeight: 700, color: 'var(--primary)' }}>
                          S/ {tipoCambio.toFixed(3)}
                        </span>
                      </div>
                      <button
                        type="button"
                        onClick={() => {
                          handleClearExchangeRate();
                          const input = document.getElementById('custom-exchange-rate-input') as HTMLInputElement;
                          if (input) input.value = '';
                        }}
                        style={{
                          width: '100%',
                          padding: '6px',
                          fontSize: '0.75rem',
                          borderRadius: '4px',
                          border: '1px solid #ef4444',
                          background: 'rgba(239, 68, 68, 0.08)',
                          color: '#ef4444',
                          fontWeight: 600,
                          cursor: 'pointer',
                          marginTop: '4px'
                        }}
                      >
                        Quitar Tipo de Cambio
                      </button>
                    </div>
                  )}

                </div>
              </div>
              <div className="modal-footer" style={{ borderTop: '1px solid var(--border-color)', paddingTop: '0.75rem' }}>
                <button type="button" className="btn-secondary" onClick={() => setIsExchangeRateModalOpen(false)} style={{ width: '100%' }}>
                  Cerrar
                </button>
              </div>
            </div>
          </div>
        </ModalPortal>
      )}

      {/* POPUP MODAL: QUOTATION SUCCESS GENERATED SUMMARY */}
      {isSuccessOpen && generatedQuoteDetails && (
        <ModalPortal>
          <div className="modal-overlay" onClick={() => setIsSuccessOpen(false)} id="success-quote-modal">
            <div className="modal-container" style={{ maxWidth: '520px' }} onClick={(e) => e.stopPropagation()}>
              <div className="modal-header">
                <h3 className="modal-title" style={{ color: '#10b981', display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <CheckCircle2 size={20} />
                  <span>Cotización Emitida con Éxito</span>
                </h3>
                <button 
                  type="button" 
                  className="modal-close-btn" 
                  onClick={() => setIsSuccessOpen(false)}
                  id="close-success-modal"
                >
                  <X size={20} />
                </button>
              </div>
              
              <div className="modal-body success-quote-card" style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                <span className="success-quote-badge" id="quote-success-badge" style={{ display: 'inline-block', padding: '4px 10px', background: 'rgba(16, 185, 129, 0.12)', color: '#10b981', borderRadius: '4px', fontWeight: 'bold', fontSize: '0.85rem', alignSelf: 'flex-start' }}>
                  {tipoComprobante}: {generatedQuoteDetails.codigoCotizacion}
                </span>
                
                <p style={{ fontSize: '0.82rem', color: 'var(--text-secondary)', margin: 0 }}>
                  El documento de cotización comercial se ha registrado en el sistema. Los datos financieros y de auditoría se encuentran persistidos.
                </p>

                <div style={{ width: '100%', background: 'var(--bg-primary)', border: '1px solid var(--border-color)', padding: '0.85rem', borderRadius: '6px', textAlign: 'left', display: 'flex', flexDirection: 'column', gap: '6px', fontSize: '0.78rem' }}>
                  <div><strong>Emisor:</strong> {generatedQuoteDetails.empresaEmisora.nombre} (RUC: {generatedQuoteDetails.empresaEmisora.ruc})</div>
                  <div><strong>Vendedor Emisor:</strong> {generatedQuoteDetails.usuarioEmisor}</div>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <div><strong>Condición de Pago:</strong> {generatedQuoteDetails.condicionPago}</div>
                    <div><strong>Moneda:</strong> {generatedQuoteDetails.empresaEmisora.moneda}</div>
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <div><strong>Fecha Emisión:</strong> {generatedQuoteDetails.fechaEmision}</div>
                    <div><strong>Vence:</strong> {generatedQuoteDetails.fechaVencimiento}</div>
                  </div>
                  <div style={{ borderTop: '1px solid var(--border-color)', paddingTop: '6px' }}>
                    <strong>Cliente:</strong> {generatedQuoteDetails.cliente.nombre} ({generatedQuoteDetails.cliente.documento})
                  </div>
                  {generatedQuoteDetails.cliente.correo && <div><strong>Correo Cliente:</strong> {generatedQuoteDetails.cliente.correo}</div>}
                  {generatedQuoteDetails.cliente.direccion && <div><strong>Dirección Cliente:</strong> {generatedQuoteDetails.cliente.direccion}</div>}
                  {placaVehiculo && <div><strong>Placa Vehículo:</strong> {placaVehiculo}</div>}
                  {ordenCompra && <div><strong>Orden de Compra:</strong> {ordenCompra}</div>}
                  {guiaRemision && <div><strong>Guía de Remisión:</strong> {guiaRemision}</div>}
                  
                  <div style={{ borderTop: '1px solid var(--border-color)', paddingTop: '6px', fontSize: '0.72rem', color: 'var(--accent)', fontStyle: 'italic' }}>
                    Son: {generatedQuoteDetails.finanzas.totalEnLetras}
                  </div>

                  <div style={{ borderTop: '1px solid var(--border-color)', paddingTop: '6px', display: 'flex', justifyContent: 'space-between', fontWeight: 'bold', fontSize: '0.85rem' }}>
                    <span>Importe Total:</span>
                    <span style={{ color: 'var(--accent)' }}>{formatCurrency(generatedQuoteDetails.finanzas.total)}</span>
                  </div>
                </div>
              </div>
              
              <div className="modal-footer" style={{ borderTop: '1px solid var(--border-color)', paddingTop: '0.75rem', display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
                <button
                  type="button"
                  className="btn-secondary"
                  onClick={() => setIsSuccessOpen(false)}
                  id="success-keep-editing-btn"
                  style={{ height: '36px', fontSize: '0.8rem' }}
                >
                  Cerrar
                </button>
                <button
                  type="button"
                  className="btn-primary"
                  onClick={() => {
                    setIsSuccessOpen(false);
                    setIsPreviewMode(false);
                    setIsPreviewOpen(true);
                  }}
                  id="success-print-btn"
                  style={{ display: 'flex', alignItems: 'center', gap: '6px', background: 'var(--accent)', borderColor: 'var(--accent)', color: 'white', height: '36px', fontSize: '0.8rem' }}
                >
                  <Printer size={13} />
                  <span>Imprimir / PDF</span>
                </button>
                <button
                  type="button"
                  className="btn-primary"
                  onClick={handleResetQuote}
                  id="success-new-quote-btn"
                  style={{ height: '36px', fontSize: '0.8rem' }}
                >
                  Nueva Cotización
                </button>
              </div>
            </div>
          </div>
        </ModalPortal>
      )}

      {/* Floating Notifications Toast */}
      {toast.show && (
        <div className="toast-container" id="quoting-toast-container">
          <div 
            className="toast" 
            id="quoting-toast"
            style={{ 
              borderColor: toast.type === 'success' ? 'rgba(16, 185, 129, 0.3)' : 'rgba(239, 68, 68, 0.3)',
              background: 'var(--bg-secondary)',
              color: 'var(--text-primary)',
              boxShadow: 'var(--shadow-lg)'
            }}
          >
            <div 
              className="toast-success-icon"
              style={{ 
                color: toast.type === 'success' ? '#10b981' : '#ef4444',
                background: toast.type === 'success' ? 'rgba(16, 185, 129, 0.1)' : 'rgba(239, 68, 68, 0.1)'
              }}
            >
              {toast.type === 'success' ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />}
            </div>
            <span style={{ fontSize: '0.85rem', fontWeight: 600 }}>
              {toast.message}
            </span>
          </div>
        </div>
      )}

      {/* Printable template workspace preview */}
      <PrintPreview 
        isOpen={isPreviewOpen} 
        onClose={() => setIsPreviewOpen(false)} 
        quote={generatedQuoteDetails} 
        isPreviewMode={isPreviewMode}
      />
    </div>
  );
};
