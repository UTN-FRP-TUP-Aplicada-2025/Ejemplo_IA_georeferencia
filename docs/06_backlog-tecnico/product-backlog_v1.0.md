# Product Backlog

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** product-backlog_v1.0.md
**Versión:** 1.1
**Estado:** Activo
**Fecha:** 2026-04-16
**Autor:** Equipo Técnico

---

# 1. Propósito

Este documento constituye el Product Backlog del proyecto GeoFoto. Se centralizan todas las historias de usuario con sus identificadores Jira, priorización, story points, criterios de aceptación en formato BDD y trazabilidad completa a épicas, sprints y tareas técnicas hijas.

---

# 2. Formato

Las historias de usuario siguen el formato estándar:

> **Como** [rol], **quiero** [acción], **para** [beneficio]

Cada historia incluye criterios de aceptación en formato Given-When-Then (Dado-Cuando-Entonces) y un desglose de tareas técnicas hijas con estimación en horas.

---

# 3. Épicas

| ID Jira  | Nombre                              | Color   |
| -------- | ----------------------------------- | ------- |
| GEO-E01  | Fundaciones del proyecto            | Azul    |
| GEO-E02  | Captura y visualización online      | Verde   |
| GEO-E03  | App móvil MAUI Android              | Naranja |
| GEO-E04  | Motor offline-first (SQLite)        | Rojo    |
| GEO-E05  | Motor de sincronización             | Morado  |
| GEO-E06  | Calidad, UX y deploy productivo     | Gris    |
| GEO-E07  | UX Avanzado Mobile + Web            | Cian    |

---

# 4. Backlog de Producto

| ID Jira    | Historia de Usuario                                                                                              | Épica   | Sprint    | Story Points | Prioridad | Estado |
| ---------- | ---------------------------------------------------------------------------------------------------------------- | ------- | --------- | ------------ | --------- | ------ |
| GEO-US01   | Estructura de la solución con los 4 proyectos compilando                                                         | GEO-E01 | Sprint 01 | 5            | Highest   | To Do  |
| GEO-US02   | Base de datos SQL Server configurada con EF Core y migrations iniciales                                          | GEO-E01 | Sprint 01 | 8            | Highest   | To Do  |
| GEO-US03   | MudBlazor integrado en GeoFoto.Shared con tema configurado                                                       | GEO-E01 | Sprint 01 | 3            | High      | To Do  |
| GEO-US04   | Subir fotos desde la web y verlas como markers en el mapa                                                        | GEO-E02 | Sprint 02 | 8            | Highest   | To Do  |
| GEO-US05   | Click en marker y ver popup con fotos y datos del punto                                                          | GEO-E02 | Sprint 02 | 5            | High      | To Do  |
| GEO-US06   | Editar nombre y descripción de un punto desde el popup                                                           | GEO-E02 | Sprint 02 | 5            | High      | To Do  |
| GEO-US07   | Ver todos los puntos en una tabla con filtros                                                                     | GEO-E02 | Sprint 02 | 3            | Medium    | To Do  |
| GEO-US08   | Eliminar un punto con confirmación                                                                               | GEO-E02 | Sprint 03 | 3            | Medium    | To Do  |
| GEO-US09   | Instalar la app en Android y ver el mapa con puntos existentes                                                   | GEO-E03 | Sprint 03 | 8            | Highest   | To Do  |
| GEO-US10   | Tomar foto con la cámara del celular y registrarla en el mapa                                                    | GEO-E03 | Sprint 03 | 8            | Highest   | To Do  |
| GEO-US11   | Fotos y puntos se guardan sin internet                                                                           | GEO-E04 | Sprint 04 | 13           | Highest   | To Do  |
| GEO-US12   | Ver cuántas operaciones pendientes de sincronizar                                                                | GEO-E04 | Sprint 04 | 5            | High      | To Do  |
| GEO-US13   | App funciona igual offline y online sin diferencia perceptible                                                   | GEO-E04 | Sprint 04 | 5            | High      | To Do  |
| GEO-US14   | Datos offline se sincronizan automáticamente al recuperar la red                                                 | GEO-E05 | Sprint 05 | 13           | Highest   | To Do  |
| GEO-US15   | Forzar sincronización manualmente                                                                                | GEO-E05 | Sprint 05 | 5            | High      | To Do  |
| GEO-US16   | Ver historial de sincronización con resultado de cada operación                                                  | GEO-E05 | Sprint 05 | 5            | Medium    | To Do  |
| GEO-US17   | Resolver conflictos automáticamente por Last-Write-Wins                                                          | GEO-E05 | Sprint 05 | 8            | High      | To Do  |
| GEO-US18   | Pipeline CI/CD que compile, testee y genere APK en cada push                                                     | GEO-E06 | Sprint 06 | 8            | High      | To Do  |
| GEO-US19   | Tests de integración del motor de sync con cobertura >= 80%                                                      | GEO-E06 | Sprint 06 | 8            | Highest   | To Do  |
| GEO-US20   | Mapa y app responden fluidamente con 100+ puntos                                                                 | GEO-E06 | Sprint 06 | 5            | Medium    | To Do  |
| GEO-US20b  | Centrar el mapa en mi posición actual tocando un botón                                                           | GEO-E07 | Sprint 07 | 5            | Must      | ✅ Done |
| GEO-US21   | Ver mi posición actual como un punto en el mapa                                                                  | GEO-E07 | Sprint 07 | 5            | Must      | ✅ Done |
| GEO-US22   | Visualizar y ajustar el radio del marker actual                                                                  | GEO-E07 | Sprint 07 | 8            | Must      | ✅ Done |
| GEO-US23   | Popup de marker con carrusel de fotos, título y descripción del punto, y ampliar cada foto                       | GEO-E07 | Sprint 07 | 13           | Must      | ✅ Done |
| GEO-US24   | Trabajar offline y que todo se sincronice automáticamente cuando haya red                                        | GEO-E07 | Sprint 07 | 8            | Must      | ✅ Done |
| GEO-US25   | Quitar fotos desde el carrusel del marker                                                                        | GEO-E07 | Sprint 07 | 5            | Should    | ✅ Done |
| GEO-US26   | Ampliar fotos desde el carrusel para verlas en detalle                                                           | GEO-E07 | Sprint 07 | 3            | Should    | ✅ Done |
| GEO-US27   | Ver el estado de sincronización e iniciarlo manualmente                                                          | GEO-E07 | Sprint 07 | 5            | Must      | ✅ Done |
| GEO-US28   | Ver la lista de todos los markers, buscarlos y editar su carrusel                                                | GEO-E07 | Sprint 07 | 8            | Must      | ✅ Done |
| GEO-US29   | Eliminar un marker (con todas sus fotos) al seleccionarlo                                                        | GEO-E07 | Sprint 07 | 5            | Should    | ✅ Done |
| GEO-US30   | Compartir una o más fotos del carrusel a través de apps del dispositivo                                          | GEO-E07 | Sprint 08 | 5            | Could     | ✅ Done |
| GEO-US31   | En la web, la misma experiencia de mapa que en Android                                                           | GEO-E07 | Sprint 08 | 5            | Must      | ✅ Done |
| GEO-US32   | Descargar localmente todas las fotos de un marker en un zip                                                      | GEO-E07 | Sprint 08 | 5            | Must      | ✅ Done |
| GEO-US33   | Subir fotos al carrusel de un marker existente desde el navegador                                                | GEO-E07 | Sprint 08 | 5            | Must      | ✅ Done |

---

## 4.1. Épica GEO-E01 — Fundaciones del proyecto

---

### GEO-US01: Estructura de la solución con los 4 proyectos compilando

**Épica padre:** GEO-E01
**Sprint asignado:** Sprint 01
**Story Points:** 5
**Prioridad:** Highest
**Descripción:** Como desarrollador, quiero la estructura de la solución con los 4 proyectos compilando, para tener la base sobre la que construir.

#### Criterios de Aceptación

##### CA-01 Compilación exitosa de la solución completa

**Dado** que se ha clonado el repositorio y restaurado las dependencias NuGet
**Cuando** se ejecute `dotnet build` sobre la solución
**Entonces** los cuatro proyectos (GeoFoto.Api, GeoFoto.Web, GeoFoto.Mobile, GeoFoto.Shared) compilarán sin errores ni advertencias críticas.

##### CA-02 Referencias entre proyectos correctas

**Dado** que la solución contiene los cuatro proyectos configurados
**Cuando** se inspeccionen las referencias de proyecto
**Entonces** GeoFoto.Api, GeoFoto.Web y GeoFoto.Mobile referenciarán a GeoFoto.Shared, y no existirán referencias circulares.

