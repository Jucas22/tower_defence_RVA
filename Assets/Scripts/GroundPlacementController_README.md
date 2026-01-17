   # GroundPlacementController

## Descripción

`GroundPlacementController` es un script para Unity que permite detectar un plano en el suelo usando Vuforia y colocar un avatar mediante toque en pantalla. Una vez colocado, el avatar puede moverse tocando otros puntos del suelo.

## Características

- ✅ Detección automática de planos usando `PlaneFinderBehaviour` de Vuforia
- ✅ Colocación de avatar mediante toque en pantalla (HitTest)
- ✅ Movimiento del avatar tocando puntos en el suelo
- ✅ Soporte para animaciones básicas (caminar/idle)
- ✅ Auto-descubrimiento del `PlaneFinderBehaviour` en la escena
- ✅ Soporte para mouse en el editor (facilita testing)
- ✅ Fallback a Physics.Raycast si HitTest de Vuforia no está disponible

## Requisitos

- Unity 2020.3 o superior
- Vuforia Engine instalado y configurado
- Un `Plane Finder` configurado en la escena (Vuforia Ground Plane)
- Un prefab de avatar para instanciar

## Instalación y Uso

### 1. Configurar Vuforia Ground Plane

Primero, asegúrate de tener configurado Vuforia en tu escena:

1. Ve a `GameObject` → `Vuforia Engine` → `Ground Plane`
2. Esto creará automáticamente:
   - `ARCamera`
   - `Plane Finder`
   - `PositionalDeviceTracker`

### 2. Añadir el Script

Puedes añadir el script de dos formas:

**Opción A: Añadir a un GameObject vacío**
1. Crear un GameObject vacío: `GameObject` → `Create Empty`
2. Renombrarlo a "GroundPlacementManager" (o el nombre que prefieras)
3. Añadir el componente `GroundPlacementController`

**Opción B: Añadir a la ARCamera**
1. Seleccionar el `ARCamera` en la jerarquía
2. Añadir el componente `GroundPlacementController`

### 3. Configurar el Script

Una vez añadido el componente, verás los siguientes campos en el Inspector:

#### Configuración de Avatar
- **Avatar Prefab**: Arrastra aquí el prefab del avatar que quieres instanciar

#### Configuración de PlaneFinderBehaviour
- **Plane Finder**: Opcional. Si está vacío, el script buscará automáticamente el `PlaneFinderBehaviour` en la escena

#### Configuración de Movimiento
- **Move Speed**: Velocidad de movimiento del avatar (metros/segundo). Por defecto: 2.0
- **Stopping Distance**: Distancia mínima al destino para considerarse llegado. Por defecto: 0.1

#### Configuración de Animaciones
- **Idle Bool Name**: Nombre del parámetro bool del Animator para idle/staying. Por defecto: "isIdle"
- **Walk Bool Name**: Nombre del parámetro bool del Animator para caminar. Por defecto: "isWalking"

### 4. Preparar el Avatar Prefab

Tu prefab de avatar debe:

1. Tener un modelo 3D
2. (Opcional) Tener un componente `Animator` con:
   - Un parámetro bool para idle (por defecto "isIdle")
   - Un parámetro bool para caminar (por defecto "isWalking")

Si tu avatar no tiene Animator o usa nombres diferentes para los parámetros, ajusta los campos de configuración en el Inspector.

### 5. Ejecutar la Aplicación

1. **En el Editor**: 
   - Ejecuta la escena
   - Haz clic con el mouse donde quieras colocar el avatar
   - Haz clic en otros puntos para mover el avatar

2. **En el Dispositivo**:
   - Compila y ejecuta en tu dispositivo AR
   - Apunta la cámara hacia una superficie plana (suelo, mesa, etc.)
   - Cuando Vuforia detecte el plano, toca la pantalla para colocar el avatar
   - Toca otros puntos para mover el avatar

## Cómo Funciona

### Flujo de Detección y Colocación

1. **Inicio**: El script busca automáticamente el `PlaneFinderBehaviour` si no está asignado
2. **Detección de Plano**: Se suscribe a los eventos de `ContentPositioningBehaviour` para detectar cuando Vuforia encuentra un plano
3. **Primer Toque**: Usa el `HitTest` de Vuforia para obtener la posición 3D exacta donde colocar el avatar
4. **Instanciación**: Crea el avatar en la posición detectada y establece el estado de animación idle
5. **Toques Subsecuentes**: Usa Raycast sobre un plano matemático para calcular nuevas posiciones
6. **Movimiento**: Mueve el avatar suavemente hacia el destino con rotación

### Sistema de Animación

El script maneja automáticamente las animaciones si el avatar tiene un `Animator`:

- **Idle**: Se activa cuando el avatar está quieto
- **Walking**: Se activa cuando el avatar se está moviendo

Los nombres de los parámetros son configurables en el Inspector.

## Ejemplo de Configuración Rápida

```
1. GameObject → Vuforia Engine → Ground Plane
2. GameObject → Create Empty → Renombrar a "PlacementManager"
3. Add Component → GroundPlacementController
4. Arrastrar tu prefab de avatar al campo "Avatar Prefab"
5. Play ▶️
```

## Solución de Problemas

### El avatar no se coloca al tocar la pantalla

- Verifica que el `PlaneFinderBehaviour` está en la escena
- Asegúrate de que Vuforia está inicializado correctamente
- En dispositivo, asegúrate de tener permisos de cámara
- Revisa la consola para ver mensajes de debug

### El avatar no se mueve

- Verifica que se haya colocado correctamente primero
- Asegúrate de tocar en la misma superficie/plano
- Revisa el valor de `Move Speed` (debe ser > 0)

### Las animaciones no funcionan

- Verifica que tu avatar tiene un componente `Animator`
- Asegúrate de que los nombres de los parámetros en el Inspector coinciden con los de tu Animator Controller
- Los parámetros deben ser de tipo `Bool`

### El script no encuentra el PlaneFinderBehaviour

- Asegúrate de tener un `Plane Finder` en la escena
- Intenta asignar manualmente el `PlaneFinderBehaviour` en el Inspector

## API y Métodos Principales

### Métodos Públicos

Actualmente no hay métodos públicos expuestos, pero puedes extender el script según tus necesidades.

### Eventos

El script se suscribe a:
- `ContentPositioningBehaviour.OnContentPlaced`: Para detectar cuando se encuentra un plano

## Personalización

Puedes extender el script para:

- Añadir efectos visuales al colocar el avatar
- Implementar restricciones de movimiento (áreas permitidas)
- Añadir feedback visual del destino (un marcador en el suelo)
- Implementar múltiples avatares
- Añadir interacciones adicionales (saltar, rotar, etc.)

## Compatibilidad

- ✅ Unity 2020.3+
- ✅ Unity 2021.x
- ✅ Unity 2022.x
- ✅ Vuforia Engine 10.x
- ✅ iOS
- ✅ Android

## Licencia

Este script forma parte del proyecto Tower Defence RVA.

## Autor

Creado para el proyecto Tower Defence RVA.

## Changelog

### v1.0.0 (2026-01-15)
- ✨ Implementación inicial
- ✨ Detección de planos con Vuforia
- ✨ Colocación de avatar mediante HitTest
- ✨ Movimiento con Raycast
- ✨ Soporte para animaciones
- ✨ Auto-descubrimiento de PlaneFinderBehaviour
