# 01 — Visión General del Proyecto

> **Documento:** 01 de 14  
> **Versión:** 1.0  
> **Última actualización:** 22 de febrero de 2026  
> **Volver al índice:** [00_Indice.md](./00_Indice.md)

---

## 1. Descripción del Proyecto

**MiniRisk** es una adaptación digital del juego de mesa clásico **Risk** (también conocido como *TEG* o *War* en algunos países), desarrollada como aplicación web con **Blazor Server** y **.NET 10**. La aplicación permite a un grupo de **2 a 6 amigos** jugar partidas de Risk en tiempo real desde sus navegadores, conectados a un servidor en la red local.

### 1.1 Contexto y Motivación

El proyecto nace de la necesidad de poder jugar al Risk con amigos sin necesidad de reunirse físicamente ni depender de tableros y piezas. Al ser una aplicación de uso privado entre un grupo reducido de personas de confianza:

- **No se publica en internet**, se ejecuta en la red local de uno de los jugadores.
- **No hay autenticación formal**: no se necesitan cuentas, contraseñas ni roles. Simplemente se pregunta el nombre del jugador al entrar.
- **No hay monetización** ni tienda de ningún tipo.
- La prioridad es la **diversión y la usabilidad**, no la seguridad ante usuarios malintencionados.

### 1.2 Objetivos del Proyecto

| Objetivo | Descripción |
|----------|-------------|
| **OBJ-01** | Implementar una versión jugable del Risk clásico en navegador web |
| **OBJ-02** | Soportar partidas multijugador en tiempo real (2–6 jugadores) |
| **OBJ-03** | Utilizar SignalR para comunicación bidireccional de baja latencia |
| **OBJ-04** | Ofrecer una interfaz visual intuitiva con un mapa del mundo interactivo |
| **OBJ-05** | Permitir el chat entre jugadores durante la partida |
| **OBJ-06** | Minimizar la fricción de entrada: solo pedir un nombre para jugar |
| **OBJ-07** | Ejecutar la aplicación en red local sin dependencias externas |

---

## 2. Alcance Funcional

### 2.1 Funcionalidades Incluidas ✅

#### Gestión de partidas
- Crear una nueva partida con nombre personalizado
- Unirse a una partida existente desde el lobby
- Configurar la partida antes de empezar:
  - Número máximo de jugadores (2–6)
  - Modo de distribución de territorios (aleatorio o por turnos)
- Iniciar la partida cuando todos los jugadores estén listos
- Abandonar una partida en curso

#### Identificación del jugador
- Pantalla de bienvenida que solicita el nombre del jugador
- Asignación automática de color (de una paleta predefinida)
- Validación de nombre único dentro de la partida
- Sin autenticación: no hay login, registro, contraseñas ni roles

#### Flujo de juego completo
- **Distribución inicial de territorios**: asignación aleatoria o por selección por turnos
- **Colocación inicial de ejércitos**: reparto de ejércitos según número de jugadores
- **Fase de refuerzo**: cálculo automático de ejércitos según:
  - Número de territorios controlados (mínimo 3 ejércitos)
  - Bonificaciones por continentes completos
  - Canje de conjuntos de cartas
- **Fase de ataque**:
  - Selección de territorio atacante y defensor
  - Elección del número de dados (1–3 atacante, 1–2 defensor)
  - Tirada de dados con resolución automática
  - Conquista de territorios y movimiento obligatorio de tropas
  - Opción de continuar atacando o pasar a la siguiente fase
- **Fase de fortificación**:
  - Mover ejércitos entre territorios propios conectados
  - Un solo movimiento de fortificación por turno
- **Obtención de cartas de territorio**: una carta al final del turno si se conquistó al menos un territorio
- **Canje de cartas**: intercambiar conjuntos válidos por ejércitos adicionales
- **Eliminación de jugadores**: cuando un jugador pierde todos sus territorios
  - El conquistador recibe las cartas del jugador eliminado
- **Condición de victoria**: controlar los 42 territorios del mapa

