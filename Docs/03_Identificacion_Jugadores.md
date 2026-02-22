# 03 — Identificación de Jugadores

> **Documento:** 03 de 14  
> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Volver al índice:** [00_Indice.md](./00_Indice.md)  
> **Documento anterior:** [02 — Arquitectura General](./02_Arquitectura_General.md)

---

## 1. Principio Fundamental: Sin Autenticación

MiniRisk **no implementa autenticación formal**. No hay:

- ❌ Cuentas de usuario
- ❌ Contraseñas
- ❌ Inicio de sesión (login)
- ❌ Registro de usuario
- ❌ Roles ni permisos
- ❌ Tokens JWT, cookies de autenticación ni claims
- ❌ Proveedores de identidad externos (Google, Microsoft, etc.)
- ❌ Base de datos de usuarios

**Justificación:** La aplicación es de uso privado entre un grupo reducido de amigos que se conocen y confían entre sí. Se ejecuta exclusivamente en red local y no se publica en internet. Añadir autenticación sería una complejidad innecesaria sin beneficio real.

### 1.1 ¿Qué se hace en su lugar?

Se utiliza un mecanismo de **identificación por nombre**, simple y directo:

1. El jugador abre la aplicación en su navegador.
2. Se le muestra una pantalla de bienvenida que le pide su nombre.
3. El jugador escribe su nombre y pulsa "Entrar".
4. El nombre se almacena en el estado de sesión del circuito Blazor (scoped).
5. El jugador accede al lobby y puede crear o unirse a partidas.

No hay verificación de identidad: si alguien escribe "Carlos", el sistema confía en que es Carlos. Esto es aceptable dado el contexto de uso.

---

## 2. Flujo de Entrada del Jugador

### 2.1 Diagrama del Flujo Completo

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│   JUGADOR ABRE EL NAVEGADOR                                            │
│   http://192.168.1.100:5000                                             │
│          │                                                              │
│          ▼                                                              │
│   ┌──────────────────────────────────────────┐                          │
│   │         PANTALLA DE BIENVENIDA           │                          │
│   │            Home.razor (@page "/")        │                          │
│   │                                          │                          │
│   │   ┌──────────────────────────────────┐   │                          │
│   │   │  🎲 MiniRisk                     │   │                          │
│   │   │                                  │   │                          │
│   │   │  ¡Bienvenido! Introduce tu      │   │                          │
│   │   │  nombre para empezar a jugar:    │   │                          │
│   │   │                                  │   │                          │
│   │   │  ┌──────────────────────────┐    │   │                          │
│   │   │  │ Tu nombre...             │    │   │                          │
│   │   │  └──────────────────────────┘    │   │                          │
│   │   │                                  │   │                          │
│   │   │  [      🚀 Entrar      ]         │   │                          │
│   │   │                                  │   │                          │
│   │   │  ⚠️ Nombre ya en uso (oculto)    │   │                          │
│   │   └──────────────────────────────────┘   │                          │
│   └──────────────┬───────────────────────────┘                          │
│                  │                                                      │
│                  ▼                                                      │
│   ┌──────────────────────────────────────────┐                          │
│   │           VALIDACIÓN DEL NOMBRE          │                          │
│   │                                          │                          │
│   │  1. ¿Está vacío o solo espacios?         │── SÍ ──▶ Error:         │
│   │  2. ¿Tiene menos de 2 caracteres?        │          "Nombre        │
│   │  3. ¿Tiene más de 20 caracteres?         │           requerido"    │
│   │  4. ¿Contiene caracteres no permitidos?  │                          │
│   │  5. ¿Ya hay alguien conectado con        │── SÍ ──▶ Error:         │
│   │     ese nombre en alguna partida?        │          "Nombre ya     │
│   │                                          │           en uso"       │
│   └──────────────┬───────────────────────────┘                          │
│                  │ TODO OK                                              │
│                  ▼                                                      │
│   ┌──────────────────────────────────────────┐                          │
│   │     ALMACENAR EN SESIÓN (Scoped)         │                          │
│   │                                          │                          │
│   │  PlayerSessionService.SetPlayer(nombre)  │                          │
│   │                                          │                          │
│   │  → PlayerName = "Carlos"                 │                          │
│   │  → PlayerId = Guid.NewGuid()             │                          │
│   │  → ConnectedAt = DateTime.UtcNow         │                          │
│   │  → IsIdentified = true                   │                          │
│   └──────────────┬───────────────────────────┘                          │
│                  │                                                      │
│                  ▼                                                      │
│   ┌──────────────────────────────────────────┐                          │
│   │         NAVEGACIÓN AL LOBBY              │                          │
│   │                                          │                          │
│   │  NavigationManager.NavigateTo("/lobby")   │                          │
│   └──────────────────────────────────────────┘                          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Diagrama de Secuencia Técnico

