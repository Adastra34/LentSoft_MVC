# Reglas de Flujo de Trabajo y Confirmación

1. **Sincronización de Código:**
   - Todos los cambios se realizan directamente en los archivos de la solución en disco.
   - Cualquier cambio realizado en `LentSoft.Mobile` o `LentSoft.Web` se reflejará automáticamente cuando el usuario abra o inspeccione el proyecto en Visual Studio Community.

2. **Explicación Previa Obligatoria Antes de Modificar Código:**
   - Cuando el usuario pida realizar cambios en el proyecto (especialmente en la aplicación móvil o web), **ANTES de ejecutar cualquier modificación en el código**, se le debe presentar una explicación clara que contenga:
     a) **Descripción de los cambios a realizar:** Detalle de las modificaciones en UI, lógica o archivos.
     b) **Impacto en la Base de Datos:** Una aclaración explícita al final indicando:
        - `⚠️ Modifica la base de datos` (si añade/elimina tablas, campos o altera datos).
        - `✅ NO modifica la base de datos` (si son cambios exclusivamente visuales o de interfaz móvil/web).