##### CA-03 Ejecución de proyecto API y Web

**Dado** que la solución compila sin errores
**Cuando** se ejecuten GeoFoto.Api y GeoFoto.Web de forma independiente
**Entonces** cada proyecto se iniciará correctamente y responderá en su endpoint por defecto.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                    | Componente      | Estimación |
| -------- | --------------------------------------------------------- | --------------- | ---------- |
| GEO-T01  | Crear solución .sln y proyecto GeoFoto.Api (Web API)      | Infraestructura | 2h         |
| GEO-T02  | Crear proyecto GeoFoto.Web (Blazor Server)                | Infraestructura | 2h         |
| GEO-T03  | Crear proyecto GeoFoto.Mobile (MAUI Blazor Hybrid)        | Infraestructura | 2h         |
| GEO-T04  | Crear proyecto GeoFoto.Shared (Class Library)             | Infraestructura | 1h         |

---

### GEO-US02: Base de datos SQL Server con EF Core y migrations iniciales

**Épica padre:** GEO-E01
**Sprint asignado:** Sprint 01
**Story Points:** 8
**Prioridad:** Highest
**Descripción:** Como desarrollador, quiero la base de datos SQL Server configurada con EF Core y las migrations iniciales, para persistir datos del servidor.

#### Criterios de Aceptación

##### CA-01 Migration inicial aplicada

**Dado** que se dispone de una instancia de SQL Server accesible
**Cuando** se ejecute `dotnet ef database update`
**Entonces** se creará la base de datos con las tablas PuntoGeografico y Fotografia según el modelo de dominio.

##### CA-02 DbContext configurado con connection string

**Dado** que la configuración de `appsettings.json` contiene la cadena de conexión a SQL Server
**Cuando** se inyecte `GeoFotoDbContext` en un servicio
**Entonces** el contexto se resolverá correctamente y permitirá operaciones CRUD.

##### CA-03 Seed de datos de prueba

**Dado** que la migration inicial se ha aplicado exitosamente
**Cuando** se ejecute la aplicación por primera vez
**Entonces** se poblarán al menos 3 puntos geográficos de ejemplo con sus fotografías asociadas.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                       | Componente    | Estimación |
| -------- | ------------------------------------------------------------ | ------------- | ---------- |
| GEO-T05  | Definir entidades de dominio (PuntoGeografico, Fotografia)   | Modelo        | 2h         |
| GEO-T06  | Configurar GeoFotoDbContext con Fluent API                   | Persistencia  | 3h         |
| GEO-T07  | Crear migration inicial                                      | Persistencia  | 1h         |
| GEO-T08  | Configurar connection string en appsettings.json             | Configuración | 1h         |
| GEO-T09  | Implementar Data Seeder con datos de prueba                  | Persistencia  | 2h         |

---

### GEO-US03: MudBlazor integrado en GeoFoto.Shared con tema configurado

**Épica padre:** GEO-E01
**Sprint asignado:** Sprint 01
**Story Points:** 3
**Prioridad:** High
**Descripción:** Como desarrollador, quiero MudBlazor integrado en GeoFoto.Shared con tema configurado, para tener la librería UI disponible en todos los proyectos.

#### Criterios de Aceptación

##### CA-01 Paquete NuGet instalado y tema aplicado

**Dado** que se ha agregado el paquete MudBlazor a GeoFoto.Shared
**Cuando** se ejecute la aplicación Web o Mobile
**Entonces** los componentes MudBlazor se renderizarán con la paleta de colores definida en el tema personalizado.

##### CA-02 Componentes compartidos accesibles

**Dado** que MudBlazor está configurado en GeoFoto.Shared
**Cuando** se cree un componente Blazor en GeoFoto.Web o GeoFoto.Mobile que use `<MudButton>`
**Entonces** el componente se renderizará correctamente sin errores de referencia.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                  | Componente | Estimación |
| -------- | ------------------------------------------------------- | ---------- | ---------- |
| GEO-T10  | Instalar paquete NuGet MudBlazor en GeoFoto.Shared     | UI         | 1h         |
| GEO-T11  | Configurar MudThemeProvider con paleta personalizada    | UI         | 2h         |
| GEO-T12  | Crear layout base compartido con MudLayout              | UI         | 2h         |

---

## 4.2. Épica GEO-E02 — Captura y visualización online

---

### GEO-US04: Subir fotos desde la web y verlas como markers en el mapa

**Épica padre:** GEO-E02
**Sprint asignado:** Sprint 02
**Story Points:** 8
**Prioridad:** Highest
**Descripción:** Como supervisor, quiero subir fotos desde la web y verlas como markers en el mapa, para registrar puntos desde escritorio.

#### Criterios de Aceptación

##### CA-01 Carga de fotografía con geolocalización

**Dado** que el supervisor se encuentra en la pantalla del mapa en la aplicación web
**Cuando** seleccione una ubicación en el mapa y suba una fotografía (JPEG/PNG, máx. 10 MB)
**Entonces** se creará un punto geográfico con las coordenadas seleccionadas y la foto se almacenará asociada al punto.

##### CA-02 Visualización de markers en el mapa

**Dado** que existen puntos geográficos con fotografías registradas en la base de datos
**Cuando** se cargue la vista de mapa
**Entonces** cada punto se representará como un marker en la posición correspondiente del mapa Leaflet.

##### CA-03 Validación de formato y tamaño de archivo

**Dado** que el supervisor intenta subir un archivo
**Cuando** el archivo no sea JPEG ni PNG, o supere los 10 MB
**Entonces** se mostrará un mensaje de error descriptivo y la operación se rechazará sin crear el punto.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                          | Componente | Estimación |
| -------- | --------------------------------------------------------------- | ---------- | ---------- |
| GEO-T13  | Crear endpoint POST /api/puntos con carga de archivo            | API        | 3h         |
| GEO-T14  | Implementar servicio de almacenamiento de imágenes              | API        | 2h         |
| GEO-T15  | Crear endpoint GET /api/puntos para listar puntos               | API        | 2h         |
| GEO-T16  | Integrar Leaflet.js en componente Blazor de mapa                | Web        | 3h         |
| GEO-T17  | Implementar componente de carga de fotos con previsualización   | Web        | 2h         |
| GEO-T18  | Renderizar markers desde datos de API                           | Web        | 2h         |
| GEO-T19  | Validar formato y tamaño de archivo en cliente y servidor       | API / Web  | 2h         |

---

### GEO-US05: Click en marker y ver popup con fotos y datos del punto

**Épica padre:** GEO-E02
**Sprint asignado:** Sprint 02
**Story Points:** 5
**Prioridad:** High
**Descripción:** Como supervisor, quiero hacer click en un marker y ver el popup con fotos y datos del punto, para acceder al detalle sin salir del mapa.

#### Criterios de Aceptación

##### CA-01 Popup con información del punto

**Dado** que el mapa muestra markers de puntos geográficos
**Cuando** el supervisor haga click en un marker
**Entonces** se desplegará un popup que muestre el nombre del punto, la descripción, las coordenadas y la fecha de creación.

##### CA-02 Galería de fotos en popup

**Dado** que un punto tiene una o más fotografías asociadas
**Cuando** se abra el popup del marker correspondiente
**Entonces** se mostrarán las miniaturas de todas las fotos del punto, con la posibilidad de ampliar cada una.

##### CA-03 Popup sin fotos

**Dado** que un punto no tiene fotografías asociadas
**Cuando** se abra el popup del marker
**Entonces** se mostrará la información textual del punto y un mensaje indicando que no se dispone de fotografías.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                     | Componente | Estimación |
| -------- | ---------------------------------------------------------- | ---------- | ---------- |
| GEO-T20  | Crear endpoint GET /api/puntos/{id} con fotos incluidas    | API        | 2h         |
| GEO-T21  | Implementar componente PopupPunto con datos y galería      | Web        | 3h         |
| GEO-T22  | Integrar popup con evento click de marker en Leaflet       | Web        | 2h         |
| GEO-T23  | Generar miniaturas de fotos para carga rápida              | API        | 2h         |

---

### GEO-US06: Editar nombre y descripción de un punto desde el popup

**Épica padre:** GEO-E02
**Sprint asignado:** Sprint 02
**Story Points:** 5
**Prioridad:** High
**Descripción:** Como supervisor, quiero editar el nombre y descripción de un punto desde el popup, para enriquecer el registro.

#### Criterios de Aceptación

##### CA-01 Edición inline del punto

