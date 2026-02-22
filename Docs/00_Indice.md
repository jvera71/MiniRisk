# 📚 MiniRisk — Índice de Documentación de Diseño y Arquitectura

> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Tecnología:** Blazor Server (.NET 10) + SignalR  
> **Tipo de aplicación:** Juego de mesa Risk multijugador en tiempo real  
> **Ámbito:** Uso privado entre amigos (sin publicación en internet)

---

## 🎯 Propósito de este documento

Este documento sirve como **índice maestro** de toda la documentación de diseño y arquitectura del proyecto **MiniRisk**. Cada sección enlaza a un documento independiente que profundiza en el tema correspondiente. El objetivo es proporcionar una visión clara y organizada de todos los aspectos del sistema.

---

## 📑 Índice de Documentos

### 1. [Visión General del Proyecto](./01_Vision_General.md)
- Descripción del juego y objetivos del proyecto
- Alcance funcional (qué se implementa y qué queda fuera)
- Reglas simplificadas del Risk adaptadas al juego digital
- Número de jugadores soportados (2–6)
- Requisitos no funcionales (rendimiento, usabilidad)
- Glosario de términos del dominio

### 2. [Arquitectura General](./02_Arquitectura_General.md)
- Diagrama de arquitectura de alto nivel
- Patrón Blazor Server: ciclo de vida de componentes y circuitos SignalR
- Comunicación en tiempo real con **SignalR Hubs** dedicados al juego
- Gestión de estado en servidor (no hay persistencia en base de datos)
- Estructura de la solución y organización de carpetas
- Flujo de datos entre cliente y servidor
- Diagrama de componentes principales

### 3. [Identificación de Jugadores](./03_Identificacion_Jugadores.md)
- Flujo de entrada: pantalla de bienvenida con solicitud de nombre
- Sin autenticación formal (no hay login, contraseñas ni roles)
- Almacenamiento del nombre del jugador en estado de sesión
- Asignación de color y avatar al jugador
- Validaciones del nombre (unicidad dentro de la partida, longitud)
- Reconexión: qué ocurre si un jugador pierde la conexión

### 4. [Comunicación en Tiempo Real — SignalR](./04_SignalR.md)
- Justificación del uso de SignalR sobre polling o REST
- Diseño del **GameHub**: métodos del servidor y del cliente
- Grupos de SignalR por partida (`JoinGame`, `LeaveGame`)
- Tipos de mensajes y eventos:
  - Actualización del estado de la partida
  - Turno del jugador actual
  - Resultado de ataques y dados
  - Chat entre jugadores
  - Notificaciones del sistema (jugador conectado/desconectado)
- Gestión de reconexión y desconexión
- Serialización de mensajes (JSON)
- Diagrama de secuencia de comunicación

### 5. [Modelo de Dominio](./05_Modelo_Dominio.md)
- Entidades principales:
  - `Game` (partida)
  - `Player` (jugador)
  - `Territory` (territorio)
  - `Continent` (continente)
  - `Army` (ejércitos)
  - `Card` (cartas de territorio)
  - `Dice` (dados)
- Relaciones entre entidades (diagrama de clases)
- Enumeraciones:
  - `GamePhase` (Configuración, Distribución, Refuerzo, Ataque, Fortificación)
  - `TerritoryName`, `ContinentName`
  - `CardType` (Infantería, Caballería, Artillería, Comodín)
- Reglas de negocio del dominio
- Invariantes y validaciones

### 6. [Motor del Juego (Game Engine)](./06_Motor_Juego.md)
- Máquina de estados de la partida (diagrama de estados)
- Fases del turno:
  1. **Refuerzo:** Cálculo y colocación de ejércitos
  2. **Ataque:** Mecánica de combate y dados
  3. **Fortificación:** Movimiento de tropas
- Cálculo de refuerzos (territorios, continentes, cartes canjeadas)
- Mecánica de dados y resolución de combate
- Sistema de cartas de territorio: obtención y canje
- Condiciones de victoria y eliminación de jugadores
- Gestión de turnos y paso de turno
- Reglas de fortificación (camino conectado)