```
  Navegador                    Home.razor              PlayerSessionService         GameManager
  ─────────                    ──────────              ────────────────────         ───────────
      │                            │                           │                        │
      │── GET / ──────────────────▶│                           │                        │
      │                            │                           │                        │
      │                            │── IsIdentified? ─────────▶│                        │
      │                            │◀─── false ───────────────│                        │
      │                            │                           │                        │
      │◀── Render formulario ─────│                           │                        │
      │    de nombre               │                           │                        │
      │                            │                           │                        │
      │── Submit "Carlos" ───────▶│                           │                        │
      │                            │                           │                        │
      │                            │── Validar formato ───────▶│                        │
      │                            │◀─── OK ──────────────────│                        │
      │                            │                           │                        │
      │                            │── IsNameTaken("Carlos")? ──────────────────────────▶│
      │                            │◀─── false ─────────────────────────────────────────│
      │                            │                           │                        │
      │                            │── SetPlayer("Carlos") ───▶│                        │
      │                            │                           │ Almacena en sesión:    │
      │                            │                           │ PlayerId = guid        │
      │                            │                           │ PlayerName = "Carlos"  │
      │                            │◀─── OK ──────────────────│                        │
      │                            │                           │                        │
      │◀── NavigateTo("/lobby") ──│                           │                        │
      │                            │                           │                        │
```

### 2.3 Protección de Rutas

Dado que no hay autenticación, se implementa un **guard de identificación** simple: si el jugador intenta acceder al lobby o a una partida sin haber introducido su nombre, se le redirige a la pantalla de bienvenida.

```
  Jugador intenta acceder a /lobby o /game/{id}
              │
              ▼
  ┌─────────────────────────────┐
  │ PlayerSessionService        │
  │ .IsIdentified               │
  └──────────┬──────────────────┘
             │
      ┌──────┴──────┐
      │             │
    true          false
      │             │
      ▼             ▼
  Renderizar    NavigateTo("/")
  la página     (volver a Home)
```

**Implementación en cada página protegida:**

```csharp
// En Lobby.razor y Game.razor
@inject IPlayerSessionService PlayerSession
@inject NavigationManager Navigation

@code {
    protected override void OnInitialized()
    {
        if (!PlayerSession.IsIdentified)
        {
            Navigation.NavigateTo("/");
            return;
        }
    }
}
```

> **Nota:** No se usa `[Authorize]` de ASP.NET Core porque no hay sistema de autenticación. Es un guard manual ligero.

---

## 3. PlayerSessionService: Diseño del Servicio de Sesión

### 3.1 Interfaz

```csharp
public interface IPlayerSessionService
{
    /// <summary>
    /// Indica si el jugador ha introducido su nombre y está identificado.
    /// </summary>
    bool IsIdentified { get; }

    /// <summary>
    /// Identificador único del jugador (generado al identificarse).
    /// </summary>
    string PlayerId { get; }

    /// <summary>
    /// Nombre mostrado del jugador.
    /// </summary>
    string PlayerName { get; }

    /// <summary>
    /// Color asignado al jugador en la partida actual.
    /// </summary>
    PlayerColor? AssignedColor { get; }

    /// <summary>
    /// ID de la partida en la que el jugador está actualmente (null si está en el lobby).
    /// </summary>
    string? CurrentGameId { get; }

    /// <summary>
    /// Momento en que el jugador se identificó.
    /// </summary>
    DateTime ConnectedAt { get; }

    /// <summary>
    /// Establece la identidad del jugador con el nombre proporcionado.
    /// Genera un PlayerId único.
    /// </summary>
    void SetPlayer(string name);

    /// <summary>
    /// Asigna un color al jugador (al unirse a una partida).
    /// </summary>
    void SetColor(PlayerColor color);

    /// <summary>
    /// Registra que el jugador se ha unido a una partida.
    /// </summary>
    void JoinGame(string gameId);

    /// <summary>
    /// Registra que el jugador ha abandonado la partida actual.
    /// </summary>
    void LeaveGame();

    /// <summary>
    /// Limpia toda la información de sesión (vuelve al estado inicial).
    /// </summary>
    void Clear();
}
```

### 3.2 Implementación