#### Mapa del mundo
- Representación visual de los 42 territorios del Risk clásico
- Organización en 6 continentes con bonificaciones
- Visualización del propietario de cada territorio (color del jugador)
- Visualización del número de ejércitos en cada territorio
- Interacción por click para seleccionar territorios
- Resaltado de territorios válidos según la acción actual (ataque, fortificación)

#### Comunicación en tiempo real
- Actualización instantánea del tablero para todos los jugadores vía SignalR
- Notificaciones de turno ("Es tu turno", "Jugador X está atacando...")
- Resultados de dados visibles para todos los jugadores
- Chat integrado en la partida
- Indicadores de conexión/desconexión de jugadores

#### Interfaz de usuario
- Panel de información del jugador actual (ejércitos, cartas, territorios)
- Indicador de fase actual del turno
- Log de eventos de la partida (historial de acciones)
- Panel de dados con resultado visual
- Vista de cartas en mano con opción de canje
- Pantalla de victoria/derrota

### 2.2 Funcionalidades Excluidas ❌

| Funcionalidad | Motivo de exclusión |
|---------------|-------------------|
| **Autenticación y autorización** | Uso privado entre amigos; innecesario |
| **Persistencia en base de datos** | Las partidas son efímeras; no se guardan |
| **Guardado y carga de partidas** | Complejidad alta, bajo beneficio para el uso esperado |
| **Jugadores IA (bots)** | Fuera del alcance inicial; todos los jugadores son humanos |
| **Misiones secretas** | Se juega con la regla clásica de conquista mundial |
| **Variantes de reglas** | Solo se implementan las reglas estándar del Risk clásico |
| **Modo espectador** | No prioritario para el grupo de amigos |
| **Internacionalización (i18n)** | La interfaz será en español |
| **Diseño mobile-first** | Se prioriza la experiencia en escritorio |
| **Audio y efectos de sonido** | Fuera del alcance inicial |
| **Ranking o estadísticas históricas** | No hay persistencia entre sesiones |
| **Temporizador de turno** | Se confía en que los jugadores jugarán a un ritmo razonable |
| **Alianzas formales** | Las alianzas se negocian de voz, sin soporte en el sistema |

### 2.3 Funcionalidades Futuras (Posibles) 🔮

Estas funcionalidades no se incluyen en la versión inicial pero podrían incorporarse en el futuro si el grupo lo desea:

- Jugadores IA con distintos niveles de dificultad
- Misiones secretas como condición de victoria alternativa
- Guardado y reanudación de partidas
- Temporizador configurable por turno
- Efectos de sonido y música ambiental
- Estadísticas de partidas anteriores
- Modo espectador

---

## 3. Reglas del Juego

Este apartado describe las reglas del Risk clásico tal como se implementarán en MiniRisk. Se han adaptado al formato digital eliminando la necesidad de gestión manual de piezas y dados.

### 3.1 Preparación de la Partida

#### Número de jugadores y ejércitos iniciales

| Jugadores | Ejércitos iniciales por jugador |
|:---------:|:-------------------------------:|
| 2 | 40 |
| 3 | 35 |
| 4 | 30 |
| 5 | 25 |
| 6 | 20 |

#### Distribución de territorios

Se ofrecen dos modos:

1. **Distribución aleatoria** (por defecto): El sistema reparte los 42 territorios equitativamente entre los jugadores de forma aleatoria. Cada territorio comienza con 1 ejército. Los ejércitos restantes se colocan por turnos.

2. **Distribución por selección**: Los jugadores, por turnos, eligen un territorio libre y colocan 1 ejército en él. Una vez repartidos los 42 territorios, los jugadores colocan sus ejércitos restantes uno a uno por turnos en sus territorios.

#### Orden de turno

Se determina aleatoriamente al inicio de la partida y se mantiene fijo durante toda la partida (sentido horario virtual).

### 3.2 Estructura del Turno

Cada turno se compone de **tres fases obligatorias** que se ejecutan en orden:

```
┌─────────────────────────────────────────────────────────┐
│                    TURNO DEL JUGADOR                    │
│                                                         │
│   ┌──────────┐    ┌──────────┐    ┌───────────────┐    │
│   │ 1. REFUERZO│──▶│ 2. ATAQUE │──▶│3. FORTIFICACIÓN│   │
│   │          │    │(opcional) │    │   (opcional)   │    │
│   └──────────┘    └──────────┘    └───────────────┘    │
│                                                         │
│   Obtener y       Atacar          Mover ejércitos       │
│   colocar         territorios     entre territorios     │
│   ejércitos       enemigos        propios conectados    │
└─────────────────────────────────────────────────────────┘
```

