# Architecture Patterns: Unity Mahjong Solitaire

**Domain:** Mobile Puzzle Game (Unity C#, Android)
**Project:** Vita Mahjong Number
**Researched:** 2026-03-19
**Confidence:** MEDIUM — drawn from Unity puzzle game patterns in training data (August 2025 cutoff); WebSearch unavailable for current verification. Core Unity architecture patterns are stable and unlikely to have changed substantially.

---

## Recommended Architecture

### High-Level System Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        APPLICATION LAYER                        │
│   MainMenu → ModeSelect → Gameplay Scene → ResultsScreen        │
└────────────────────────┬────────────────────────────────────────┘
                         │ Scene Load
┌────────────────────────▼────────────────────────────────────────┐
│                      GAMEPLAY SCENE                             │
│                                                                 │
│  ┌─────────────────┐     ┌──────────────────────────────────┐  │
│  │  GameplayController│◄──►│  Mode Strategy                  │  │
│  │  (MonoBehaviour)│     │  ClassicMode / MathMode /        │  │
│  │                 │     │  ActiveMindMode (pure C#)        │  │
│  └────────┬────────┘     └──────────────────────────────────┘  │
│           │                                                     │
│     ┌─────▼──────┐  ┌──────────────┐  ┌────────────────────┐  │
│     │ BoardManager│  │  ScoreManager│  │   HintSystem       │  │
│     │            │  │              │  │                    │  │
│     └─────┬──────┘  └──────────────┘  └────────────────────┘  │
│           │                                                     │
│     ┌─────▼──────┐  ┌──────────────┐  ┌────────────────────┐  │
│     │FreeTileChecker│  │MatchValidator│  │   AdsManager       │  │
│     │ (pure C#)  │  │  (pure C#)   │  │                    │  │
│     └────────────┘  └──────────────┘  └────────────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                    DATA / CONFIG LAYER                   │  │
│  │  BoardLayout SO   LevelConfig SO   ThemeConfig SO        │  │
│  │  TileData struct (x, y, z, value, isMatched)            │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Component Boundaries

### Ownership Principle
Each component owns one concern. No component reaches into another's internal state — it calls a method or reads a struct.

| Component | Owns | Does NOT Own | Communicates With |
|-----------|------|-------------|-------------------|
| `GameplayController` | Input handling, mode lifecycle, win/lose flow | Board state, score arithmetic | BoardManager, ScoreManager, HintSystem, Mode classes |
| `BoardManager` | TileData array, tile GameObject pool, board visual state | Match rules, scoring | FreeTileChecker, MatchValidator, TileView (via events) |
| `FreeTileChecker` | Free-tile algorithm | Nothing else | Called by BoardManager and HintSystem |
| `MatchValidator` | Pair validation per mode | Nothing else | Called by BoardManager (delegates to active mode) |
| `LevelGenerator` | Guaranteed-solvable board generation | Board rendering | Produces `TileData[]`, consumed by BoardManager |
| `ScoreManager` | Score, combo counter, level progression | Game flow | Receives events from GameplayController |
| `HintSystem` | Finding a valid free pair | Ad triggering | Calls FreeTileChecker, signals AdsManager |
| `AdsManager` | Unity Ads SDK lifecycle | Game logic | Called by HintSystem, GameplayController (interstitial) |
| `ClassicMode` | Identical-value match logic | Everything else | Implements `IModeStrategy`, used by GameplayController |
| `MathMode` | Sum-to-10 match logic | Everything else | Implements `IModeStrategy` |
| `ActiveMindMode` | Memory/concealment logic, reveal timing | Everything else | Implements `IModeStrategy` |
| `BoardLayout SO` | Tile position definitions | Runtime state | Read by LevelGenerator and BoardManager |
| `LevelConfig SO` | Tile count, layer count, difficulty params | Runtime state | Read by LevelGenerator |
| `ThemeConfig SO` | Sprites, colors, fonts | Game logic | Read by TileView and UIManager |

---

## Core Interface: IModeStrategy

All game modes implement one interface. `GameplayController` holds a reference to the current `IModeStrategy` and delegates all mode-specific logic through it. This is the single most load-bearing design decision in the project.

```csharp
public interface IModeStrategy
{
    bool IsValidMatch(TileData a, TileData b);
    void OnTileSelected(TileData tile);
    void OnMatchConfirmed(TileData a, TileData b);
    void OnLevelStart(LevelConfig config);
    void OnTick(float deltaTime);           // for ActiveMindMode reveal timer
    string GetHUDLabel();                   // mode-specific HUD text
}
```

Swapping modes at runtime: `GameplayController` assigns a new `IModeStrategy` instance. No scene reload required.

---

## Data Flow

### 1. Level Load Flow

```
ModeSelect (user picks mode + level)
  → GameplayController.StartLevel(LevelConfig, IModeStrategy)
  → LevelGenerator.Generate(LevelConfig, BoardLayout)
      → produces TileData[] (guaranteed solvable, pairs placed in reverse)
  → BoardManager.BuildBoard(TileData[])
      → Instantiates TileView GameObjects from pool
      → Maps TileData positions to world space
  → FreeTileChecker.Rebuild(TileData[])    // pre-compute free tile set
  → ScoreManager.Reset()
  → IModeStrategy.OnLevelStart(config)
```

### 2. Tile Selection Flow

```
User Touch → TileView.OnPointerClick()
  → GameplayController.OnTileSelected(tileId)
  → BoardManager.GetTile(tileId) → TileData
  → FreeTileChecker.IsFree(tile) → bool
      if NOT free: reject (visual feedback, no state change)
  → IModeStrategy.OnTileSelected(tile)
      if first tile: highlight, store selection
      if second tile:
  → MatchValidator.Validate(tileA, tileB, currentMode) → bool
      if MATCH:
  → BoardManager.RemovePair(tileA, tileB)
        → update TileData[].isMatched = true
        → FreeTileChecker.Rebuild() or incremental update
        → Pool TileView objects
  → ScoreManager.RegisterMatch(combo)
  → GameplayController.CheckWinCondition()
      if NO MATCH:
  → deselect, visual feedback
```

### 3. Hint Flow

```
User taps Hint button
  → GameplayController.RequestHint()
  → AdsManager.ShowRewardedAd(onSuccess: GrantHint)
  → [Ad completes] → HintSystem.FindValidPair(TileData[], FreeTileChecker)
      → iterate free tiles, test each pair with MatchValidator
      → returns (TileData a, TileData b) or null
  → BoardManager.HighlightTiles(a, b)   // flash visual
```

### 4. Score Flow

```
BoardManager fires event: OnPairMatched(int comboCount)
  → ScoreManager.AddScore(baseScore * comboMultiplier)
  → ScoreManager.IncrementCombo()
  → HUD listens to ScoreManager.OnScoreChanged → updates UI text
  → ScoreManager.OnLevelComplete → GameplayController.ShowResultScreen()
```

---

## Suggested Build Order

Build order follows dependency direction: pure data structures first, logic second, orchestration third, presentation last.

### Phase 1 — Data Structures (no dependencies)
1. `TileData` struct
2. `BoardLayout` ScriptableObject (tile position list)
3. `LevelConfig` ScriptableObject (difficulty params)
4. `ThemeConfig` ScriptableObject (sprites, colors)
5. `IModeStrategy` interface

**Why first:** Everything else depends on TileData. ScriptableObjects can be authored in the Editor immediately and iterated without code changes.

### Phase 2 — Pure Logic (depends on structs only)
6. `FreeTileChecker` — implement and unit-test in isolation
7. `MatchValidator` — implement and unit-test per mode (Classic, Math, ActiveMind)
8. `LevelGenerator` — reverse-generation algorithm; unit-test solvability guarantee

**Why second:** These are pure C# with no MonoBehaviour dependency. Testable without running Unity. Getting these correct before wiring them to the board prevents hard-to-debug regressions later.

### Phase 3 — Board Core (depends on Phase 1 + 2)
9. `BoardManager` — build board from TileData[], manage tile pool
10. `TileView` — single tile MonoBehaviour, click event, visual states
11. Basic `GameplayController` — wires input to BoardManager, no modes yet
12. `ClassicMode` implementing `IModeStrategy` — first playable loop

**Why third:** You get a playable loop (tap, match, remove) before adding any ancillary systems. Lets you validate board feel and rendering performance early.

### Phase 4 — Remaining Modes + Score
13. `MathMode`
14. `ActiveMindMode` (hardest — adds timer, concealment state)
15. `ScoreManager` + combo logic
16. `HUD` — timer, score, combo display

### Phase 5 — Meta Systems
17. `HintSystem`
18. `AdsManager` (Unity Ads SDK integration)
19. `MainMenu`, `ModeSelect`, `ResultsScreen` scenes
20. Level progression persistence (PlayerPrefs or ScriptableObject-backed save)

### Phase 6 — Polish
21. `ThemeConfig` hot-swap at runtime
22. Sound/haptics
23. Analytics events

---

## Unity-Specific Architecture Patterns

### Pattern 1: ScriptableObject as Data Container (NOT Service Locator)
**What:** BoardLayout, LevelConfig, ThemeConfig are pure data assets. They hold no runtime state and no MonoBehaviour logic.
**Why:** ScriptableObjects survive scene loads. They can be swapped in the Inspector without code changes. They're serializable and editable by a designer without touching C#.
**Avoid:** Putting mutable runtime state (current score, active tiles) into ScriptableObjects — this causes state bleed between Editor play sessions and is a common Unity pitfall.

### Pattern 2: Object Pool for TileViews
**What:** Pre-instantiate a fixed pool of TileView GameObjects at level load. Activate/deactivate rather than Instantiate/Destroy.
**Why:** Mahjong boards have 144 tiles. Instantiate/Destroy 144 times on level start causes frame spikes. On low-end Android (target: 2GB RAM devices), GC pressure from repeated instantiation causes hitches mid-game.
**Implementation:** `BoardManager` owns the pool. Size pool to max tile count in any `BoardLayout` asset.

### Pattern 3: Pure C# Logic Classes, MonoBehaviour as Thin Shell
**What:** `FreeTileChecker`, `MatchValidator`, `LevelGenerator`, all mode classes are plain C# with no Unity dependencies.
**Why:** Unit-testable without Unity Test Runner's slower play-mode tests. Edit-mode tests run in milliseconds. Faster iteration on the core puzzle loop.
**Where the line is:** MonoBehaviour is only justified when you need Unity lifecycle hooks (Awake, Start, Update) or coroutines. `GameplayController` needs Update for the combo timer — it's a MonoBehaviour. `FreeTileChecker` never needs Update — it's a plain class.

### Pattern 4: Event-Driven HUD Updates
**What:** ScoreManager exposes C# events (`OnScoreChanged`, `OnComboChanged`). HUD components subscribe.
**Why:** Avoids `GameplayController` needing a reference to every UI element. Adding a new HUD widget (e.g., an animated combo number) requires zero changes to game logic.
**Implementation:** Use `System.Action<T>` events, not UnityEvents for internal gameplay signals (UnityEvent has serialization overhead and Inspector coupling). Reserve UnityEvents for Designer-wired UI buttons.

### Pattern 5: Strategy Pattern for Modes
**What:** `IModeStrategy` interface; `GameplayController` holds one instance at a time.
**Why:** Adding a fourth mode (e.g., TimeAttack) requires zero changes to `GameplayController`, `BoardManager`, or `MatchValidator`. It's a new class implementing `IModeStrategy`.
**Avoid:** Switch statements on mode enum scattered across multiple classes — this is the most common architecture mistake in mobile puzzle games and leads to bugs when adding modes.

### Pattern 6: Reverse-Generation for Guaranteed Solvability
**What:** `LevelGenerator` builds the board by placing matched pairs into valid (free) positions in reverse, then shuffles.
**Why:** Forward generation (random placement, post-hoc solvability check) requires expensive backtracking and can still produce unsolvable boards at high tile counts. Reverse generation is O(n) and always produces solvable boards.
**Risk:** Reverse generation can produce boards with obvious patterns (pairs clumped near top layers). A shuffle pass after generation is required to break visual predictability.

---

## Component Dependency Graph

Arrows indicate "depends on" direction.

```
TileData struct          (no deps)
BoardLayout SO           (no deps)
LevelConfig SO           (no deps)
ThemeConfig SO           (no deps)
IModeStrategy interface  (no deps)

FreeTileChecker    ← TileData
MatchValidator     ← TileData, IModeStrategy
LevelGenerator     ← TileData, BoardLayout SO, LevelConfig SO, FreeTileChecker

ClassicMode        ← IModeStrategy, TileData
MathMode           ← IModeStrategy, TileData
ActiveMindMode     ← IModeStrategy, TileData

TileView           ← TileData, ThemeConfig SO
BoardManager       ← TileData, FreeTileChecker, TileView (pool)
ScoreManager       ← LevelConfig SO (for scoring params)
HintSystem         ← FreeTileChecker, MatchValidator
AdsManager         ← (Unity Ads SDK, no game deps)

GameplayController ← BoardManager, ScoreManager, HintSystem,
                     AdsManager, IModeStrategy, LevelGenerator

HUD                ← ScoreManager (events), GameplayController (events)
MainMenu           ← (scene nav only)
ModeSelect         ← LevelConfig SO assets (list), GameplayController
```

No circular dependencies. The dependency graph is a DAG.

---

## Scalability Considerations

| Concern | At 1 level | At 50 levels | At 200 levels |
|---------|-----------|--------------|---------------|
| Level data storage | Single LevelConfig SO | One SO per level, bundle in addressable group | Addressable asset bundles, download-on-demand |
| Board variety | 1 BoardLayout | 5-10 layouts reused with different configs | Procedural generation from a layout grammar |
| Tile pool size | Fixed 144 | Max across all layouts (e.g., 180) | Dynamic pool resize on level load |
| Mode count | 3 | 3 | Add modes via IModeStrategy, zero refactor |
| Save state | PlayerPrefs (level unlock) | PlayerPrefs still fine | SQLite via Unity.SQLite or cloud save |

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: God Controller
**What:** `GameplayController` grows to contain match logic, score math, ad calls, hint search.
**Why bad:** Untestable, modification risk for every feature, merge conflicts in team settings.
**Instead:** Each concern is its own class. GameplayController only orchestrates — it calls other systems' public methods.

### Anti-Pattern 2: MonoBehaviour on TileData
**What:** Making the tile data a MonoBehaviour (TileData : MonoBehaviour).
**Why bad:** Cannot use in unit tests without a GameObject. Cannot use in LevelGenerator (pure C#). GC pressure from 144 MonoBehaviour instances.
**Instead:** TileData is a pure C# struct. TileView is the MonoBehaviour wrapper that renders one TileData.

### Anti-Pattern 3: FindObjectOfType in Gameplay Code
**What:** Using `FindObjectOfType<ScoreManager>()` or `FindObjectOfType<AdsManager>()` in tight loops.
**Why bad:** O(n) scene graph search. Called on match events (potentially 72 times per level). Causes hitches on complex scenes.
**Instead:** Wire references via Inspector serialized fields or a lightweight service locator initialized at scene load.

### Anti-Pattern 4: Storing Mutable State in ScriptableObjects
**What:** Writing the player's current score or active tile selections into a ScriptableObject field.
**Why bad:** State persists between Editor play sessions. In builds, multiple levels running sequentially share corrupted state.
**Instead:** ScriptableObjects hold only design-time data. Runtime state lives in MonoBehaviour instances that are destroyed with the scene.

### Anti-Pattern 5: Solvability Check After Random Generation
**What:** Random tile placement → check if solvable → retry if not.
**Why bad:** At 144 tiles and 3 layers, retry rate can exceed 80%. Level load takes multiple seconds on low-end devices.
**Instead:** Reverse-generation algorithm guarantees solvability in a single O(n) pass.

---

## Sources

- Unity ScriptableObject architecture: training data, Unity documentation patterns (pre-August 2025). Confidence: MEDIUM.
- Strategy pattern for mobile game modes: common pattern in Unity mobile game development, multiple sources in training data. Confidence: HIGH.
- Object pooling for tile-heavy puzzle games: Unity official Object Pooling documentation (stable guidance). Confidence: HIGH.
- Reverse-generation solvability: standard algorithm discussed in Mahjong Solitaire game development communities and algorithm forums. Confidence: MEDIUM.
- FreeTileChecker algorithm (no tile above, one open side): standard Mahjong Solitaire free-tile definition. Confidence: HIGH.
- WebSearch unavailable during this research session. All claims are from training data (cutoff August 2025). Findings flagged as MEDIUM confidence where verification against current Unity documentation would strengthen confidence.