```csharp
public class PlayerSessionService : IPlayerSessionService
{
    public bool IsIdentified { get; private set; }
    public string PlayerId { get; private set; } = string.Empty;
    public string PlayerName { get; private set; } = string.Empty;
    public PlayerColor? AssignedColor { get; private set; }
    public string? CurrentGameId { get; private set; }
    public DateTime ConnectedAt { get; private set; }

    public void SetPlayer(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre no puede estar vacío.", nameof(name));

        PlayerId = Guid.NewGuid().ToString();
        PlayerName = name.Trim();
        ConnectedAt = DateTime.UtcNow;
        IsIdentified = true;
    }

    public void SetColor(PlayerColor color)
    {
        AssignedColor = color;
    }

    public void JoinGame(string gameId)
    {
        CurrentGameId = gameId ?? throw new ArgumentNullException(nameof(gameId));
    }

    public void LeaveGame()
    {
        CurrentGameId = null;
        AssignedColor = null;
    }

    public void Clear()
    {
        IsIdentified = false;
        PlayerId = string.Empty;
        PlayerName = string.Empty;
        AssignedColor = null;
        CurrentGameId = null;
    }
}
```

### 3.3 Ciclo de Vida: Scoped

El `PlayerSessionService` se registra como **Scoped** en el contenedor de DI:

```csharp
builder.Services.AddScoped<IPlayerSessionService, PlayerSessionService>();
```

**¿Por qué Scoped?**

| Aspecto | Explicación |
|---------|-------------|
| **Un servicio por circuito** | En Blazor Server, Scoped = una instancia por circuito. Cada pestaña del navegador (cada jugador) tiene su propio `PlayerSessionService`. |
| **Aislamiento** | El nombre de "Carlos" no se mezcla con el de "Ana". Cada circuito tiene su propia instancia. |
| **Duración** | El servicio vive mientras el circuito esté abierto. Si el jugador cierra la pestaña, se destruye. |
| **No Singleton** | Si fuera Singleton, todos los jugadores compartirían el mismo nombre → desastroso. |
| **No Transient** | Si fuera Transient, cada inyección crearía una instancia nueva → el nombre se perdería entre componentes. |

### 3.4 Diagrama de Vida del Servicio

```
  Jugador 1 abre navegador          Jugador 2 abre navegador
         │                                  │
         ▼                                  ▼
  ┌─────────────────┐              ┌─────────────────┐
  │ Circuito 1      │              │ Circuito 2      │
  │                 │              │                 │
  │ PlayerSession   │              │ PlayerSession   │
  │ ┌─────────────┐ │              │ ┌─────────────┐ │
  │ │Id: "abc-123"│ │              │ │Id: "def-456"│ │
  │ │Name:"Carlos"│ │              │ │Name: "Ana"  │ │
  │ │Color: Red   │ │              │ │Color: Blue  │ │
  │ │Game: "xyz"  │ │              │ │Game: "xyz"  │ │
  │ └─────────────┘ │              │ └─────────────┘ │
  │                 │              │                 │
  │ Instancias de   │              │ Instancias de   │
  │ componentes:    │              │ componentes:    │
  │ Home, Lobby,    │              │ Home, Lobby,    │
  │ Game, etc.      │              │ Game, etc.      │
  │ (todos ven el   │              │ (todos ven el   │
  │  mismo "Carlos")│              │  mismo "Ana")   │
  └─────────────────┘              └─────────────────┘
         │                                  │
     Todos scoped                       Todos scoped
     al circuito 1                      al circuito 2
```

---

## 4. Validaciones del Nombre

### 4.1 Reglas de Validación

| Regla | Criterio | Mensaje de error |
|-------|---------|-----------------|
| **V-01** Obligatorio | El nombre no puede estar vacío ni ser solo espacios | "Debes introducir un nombre" |
| **V-02** Longitud mínima | Al menos 2 caracteres (tras trim) | "El nombre debe tener al menos 2 caracteres" |
| **V-03** Longitud máxima | Máximo 20 caracteres (tras trim) | "El nombre no puede tener más de 20 caracteres" |
| **V-04** Caracteres permitidos | Solo letras (incluyendo acentos/ñ), números, espacios y guiones | "El nombre contiene caracteres no permitidos" |
| **V-05** Unicidad global | No puede haber otro jugador conectado con el mismo nombre (case-insensitive) | "Ya hay un jugador conectado con ese nombre" |
| **V-06** Sin espacios consecutivos | No se permiten múltiples espacios seguidos | Se normaliza automáticamente (no se muestra error) |

### 4.2 Flujo de Validación