### 3.3 Fase 1 — Refuerzo

El jugador recibe ejércitos de refuerzo que debe colocar en sus territorios antes de poder atacar.

#### Cálculo de ejércitos de refuerzo

Los ejércitos se calculan sumando tres fuentes:

**a) Por territorios controlados:**

```
ejércitos = max(3, territorios_controlados / 3)    // División entera, mínimo 3
```

**b) Por continentes completos:**

Si el jugador controla todos los territorios de un continente, recibe una bonificación:

| Continente | Territorios | Bonificación |
|------------|:-----------:|:------------:|
| Asia | 12 | +7 |
| América del Norte | 9 | +5 |
| Europa | 7 | +5 |
| África | 6 | +3 |
| América del Sur | 4 | +2 |
| Oceanía | 4 | +2 |
| **Total** | **42** | — |

**c) Por canje de cartas:**

Si el jugador tiene 3 o más cartas y realiza un canje válido (ver sección 3.6), recibe ejércitos adicionales. Si tiene 5 o más cartas, el canje es **obligatorio** antes de continuar.

#### Colocación de ejércitos

El jugador debe colocar **todos** los ejércitos de refuerzo recibidos en sus territorios antes de avanzar a la fase de ataque. Puede distribuirlos libremente entre sus territorios.

### 3.4 Fase 2 — Ataque

El ataque es **opcional**. El jugador puede atacar tantas veces como desee o pasar directamente a la fase de fortificación.

#### Requisitos para atacar

- El territorio atacante debe ser **propio** y tener **al menos 2 ejércitos** (siempre debe quedar al menos 1 en el territorio).
- El territorio defensor debe ser **enemigo** y **adyacente** al territorio atacante.

#### Mecánica de dados

| Rol | Dados disponibles | Condición |
|-----|:-----------------:|-----------|
| **Atacante** | 1, 2 o 3 dados | Máximo = min(3, ejércitos_en_territorio - 1) |
| **Defensor** | 1 o 2 dados | Máximo = min(2, ejércitos_en_territorio) |

#### Resolución del combate

1. Ambos jugadores tiran sus dados.
2. Los dados de cada jugador se ordenan de mayor a menor.
3. Se comparan los dados en **parejas** (el mayor del atacante contra el mayor del defensor, el segundo mayor contra el segundo mayor si ambos tienen al menos 2 dados).
4. Por cada pareja:
   - Si el dado del **atacante** es **estrictamente mayor** que el del defensor → el defensor pierde 1 ejército.
   - Si el dado del **defensor** es **mayor o igual** que el del atacante → el atacante pierde 1 ejército.
5. Las pérdidas se aplican simultáneamente.

**Ejemplo:**

```
Atacante tira: [6, 3, 2]    Defensor tira: [5, 4]

Pareja 1: 6 vs 5 → Atacante gana → Defensor pierde 1 ejército
Pareja 2: 3 vs 4 → Defensor gana → Atacante pierde 1 ejército
(El dado 2 del atacante no se compara, no hay pareja)
```

#### Conquista de territorio

Si el defensor pierde todos sus ejércitos en un territorio:

1. **El atacante conquista el territorio.**
2. El atacante **debe** mover al territorio conquistado al menos tantos ejércitos como dados utilizó en el último ataque (mínimo 1).
3. El atacante puede mover ejércitos adicionales desde el territorio atacante, dejando al menos 1 en el territorio de origen.

#### Eliminación de un jugador

Si un jugador pierde su **último territorio**, queda **eliminado** de la partida:

- Todas sus cartas de territorio pasan al jugador que lo eliminó.
- Si el conquistador acumula 6 o más cartas, debe canjear inmediatamente.

### 3.5 Fase 3 — Fortificación

Al final del turno, el jugador puede realizar **un único movimiento de fortificación**:

- Mover cualquier cantidad de ejércitos de un territorio propio a otro territorio propio.
- Ambos territorios deben estar **conectados** por un camino de territorios propios (no basta con ser adyacentes si hay territorios enemigos en medio).
- Debe dejar al menos 1 ejército en el territorio de origen.
- La fortificación es **opcional**; el jugador puede pasar sin mover tropas.

### 3.6 Cartas de Territorio

#### Obtención

- Al final de cada turno, si el jugador **conquistó al menos un territorio** durante ese turno, recibe **1 carta** del mazo.
- Solo se recibe una carta por turno, independientemente del número de conquistas.

#### Tipos de cartas

| Tipo | Icono | Cantidad en el mazo |
|------|-------|:-------------------:|
| **Infantería** | 🚶 | 14 |
| **Caballería** | 🐴 | 14 |
| **Artillería** | 💣 | 14 |
| **Comodín** | ⭐ | 2 |
| **Total** | — | **44** |

Cada carta (excepto los comodines) tiene asociado un territorio específico del mapa.

#### Canje de cartas

Un canje válido consiste en **3 cartas** que cumplan una de estas combinaciones:

| Combinación | Descripción |
|------------|-------------|
| 3 iguales | Tres cartas del mismo tipo (3 infanterías, 3 caballerías o 3 artillerías) |
| 1 de cada | Una carta de cada tipo (1 infantería + 1 caballería + 1 artillería) |
| Comodín + 2 | Un comodín más 2 cartas cualesquiera |

#### Ejércitos por canje

Los ejércitos obtenidos por canje se incrementan con cada canje realizado en la partida (globalmente, no por jugador):

| Nº de canje (global) | Ejércitos recibidos |
|:--------------------:|:-------------------:|
| 1º | 4 |
| 2º | 6 |
| 3º | 8 |
| 4º | 10 |
| 5º | 12 |
| 6º | 15 |
| 7º+ | +5 por cada canje adicional |

#### Bonificación por territorio en la carta

Si alguna de las cartas canjeadas corresponde a un territorio que el jugador **controla**, recibe **2 ejércitos adicionales** que deben colocarse en ese territorio.

### 3.7 Condición de Victoria

Un jugador **gana la partida** cuando controla los **42 territorios** del mapa. No se implementan misiones secretas ni condiciones de victoria alternativas en esta versión.

### 3.8 Regla para 2 Jugadores

En partidas de **2 jugadores**, se utilizan las siguientes adaptaciones:

- Un **tercer jugador neutral** controla un tercio de los territorios.
- Los territorios neutrales tienen ejércitos pero **nunca atacan ni se fortifican**.
- Cuando un jugador ataca un territorio neutral, se usa la mecánica de defensa estándar (el sistema tira dados por el jugador neutral).
- Los jugadores deben conquistar todos los territorios (incluyendo los neutrales) para ganar.

---

## 4. Los 42 Territorios y 6 Continentes

### 4.1 América del Norte (9 territorios, bonificación: +5)

| # | Territorio | Adyacencias destacadas |
|---|-----------|----------------------|
| 1 | Alaska | Kamchatka (conexión intercontinental), Alberta, Territorio del Noroeste |
| 2 | Territorio del Noroeste | Alaska, Alberta, Ontario, Groenlandia |
| 3 | Groenlandia | Territorio del Noroeste, Ontario, Quebec, Islandia (conexión intercontinental) |
| 4 | Alberta | Alaska, Territorio del Noroeste, Ontario, Estados Unidos Occidentales |
| 5 | Ontario | Territorio del Noroeste, Alberta, Quebec, Groenlandia, Estados Unidos Occidentales, Estados Unidos Orientales |
| 6 | Quebec | Ontario, Groenlandia, Estados Unidos Orientales |
| 7 | Estados Unidos Occidentales | Alberta, Ontario, Estados Unidos Orientales, América Central |
| 8 | Estados Unidos Orientales | Ontario, Quebec, Estados Unidos Occidentales, América Central |
| 9 | América Central | Estados Unidos Occidentales, Estados Unidos Orientales, Venezuela (conexión intercontinental) |

### 4.2 América del Sur (4 territorios, bonificación: +2)