**Dado** que el popup de un punto se encuentra abierto
**Cuando** el supervisor haga click en el botón de edición
**Entonces** los campos de nombre y descripción se transformarán en inputs editables con los valores actuales precargados.

##### CA-02 Persistencia de cambios

**Dado** que el supervisor ha modificado el nombre o la descripción de un punto
**Cuando** presione el botón "Guardar"
**Entonces** los cambios se enviarán al servidor mediante PUT /api/puntos/{id} y el popup reflejará los valores actualizados.

##### CA-03 Validación de campos obligatorios

**Dado** que el supervisor intenta guardar con el campo nombre vacío
**Cuando** presione el botón "Guardar"
**Entonces** se mostrará un mensaje de validación indicando que el nombre es obligatorio y no se enviará la petición al servidor.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                   | Componente | Estimación |
| -------- | -------------------------------------------------------- | ---------- | ---------- |
| GEO-T24  | Crear endpoint PUT /api/puntos/{id}                      | API        | 2h         |
| GEO-T25  | Implementar modo edición en componente PopupPunto        | Web        | 3h         |
| GEO-T26  | Agregar validaciones de formulario con DataAnnotations   | Web        | 2h         |

---

### GEO-US07: Ver todos los puntos en una tabla con filtros

**Épica padre:** GEO-E02
**Sprint asignado:** Sprint 02
**Story Points:** 3
**Prioridad:** Medium
**Descripción:** Como supervisor, quiero ver todos los puntos en una tabla con filtros, para gestionar el inventario.

#### Criterios de Aceptación

##### CA-01 Tabla paginada de puntos

**Dado** que existen puntos registrados en el sistema
**Cuando** el supervisor navegue a la vista de listado
**Entonces** se mostrará una tabla MudDataGrid con columnas: nombre, coordenadas, cantidad de fotos, fecha de creación, y paginación de 10 registros por página.

##### CA-02 Filtrado por nombre

**Dado** que la tabla muestra puntos registrados
**Cuando** el supervisor ingrese texto en el campo de búsqueda
**Entonces** la tabla se filtrará mostrando únicamente los puntos cuyo nombre contenga el texto ingresado.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                               | Componente | Estimación |
| -------- | ---------------------------------------------------- | ---------- | ---------- |
| GEO-T27  | Implementar paginación y filtrado en GET /api/puntos | API        | 2h         |
| GEO-T28  | Crear componente ListadoPuntos con MudDataGrid       | Web        | 3h         |
| GEO-T29  | Agregar navegación entre vista mapa y vista tabla    | Web        | 1h         |

---

### GEO-US08: Eliminar un punto con confirmación

**Épica padre:** GEO-E02
**Sprint asignado:** Sprint 03
**Story Points:** 3
**Prioridad:** Medium
**Descripción:** Como supervisor, quiero eliminar un punto con confirmación, para limpiar registros incorrectos.

#### Criterios de Aceptación

##### CA-01 Diálogo de confirmación

**Dado** que el supervisor ha seleccionado un punto para eliminar (desde popup o tabla)
**Cuando** presione el botón "Eliminar"
**Entonces** se mostrará un diálogo de confirmación MudDialog indicando el nombre del punto y solicitando confirmación explícita.

##### CA-02 Eliminación efectiva

**Dado** que el supervisor ha confirmado la eliminación
**Cuando** se procese la operación
**Entonces** el punto y todas sus fotografías asociadas se eliminarán del servidor (soft-delete) y el marker desaparecerá del mapa.

##### CA-03 Cancelación de eliminación

**Dado** que se muestra el diálogo de confirmación de eliminación
**Cuando** el supervisor presione "Cancelar"
**Entonces** el diálogo se cerrará y el punto permanecerá sin modificaciones.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                    | Componente | Estimación |
| -------- | --------------------------------------------------------- | ---------- | ---------- |
| GEO-T30  | Crear endpoint DELETE /api/puntos/{id} (soft-delete)      | API        | 2h         |
| GEO-T31  | Implementar diálogo de confirmación con MudDialog         | Web        | 2h         |
| GEO-T32  | Actualizar mapa y tabla tras eliminación exitosa          | Web        | 1h         |

---

## 4.3. Épica GEO-E03 — App móvil MAUI Android

---

### GEO-US09: Instalar la app en Android y ver el mapa con puntos existentes

**Épica padre:** GEO-E03
**Sprint asignado:** Sprint 03
**Story Points:** 8
**Prioridad:** Highest
**Descripción:** Como técnico de campo, quiero instalar la app en mi Android y ver el mapa con los puntos existentes, para trabajar desde el móvil.

#### Criterios de Aceptación

##### CA-01 Instalación del APK en dispositivo Android

**Dado** que se dispone del archivo APK generado por el build
**Cuando** se instale en un dispositivo Android 10+
**Entonces** la aplicación se instalará correctamente y se abrirá mostrando la pantalla principal con el mapa.

##### CA-02 Visualización de puntos existentes en mapa móvil

**Dado** que el dispositivo tiene conexión a internet y la API está accesible
**Cuando** se abra la aplicación
**Entonces** el mapa mostrará todos los puntos geográficos existentes como markers, con la misma información disponible en la versión web.

##### CA-03 Centrado del mapa en ubicación del usuario

**Dado** que el usuario ha otorgado permisos de ubicación a la aplicación
**Cuando** se cargue el mapa
**Entonces** la vista se centrará en la ubicación GPS actual del dispositivo con un nivel de zoom adecuado.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                        | Componente | Estimación |
| -------- | ------------------------------------------------------------- | ---------- | ---------- |
| GEO-T33  | Configurar proyecto MAUI Blazor Hybrid para Android           | Mobile     | 3h         |
| GEO-T34  | Integrar componente de mapa compartido desde GeoFoto.Shared   | Mobile     | 3h         |
| GEO-T35  | Configurar permisos Android (ubicación, cámara, storage)      | Mobile     | 2h         |
| GEO-T36  | Implementar servicio de geolocalización con MAUI Essentials   | Mobile     | 2h         |
| GEO-T37  | Configurar HttpClient para comunicación con API               | Mobile     | 2h         |

---

### GEO-US10: Tomar foto con cámara del celular y registrarla en el mapa

**Épica padre:** GEO-E03
**Sprint asignado:** Sprint 03
**Story Points:** 8
**Prioridad:** Highest
**Descripción:** Como técnico de campo, quiero tomar una foto con la cámara del celular y que quede registrada en el mapa, para capturar en campo.

#### Criterios de Aceptación

##### CA-01 Captura con cámara nativa

**Dado** que el técnico se encuentra en la pantalla del mapa en la app móvil
**Cuando** presione el botón flotante de captura de foto
**Entonces** se abrirá la cámara nativa del dispositivo y, tras capturar la imagen, se registrará un nuevo punto con las coordenadas GPS actuales.

##### CA-02 Marker inmediato en el mapa

**Dado** que el técnico ha capturado una foto exitosamente
**Cuando** regrese a la vista del mapa
**Entonces** aparecerá un nuevo marker en la posición GPS donde se tomó la foto, con la imagen asociada visible en el popup.

##### CA-03 Metadata de la captura

**Dado** que se ha capturado una foto desde el dispositivo móvil
**Cuando** se consulte el detalle del punto creado
**Entonces** se habrán registrado automáticamente: coordenadas GPS, fecha/hora de captura y nombre autogenerado (formato "Punto_YYYYMMDD_HHmmss").

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                      | Componente | Estimación |
| -------- | ----------------------------------------------------------- | ---------- | ---------- |
| GEO-T38  | Implementar servicio de captura de cámara con MediaPicker   | Mobile     | 3h         |
| GEO-T39  | Obtener coordenadas GPS al momento de captura               | Mobile     | 2h         |
| GEO-T40  | Enviar foto y datos a POST /api/puntos desde la app         | Mobile     | 3h         |

---

## 4.4. Épica GEO-E04 — Motor offline-first (SQLite)

---

### GEO-US11: Fotos y puntos se guardan sin internet

**Épica padre:** GEO-E04
**Sprint asignado:** Sprint 04
**Story Points:** 13
**Prioridad:** Highest
**Descripción:** Como técnico de campo, quiero que las fotos y puntos se guarden aunque no tenga internet, para trabajar en zonas sin señal.

#### Criterios de Aceptación

##### CA-01 Persistencia local en SQLite

**Dado** que el dispositivo no tiene conexión a internet
**Cuando** el técnico capture una foto y se cree un punto geográfico
**Entonces** los datos se almacenarán en la base de datos SQLite local del dispositivo y estarán disponibles para consulta inmediata.