```
Jugador escribe nombre y pulsa "Entrar"
         │
         ▼
  ┌──────────────────────┐
  │ 1. Trim del nombre   │    "  Carlos  " → "Carlos"
  └──────────┬───────────┘
             ▼
  ┌──────────────────────┐
  │ 2. Normalizar        │    "Carlos   García" → "Carlos García"
  │    espacios múltiples│
  └──────────┬───────────┘
             ▼
  ┌──────────────────────┐
  │ 3. Validar vacío     │── FALLA ──▶ "Debes introducir un nombre"
  └──────────┬───────────┘
             │ OK
             ▼
  ┌──────────────────────┐
  │ 4. Validar longitud  │── FALLA ──▶ "Mínimo 2 / Máximo 20 caracteres"
  │    (2-20 chars)      │
  └──────────┬───────────┘
             │ OK
             ▼
  ┌──────────────────────┐
  │ 5. Validar caracteres│── FALLA ──▶ "Caracteres no permitidos"
  │    permitidos (regex) │
  └──────────┬───────────┘
             │ OK
             ▼
  ┌──────────────────────┐
  │ 6. Verificar unicidad│── FALLA ──▶ "Nombre ya en uso"
  │    (GameManager)     │
  └──────────┬───────────┘
             │ OK
             ▼
  ┌──────────────────────┐
  │ 7. SetPlayer(nombre) │
  │ 8. Ir al lobby       │
  └──────────────────────┘
```

### 4.3 Expresión Regular para Caracteres Permitidos

```csharp
// Permite: letras (incluyendo acentos, ñ, ü), números, espacios y guiones
private static readonly Regex ValidNamePattern = new(
    @"^[\p{L}\p{N}\s\-]+$",
    RegexOptions.Compiled
);

// Ejemplos válidos:   "Carlos", "Ana María", "José-Luis", "Ñoño", "Player1"
// Ejemplos inválidos: "Carlos@", "Ana<script>", "!hacker", ""
```

### 4.4 Verificación de Unicidad

La unicidad del nombre se verifica contra **todos los jugadores actualmente conectados** al sistema, no solo dentro de una partida:

```csharp
// En GameManager (Singleton)
public bool IsNameTaken(string name)
{
    return _connectedPlayers.Values
        .Any(p => p.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase));
}
```

**¿Por qué unicidad global y no por partida?**

- Evita confusiones en el lobby si dos jugadores se llaman igual.
- Simplifica el seguimiento de conexiones/reconexiones.
- Con un grupo pequeño de amigos, es prácticamente imposible que haya colisiones de nombres.

---

## 5. Asignación de Color

### 5.1 Paleta de Colores Disponibles

Cada jugador recibe un color que lo identifica visualmente en el tablero. Los colores se asignan **automáticamente** al unirse a una partida, en orden de llegada:

| Orden | Color | Código HEX | Código CSS | Uso en el mapa |
|:-----:|-------|:----------:|:----------:|---------------|
| 1 | 🔴 Rojo | `#E63946` | `var(--player-red)` | Territorios, ejércitos, panel |
| 2 | 🔵 Azul | `#457B9D` | `var(--player-blue)` | Territorios, ejércitos, panel |
| 3 | 🟢 Verde | `#2A9D8F` | `var(--player-green)` | Territorios, ejércitos, panel |
| 4 | 🟡 Amarillo | `#E9C46A` | `var(--player-yellow)` | Territorios, ejércitos, panel |
| 5 | 🟣 Púrpura | `#7B2D8E` | `var(--player-purple)` | Territorios, ejércitos, panel |
| 6 | 🟠 Naranja | `#F4845F` | `var(--player-orange)` | Territorios, ejércitos, panel |
| — | ⚪ Neutral | `#ADB5BD` | `var(--player-neutral)` | Solo en partidas de 2 jugadores |

### 5.2 Criterios de la Paleta

| Criterio | Explicación |
|----------|-------------|
| **Distinguibles entre sí** | Los 6 colores son claramente diferentes, incluso en monitores con calibración pobre |
| **Legibles sobre el mapa** | Se evitan colores demasiado claros o similares al fondo del mapa |
| **Accesibilidad básica** | Se eligen tonos con suficiente contraste entre sí para daltonismo parcial |
| **Consistentes con el Risk clásico** | Se mantienen los colores icónicos (rojo, azul, verde, amarillo) |

### 5.3 Enumeración PlayerColor

```csharp
public enum PlayerColor
{
    Red = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3,
    Purple = 4,
    Orange = 5,
    Neutral = 99   // Solo para el jugador neutral en partidas de 2
}
```

### 5.4 Lógica de Asignación

