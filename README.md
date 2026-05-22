# 🛒 ElitePOS v1 (Legacy) - Sistema de Punto de Venta B2B

Bienvenido al código fuente de **ElitePOS v1**. Este proyecto fue concebido como un Punto de Venta (POS) y sistema de facturación en la nube, construido con **C# Blazor WebAssembly**. 

## ⚠️ Estado del Proyecto: Laboratorio / Descontinuado
**Este repositorio está oficialmente archivado.** Sirvió como un campo de pruebas intensivo y un laboratorio de experimentación arquitectónica. El código pasó por múltiples iteraciones de diseño, migraciones de MudBlazor a Vanilla CSS puro, y lógicas complejas de caché. 

Como resultado de tanta experimentación, el código base se volvió híbrido. Se ha decidido reescribir el proyecto desde cero con una Clean Architecture estricta, por lo que esta versión 1 queda libre para la comunidad.

## 💎 ¿Por qué te puede interesar este código?
Si eres un desarrollador y tienes la motivación de refactorizar, o simplemente quieres extraer "órganos vitales" para tus propios proyectos, aquí encontrarás código de mucho valor:

* **Sincronización Offline (Modo sin conexión):** Lógica que guarda ventas localmente mediante IDs temporales y las sincroniza cuando regresa el internet.
* **Caché SWR Inteligente:** Servicios de estado global en C# que evitan consultas infinitas y ahorran lecturas en Firestore.
* **Integración Base Firebase (Firestore):** Servicios listos para leer y escribir colecciones. *(Nota: Las credenciales reales han sido sanitizadas por seguridad).*
* **Reglas de SUNAT / IGV:** Cálculos matemáticos preparados para la desagregación de impuestos (IGV 18%) de cara a la facturación electrónica peruana.
* **UI B2B Premium:** Maquetación HTML/CSS pura inyectada para simular una experiencia corporativa.

## 🛠️ Stack Tecnológico
* **Frontend:** C# / Blazor WebAssembly (.NET)
* **Diseño:** Vanilla CSS (Sistema B2B Custom)
* **Backend as a Service:** Firebase (Firestore)
* **Integraciones Extra:** APIs de WhatsApp (estructuras base)

## 🤝 ¿Quieres continuarlo?
¡Haz un *Fork* y hazlo tuyo! El reto principal si decides revivir este proyecto será desenredar la lógica de UI de la lógica de negocio (`@code`) y unificar el sistema de diseño. Todo el núcleo duro (conexiones, cálculos y offline) ya está hecho. 

> *"El código viejo nunca se borra, se dona a la ciencia."*
