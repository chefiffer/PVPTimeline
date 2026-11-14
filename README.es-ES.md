# PvP Timeline

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [한국어](README.ko-KR.md) | [Русский](README.ru-RU.md) | [Português (Brasil)](README.pt-BR.md) | **Español** | [ไทย](README.th-TH.md) | [Tiếng Việt](README.vi-VN.md) | [日本語](README.ja-JP.md) | [Deutsch](README.de-DE.md) | [Français](README.fr-FR.md)

Una línea de tiempo personal y en tiempo real de acciones PvP en FINAL FANTASY XIV.

Muestra en una sola línea el uso de acciones, el lanzamiento, el tiempo de reutilización global (GCD), el bloqueo de animación, tus propias animaciones y la respuesta de los efectos. Así puedes revisar cuándo usaste una acción, cuándo se recibieron sus efectos y qué habilidades intercalaste.

![Vista previa de PvP Timeline](assets/pvp-timeline-preview.gif)

> Esta es una versión de prueba. Las actualizaciones del juego pueden afectar a la observación y la visualización, y aún puede faltar respuesta para determinadas fases. La línea muestra observaciones locales, no una reconstrucción exacta de la detección interna del servidor. No automatiza acciones.

## Cómo funciona

![Observación local y flujo de mensajes](assets/how-pvp-timeline-works.png)

El diagrama separa los mensajes entre cliente y servidor de los tiempos observados localmente. PvP Timeline registra solicitudes de acción aceptadas, estados visibles de lanzamiento y reutilización, bloqueo de animación, reproducción de tus animaciones, mensajes de efectos recibidos e instantáneas de tus estados. Los colores coinciden con la vista predeterminada.

El complemento no envía paquetes de combate adicionales ni puede consultar las decisiones internas del servidor. Una pulsación que no se convierte en solicitud de acción no es observable. La marca de tiempo de un efecto es el momento en que lo recibe el cliente local e incluye las demoras de red, servidor y cliente.

## Funciones

- **Carriles separados:** iconos grandes abajo para hechizos, técnicas de arma y ciertas acciones especiales; iconos pequeños arriba para habilidades, en un máximo de dos filas.
- **Pistas temporales distintas:** GCD, lanzamiento, bloqueo y reproducción de animación se registran por separado.
- **Respuesta de efectos:** un rombo rosa señala daño, curación, aplicación de estado u otra respuesta compatible; los golpes críticos y directos iluminan suavemente el icono original.
- **Efectos diferidos:** las detonaciones automáticas y fases posteriores compatibles se vinculan al uso original o se muestran con iconos translúcidos sin inventar otra pulsación.
- **Tus estados:** historial compacto de mejoras seleccionadas y perjuicios observados.
- **Apariencia configurable:** bloqueo que deja pasar clics, diseño horizontal/vertical, desplazamiento inverso, colores, opacidad, tamaños y escala general.
- **Pausa y revisión:** congela la vista o borra los registros.

La observación se limita a zonas compatibles de **Wolves' Den Pier, Crystalline Conflict, Frontline y Rival Wings**. No se activa en zonas PvE normales; los mapas nuevos requieren actualizar la lista.

## Primeros pasos

### Instalación

Necesitas un entorno compatible con complementos de **Dalamud API 15**. Usa una versión adecuada para el juego y Dalamud actuales.

1. Instala e inicia XIVLauncher con Dalamud activado.
2. Abre **Dalamud Settings → Experimental**.
3. Añade este repositorio:

   ```text
   https://raw.githubusercontent.com/chefiffer/dalamud-plugins/main/repo.json
   ```

4. Busca **PVPTimeline** en el instalador e instálalo.
5. Abre los ajustes con:

   ```text
   /pvptimeline
   ```

6. Entra en Wolves' Den Pier o en un mapa PvP compatible y usa acciones.

### Configuración inicial

Desactiva primero **Combat only** y prueba con un muñeco de entrenamiento en Wolves' Den Pier:

1. Desbloquea la superposición en **Display**, muévela y cambia su tamaño.
2. Ajusta en **Layout** los tamaños de GCD/habilidades, posiciones y grosores.
3. Activa **Locked** para que los clics atraviesen la línea.

Una línea bloqueada no se puede pulsar directamente. Usa `/pvptimeline`; desbloqueada también admite clic derecho.

## Leer la línea de tiempo

### Iconos y tiempos

`0s` es el instante actual y los valores negativos son el pasado. La cuadrícula y las etiquetas permanecen fijas mientras las acciones se desplazan al historial.

El carril inferior no significa que todas sus acciones usen GCD: algunos Limit Break y acciones especiales también aparecen allí. La barra GCD azul solo se dibuja cuando se observa una reutilización compartida real.

| Aspecto predeterminado | Significado |
| --- | --- |
| Barra azul | GCD observado |
| Barra verde | Intervalo de lanzamiento observado localmente |
| Barra amarilla corta | Bloqueo de animación, separado del GCD |
| Barra morada | Reproducción observada de tu animación |
| Rombo rosa lleno | Respuesta directa: daño, curación o estado; en algunos movimientos verificados puede ser confirmación del servidor |
| Rombo rosa vacío | Efecto visual o cambio de indicador; por sí solo no prueba daño o curación |
| Icono translúcido | Lanzamiento sin confirmar o efecto real sin uso manual correspondiente; no implica otra pulsación |
| Brillo del icono | Golpe crítico, directo o crítico directo procedente de mensajes reales |
| Barra gris fina (desactivada por defecto) | Reutilización independiente de la acción, no el GCD |

Todos los colores se pueden cambiar. Una acción multiobjetivo puede dar resultados distintos; el icono combina los indicadores observados. Un brillo crítico directo no significa que todos los objetivos recibieran uno.

Recuperate y Shadowbringer también tienen **barras azules cortas e independientes** en el carril de habilidades. Siguen siendo reutilizaciones independientes, no acciones GCD.

### ¿Por qué falta a veces una barra morada o un rombo?

Las barras moradas dependen de la observación real de tus animaciones. Una observación ausente, asociación dudosa, pausa o sustitución puede hacer que falten o se acorten. Solo se muestra un marcador de sustitución si otra acción sobrescribió de forma confirmada la animación; un final normal no añade un rombo naranja.

Los rombos rosas también exigen respuesta real. Si falta, no puede asociarse de forma fiable o pertenece a una fase no compatible, el complemento no la adivina a partir de la descripción.

**La ausencia de una barra o marcador no demuestra que la acción fallara.** El GCD o el bloqueo no sustituyen la duración de reproducción.

## Leer las filas de estados

Muestran principalmente **estados realmente presentes en tu personaje**, procedentes de ti, un aliado o un enemigo. Que un aliado use una acción no prueba por sí solo que recibieras su efecto.

- El icono inicial avanza con el tiempo y no se duplica en el borde izquierdo antes de llegar.
- Al llegar entra en la cola; si se conoce el tiempo restante, se muestran como máximo dos cifras sin unidad ni cero.
- Los estados que terminan antes se colocan más a la derecha.
- **Un pequeño subrayado rojo identifica un punto final.** Puede aparecer en el área futura antes del fin; no significa que ya haya terminado.
- Tras finalizar, el punto se mueve al pasado y el segmento desaparece al encontrarse con el inicio.

Las fuentes pueden registrarse por separado. Renovaciones, acumulaciones y eliminaciones siguen la lista real de estados; no se inventan acumulaciones según el número de usos.

No se muestran todos los estados. Se filtran mejoras de trabajos revisadas y determinados indicadores de selección, preparación o continuación. No es una base completa; abre una incidencia si quieres añadir otro efecto.

**Solo se muestran estados y cuentas atrás confirmables.** Sin tiempo restante legible no hay cuenta atrás; condiciones como «en combate» aún no se evalúan por separado.

## Ajustes habituales