```
Jugador se une a una partida
         │
         ▼
  ┌────────────────────────────────┐
  │ Obtener colores ya asignados   │
  │ en esta partida                │
  └──────────┬─────────────────────┘
             ▼
  ┌────────────────────────────────┐
  │ Recorrer la paleta en orden:   │
  │ Red → Blue → Green → Yellow    │
  │ → Purple → Orange              │
  │                                │
  │ Asignar el primer color        │
  │ disponible (no asignado)       │
  └──────────┬─────────────────────┘
             ▼
  ┌────────────────────────────────┐
  │ PlayerSession.SetColor(color)  │
  │ Player.Color = color           │
  └────────────────────────────────┘
```

**Ejemplo:**

```
Partida "Los viernes"
│
├── Jugador 1: Carlos  → 🔴 Rojo    (primer color disponible)
├── Jugador 2: Ana     → 🔵 Azul    (segundo color disponible)
├── Jugador 3: Luis    → 🟢 Verde   (tercer color disponible)
│
│   Luis se desconecta y es reemplazado por María
│
├── Jugador 3: María   → 🟢 Verde   (reutiliza el color de Luis)
│
│   Pedro se une
│
└── Jugador 4: Pedro   → 🟡 Amarillo (cuarto color disponible)
```

### 5.5 Representación Visual del Color

El color del jugador se utiliza en múltiples elementos de la UI:

| Elemento | Uso del color |
|----------|--------------|
| **Territorios del mapa** | El fondo del territorio se colorea con el color del propietario |
| **Número de ejércitos** | El badge con el número de ejércitos usa el color del jugador |
| **Panel del jugador** | El borde y acento del panel usa el color del jugador |
| **Indicador de turno** | El nombre del jugador en turno se muestra con su color |
| **Chat** | El nombre del jugador en los mensajes de chat usa su color |
| **Log de eventos** | Las acciones en el log se colorean según el jugador involucrado |

---

## 6. Avatares

### 6.1 Sistema de Avatares

En lugar de un complejo sistema de subida de imágenes, MiniRisk utiliza **avatares generados** basados en las iniciales del nombre del jugador:

```
┌─────────┐   ┌─────────┐   ┌─────────┐
│         │   │         │   │         │
│   CA    │   │   AN    │   │   LU    │
│  (🔴)   │   │  (🔵)   │   │  (🟢)   │
│         │   │         │   │         │
│ Carlos  │   │  Ana    │   │  Luis   │
└─────────┘   └─────────┘   └─────────┘
```

| Aspecto | Decisión |
|---------|---------|
| **Forma** | Círculo con fondo del color del jugador |
| **Contenido** | Primeras 2 letras del nombre (mayúsculas) |
| **Fuente** | Blanca, negrita, centrada |
| **Tamaño** | 40×40px en paneles, 24×24px en chat y log |

### 6.2 Generación del Avatar

```csharp
// Obtener iniciales para el avatar
public static string GetInitials(string name)
{
    var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    if (parts.Length >= 2)
    {
        // "Carlos García" → "CG"
        return $"{parts[0][0]}{parts[1][0]}".ToUpper();
    }

    // "Carlos" → "CA"
    return name.Length >= 2
        ? name[..2].ToUpper()
        : name.ToUpper();
}
```

### 6.3 Renderizado CSS del Avatar

```css
.player-avatar {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 40px;
    height: 40px;
    border-radius: 50%;
    color: white;
    font-weight: 700;
    font-size: 0.875rem;
    text-transform: uppercase;
    user-select: none;
}

.player-avatar--small {
    width: 24px;
    height: 24px;
    font-size: 0.625rem;
}

/* Colores por jugador */
.player-avatar--red    { background-color: var(--player-red); }
.player-avatar--blue   { background-color: var(--player-blue); }
.player-avatar--green  { background-color: var(--player-green); }
.player-avatar--yellow { background-color: var(--player-yellow); color: #333; }
.player-avatar--purple { background-color: var(--player-purple); }
.player-avatar--orange { background-color: var(--player-orange); }
```

---

## 7. Gestión de Conexión y Desconexión

### 7.1 Estados de Conexión de un Jugador