##### CA-02 Almacenamiento local de imágenes

**Dado** que el dispositivo se encuentra sin conexión
**Cuando** se capture una fotografía
**Entonces** el archivo de imagen se guardará en el sistema de archivos local del dispositivo y se vinculará al registro en SQLite.

##### CA-03 Registro de operación pendiente

**Dado** que se ha creado un punto offline
**Cuando** se inspeccione la tabla de operaciones pendientes en SQLite
**Entonces** existirá un registro con tipo "Create", estado "Pending" y timestamp de la operación.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                               | Componente | Estimación |
| -------- | -------------------------------------------------------------------- | ---------- | ---------- |
| GEO-T41  | Definir esquema SQLite para PuntoGeografico y Fotografia             | Offline    | 2h         |
| GEO-T42  | Implementar SQLiteDbContext con EF Core para SQLite                  | Offline    | 3h         |
| GEO-T43  | Crear tabla OperacionPendiente en SQLite                             | Offline    | 2h         |
| GEO-T44  | Implementar repositorio local IPuntoRepository (SQLite)              | Offline    | 3h         |
| GEO-T45  | Implementar almacenamiento local de archivos de imagen               | Offline    | 2h         |
| GEO-T46  | Crear servicio de detección de conectividad (IConnectivity)          | Offline    | 2h         |
| GEO-T47  | Implementar patrón Strategy para resolver repositorio online/offline | Offline    | 3h         |
| GEO-T48  | Registrar operaciones CRUD como pendientes al operar offline         | Offline    | 2h         |
| GEO-T49  | Crear migrations de SQLite para esquema local                        | Offline    | 1h         |
| GEO-T50  | Escribir tests unitarios para repositorio local                      | Testing    | 3h         |

---

### GEO-US12: Ver cuántas operaciones pendientes de sincronizar

**Épica padre:** GEO-E04
**Sprint asignado:** Sprint 04
**Story Points:** 5
**Prioridad:** High
**Descripción:** Como técnico de campo, quiero ver cuántas operaciones tengo pendientes de sincronizar, para saber el estado de mis datos.

#### Criterios de Aceptación

##### CA-01 Badge de operaciones pendientes

**Dado** que existen operaciones pendientes de sincronización en SQLite
**Cuando** el técnico visualice la pantalla principal de la app
**Entonces** se mostrará un badge numérico en el botón de sincronización indicando la cantidad de operaciones pendientes.

##### CA-02 Actualización dinámica del contador

**Dado** que el técnico crea un nuevo punto estando offline
**Cuando** se registre la operación pendiente
**Entonces** el badge se actualizará incrementándose en 1 sin necesidad de recargar la pantalla.

##### CA-03 Contador en cero cuando no hay pendientes

**Dado** que todas las operaciones han sido sincronizadas o no existen pendientes
**Cuando** se visualice la pantalla principal
**Entonces** el badge no se mostrará o indicará "0 pendientes".

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                         | Componente | Estimación |
| -------- | -------------------------------------------------------------- | ---------- | ---------- |
| GEO-T51  | Crear servicio IOperacionPendienteService con método Count()   | Offline    | 2h         |
| GEO-T52  | Implementar componente BadgeSincronizacion con MudBadge        | Mobile     | 2h         |
| GEO-T53  | Suscribir componente a eventos de cambio de operaciones        | Mobile     | 2h         |

---

### GEO-US13: App funciona igual offline y online sin diferencia perceptible

**Épica padre:** GEO-E04
**Sprint asignado:** Sprint 04
**Story Points:** 5
**Prioridad:** High
**Descripción:** Como técnico de campo, quiero que la app funcione igual offline y online sin que yo note diferencia, para concentrarme en el trabajo.

#### Criterios de Aceptación

##### CA-01 Transición transparente a modo offline

**Dado** que el técnico está utilizando la app con conexión
**Cuando** se pierda la conexión a internet
**Entonces** la app continuará funcionando sin interrupciones, errores ni mensajes bloqueantes, utilizando los datos locales.

##### CA-02 Misma UX en ambos modos

**Dado** que el dispositivo no tiene conexión a internet
**Cuando** el técnico navegue por el mapa, vea puntos, cree o edite registros
**Entonces** la experiencia de usuario será idéntica a la del modo online (mismos componentes, tiempos de respuesta similares).

##### CA-03 Indicador de modo de operación

**Dado** que la app detecta el estado de conectividad
**Cuando** el técnico observe la barra de estado o el encabezado de la app
**Entonces** se mostrará un icono sutil indicando si se opera en modo online (verde) u offline (gris), sin interrumpir el flujo de trabajo.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                         | Componente | Estimación |
| -------- | -------------------------------------------------------------- | ---------- | ---------- |
| GEO-T54  | Implementar interceptor HTTP para manejo de fallo de red       | Mobile     | 3h         |
| GEO-T55  | Crear componente IndicadorConectividad en barra superior       | Mobile     | 2h         |
| GEO-T56  | Integrar caché local para consultas frecuentes (puntos, fotos) | Offline    | 3h         |

---

## 4.5. Épica GEO-E05 — Motor de sincronización

---

### GEO-US14: Datos offline se sincronizan automáticamente al recuperar la red

**Épica padre:** GEO-E05
**Sprint asignado:** Sprint 05
**Story Points:** 13
**Prioridad:** Highest
**Descripción:** Como técnico de campo, quiero que los datos capturados offline se sincronicen automáticamente al recuperar la red, para no hacer nada manual.

#### Criterios de Aceptación

##### CA-01 Sincronización automática al detectar conectividad

**Dado** que existen operaciones pendientes en SQLite y el dispositivo recupera conexión
**Cuando** el servicio de conectividad detecte que la red está disponible
**Entonces** se iniciará automáticamente el proceso de sincronización, enviando las operaciones pendientes a la API en orden cronológico.

##### CA-02 Procesamiento exitoso de operaciones

**Dado** que la sincronización automática se ha iniciado
**Cuando** una operación pendiente se envíe al servidor y se reciba respuesta HTTP 200/201
**Entonces** la operación se marcará como "Completed" en SQLite y se eliminará de la cola de pendientes.

##### CA-03 Manejo de errores parciales

**Dado** que durante la sincronización una operación falla (HTTP 4xx/5xx)
**Cuando** se procese el error
**Entonces** la operación fallida se marcará como "Failed" con el mensaje de error, y las operaciones restantes continuarán procesándose sin interrumpirse.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                           | Componente | Estimación |
| -------- | ---------------------------------------------------------------- | ---------- | ---------- |
| GEO-T57  | Implementar ISyncEngine con cola de procesamiento FIFO           | Sync       | 4h         |
| GEO-T58  | Crear SyncBackgroundService como HostedService                   | Sync       | 3h         |
| GEO-T59  | Implementar listener de cambio de conectividad                   | Sync       | 2h         |
| GEO-T60  | Procesar operaciones Create (enviar punto + foto a API)          | Sync       | 3h         |
| GEO-T61  | Procesar operaciones Update (enviar PUT a API)                   | Sync       | 2h         |
| GEO-T62  | Procesar operaciones Delete (enviar DELETE a API)                | Sync       | 2h         |
| GEO-T63  | Implementar manejo de errores y retry con backoff exponencial    | Sync       | 3h         |
| GEO-T64  | Actualizar estado de operaciones pendientes tras sync            | Sync       | 2h         |

---

### GEO-US15: Forzar sincronización manualmente

**Épica padre:** GEO-E05
**Sprint asignado:** Sprint 05
**Story Points:** 5
**Prioridad:** High
**Descripción:** Como técnico de campo, quiero forzar la sincronización manualmente, para controlar cuándo subo los datos.

#### Criterios de Aceptación

##### CA-01 Botón de sincronización manual

**Dado** que existen operaciones pendientes de sincronización
**Cuando** el técnico presione el botón "Sincronizar ahora"
**Entonces** se iniciará inmediatamente el proceso de sincronización y se mostrará un indicador de progreso (spinner).

##### CA-02 Feedback de resultado

**Dado** que la sincronización manual ha finalizado
**Cuando** se complete el procesamiento de todas las operaciones
**Entonces** se mostrará un Snackbar con el resultado: "X operaciones sincronizadas exitosamente" o "X exitosas, Y fallidas".

##### CA-03 Botón deshabilitado sin pendientes