| Ubicación | Opciones |
| --- | --- |
| Parte superior | Idioma, escala, pausa/reanudación, borrado |
| Display | Activación, clics atravesables, solo combate, ocultar en escenas, estados, observación nativa, orientación, fondos |
| Layout | Tamaños, altura/separación, posiciones y grosores de GCD/habilidades, lanzamiento/animación |
| Tracks | Visibilidad y colores, reutilizaciones cortas, habilidades, autoataques |
| Time | Píxeles por segundo, historial, futuro, cuadrícula y subdivisiones |
| Feedback | Colores de brillo, sustitución de animación, opacidad de efectos |

Introduce valores o usa la rueda sobre el campo. La escala general admite **0,2–2,0×**, incluido el texto.

La interfaz admite chino simplificado/tradicional, inglés, alemán, francés y japonés. Sigue inicialmente el idioma de Dalamud, con selección manual. Los nombres usan recursos multilingües y pueden recurrir al inglés, idioma del cliente o ID. Los nombres de jugadores no se traducen. `ui-localization.txt`, junto a la DLL, controla solo textos de interfaz; edítalo y recarga el complemento.

## Preguntas frecuentes

### No se ve la línea

Comprueba la zona PvP compatible, que el complemento esté activo, **Combat only** y **Hide in cutscene**. Consulta **Diagnostics**. Una actualización puede exigir una corrección de compatibilidad.

### ¿Por qué no aparece cada pulsación repetida?

Se muestra el uso observable, no clics físicos. Las pulsaciones sin solicitud del juego son invisibles y los intentos fallidos no crean una fila de iconos.

### ¿Por qué el daño automático es translúcido?

Puede llegar un efecto tuyo sin registro manual. Una detonación automática causa daño real sin otra pulsación. Las fases vinculadas con fiabilidad actualizan la acción original; otros efectos reales pueden quedar translúcidos.

### ¿Es la hora de impacto del servidor?

No. Es la recepción del cliente e incluye todos los retrasos. La animación tampoco es la instantánea del servidor y el fin del lanzamiento no establece el primer momento posible de movimiento.

### ¿Carga datos o guarda partidas completas?

No automáticamente. Conserva registros recientes limitados en memoria y los reinicia al cambiar de zona o borrar. Los diagnósticos solo se copian al portapapeles al pulsar el botón y pueden incluir ID y tiempos; revísalos antes de compartir.

## Límites y precauciones

- La ventana de asociación es como máximo de **14 segundos**; no se fuerzan respuestas tardías o ambiguas.
- El historial se limita a 256 entradas y 120 segundos; los estados también tienen límites.
- La observación se centra en tu personaje y no reconstruye por completo animaciones del objetivo, proyectiles ni efectos del suelo.
- La observación nativa usa interfaces internas; las actualizaciones pueden romperla o afectar la estabilidad. Desactivarla no elimina necesariamente todos los puntos de observación.
- Es un complemento no oficial sin garantía de seguridad de la cuenta ni ausencia de fallos. Respeta las reglas del juego y la plataforma.

## Informar de un problema

Indica versión, región/versión del juego, idioma, trabajo y acción, activación manual/automática, resultado esperado/real y pasos. Añade captura o GIF, diagnóstico de efectos para asociaciones y registros de Dalamud sin datos privados para problemas de carga. No compartas tokens, contraseñas ni registros personales ajenos.

## Agradecimientos

- [ActionTimelineEx](https://github.com/Ted-Jin-Lab/ActionTimelineEx)
- [ActionTimelineReplacement](https://github.com/Loskh/ActionTimelineReplacement)

Estos proyectos inspiraron la presentación, el bloqueo y la personalización. PvP Timeline no es una versión oficial. Los iconos y materiales pertenecen a sus titulares.

## Licencia

PvP Timeline se distribuye bajo GNU Affero General Public License v3.0 (AGPL-3.0). Consulta [LICENSE](https://github.com/chefiffer/PVPTimeline/blob/main/LICENSE).

Los avisos del proyecto original bajo MIT se conservan en [THIRD-PARTY-NOTICES.txt](https://github.com/chefiffer/PVPTimeline/blob/main/THIRD-PARTY-NOTICES.txt).