```
                    ┌───────────────────────┐
                    │      DESCONOCIDO      │
                    │  (no ha entrado aún)  │
                    └───────────┬───────────┘
                                │
                     Introduce nombre
                                │
                                ▼
                    ┌───────────────────────┐
  ┌────────────────│     IDENTIFICADO      │────────────────┐
  │                │   (en el lobby)        │                │
  │                └───────────┬───────────┘                │
  │                            │                            │
  │                  Crea / se une                          │
  │                  a una partida                          │
  │                            │                            │
  │                            ▼                            │
  │                ┌───────────────────────┐                │
  │   ┌───────────│      EN PARTIDA       │──────────┐     │
  │   │           │   (jugando)            │          │     │
  │   │           └───────────┬───────────┘          │     │
  │   │                       │                      │     │
  │   │            Pierde conexión                    │     │
  │   │           (cierra pestaña,                   │     │
  │   │            pierde WiFi, etc.)                │     │
  │   │                       │                      │     │
  │   │                       ▼                      │     │
  │   │           ┌───────────────────────┐          │     │
  │   │           │    DESCONECTADO       │          │     │
  │   │           │   (temporizador       │          │     │
  │   │           │    corriendo)         │          │     │
  │   │           └─────┬──────────┬──────┘          │     │
  │   │                 │          │                  │     │
  │   │        Reconecta │     Expira (5 min)        │     │
  │   │        en <60s   │          │                 │     │
  │   │                 │          ▼                  │     │
  │   │                 │  ┌────────────────┐         │     │
  │   │                 │  │  ABANDONADO    │         │     │
  │   │                 │  │ (territorios    │        │     │
  │   │                 │  │  pasan a neutral│        │     │
  │   │                 │  │  o IA pasiva)   │        │     │
  │   │                 │  └────────────────┘         │     │
  │   │                 │                             │     │
  │   │                 ▼                             │     │
  │   │    ┌───────────────────────┐                  │     │
  │   │    │   RECONECTADO        │                  │     │
  │   └───▶│  (vuelve a la        │◀─────────────────┘     │
  │        │   partida)           │    Abandona             │
  │        └───────────────────────┘    voluntariamente     │
  │                                                         │
  │              Cierra sesión / cierra navegador            │
  │                                                         │
  └─────────────────────────────────────────────────────────┘
                                │
                                ▼
                    ┌───────────────────────┐
                    │      DESCONECTADO     │
                    │   (circuito cerrado,  │
                    │    servicio scoped    │
                    │    destruido)         │
                    └───────────────────────┘
```

### 7.2 Reconexión: Flujo Detallado

Cuando un jugador pierde la conexión (WebSocket roto), se activan dos mecanismos:

#### A) Reconexión del Circuito Blazor (automática)

El `ReconnectModal` (ya incluido en la plantilla del proyecto) intenta reconectar el circuito Blazor automáticamente:

```
  Jugador pierde conexión
         │
         ▼
  ┌─────────────────────────┐
  │  ReconnectModal se      │     "Rejoining the server..."
  │  muestra en pantalla    │
  └──────────┬──────────────┘
             │
             ▼
  ┌─────────────────────────┐
  │  Blazor intenta         │     Reintentos automáticos
  │  reconectar el circuito │     con backoff exponencial
  └──────────┬──────────────┘
             │
      ┌──────┴───────┐
      │              │
   Éxito          Fracaso
      │              │
      ▼              ▼
  Circuito       "Failed to rejoin.
  restaurado      Please retry or
  (mismo scope)   reload the page."
```

**Si la reconexión del circuito tiene éxito:**
- El servicio scoped `PlayerSessionService` sigue vivo → el jugador sigue identificado.
- El `HubConnection` puede necesitar reconectarse al `GameHub`.

**Si la reconexión del circuito fracasa:**
- El jugador debe recargar la página → se crea un nuevo circuito y un nuevo scope.
- El `PlayerSessionService` anterior se destruye → el jugador debe introducir su nombre de nuevo.
- Debe unirse a la partida de nuevo (reconexión manual).

#### B) Reconexión al GameHub

Independientemente del circuito Blazor, la conexión al `GameHub` puede romperse. Se configura reconexión automática:

```csharp
// Configuración del HubConnection en Game.razor
hubConnection = new HubConnectionBuilder()
    .WithUrl(Navigation.ToAbsoluteUri("/gamehub"))
    .WithAutomaticReconnect(new[] { 
        TimeSpan.FromSeconds(0),   // Primer reintento inmediato
        TimeSpan.FromSeconds(2),   // 2 segundos
        TimeSpan.FromSeconds(5),   // 5 segundos
        TimeSpan.FromSeconds(10),  // 10 segundos
        TimeSpan.FromSeconds(30),  // 30 segundos
        TimeSpan.FromSeconds(60)   // 60 segundos (último intento)
    })
    .Build();

// Eventos de reconexión
hubConnection.Reconnecting += (error) =>
{
    // Mostrar indicador "Reconectando..."
    isReconnecting = true;
    InvokeAsync(StateHasChanged);
    return Task.CompletedTask;
};

hubConnection.Reconnected += async (connectionId) =>
{
    // Reconexión exitosa: re-unirse al grupo de la partida
    isReconnecting = false;
    await hubConnection.SendAsync("RejoinGame", gameId, playerSession.PlayerId);
    await InvokeAsync(StateHasChanged);
};

hubConnection.Closed += (error) =>
{
    // Conexión perdida definitivamente
    isDisconnected = true;
    InvokeAsync(StateHasChanged);
    return Task.CompletedTask;
};
```