| # | Territorio | Adyacencias destacadas |
|---|-----------|----------------------|
| 10 | Venezuela | América Central (conexión intercontinental), Perú, Brasil |
| 11 | Perú | Venezuela, Brasil, Argentina |
| 12 | Brasil | Venezuela, Perú, Argentina, Norte de África (conexión intercontinental) |
| 13 | Argentina | Perú, Brasil |

### 4.3 Europa (7 territorios, bonificación: +5)

| # | Territorio | Adyacencias destacadas |
|---|-----------|----------------------|
| 14 | Islandia | Groenlandia (conexión intercontinental), Gran Bretaña, Escandinavia |
| 15 | Gran Bretaña | Islandia, Escandinavia, Europa Occidental, Europa del Norte |
| 16 | Escandinavia | Islandia, Gran Bretaña, Europa del Norte, Ucrania |
| 17 | Europa Occidental | Gran Bretaña, Europa del Norte, Europa del Sur, Norte de África (conexión intercontinental) |
| 18 | Europa del Norte | Gran Bretaña, Escandinavia, Europa Occidental, Europa del Sur, Ucrania |
| 19 | Europa del Sur | Europa Occidental, Europa del Norte, Ucrania, Egipto (conexión intercontinental), Norte de África (conexión intercontinental), Oriente Medio (conexión intercontinental) |
| 20 | Ucrania | Escandinavia, Europa del Norte, Europa del Sur, Ural (conexión intercontinental), Afganistán (conexión intercontinental), Oriente Medio (conexión intercontinental) |

### 4.4 África (6 territorios, bonificación: +3)

| # | Territorio | Adyacencias destacadas |
|---|-----------|----------------------|
| 21 | Norte de África | Brasil (conexión intercontinental), Europa Occidental (conexión intercontinental), Europa del Sur (conexión intercontinental), Egipto, África Oriental, Congo |
| 22 | Egipto | Norte de África, Europa del Sur (conexión intercontinental), Oriente Medio (conexión intercontinental), África Oriental |
| 23 | África Oriental | Norte de África, Egipto, Congo, Sudáfrica, Madagascar |
| 24 | Congo | Norte de África, África Oriental, Sudáfrica |
| 25 | Sudáfrica | Congo, África Oriental, Madagascar |
| 26 | Madagascar | África Oriental, Sudáfrica |

### 4.5 Asia (12 territorios, bonificación: +7)

| # | Territorio | Adyacencias destacadas |
|---|-----------|----------------------|
| 27 | Oriente Medio | Europa del Sur (conexión intercontinental), Ucrania (conexión intercontinental), Egipto (conexión intercontinental), Afganistán, India |
| 28 | Afganistán | Ucrania (conexión intercontinental), Oriente Medio, Ural, China, India |
| 29 | Ural | Ucrania (conexión intercontinental), Afganistán, Siberia, China |
| 30 | Siberia | Ural, Yakutsk, Irkutsk, Mongolia, China |
| 31 | Yakutsk | Siberia, Irkutsk, Kamchatka |
| 32 | Irkutsk | Siberia, Yakutsk, Kamchatka, Mongolia |
| 33 | Kamchatka | Yakutsk, Irkutsk, Mongolia, Japón, Alaska (conexión intercontinental) |
| 34 | Mongolia | Siberia, Irkutsk, Kamchatka, Japón, China |
| 35 | Japón | Kamchatka, Mongolia |
| 36 | China | Afganistán, Ural, Siberia, Mongolia, India, Sureste Asiático |
| 37 | India | Oriente Medio, Afganistán, China, Sureste Asiático |
| 38 | Sureste Asiático | India, China, Indonesia (conexión intercontinental) |

### 4.6 Oceanía (4 territorios, bonificación: +2)

| # | Territorio | Adyacencias destacadas |
|---|-----------|----------------------|
| 39 | Indonesia | Sureste Asiático (conexión intercontinental), Nueva Guinea, Australia Occidental |
| 40 | Nueva Guinea | Indonesia, Australia Occidental, Australia Oriental |
| 41 | Australia Occidental | Indonesia, Nueva Guinea, Australia Oriental |
| 42 | Australia Oriental | Nueva Guinea, Australia Occidental |