**Dado** que no existen operaciones pendientes de sincronización
**Cuando** el técnico visualice el botón de sincronización
**Entonces** el botón estará deshabilitado con un tooltip indicando "No hay operaciones pendientes".

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                       | Componente | Estimación |
| -------- | ------------------------------------------------------------ | ---------- | ---------- |
| GEO-T65  | Exponer método ManualSync() en ISyncEngine                   | Sync       | 2h         |
| GEO-T66  | Implementar componente BotonSincronizar con progreso         | Mobile     | 2h         |
| GEO-T67  | Mostrar resultado de sincronización con MudSnackbar          | Mobile     | 2h         |

---

### GEO-US16: Ver historial de sincronización con resultado de cada operación

**Épica padre:** GEO-E05
**Sprint asignado:** Sprint 05
**Story Points:** 5
**Prioridad:** Medium
**Descripción:** Como técnico de campo, quiero ver el historial de sincronización con el resultado de cada operación, para diagnosticar problemas.

#### Criterios de Aceptación

##### CA-01 Lista de historial de sincronización

**Dado** que se han ejecutado procesos de sincronización (automáticos o manuales)
**Cuando** el técnico navegue a la pantalla de historial de sincronización
**Entonces** se mostrará una lista cronológica inversa con cada operación procesada, indicando: tipo (Create/Update/Delete), entidad afectada, fecha/hora, resultado (Completed/Failed) y mensaje de error si aplica.

##### CA-02 Filtrado por estado

**Dado** que el historial contiene operaciones con distintos estados
**Cuando** el técnico seleccione el filtro "Solo fallidas"
**Entonces** la lista mostrará únicamente las operaciones con estado "Failed".

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                      | Componente | Estimación |
| -------- | ----------------------------------------------------------- | ---------- | ---------- |
| GEO-T68  | Crear tabla HistorialSincronizacion en SQLite               | Offline    | 2h         |
| GEO-T69  | Registrar resultado de cada operación sincronizada          | Sync       | 2h         |
| GEO-T70  | Implementar pantalla HistorialSync con filtros              | Mobile     | 3h         |

---

### GEO-US17: Resolver conflictos automáticamente por Last-Write-Wins

**Épica padre:** GEO-E05
**Sprint asignado:** Sprint 05
**Story Points:** 8
**Prioridad:** High
**Descripción:** Como sistema, quiero resolver conflictos automáticamente por Last-Write-Wins, para no interrumpir al usuario con decisiones técnicas.

#### Criterios de Aceptación

##### CA-01 Detección de conflicto

**Dado** que un punto fue modificado en el servidor mientras existía una operación Update pendiente para el mismo punto en el dispositivo
**Cuando** se intente sincronizar la operación
**Entonces** el sistema detectará el conflicto comparando los timestamps de última modificación (campo UpdatedAt).

##### CA-02 Resolución Last-Write-Wins

**Dado** que se ha detectado un conflicto entre la versión local y la del servidor
**Cuando** el motor de sincronización procese la resolución
**Entonces** prevalecerá la versión con el timestamp más reciente y se descartará la otra, registrando el conflicto en el historial.

##### CA-03 Registro de conflicto resuelto

**Dado** que un conflicto ha sido resuelto automáticamente
**Cuando** se consulte el historial de sincronización
**Entonces** existirá un registro con tipo "Conflict", indicando la entidad afectada, la estrategia aplicada (LWW) y cuál versión prevaleció (local o remota).

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                         | Componente | Estimación |
| -------- | -------------------------------------------------------------- | ---------- | ---------- |
| GEO-T71  | Agregar campo UpdatedAt (timestamp) a entidades de dominio     | Modelo     | 2h         |
| GEO-T72  | Implementar IConflictResolver con estrategia LWW               | Sync       | 3h         |
| GEO-T73  | Integrar detección de conflictos en flujo de sync              | Sync       | 3h         |
| GEO-T74  | Registrar conflictos resueltos en HistorialSincronizacion      | Sync       | 2h         |

---

## 4.6. Épica GEO-E06 — Calidad, UX y deploy productivo

---

### GEO-US18: Pipeline CI/CD que compile, testee y genere APK en cada push

**Épica padre:** GEO-E06
**Sprint asignado:** Sprint 06
**Story Points:** 8
**Prioridad:** High
**Descripción:** Como desarrollador, quiero un pipeline CI/CD que compile, testee y genere APK en cada push, para garantizar calidad continua.

#### Criterios de Aceptación

##### CA-01 Pipeline ejecutado en cada push

**Dado** que se realiza un push a la rama `main` o a una rama de pull request
**Cuando** el pipeline de CI se dispare
**Entonces** ejecutará en orden: restore, build, test (xUnit) y publicará el resultado como check del commit.

##### CA-02 Generación de APK como artefacto

**Dado** que la etapa de build ha sido exitosa
**Cuando** el pipeline alcance la etapa de publicación
**Entonces** se generará el archivo APK firmado y se publicará como artefacto descargable del pipeline.

##### CA-03 Fallo del pipeline si tests fallan

**Dado** que uno o más tests unitarios o de integración fallan
**Cuando** el pipeline complete la etapa de test
**Entonces** el pipeline se marcará como fallido y no se generará el artefacto APK.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                           | Componente | Estimación |
| -------- | ---------------------------------------------------------------- | ---------- | ---------- |
| GEO-T75  | Crear workflow GitHub Actions para build y test                  | DevOps     | 3h         |
| GEO-T76  | Configurar etapa de build MAUI Android con firma                 | DevOps     | 3h         |
| GEO-T77  | Configurar publicación de APK como artefacto                     | DevOps     | 2h         |
| GEO-T78  | Agregar badge de estado del pipeline al README                   | DevOps     | 1h         |
| GEO-T79  | Configurar notificación de fallo por email/Slack                 | DevOps     | 2h         |

---

### GEO-US19: Tests de integración del motor de sync con cobertura >= 80%

**Épica padre:** GEO-E06
**Sprint asignado:** Sprint 06
**Story Points:** 8
**Prioridad:** Highest
**Descripción:** Como desarrollador, quiero tests de integración del motor de sync con cobertura >= 80%, para garantizar su confiabilidad.

#### Criterios de Aceptación

##### CA-01 Suite de tests de integración del motor de sync

**Dado** que existe una suite de tests de integración para ISyncEngine
**Cuando** se ejecuten con `dotnet test`
**Entonces** se validarán los escenarios: sync exitosa, sync con error parcial, detección de conflicto, resolución LWW, retry con backoff y procesamiento FIFO.

##### CA-02 Cobertura mínima del 80%

**Dado** que se ejecutan los tests con reporte de cobertura
**Cuando** se analice el reporte generado por Coverlet
**Entonces** la cobertura de líneas del namespace `GeoFoto.Sync` será igual o superior al 80%.

##### CA-03 Tests ejecutados en pipeline CI

**Dado** que los tests de integración están incluidos en el proyecto de tests
**Cuando** el pipeline CI ejecute la etapa de test
**Entonces** los tests de integración del motor de sync se ejecutarán automáticamente y cualquier fallo bloqueará el pipeline.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                            | Componente | Estimación |
| -------- | ----------------------------------------------------------------- | ---------- | ---------- |
| GEO-T80  | Crear proyecto GeoFoto.Tests.Integration                          | Testing    | 2h         |
| GEO-T81  | Implementar tests de SyncEngine (happy path y errores)            | Testing    | 4h         |
| GEO-T82  | Implementar tests de ConflictResolver (LWW)                       | Testing    | 3h         |
| GEO-T83  | Configurar Coverlet para reporte de cobertura                     | Testing    | 2h         |
| GEO-T84  | Integrar reporte de cobertura en pipeline CI                      | DevOps     | 2h         |

---

### GEO-US20: Mapa y app responden fluidamente con 100+ puntos

**Épica padre:** GEO-E06
**Sprint asignado:** Sprint 06
**Story Points:** 5
**Prioridad:** Medium
**Descripción:** Como técnico de campo, quiero que el mapa y la app respondan fluidamente con 100+ puntos, para trabajar cómodamente en proyectos grandes.

#### Criterios de Aceptación

##### CA-01 Renderización de 100 markers sin degradación

**Dado** que la base de datos local contiene 100 o más puntos geográficos
**Cuando** se cargue la vista del mapa en la app móvil
**Entonces** todos los markers se renderizarán en menos de 3 segundos y las interacciones de pan/zoom mantendrán al menos 30 FPS.

##### CA-02 Clusterización de markers

**Dado** que el mapa muestra más de 50 puntos en una misma región visual
**Cuando** el nivel de zoom sea bajo (vista alejada)
**Entonces** los markers cercanos se agruparán en clusters numéricos que se desagregarán al hacer zoom in.