### 7. [Gestión de Estado](./07_Gestion_Estado.md)
- Estado global de la partida (`GameState`)
- Estado por jugador (`PlayerState`)
- Servicio singleton vs scoped: decisiones de diseño
- `GameManager`: orquestador central de partidas
- Concurrencia y thread-safety
- Ciclo de vida del estado:
  - Creación de partida
  - Partida en curso
  - Finalización y limpieza
- Recuperación ante desconexión de jugadores

### 8. [Diseño de la Interfaz de Usuario](./08_Diseno_UI.md)
- Wireframes y mockups de las pantallas principales:
  - Pantalla de bienvenida (ingreso de nombre)
  - Lobby de partidas (crear/unirse)
  - Tablero de juego (mapa del mundo)
  - Panel de información del jugador
  - Panel de ataque (selección de dados)
  - Panel de cartas
  - Chat de la partida
  - Pantalla de victoria/derrota
- Paleta de colores y tipografía
- Diseño responsivo (escritorio prioritario)
- Accesibilidad básica

### 9. [Componentes Blazor](./09_Componentes_Blazor.md)
- Árbol de componentes y jerarquía
- Componentes de página:
  - `Home.razor` — Bienvenida e ingreso de nombre
  - `Lobby.razor` — Lista de partidas y creación
  - `Game.razor` — Tablero de juego principal
- Componentes reutilizables:
  - `WorldMap.razor` — Mapa del mundo interactivo (SVG)
  - `TerritoryView.razor` — Territorio individual
  - `DiceRoller.razor` — Animación y resultado de dados
  - `PlayerPanel.razor` — Información del jugador
  - `CardHand.razor` — Mano de cartas del jugador
  - `ChatBox.razor` — Chat en tiempo real
  - `GameLog.razor` — Registro de eventos de la partida
  - `PhaseIndicator.razor` — Indicador de fase actual
- Comunicación entre componentes (Parameters, EventCallbacks, Cascading Values)
- Render modes y consideraciones de rendimiento

### 10. [Mapa del Mundo — Diseño del Tablero](./10_Mapa_Mundo.md)
- Representación del mapa como SVG interactivo
- Los 42 territorios del Risk clásico organizados en 6 continentes
- Definición de adyacencias entre territorios
- Interacción con territorios (click, hover, selección)
- Visualización de ejércitos por territorio
- Colores de jugadores sobre el mapa
- Animaciones de ataque y movimiento de tropas
- Zoom y desplazamiento del mapa (opcional)

### 11. [Servicios e Inyección de Dependencias](./11_Servicios_DI.md)
- Registro de servicios en `Program.cs`
- Servicios principales:
  - `IGameManager` — Gestión del ciclo de vida de partidas
  - `IGameEngine` — Lógica de reglas del juego
  - `IDiceService` — Generación de tiradas de dados
  - `ICardService` — Gestión de cartas de territorio
  - `IMapService` — Datos del mapa (territorios, adyacencias, continentes)
  - `IPlayerSessionService` — Estado de sesión del jugador actual
- Ciclos de vida: Singleton, Scoped, Transient — justificación por servicio
- Diagrama de dependencias entre servicios

### 12. [Manejo de Errores y Resiliencia](./12_Errores_Resiliencia.md)
- Estrategia de manejo de errores en Blazor Server
- Reconexión automática de circuitos SignalR (componente `ReconnectModal`)
- Qué ocurre cuando un jugador se desconecta:
  - Tiempo de espera para reconexión
  - Turno automático / salto de turno
  - Eliminación por abandono
- Logging y diagnóstico
- Manejo de excepciones no controladas

### 13. [Testing](./13_Testing.md)
- Estrategia de testing por capa:
  - **Unitarios:** Motor del juego, modelo de dominio, servicios
  - **Integración:** Hubs de SignalR, flujos completos de turno
  - **Componentes (bUnit):** Componentes Blazor individuales
- Frameworks: xUnit, bUnit, Moq/NSubstitute
- Escenarios de prueba clave:
  - Cálculo de refuerzos
  - Resolución de combate
  - Canje de cartas
  - Condición de victoria
  - Reconexión de jugador
- Cobertura mínima objetivo

### 14. [Despliegue y Ejecución Local](./14_Despliegue.md)
- Requisitos previos (.NET 10 SDK)
- Instrucciones de compilación y ejecución (`dotnet run`)
- Configuración de `appsettings.json`
- Acceso desde la red local (configuración de URLs/Kestrel)
- Puertos y firewall para jugar entre amigos