---

## 5. Requisitos No Funcionales

### 5.1 Rendimiento

| Requisito | Especificación |
|-----------|---------------|
| **RNF-01** | La latencia de las acciones del jugador (tirar dados, mover ejércitos) debe ser imperceptible (<200ms) en red local |
| **RNF-02** | La actualización del tablero tras un ataque debe reflejarse en todos los clientes en menos de 500ms |
| **RNF-03** | La aplicación debe soportar al menos 3 partidas simultáneas sin degradación perceptible |
| **RNF-04** | El consumo de memoria del servidor debe mantenerse por debajo de 500 MB con 3 partidas activas |

### 5.2 Usabilidad

| Requisito | Especificación |
|-----------|---------------|
| **RNF-05** | Un jugador nuevo debe poder entender la interfaz sin instrucción previa; las acciones disponibles deben ser evidentes |
| **RNF-06** | El mapa del mundo debe ser legible e interactivo en resoluciones de 1280×720 o superiores |
| **RNF-07** | Los territorios del mapa deben tener áreas de click suficientemente grandes para evitar clicks accidentales |
| **RNF-08** | El estado actual de la partida (turno, fase, ejércitos) debe ser visible sin necesidad de navegar fuera del tablero |
| **RNF-09** | Los resultados de los dados deben mostrarse de forma visualmente clara con animación |

### 5.3 Disponibilidad y Resiliencia

| Requisito | Especificación |
|-----------|---------------|
| **RNF-10** | Si un jugador pierde la conexión, debe poder reconectarse en un plazo de 60 segundos sin perder su posición en la partida |
| **RNF-11** | Si un jugador no se reconecta en 60 segundos, su turno se salta automáticamente |
| **RNF-12** | Si un jugador no se reconecta en 5 minutos, se le considera desconectado y sus territorios se convierten en neutrales |
| **RNF-13** | La desconexión de un jugador no debe bloquear la partida para el resto |

### 5.4 Compatibilidad

| Requisito | Especificación |
|-----------|---------------|
| **RNF-14** | La aplicación debe funcionar en los navegadores modernos: Chrome, Firefox, Edge (últimas 2 versiones) |
| **RNF-15** | El servidor debe poder ejecutarse en Windows, Linux o macOS con .NET 10 instalado |
| **RNF-16** | Los jugadores deben poder conectarse desde cualquier dispositivo en la misma red local |

### 5.5 Mantenibilidad

| Requisito | Especificación |
|-----------|---------------|
| **RNF-17** | El motor del juego (lógica de reglas) debe estar desacoplado de la UI para facilitar su testeo unitario |
| **RNF-18** | Las reglas del juego deben estar implementadas en servicios inyectables, no en los componentes de UI |
| **RNF-19** | El código debe seguir las convenciones de C# y .NET |

---

## 6. Restricciones Técnicas

| Restricción | Detalle |
|-------------|---------|
| **Framework** | Blazor Server con .NET 10 (proyecto ya inicializado) |
| **Comunicación** | SignalR (integrado en Blazor Server) |
| **Base de datos** | Ninguna. Todo el estado se mantiene en memoria |
| **Autenticación** | Ninguna. Solo se pide un nombre de jugador |
| **Despliegue** | Red local únicamente. No se expone a internet |
| **Idioma de la UI** | Español |
| **Idioma del código** | Inglés (nombres de clases, métodos, variables, comentarios de código) |
| **Idioma de la documentación** | Español |

---

## 7. Glosario de Términos

### Términos del Dominio del Juego