##### CA-03 Paginación de carga de datos

**Dado** que existen más de 100 puntos en la base de datos
**Cuando** la app consulte los puntos para el mapa
**Entonces** la carga se realizará de forma paginada o por bounding box visible, evitando cargar todos los registros en memoria simultáneamente.

#### Tareas Técnicas Hijas

| ID Jira  | Título                                                          | Componente   | Estimación |
| -------- | --------------------------------------------------------------- | ------------ | ---------- |
| GEO-T85  | Integrar plugin Leaflet.markercluster para agrupación           | Web / Mobile | 3h         |
| GEO-T86  | Implementar consulta por bounding box en API y repositorio      | API          | 3h         |
| GEO-T87  | Optimizar carga de thumbnails con lazy loading                  | Mobile       | 2h         |
| GEO-T88  | Crear test de rendimiento con 200 puntos sintéticos             | Testing      | 3h         |

---

## 4.7. Épica GEO-E07 — UX Avanzado Mobile + Web

---

### GEO-US20b: Centrar el mapa en mi posición actual tocando un botón

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 07
**Story Points:** 5
**Prioridad:** Must
**Descripción:** Como técnico de campo, quiero poder centrar el mapa en mi posición actual tocando un botón, para orientarme rápidamente en terreno.

#### Criterios de Aceptación

##### CA-01 Centrado rápido con setView

**Dado** que el técnico se encuentra en la pantalla del mapa con permiso GPS concedido
**Cuando** toca el FAB de GPS
**Entonces** el mapa se centra con setView(lat, lng, 15) en menos de 2 segundos.

##### CA-02 Permiso GPS denegado

**Dado** que el permiso GPS está denegado en el sistema
**Cuando** el técnico toca el FAB de GPS
**Entonces** se muestra un dialog de solicitud de permiso antes de intentar centrar el mapa.

##### CA-03 Timeout de GPS

**Dado** que el técnico toca el FAB de GPS y el GPS no responde
**Cuando** transcurren más de 10 segundos sin obtener ubicación
**Entonces** se muestra un MudSnackbar con Warning "No se pudo obtener ubicación".

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                        | Componente | Estimación |
| --------- | ------------------------------------------------------------- | ---------- | ---------- |
| GEO-T89   | Agregar FAB GPS en Mapa.razor con lógica de centrado          | Shared     | 2h         |
| GEO-T90   | Implementar timeout 10s en GetCurrentLocationAsync            | Mobile     | 2h         |
| GEO-T91   | Manejar ESC-02 (mapa no disponible) con div de error+reintentar | Shared   | 2h         |
| GEO-T92   | Manejar ESC-03 (permisos GPS) con los 3 casos posibles        | Mobile     | 3h         |

---

### GEO-US21: Ver mi posición actual como un punto en el mapa

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 07
**Story Points:** 5
**Prioridad:** Must
**Descripción:** Como técnico de campo, quiero ver mi posición actual como un punto en el mapa para saber dónde estoy antes de agregar una foto.

#### Criterios de Aceptación

##### CA-01 Marcador de posición propia

**Dado** que el GPS está activo y el permiso fue concedido
**Cuando** el técnico está en la pantalla del mapa
**Entonces** un marcador de posición propia (círculo azul pulsante) aparece y se actualiza en tiempo real.

##### CA-02 Diferenciación visual del marcador de posición

**Dado** que el mapa muestra markers de fotos y el marcador de posición propia
**Cuando** el técnico observa el mapa
**Entonces** el marcador de posición propia usa ícono diferente (círculo azul pulsante), no confundible con markers de fotos.

##### CA-03 Pérdida de GPS

**Dado** que el marcador de posición está visible
**Cuando** se pierde la señal GPS
**Entonces** el marcador de posición desaparece y se muestra un MudSnackbar warning.

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                           | Componente | Estimación |
| --------- | ---------------------------------------------------------------- | ---------- | ---------- |
| GEO-T93   | Agregar updateUserPosition(lat,lng) en leaflet-interop.js        | Shared     | 2h         |
| GEO-T94   | Agregar clearUserPosition() en leaflet-interop.js                | Shared     | 1h         |
| GEO-T95   | Iniciar polling de posición cada 5s en Mapa.razor                | Shared     | 2h         |

---

### GEO-US22: Visualizar y ajustar el radio del marker actual

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 07
**Story Points:** 8
**Prioridad:** Must
**Descripción:** Como técnico de campo, quiero visualizar y ajustar el radio del marker actual para decidir si una nueva foto va al marker existente o crea uno nuevo.

#### Criterios de Aceptación

##### CA-01 Círculo semi-transparente en el mapa

**Dado** que el técnico toca un marker
**Cuando** se abre el popup
**Entonces** se muestra un círculo semi-transparente en el mapa representando el radio de agrupación actual (default 50 m).

##### CA-02 Slider para ajustar radio

**Dado** que el popup del marker está abierto
**Cuando** el técnico mueve el slider (rango 10 m – 500 m)
**Entonces** el círculo se actualiza visualmente y el cambio persiste en Preferences y SQLite.

##### CA-03 Radio global

**Dado** que el técnico modifica el radio desde un marker
**Cuando** se agrega una nueva foto cerca de cualquier marker
**Entonces** el radio configurable se aplica globalmente a todos los markers para decisión de agrupación.

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                                | Componente | Estimación |
| --------- | --------------------------------------------------------------------- | ---------- | ---------- |
| GEO-T96   | Agregar showMarkerRadius/hideMarkerRadius/updateMarkerRadius en JS    | Shared     | 3h         |
| GEO-T97   | Agregar MudSlider (10-500m) en MarkerPopup.razor                      | Shared     | 2h         |
| GEO-T98   | Implementar UpdatePuntoRadioAsync en LocalDbService                   | Mobile     | 2h         |
| GEO-T99   | Persistir radio en IPreferencesService                                | Mobile     | 1h         |

---

### GEO-US23: Popup de marker con carrusel de fotos, título y descripción

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 07
**Story Points:** 13
**Prioridad:** Must
**Descripción:** Como técnico de campo, quiero tocar un marker para abrir un diálogo con el carrusel de fotos, título y descripción del punto, y poder ampliar cada foto con su propia descripción.

#### Criterios de Aceptación

##### CA-01 MudDialog con carrusel y campos editables

**Dado** que el técnico toca un marker
**Cuando** se abre el popup
**Entonces** aparece un MudDialog con: título editable, descripción editable del punto y FotoCarousel con todas las fotos.

##### CA-02 Ampliar foto en fullscreen

**Dado** que el popup está abierto y muestra fotos en el carrusel
**Cuando** el técnico toca una foto
**Entonces** se amplía en fullscreen usando MudOverlay.

##### CA-03 Descripción por foto en fullscreen

**Dado** que la foto está en modo fullscreen
**Cuando** el técnico edita el campo de descripción
**Entonces** puede agregar o editar una descripción individual para esa foto.

##### CA-04 Persistencia de cambios

**Dado** que el técnico modifica título, descripción del punto o comentarios de fotos
**Cuando** pierde el foco del campo (on-blur)
**Entonces** los cambios persisten en SQLite y se encolan en SyncQueue para sincronización.

##### CA-05 Sin fotos: mensaje informativo

**Dado** que el punto no tiene fotos asociadas
**Cuando** se abre el popup
**Entonces** se muestra el mensaje "Este punto no tiene fotos — usá el botón de cámara para agregar la primera."

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                                | Componente | Estimación |
| --------- | --------------------------------------------------------------------- | ---------- | ---------- |
| GEO-T100  | Crear/actualizar MarkerPopup.razor con título y descripción editables | Shared     | 3h         |
| GEO-T101  | Crear/actualizar FotoCarousel.razor con prev/next y botón ✕          | Shared     | 3h         |
| GEO-T102  | Crear FotoViewer.razor (fullscreen MudOverlay + comentario)           | Shared     | 2h         |
| GEO-T103  | Integrar OnMarkerClick en Mapa.razor con JS Interop                   | Shared     | 2h         |
| GEO-T104  | GuardarTituloPuntoAsync en LocalDbService                             | Mobile     | 1h         |
| GEO-T105  | GuardarDescripcionPuntoAsync en LocalDbService                        | Mobile     | 1h         |
| GEO-T106  | GuardarComentarioFotoAsync en LocalDbService                          | Mobile     | 1h         |

---