---

## 🗂️ Estructura de Carpetas Propuesta

```
MiniRisk/
├── Components/
│   ├── Layout/                  # Layout principal y navegación
│   ├── Pages/                   # Páginas: Home, Lobby, Game
│   └── Shared/                  # Componentes reutilizables del juego
│       ├── WorldMap.razor
│       ├── TerritoryView.razor
│       ├── DiceRoller.razor
│       ├── PlayerPanel.razor
│       ├── CardHand.razor
│       ├── ChatBox.razor
│       ├── GameLog.razor
│       └── PhaseIndicator.razor
├── Hubs/
│   └── GameHub.cs               # Hub de SignalR para comunicación en tiempo real
├── Models/
│   ├── Game.cs
│   ├── Player.cs
│   ├── Territory.cs
│   ├── Continent.cs
│   ├── Card.cs
│   └── Enums/
│       ├── GamePhase.cs
│       ├── CardType.cs
│       └── TerritoryName.cs
├── Services/
│   ├── GameManager.cs           # Orquestador de partidas (Singleton)
│   ├── GameEngine.cs            # Motor de reglas del juego
│   ├── DiceService.cs           # Servicio de dados
│   ├── CardService.cs           # Gestión de cartas
│   ├── MapService.cs            # Datos del mapa y adyacencias
│   └── PlayerSessionService.cs  # Sesión del jugador actual (Scoped)
├── Docs/                        # 📂 Documentación de diseño (este directorio)
│   ├── 00_Indice.md             # ← Estás aquí
│   ├── 01_Vision_General.md
│   ├── 02_Arquitectura_General.md
│   ├── 03_Identificacion_Jugadores.md
│   ├── 04_SignalR.md
│   ├── 05_Modelo_Dominio.md
│   ├── 06_Motor_Juego.md
│   ├── 07_Gestion_Estado.md
│   ├── 08_Diseno_UI.md
│   ├── 09_Componentes_Blazor.md
│   ├── 10_Mapa_Mundo.md
│   ├── 11_Servicios_DI.md
│   ├── 12_Errores_Resiliencia.md
│   ├── 13_Testing.md
│   └── 14_Despliegue.md
├── wwwroot/
│   ├── css/                     # Estilos CSS
│   ├── images/                  # Recursos gráficos
│   └── js/                      # JavaScript interop (si es necesario)
├── Program.cs
├── MiniRisk.csproj
└── appsettings.json
```

---

## 📋 Orden de Desarrollo Recomendado

| Fase | Documentos | Descripción |
|------|-----------|-------------|
| **Fase 1 — Fundamentos** | 01, 02, 03, 11 | Visión, arquitectura base, identificación y servicios |
| **Fase 2 — Dominio y Motor** | 05, 06, 07 | Modelo de dominio, motor del juego, gestión de estado |
| **Fase 3 — Comunicación** | 04 | SignalR Hub y eventos en tiempo real |
| **Fase 4 — Interfaz** | 08, 09, 10 | UI, componentes Blazor y mapa del mundo |
| **Fase 5 — Calidad** | 12, 13 | Manejo de errores, testing |
| **Fase 6 — Ejecución** | 14 | Despliegue y acceso en red local |

---

## 📌 Decisiones Arquitectónicas Clave

| Decisión | Elección | Justificación |
|----------|---------|---------------|
| **Framework** | Blazor Server | Renderizado en servidor, menor complejidad en cliente, ideal para juego en red local |
| **Comunicación** | SignalR (integrado) | Tiempo real bidireccional, ya incluido en Blazor Server |
| **Autenticación** | Ninguna formal | Uso privado entre amigos; solo se solicita nombre al entrar |
| **Base de datos** | Ninguna | Estado en memoria; las partidas son efímeras |
| **Mapa del mundo** | SVG interactivo | Escalable, interactivo vía Blazor, sin dependencias externas |
| **Target Framework** | .NET 10 | Versión actual del proyecto |

---

> **Nota:** Cada documento se desarrollará de forma incremental. Este índice se actualizará conforme se añadan nuevas secciones o se reorganice la documentación.
