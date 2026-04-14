--
# Plan de Iteración — Sprint 01

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** plan-iteracion_sprint-01_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Información del Sprint

| Campo | Valor |
|-------|-------|
| Sprint | 01 |
| Fase | Fase 1 — Núcleo Online |
| Fecha inicio | 2026-04-20 |
| Fecha fin | 2026-05-03 |
| Duración | 2 semanas |
| Sprint Goal | Infraestructura base funcional: solución .NET 10 creada, BD SQL Server con migraciones aplicadas, layout MudBlazor operativo. Al finalizar el sprint, se puede ejecutar `dotnet run` en Api y Web sin errores. |
| Velocidad planificada | 16 pts |

---

## 2. Épicas y User Stories

| Épica | US | Título | Puntos | Prioridad |
|-------|----|--------|--------|-----------|
| GEO-E01 | GEO-US01 | Estructura de la solución | 5 | Must |
| GEO-E01 | GEO-US02 | Base de datos SQL Server con EF Core | 8 | Must |
| GEO-E01 | GEO-US03 | MudBlazor integrado | 3 | Must |

**Total Story Points:** 16

---

## 3. Objetivo de la Demo

Al cierre del Sprint 01, se podrá demostrar:

1. **Solución compilable:** `dotnet build GeoFoto.sln` sin errores — los 4 proyectos (Api, Web, Shared, Mobile) compilan.
2. **API con Swagger:** `dotnet run --project GeoFoto.Api` inicia y expone Swagger UI en `https://localhost:5001/swagger`.
3. **Base de datos creada:** Las tablas `Puntos` y `Fotos` existen en SQL Server, la migration se aplicó con `dotnet ef database update`.
4. **Layout MudBlazor:** `dotnet run --project GeoFoto.Web` muestra una página con MudAppBar ("GeoFoto"), MudDrawer con navegación lateral, y tema Material Design aplicado.

---

## 4. Descomposición de Tareas

### GEO-US01 — Estructura de la solución (5 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T01 | Crear solución GeoFoto.sln con 4 proyectos (.Api, .Web, .Shared, .Mobile) | DevOps | 2h | - | To Do |
| GEO-T02 | Configurar referencias entre proyectos (Shared referenciado por Web, Mobile, Api) | DevOps | 1h | - | To Do |
| GEO-T03 | Configurar Program.cs de GeoFoto.Api con Swagger y CORS | Api | 2h | - | To Do |
| GEO-T04 | Configurar Program.cs de GeoFoto.Web con InteractiveServer y MudBlazor | Web | 1h | - | To Do |

### GEO-US02 — Base de datos SQL Server con EF Core (8 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T05 | Instalar EF Core y SQL Server provider en GeoFoto.Api | Api | 1h | - | To Do |
| GEO-T06 | Crear GeoFotoDbContext con DbSet<Punto> y DbSet<Foto> | Api | 2h | - | To Do |
| GEO-T07 | Crear migration inicial con tablas Puntos y Fotos | Api | 1h | - | To Do |
| GEO-T08 | Configurar connection string por appsettings.json / appsettings.Development.json | Api | 1h | - | To Do |
| GEO-T09 | Verificar migration apply y seed de datos de prueba | Api | 2h | - | To Do |

### GEO-US03 — MudBlazor integrado (3 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T10 | Instalar MudBlazor NuGet en GeoFoto.Shared | Shared | 1h | - | To Do |
| GEO-T11 | Configurar MudThemeProvider con tema personalizado GeoFoto | Shared | 1h | - | To Do |
| GEO-T12 | Crear layout base con MudAppBar + MudNavMenu + MudDrawer | Shared | 2h | - | To Do |

---

## 5. Criterios de Aceptación del Sprint

### CA-S01 — Solución compilable

```gherkin
Dado que el repositorio contiene GeoFoto.sln con 4 proyectos
Cuando se ejecuta "dotnet build GeoFoto.sln"
Entonces la compilación finaliza sin errores ni warnings bloqueantes
```

### CA-S02 — API con Swagger

```gherkin
Dado que GeoFoto.Api está en ejecución
Cuando se navega a /swagger
Entonces se muestra la interfaz Swagger UI con la documentación de la API
```

### CA-S03 — Base de datos migrada

```gherkin
Dado que se ejecuta "dotnet ef database update" contra SQL Server
Cuando se consulta la base de datos
Entonces existen las tablas Puntos y Fotos con las columnas definidas en el modelo
```

### CA-S04 — Layout MudBlazor visible

```gherkin
Dado que GeoFoto.Web está en ejecución
Cuando se navega al home (/)
Entonces se muestra MudAppBar con título "GeoFoto" y MudDrawer con navegación
```

---

## 6. Dependencias y Riesgos

| # | Riesgo / Dependencia | Probabilidad | Impacto | Mitigación |
|---|---------------------|-------------|---------|-----------|
| 1 | SQL Server no disponible en entorno de desarrollo | Media | Alto | Usar SQL Server LocalDB o contenedor Docker |
| 2 | Versión .NET 10 preview inestable | Baja | Alto | Usar última versión estable de .NET 10 |
| 3 | Conflictos de versiones MudBlazor con .NET 10 | Baja | Medio | Verificar compatibilidad antes de instalar |

---

## 7. Definiciones

| Ceremonia | Fecha tentativa | Duración |
|-----------|----------------|----------|
| Sprint Planning | 2026-04-20 | 2h |
| Daily Standup | Lunes a viernes | 15min |
| Sprint Review | 2026-05-03 | 1h |
| Sprint Retrospective | 2026-05-03 | 45min |

---

## 8. Métricas planificadas

| Métrica | Objetivo |
|---------|----------|
| Velocidad | 16 pts |
| Tareas completadas | 12/12 |
| Cobertura de tests | N/A (no hay lógica de negocio aún) |
| Bugs abiertos al cierre | 0 |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**