### GEO-US24: Trabajar offline y sincronizar automáticamente al recuperar red

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 07
**Story Points:** 8
**Prioridad:** Must
**Descripción:** Como técnico de campo, quiero trabajar sin conexión a internet, agregar fotos a markers y que todo se sincronice automáticamente cuando haya red.

#### Criterios de Aceptación

##### CA-01 Funcionamiento completo offline

**Dado** que el dispositivo no tiene conexión
**Cuando** el técnico crea markers, agrega fotos o edita títulos y descripciones
**Entonces** todas las operaciones funcionan completamente sin errores de red.

##### CA-02 Sync automático al recuperar conexión

**Dado** que existen operaciones Pending en SyncQueue y el dispositivo recupera conexión
**Cuando** ConnectivityService detecta la red
**Entonces** SyncService.PushAsync() se dispara automáticamente y sincroniza toda la cola.

##### CA-03 Badge en AppBar con estado de sync

**Dado** que el técnico observa la AppBar durante distintos estados
**Cuando** hay items pendientes (offline), sincronizando o sincronizado
**Entonces** el badge muestra: número de items pendientes (naranja), spinner (sincronizando), check verde (todo sincronizado), ícono rojo (error).

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                                | Componente | Estimación |
| --------- | --------------------------------------------------------------------- | ---------- | ---------- |
| GEO-T107  | Completar SyncService.PushAsync() con Create/Update/Delete Punto+Foto | Mobile     | 4h         |
| GEO-T108  | Implementar SyncService.PullAsync() con ESC-01 (sin merge)            | Mobile     | 3h         |
| GEO-T109  | Implementar ConflictResolver.cs (UseRemote/UseLocal/AskUser)          | Mobile     | 2h         |
| GEO-T110  | Crear SyncController.GetDelta() en API                                | Api        | 2h         |
| GEO-T111  | Actualizar SyncStatusBadge con 4 estados visuales                    | Shared     | 2h         |

---

### GEO-US25: Quitar fotos desde el carrusel del marker

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 07
**Story Points:** 5
**Prioridad:** Should
**Descripción:** Como técnico de campo, quiero quitar fotos desde el carrusel del marker para corregir capturas incorrectas.

#### Criterios de Aceptación

##### CA-01 Botón ✕ por foto

**Dado** que el carrusel del marker está abierto y tiene fotos
**Cuando** el técnico visualiza cualquier foto
**Entonces** cada foto tiene un botón ✕ visible.

##### CA-02 Confirmación antes de eliminar

**Dado** que el técnico toca el botón ✕ de una foto
**Cuando** se dispara la acción
**Entonces** aparece dialog de confirmación: "¿Eliminar esta foto? Esta acción no se puede deshacer."

##### CA-03 Eliminación con actualización de carrusel

**Dado** que el técnico confirma la eliminación
**Cuando** se procesa la operación
**Entonces** la foto se elimina de SQLite, se marca IsDeleted=true y se encola PendingDelete. El carrusel se actualiza sin cerrar el popup.

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                        | Componente | Estimación |
| --------- | ------------------------------------------------------------- | ---------- | ---------- |
| GEO-T112  | Agregar botón ✕ con confirm dialog en FotoCarousel.razor      | Shared     | 2h         |
| GEO-T113  | Implementar EliminarFotoAsync con IsDeleted+SyncQueue         | Mobile     | 2h         |
| GEO-T114  | Actualizar carrusel reactivamente tras eliminación            | Shared     | 1h         |

---

### GEO-US26: Ampliar fotos desde el carrusel para verlas en detalle

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 07
**Story Points:** 3
**Prioridad:** Should
**Descripción:** Como técnico de campo, quiero ampliar fotos desde el carrusel para verlas en detalle.

#### Criterios de Aceptación

##### CA-01 Visor fullscreen

**Dado** que el carrusel está abierto
**Cuando** el técnico toca una foto
**Entonces** se abre el visor fullscreen con MudOverlay.

##### CA-02 Pinch-to-zoom

**Dado** que la foto está en fullscreen
**Cuando** el técnico hace pinch-to-zoom
**Entonces** puede hacer zoom sobre la imagen.

##### CA-03 Cerrar y volver al carrusel

**Dado** que la foto está en fullscreen
**Cuando** el técnico toca el botón ✕ o fuera de la imagen
**Entonces** vuelve al carrusel en la misma posición.

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                    | Componente | Estimación |
| --------- | --------------------------------------------------------- | ---------- | ---------- |
| GEO-T115  | FotoViewer.razor con MudOverlay + zoom CSS/JS             | Shared     | 2h         |
| GEO-T116  | Mantener índice de carrusel al cerrar fullscreen          | Shared     | 1h         |
| GEO-T117  | Integrar FotoViewer en FotoCarousel.razor                 | Shared     | 1h         |

---

### GEO-US27: Ver el estado de sincronización e iniciarlo manualmente

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 07
**Story Points:** 5
**Prioridad:** Must
**Descripción:** Como técnico de campo, quiero ver el estado de sincronización e iniciarlo manualmente cuando lo necesite.

#### Criterios de Aceptación

##### CA-01 Pantalla de sincronización

**Dado** que el técnico navega a la pantalla Sincronización
**Cuando** se carga la pantalla
**Entonces** se muestra: última sincronización, items pendientes, items fallidos con motivo.

##### CA-02 Sincronizar ahora

**Dado** que la pantalla de sincronización está abierta
**Cuando** el técnico toca "Sincronizar ahora"
**Entonces** se dispara PushAsync + PullAsync manualmente.

##### CA-03 Feedback durante y al terminar sync

**Dado** que la sincronización está en curso
**Cuando** el técnico observa la pantalla
**Entonces** el botón muestra spinner y el badge en AppBar está animado.
**Y** al terminar se actualiza la fecha/hora de última sync.

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                         | Componente | Estimación |
| --------- | -------------------------------------------------------------- | ---------- | ---------- |
| GEO-T118  | Crear/actualizar Sincronizacion.razor con tabla SyncQueue      | Shared     | 2h         |
| GEO-T119  | Botón "Sincronizar ahora" con spinner + PushAsync+PullAsync    | Shared     | 2h         |
| GEO-T120  | Mostrar fecha/hora de última sync en pantalla                  | Shared     | 1h         |

---

### GEO-US28: Ver la lista de todos los markers, buscarlos y editar su carrusel

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 07
**Story Points:** 8
**Prioridad:** Must
**Descripción:** Como técnico de campo, quiero ver la lista de todos los markers, buscarlos y editar su carrusel desde esa pantalla.

#### Criterios de Aceptación

##### CA-01 Lista de markers con datos clave

**Dado** que el técnico accede a la pantalla Lista de markers
**Cuando** se carga la pantalla
**Entonces** se muestra una lista con: nombre, coordenadas, cantidad de fotos, estado de sync (chip color).

##### CA-02 Búsqueda en tiempo real

**Dado** que la lista de markers está visible
**Cuando** el técnico escribe en el campo de búsqueda
**Entonces** la lista filtra por nombre en tiempo real sin recargar.

##### CA-03 Navegar al marker y abrir popup

**Dado** que el técnico toca un ítem de la lista
**Cuando** se ejecuta la acción
**Entonces** el mapa se centra en ese marker Y se abre el popup.

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                         | Componente | Estimación |
| --------- | -------------------------------------------------------------- | ---------- | ---------- |
| GEO-T121  | Crear/actualizar ListaPuntos.razor con MudTable y chips sync   | Shared     | 2h         |
| GEO-T122  | Campo de búsqueda MudTextField con filtrado reactivo           | Shared     | 1h         |
| GEO-T123  | Tap en fila → navegar mapa + abrir popup MarkerPopup           | Shared     | 2h         |
| GEO-T124  | GetAllPuntosAsync ordenado por nombre en LocalDbService        | Mobile     | 1h         |

---

### GEO-US29: Eliminar un marker con todas sus fotos

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 07
**Story Points:** 5
**Prioridad:** Should
**Descripción:** Como técnico de campo, quiero poder eliminar un marker (con todas sus fotos) al seleccionarlo.

#### Criterios de Aceptación

##### CA-01 Botón Eliminar marker en popup

**Dado** que el popup del marker está abierto
**Cuando** el técnico observa las opciones disponibles
**Entonces** hay un botón "Eliminar marker" visible.

##### CA-02 Confirmación con conteo de fotos

**Dado** que el técnico toca "Eliminar marker"
**Cuando** se abre el dialog de confirmación
**Entonces** se muestra: "¿Eliminar este marker y sus N fotos? Esta acción no se puede deshacer."

##### CA-03 Eliminación completa y encolada