### 7.3 Reconexión Manual (Página Recargada)

Si el jugador recarga la página o abre una nueva pestaña, pierde su circuito y debe re-identificarse:

```
  Jugador recarga la página
         │
         ▼
  ┌───────────────────────────┐
  │ Nuevo circuito            │
  │ Nuevo PlayerSessionService│
  │ (vacío)                   │
  └──────────┬────────────────┘
             ▼
  ┌───────────────────────────┐
  │ Home.razor: pedir nombre  │
  └──────────┬────────────────┘
             │
  Jugador escribe el MISMO nombre
             │
             ▼
  ┌───────────────────────────┐
  │ GameManager detecta que    │
  │ "Carlos" estaba en la      │
  │ partida "xyz" pero         │
  │ desconectado               │
  └──────────┬────────────────┘
             ▼
  ┌───────────────────────────┐
  │ Reasociar jugador:        │
  │ - Mismo PlayerId           │
  │ - Mismo color              │
  │ - Mismos territorios       │
  │ - Reconectar al hub        │
  │ - Notificar al grupo       │
  └──────────┬────────────────┘
             ▼
  ┌───────────────────────────┐
  │ NavigateTo("/game/xyz")    │
  │ Jugador vuelve a su       │
  │ partida como si nada      │
  └───────────────────────────┘
```

### 7.4 Temporizadores de Desconexión

| Temporizador | Duración | Acción |
|:------------:|:--------:|--------|
| **Turno saltado** | 60 segundos | Si es el turno del jugador desconectado, se salta su turno automáticamente y pasa al siguiente jugador |
| **Abandono** | 5 minutos | Si el jugador no se reconecta en 5 minutos, se le considera abandonado. Sus territorios pasan a ser **neutrales** (ejércitos se mantienen pero no atacan ni se fortifican) |

### 7.5 Registro de Jugadores Conectados en GameManager

El `GameManager` (Singleton) mantiene un registro global de jugadores conectados para gestionar la unicidad de nombres y las reconexiones:

```csharp
public class GameManager : IGameManager
{
    // Registro global de jugadores conectados
    // Key: PlayerId, Value: información de la sesión
    private readonly ConcurrentDictionary<string, ConnectedPlayer> _connectedPlayers = new();

    public record ConnectedPlayer(
        string PlayerId,
        string PlayerName,
        string? GameId,
        string? ConnectionId,      // ConnectionId del hub (null si desconectado)
        DateTime ConnectedAt,
        DateTime? DisconnectedAt,   // null si está conectado
        bool IsConnected
    );

    public void RegisterPlayer(string playerId, string playerName)
    {
        _connectedPlayers[playerId] = new ConnectedPlayer(
            playerId, playerName, null, null, DateTime.UtcNow, null, true
        );
    }

    public void UnregisterPlayer(string playerId)
    {
        _connectedPlayers.TryRemove(playerId, out _);
    }

    public bool IsNameTaken(string name)
    {
        return _connectedPlayers.Values
            .Any(p => p.IsConnected &&
                      p.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    // Busca un jugador desconectado con el mismo nombre (para reconexión)
    public ConnectedPlayer? FindDisconnectedPlayer(string name)
    {
        return _connectedPlayers.Values
            .FirstOrDefault(p => !p.IsConnected &&
                                  p.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
```

---

## 8. Pantalla de Bienvenida: Diseño de Home.razor

### 8.1 Wireframe

