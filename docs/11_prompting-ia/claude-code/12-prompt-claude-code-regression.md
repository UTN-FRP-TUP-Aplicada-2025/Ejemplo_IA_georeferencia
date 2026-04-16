# Prompt Claude Code — Regresión y Cierre Sprint 07/08

**Proyecto:** GeoFoto MAUI Hybrid / Blazor  
**Propósito:** Regresión automatizada, limpieza técnica y cierre de sprint  
**Usar cuando:** Después de completar Sprint 07 o Sprint 08, antes de demo o release  

---

## CONTEXTO

Este prompt se usa al finalizar un sprint para:
1. Verificar que todos los tests pasan (ningún test roto por cambios del sprint)
2. Corregir warnings de compilación acumulados
3. Verificar cobertura de tests contra los CAs definidos en el plan
4. Actualizar documentación si hubo cambios en la implementación
5. Preparar el entorno para la siguiente sesión

---

## FASE 0 — ESTADO DEL REPO

```bash
git status
git log --oneline -10
```

Identificar:
- Archivos modificados sin commit
- Tests nuevos vs tests existentes
- Archivos de docs desactualizados

---

## FASE 1 — BUILD LIMPIO

```bash
# Detener el proceso GeoFoto.Api si está corriendo (PID visible en git status)
# Luego:
dotnet build src/GeoFoto.Shared/GeoFoto.Shared.csproj
dotnet build src/GeoFoto.Api/GeoFoto.Api.csproj
dotnet build src/GeoFoto.Mobile/GeoFoto.Mobile.csproj
dotnet build src/GeoFoto.Web/GeoFoto.Web.csproj
```

**Criterio:** Cada proyecto: **0 errores**. Los warnings de tipo CS8602/CS8604 (nullable) son aceptables si son heredados y no nuevos.

Si hay errores nuevos:
- Error en Razor: verificar que `@inject` no use métodos inexistentes (cambios de interfaz)
- Error en C#: verificar que los DTOs/modelos tienen los campos usados en el código nuevo
- Error de migración: `dotnet ef database update` para sincronizar el schema

---

## FASE 2 — SUITE DE TESTS COMPLETA

```bash
dotnet build src/GeoFoto.Tests/GeoFoto.Tests.csproj "-p:BuildProjectReferences=false"
dotnet test src/GeoFoto.Tests/GeoFoto.Tests.csproj --no-build --logger "trx;LogFileName=test-results.trx"
```

**Criterio:** **0 tests fallidos**. Si hay tests fallidos:

1. Leer el mensaje de error del test
2. Si el test testea lógica correcta y el código cambió: **actualizar el código**, no el test
3. Si el test usa un mock que ya no coincide con la interfaz: actualizar el mock
4. Si el test testea un comportamiento que fue intencionalmente cambiado: actualizar el test y documentar por qué

---

## FASE 3 — VERIFICACIÓN DE COBERTURA DE CAs

Para cada story del sprint que se marcó como DONE, verificar que existe al menos un test por CA:

| Story | CA | Test esperado |
|-------|-----|--------------|
| GEO-US20 | CA-01: FAB visible con GPS | US20_CentrarMapaTests.cs |
| GEO-US21 | CA-01: marker GPS aparece | US21_PosicionPropiaTests.cs |
| GEO-US22 | CA-01: radio default 50m | US22_RadioMarkerTests.cs |
| GEO-US23 | CA-02: tap foto → fullscreen | US23_CarruselTests.cs |
| GEO-US25 | CA-02: dialog confirmación | US25_QuitarFotoTests.cs |
| GEO-US26 | CA-01: visor fullscreen | US26_AmpliarFotoTests.cs |
| GEO-US27 | CA-01: items pendientes | US27_SincronizacionTests.cs |
| GEO-US28 | CA-02: búsqueda filtra | US28_ListaMarkersTests.cs |
| GEO-US29 | CA-01: eliminar con confirm | US29_EliminarMarkerTests.cs |

Si falta un test para un CA documentado, crearlo antes de cerrar el sprint.

---

## FASE 4 — LIMPIEZA DE WARNINGS

Revisar los warnings de compilación del Paso 1. Priorizar:

**Warnings a corregir:**
- `CS0219`: variable declarada y nunca usada → eliminar la variable
- `CS0168`: variable capturada en catch pero nunca usada → usar `_` o loguear
- `RZ10010`: `@onclick` duplicado en componente → usar `<span>` wrapper nativo

**Warnings a ignorar (ya existentes):**
- `CS8602`, `CS8604`: nullable dereference en código no crítico de UI
- `xUnit1031`: blocking task en tests de integración que ya existen

---

## FASE 5 — ACTUALIZAR DOCUMENTACIÓN

Si durante el sprint se implementó algo diferente a lo especificado, actualizar:

```
docs/05_arquitectura_tecnica/modelo-datos-logico_v1.0.md   ← campos nuevos
docs/05_arquitectura_tecnica/api-rest-spec_v1.0.md         ← endpoints nuevos o modificados
docs/07_plan-sprint/plan-iteracion_sprint-0X_v1.0.md       ← tareas completadas
docs/06_backlog-tecnico/backlog-tecnico_v1.0.md            ← status actualizado
```

Marcar en el plan de sprint las tareas como ✅ completadas.

---

## FASE 6 — COMMIT DE CIERRE

```bash
git add -p  # revisar cada cambio antes de agregar
git commit -m "Sprint 07/08: completar stories GEO-US20-US33 + tests + docs"
```

**Convención de commit:** `Sprint XX: <descripción concisa de 50 chars max>`

---

## FASE 7 — PREPARACIÓN PARA SIGUIENTE SPRINT

1. Leer `docs/07_plan-sprint/plan-iteracion_sprint-0X_v1.0.md` del **próximo** sprint
2. Identificar stories de Mayor prioridad (Must) vs menor (Should/Could)  
3. Verificar que los modelos de datos del siguiente sprint ya tienen sus migraciones
4. Verificar que las interfaces de servicio necesarias ya existen en GeoFoto.Shared

Dejar anotado en este prompt (como checklist) qué tareas quedan pendientes para la próxima sesión.

---

## CHECKLIST DE CIERRE SPRINT

```
[ ] dotnet build → 0 errores en todos los proyectos
[ ] dotnet test  → 0 fallidos
[ ] Todos los CAs documentados tienen al menos 1 test
[ ] Warnings de tipo "nunca usada" corregidos
[ ] Docs actualizadas para reflejar implementación real
[ ] git commit realizado con mensaje descriptivo
[ ] Plan del próximo sprint revisado
[ ] Interfaces de servicio del próximo sprint verificadas
```

---

## NOTAS DE EJECUCIÓN

- Si el API (PID en git status) está corriendo, usar `"-p:BuildProjectReferences=false"` para compilar solo los tests
- Los tests de integración que usan SQLite en memoria son seguros de correr siempre
- Los screenshots automáticos (si existen en `scripts/`) se deben capturar en entorno con app corriendo