**Dado** que el técnico confirma la eliminación
**Cuando** se procesa la operación
**Entonces** se elimina PuntoLocal + FotoLocal[] de SQLite, se quita el marker del mapa, se encola PendingDelete en SyncQueue.

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                         | Componente | Estimación |
| --------- | -------------------------------------------------------------- | ---------- | ---------- |
| GEO-T125  | Botón "Eliminar marker" en MarkerPopup.razor con confirm dialog| Shared     | 2h         |
| GEO-T126  | EliminarPuntoConFotosAsync en LocalDbService                   | Mobile     | 2h         |
| GEO-T127  | Quitar marker del mapa vía leaflet-interop.removeMarker()      | Shared     | 1h         |

---

### GEO-US30: Compartir fotos del carrusel a través de apps del dispositivo

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 08
**Story Points:** 5
**Prioridad:** Could
**Descripción:** Como técnico de campo, quiero compartir una o más fotos del carrusel a través de apps del dispositivo (WhatsApp, email, etc.).

#### Criterios de Aceptación

##### CA-01 Botón Compartir por foto

**Dado** que el carrusel está abierto
**Cuando** el técnico observa las opciones por foto
**Entonces** cada foto tiene un botón "Compartir".

##### CA-02 Share API nativa

**Dado** que el técnico toca "Compartir" en una foto
**Cuando** se ejecuta la acción
**Entonces** se invoca la Share API nativa de Android con la foto seleccionada.

##### CA-03 Share no disponible

**Dado** que el sistema de compartir no está disponible en el dispositivo
**Cuando** el técnico toca "Compartir"
**Entonces** se muestra snackbar info: "Función no disponible en este dispositivo."

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                         | Componente | Estimación |
| --------- | -------------------------------------------------------------- | ---------- | ---------- |
| GEO-T128  | Agregar botón "Compartir" en FotoCarousel.razor                | Shared     | 1h         |
| GEO-T129  | Implementar MauiShareService con Share.RequestAsync()          | Mobile     | 2h         |
| GEO-T130  | Manejar caso Share no disponible con snackbar info             | Shared     | 1h         |

---

### GEO-US31: En la web, la misma experiencia de mapa que en Android

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 08
**Story Points:** 5
**Prioridad:** Must
**Descripción:** Como supervisor, quiero en la web la misma experiencia de mapa que en Android: centrar en mi posición, ver mi ubicación, gestionar markers, carrusel, editar títulos y descripciones, quitar fotos, ampliar fotos, ver lista de markers y eliminar markers. Los permisos de ubicación se solicitan al navegador (Geolocation API del browser).

#### Criterios de Aceptación

##### CA-01 Centrado con Geolocation API del browser

**Dado** que el supervisor está en la web y el browser tiene permiso de geolocation
**Cuando** toca el botón de centrar mapa
**Entonces** se usa navigator.geolocation del browser para centrar el mapa.

##### CA-02 Permiso de browser denegado

**Dado** que el browser deniega la geolocation
**Cuando** el supervisor intenta centrar el mapa
**Entonces** se muestra: "Permiso de ubicación denegado en el navegador. Habilitalo desde la configuración del sitio."

##### CA-03 Paridad funcional con Android

**Dado** que el supervisor usa la web
**Cuando** interactúa con markers, carrusel y lista
**Entonces** toda la funcionalidad de marker popup (título, descripción, carrusel, ampliar, quitar, eliminar marker) es idéntica a Android.

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                              | Componente | Estimación |
| --------- | ------------------------------------------------------------------- | ---------- | ---------- |
| GEO-T131  | Agregar navigator.geolocation en leaflet-interop.js para web        | Shared     | 2h         |
| GEO-T132  | Manejar permiso browser denegado con mensaje informativo            | Shared     | 1h         |
| GEO-T133  | Verificar que todos los componentes Shared funcionan en GeoFoto.Web | Web        | 2h         |

---

### GEO-US32: Descargar localmente todas las fotos de un marker en un zip

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 08
**Story Points:** 5
**Prioridad:** Must
**Descripción:** Como supervisor, quiero descargar localmente todas las fotos de un marker en un zip.

#### Criterios de Aceptación

##### CA-01 Botón Descargar fotos en popup web

**Dado** que el popup del marker está abierto en la web
**Cuando** el supervisor observa las opciones
**Entonces** hay un botón "Descargar fotos" visible.

##### CA-02 Descarga del zip con nombres descriptivos

**Dado** que el supervisor toca "Descargar fotos"
**Cuando** se procesa la operación
**Entonces** se genera un archivo .zip con todas las fotos nombradas como {nombrePunto}_{n}.jpg y se descarga al navegador.

##### CA-03 Botón deshabilitado sin fotos

**Dado** que el punto no tiene fotos asociadas
**Cuando** el popup está abierto en la web
**Entonces** el botón "Descargar fotos" está deshabilitado.

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                              | Componente | Estimación |
| --------- | ------------------------------------------------------------------- | ---------- | ---------- |
| GEO-T134  | Crear endpoint GET /api/puntos/{id}/fotos/download (zip)            | Api        | 3h         |
| GEO-T135  | Botón "Descargar fotos" en MarkerPopup.razor (IsMobile=false)       | Shared     | 1h         |
| GEO-T136  | Disparar descarga browser vía JS Interop desde Blazor               | Shared     | 1h         |

---

### GEO-US33: Subir fotos al carrusel de un marker existente desde el navegador

**Épica padre:** GEO-E07
**Sprint asignado:** Sprint 08
**Story Points:** 5
**Prioridad:** Must
**Descripción:** Como supervisor, quiero subir fotos al carrusel de un marker existente desde el navegador, aunque no tengan datos GPS.

#### Criterios de Aceptación

##### CA-01 MudFileUpload en popup web

**Dado** que el popup del marker está abierto en la web
**Cuando** el supervisor toca "Agregar foto"
**Entonces** se abre MudFileUpload para seleccionar una foto.

##### CA-02 Foto vinculada al marker por PuntoId

**Dado** que el supervisor sube una foto (con o sin EXIF GPS)
**Cuando** se sube al servidor
**Entonces** la foto se asocia al marker por PuntoId sin mostrar error por falta de coordenadas.

##### CA-03 Carrusel actualizado inmediatamente

**Dado** que la foto se subió exitosamente
**Cuando** se completa la operación
**Entonces** el carrusel se actualiza inmediatamente mostrando la foto recién subida.

##### CA-04 Sin error por falta de EXIF GPS (ESC-04)

**Dado** que la foto no tiene EXIF GPS
**Cuando** se sube desde la web
**Entonces** no se muestra error — la foto queda vinculada al marker por PuntoId.

#### Tareas Técnicas Hijas

| ID Jira   | Título                                                              | Componente | Estimación |
| --------- | ------------------------------------------------------------------- | ---------- | ---------- |
| GEO-T137  | MudFileUpload en MarkerPopup.razor (IsMobile=false) + POST upload   | Shared     | 2h         |
| GEO-T138  | Endpoint POST /api/fotos/upload con puntoId sin requerir EXIF GPS   | Api        | 2h         |
| GEO-T139  | Actualizar carrusel tras subida exitosa (recargar fotos del punto)  | Shared     | 1h         |

---

# 5. Trazabilidad

| Documento                                                          | Relación                                       |
| ------------------------------------------------------------------ | ---------------------------------------------- |
| `06_backlog-tecnico/backlog-tecnico_v1.0.md`                     | Backlog técnico derivado de estas historias     |
| `07_plan-sprint/plan-iteracion_sprint-01_v1.0.md` a `sprint-06`| Asignación de historias a sprints               |
| `02_especificacion_funcional/casos-de-uso/`                      | Casos de uso que originan las historias         |
| `08_calidad_y_pruebas/casos-prueba-referenciales_v1.0.md`       | Casos de prueba derivados de los criterios CA   |
| `05_arquitectura_tecnica/arquitectura-solucion_v1.0.md`          | Arquitectura que sustenta las decisiones        |

---

# 6. Control de Cambios

| Versión | Fecha      | Autor          | Cambios                                                                                                   |
| ------- | ---------- | -------------- | --------------------------------------------------------------------------------------------------------- |
| 1.0     | 2026-04-13 | Equipo Técnico | Creación inicial del backlog de producto                                                                  |
| 1.1     | 2026-04-16 | Equipo Técnico | Agregada épica GEO-E07 con historias GEO-US20b a GEO-US33 (Sprint 07-08). Tareas GEO-T89 a GEO-T139.    |

---

**Fin del documento**