```
┌──────────────────────────────────────────────────────────┐
│                                                          │
│                                                          │
│                                                          │
│              ┌──────────────────────────┐                │
│              │                          │                │
│              │     🎲  MiniRisk         │                │
│              │                          │                │
│              │   Conquista el mundo     │                │
│              │   con tus amigos         │                │
│              │                          │                │
│              │  ┌────────────────────┐  │                │
│              │  │ Tu nombre...       │  │                │
│              │  └────────────────────┘  │                │
│              │                          │                │
│              │  ⚠️ [Mensaje de error]   │                │
│              │     (visible solo si     │                │
│              │      hay error)          │                │
│              │                          │                │
│              │  ┌────────────────────┐  │                │
│              │  │   🚀 Entrar        │  │                │
│              │  └────────────────────┘  │                │
│              │                          │                │
│              │  🟢 3 jugadores en línea │                │
│              │  🎮 2 partidas activas   │                │
│              │                          │                │
│              └──────────────────────────┘                │
│                                                          │
│                                                          │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

### 8.2 Características de la Pantalla

| Elemento | Descripción |
|----------|-------------|
| **Logo/Título** | "🎲 MiniRisk" con tipografía grande y llamativa |
| **Subtítulo** | "Conquista el mundo con tus amigos" |
| **Campo de nombre** | Input de texto con placeholder "Tu nombre..." |
| **Botón Entrar** | Botón primario, deshabilitado si el campo está vacío |
| **Mensaje de error** | Se muestra debajo del campo solo si la validación falla |
| **Jugadores en línea** | Número de jugadores actualmente conectados (opcional) |
| **Partidas activas** | Número de partidas en curso (opcional, informativo) |
| **Diseño** | Centrado vertical y horizontalmente, tarjeta con sombra, fondo con tema del juego |

### 8.3 Comportamiento

| Acción del Usuario | Comportamiento |
|-------------------|----------------|
| Escribe nombre y pulsa Enter | Se envía el formulario (equivalente a pulsar "Entrar") |
| Pulsa "Entrar" con campo vacío | Botón deshabilitado, no ocurre nada |
| Nombre inválido | Se muestra mensaje de error bajo el campo, campo se resalta en rojo |
| Nombre válido y disponible | Se almacena en sesión, se navega al lobby |
| Nombre ya en uso | Se muestra "Ya hay un jugador conectado con ese nombre" |
| Ya está identificado (vuelve a /) | Se redirige automáticamente al lobby |

### 8.4 Estructura del Componente

```csharp
// Home.razor
@page "/"
@inject IPlayerSessionService PlayerSession
@inject IGameManager GameManager
@inject NavigationManager Navigation

@if (PlayerSession.IsIdentified)
{
    // Ya identificado: redirigir al lobby
    // NavigateTo en OnInitialized
}
else
{
    // Mostrar formulario de nombre
    <EditForm Model="model" OnValidSubmit="HandleSubmit">
        <input @bind-value="model.PlayerName" placeholder="Tu nombre..." />
        <button type="submit" disabled="@isSubmitting">🚀 Entrar</button>
        @if (!string.IsNullOrEmpty(errorMessage))
        {
            <div class="error">@errorMessage</div>
        }
    </EditForm>
}

@code {
    private NameModel model = new();
    private string? errorMessage;
    private bool isSubmitting;

    protected override void OnInitialized()
    {
        if (PlayerSession.IsIdentified)
        {
            Navigation.NavigateTo("/lobby");
        }
    }

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        errorMessage = null;

        var name = model.PlayerName?.Trim() ?? string.Empty;

        // Validaciones...
        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage = "Debes introducir un nombre";
            isSubmitting = false;
            return;
        }

        if (GameManager.IsNameTaken(name))
        {
            errorMessage = "Ya hay un jugador conectado con ese nombre";
            isSubmitting = false;
            return;
        }

        // Éxito
        PlayerSession.SetPlayer(name);
        GameManager.RegisterPlayer(PlayerSession.PlayerId, name);
        Navigation.NavigateTo("/lobby");
    }

    private class NameModel
    {
        public string? PlayerName { get; set; }
    }
}
```

---

## 9. Resumen de Decisiones

| Decisión | Elección | Alternativa descartada |
|----------|---------|----------------------|
| **Identificación** | Solo nombre, sin contraseña | Autenticación con Identity → innecesaria |
| **Almacenamiento de sesión** | `PlayerSessionService` (Scoped) | `ProtectedSessionStorage` → más complejo, sin beneficio |
| **Unicidad del nombre** | Global (todo el sistema) | Por partida → confuso en el lobby |
| **Colores** | Asignación automática por orden | Selección manual → posibles conflictos |
| **Avatares** | Iniciales + color (generado) | Subida de imagen → excesiva complejidad |
| **Reconexión** | Automática por hub + manual por nombre | Sin reconexión → mala experiencia |
| **Guard de rutas** | Check manual en `OnInitialized` | `[Authorize]` → no hay AuthenticationStateProvider |

---

> **Siguiente documento:** [04 — Comunicación en Tiempo Real — SignalR](./04_SignalR.md)