| Término | Definición |
|---------|-----------|
| **Partida (Game)** | Una sesión de juego completa desde la distribución de territorios hasta que un jugador gana o todos abandonan |
| **Jugador (Player)** | Una persona participando en una partida, identificada por su nombre y color |
| **Territorio (Territory)** | Una de las 42 regiones del mapa del mundo. Siempre pertenece a un jugador y contiene al menos 1 ejército |
| **Continente (Continent)** | Agrupación geográfica de territorios (6 en total). Controlar todos los territorios de un continente otorga una bonificación de ejércitos |
| **Ejército (Army)** | Unidad militar que ocupa un territorio. Se representa como un número sobre el territorio en el mapa |
| **Refuerzo (Reinforcement)** | Ejércitos que un jugador recibe al inicio de su turno para colocar en sus territorios |
| **Ataque (Attack)** | Acción de combate en la que un jugador intenta conquistar un territorio enemigo adyacente |
| **Fortificación (Fortification)** | Movimiento de ejércitos entre dos territorios propios conectados, al final del turno |
| **Dados (Dice)** | Dados de seis caras utilizados para resolver combates. El atacante tira hasta 3, el defensor hasta 2 |
| **Carta de territorio (Territory Card)** | Carta asociada a un territorio y un tipo de tropa. Se obtiene al conquistar al menos un territorio en el turno |
| **Canje (Trade-in)** | Acción de entregar 3 cartas con una combinación válida para recibir ejércitos adicionales |
| **Comodín (Wildcard)** | Carta especial sin territorio asignado que puede sustituir a cualquier tipo en un canje |
| **Conquista (Conquest)** | Tomar el control de un territorio enemigo tras eliminar todos sus ejércitos defensores |
| **Eliminación (Elimination)** | Momento en que un jugador pierde su último territorio y queda fuera de la partida |
| **Adyacencia (Adjacency)** | Relación entre dos territorios que comparten frontera o están conectados por una línea marítima |
| **Conexión intercontinental** | Adyacencia entre territorios de distintos continentes (ej: Alaska–Kamchatka) |
| **Turno (Turn)** | Secuencia completa de fases (refuerzo, ataque, fortificación) que realiza un jugador |
| **Fase (Phase)** | Cada una de las etapas dentro de un turno: refuerzo, ataque y fortificación |
| **Camino conectado (Connected Path)** | Secuencia de territorios propios adyacentes entre sí, necesaria para la fortificación |
| **Jugador neutral** | Jugador ficticio en partidas de 2 jugadores que controla territorios pero no juega activamente |

### Términos Técnicos

| Término | Definición |
|---------|-----------|
| **Blazor Server** | Modelo de hosting de Blazor donde la lógica de la aplicación se ejecuta en el servidor y la UI se actualiza vía SignalR |
| **SignalR** | Biblioteca de .NET para comunicación bidireccional en tiempo real entre servidor y clientes |
| **Hub** | Clase de SignalR que define los métodos que el servidor expone a los clientes y viceversa |
| **Circuito (Circuit)** | Conexión persistente entre el navegador de un usuario y el servidor en Blazor Server |
| **Componente (Component)** | Unidad de UI reutilizable en Blazor, definida en archivos `.razor` |
| **Render Mode** | Modo de renderizado de un componente Blazor (en este proyecto: InteractiveServer) |
| **Scoped** | Ciclo de vida de un servicio en DI que vive durante la duración de un circuito/conexión |
| **Singleton** | Ciclo de vida de un servicio en DI que vive durante toda la ejecución de la aplicación |
| **SVG** | Scalable Vector Graphics. Formato de imagen vectorial usado para el mapa del mundo |
| **Lobby** | Pantalla donde los jugadores crean partidas, se unen a partidas existentes y esperan a que todos estén listos |
| **Estado en memoria (In-memory state)** | Datos de la aplicación almacenados en la RAM del servidor, sin persistir en disco ni base de datos |
| **Reconexión (Reconnection)** | Proceso por el cual un jugador que perdió la conexión vuelve a conectarse a su partida en curso |

---

## 8. Referencias

| Recurso | Enlace/Descripción |
|---------|-------------------|
| Reglas oficiales del Risk | Basado en las reglas del Risk clásico de Hasbro |
| Documentación de Blazor Server | [Microsoft Docs — Blazor Server](https://learn.microsoft.com/aspnet/core/blazor/) |
| Documentación de SignalR | [Microsoft Docs — SignalR](https://learn.microsoft.com/aspnet/core/signalr/) |
| .NET 10 | [Microsoft Docs — .NET 10](https://learn.microsoft.com/dotnet/) |

---

> **Siguiente documento:** [02 — Arquitectura General](./02_Arquitectura_General.md)
