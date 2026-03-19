# Research Summary: Vita Mahjong Number

**Synthesized:** 2026-03-19
**Sources synthesized:** STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md
**Consumed by:** gsd-roadmapper

---

## Executive Summary

Vita Mahjong Number is a casual Android puzzle game built on Mahjong Solitaire mechanics, differentiated by number-based tiles (replacing Chinese symbols), an Indian cultural theme, and Math Mode (sum-to-10 pairing). The genre is well-understood and the feature set is achievable with Unity 6 LTS using established patterns: reverse-generation for guaranteed-solvable boards, the Strategy pattern for multiple game modes, ScriptableObjects for data configuration, and IL2CPP/ARM64 for Play Store compliance. The tech stack is straightforward and low-risk; the most significant engineering challenges are concentrated in board generation correctness and multi-mode interaction logic.

The recommended approach is a six-phase build order that starts with pure data structures and logic (fully unit-testable before any scene work), establishes a playable Classic Mode loop early, then layers in Math Mode, scoring, monetization, and polish. Active Mind Mode — the highest-complexity differentiator — should be deferred to v1.1 to reduce QA risk for the initial launch. The Indian cultural theme and number tiles are low-cost to execute once art assets exist, and the combo system is a high-engagement, low-complexity addition that should ship in v1.

The primary risks are not technical unknowns but well-documented execution pitfalls: free-tile detection logic inversion, reverse-generation deadlock on dense boards, ScriptableObject mutable state corruption, and Unity Ads SDK initialization blocking on slow networks. All four are preventable with unit tests and disciplined architecture rules established in Phase 1. The overall confidence in this research is MEDIUM-HIGH: core Unity patterns are stable and well-documented, genre feature expectations are consistent across comparable titles, but specific SDK versions and market positioning claims were not verifiable against live sources due to WebSearch being unavailable during research.

---

## Key Findings

### From STACK.md

**Core technologies:**

| Technology | Rationale |
|------------|-----------|
| Unity 6 LTS (6000.0) | Current production LTS; 2-year support window; avoids forced mid-project migration from 2022.3 LTS which enters end-of-support in 2025 |
| URP 2D Renderer (17.x) | Correct pipeline for 2D mobile; Built-in is legacy; GPU instancing on tile sprites keeps draw calls under 50 on 2017-era GPUs |
| IL2CPP + ARM64 | Mandatory for Google Play 64-bit requirement; better runtime performance than Mono; use Mono only for editor iteration |
| Unity Ads SDK 4.x (LevelPlay) | Single SDK covers rewarded + interstitial; bundles UMP consent flow for GDPR compliance; avoid adding AdMob in v1 |
| UI Toolkit (1.x) | Flexbox layout handles Android aspect ratio variety without per-device anchoring; strategic direction for Unity UI |
| Unity built-in audio | Sufficient for < 20 files and 1-2 music tracks; FMOD/Wwise adds 10-15MB APK weight and licensing cost for no meaningful benefit |
| Unity Test Framework (UTF) | Edit Mode tests on pure C# logic run without scene spin-up; critical for TDD on board solver and matching rules |
| Unity Addressables (2.x) | Required from day one for ThemeSO sprite atlases; prevents loading all level data into memory on 2GB RAM targets |

**Critical version notes (MEDIUM confidence — verify in Package Manager at project start):**
- Unity Ads: confirm package ID (`com.unity.ads` vs `com.unity.services.ads`) as Unity has reorganized Gaming Services packages
- UI Toolkit scroll views with dynamic content: verify against Unity 6 release notes before committing

**What to avoid:** Built-in Pipeline, FMOD, Moq (IL2CPP stripping incompatibility), Zenject/VContainer DI, Firebase full SDK, Cinemachine, Timeline.

---

### From FEATURES.md

**Table stakes — must ship in v1 (absence causes 1-star reviews):**

1. Solvable board guarantee with free-tile visual highlighting
2. Touch-to-select with responsive selection state
3. Undo (minimum 1 move)
4. Shuffle (limited or ad-gated)
5. Hint system (rewarded ad gate)
6. Win / Lose state feedback with score display
7. Timer or move counter (timer is simpler; pick one)
8. 10+ board layouts (procedural templates satisfy this)
9. Restart level button
10. Sound effects (tap, match, win)
11. Settings screen (sound/music toggle)
12. Interstitial ads between levels
13. Local high-score leaderboard per mode
14. Android back-button handling
15. Portrait lock with stable layout

**Differentiators — should ship in v1 (core positioning):**

- Math Mode (sum-to-10 pairing): unique in genre; primary differentiator; medium complexity
- Combo / Super Combo system: low complexity, high session engagement
- Indian cultural visual theme: blue ocean vs. East Asian aesthetic standard; requires authentic art
- Number-based tiles: accessibility win; eliminates Mahjong iconography barrier; already the premise

**Defer to v1.1:**
- Active Mind Mode (memory/recall): high complexity, high QA risk; validate v1 metrics first
- Daily puzzle mode: requires seeded RNG infrastructure
- Background variation per difficulty tier

**Anti-features (do not build for v1):**
- Online/async multiplayer, global real-time leaderboard, IAP consumables, subscription monetization, account/login system, story mode, social sharing, custom tile editors, Play Games achievement system, cloud save, animated 3D tile models, more than 3 game modes at launch.

**Feature dependency chain (critical path):**
Solvable board generation → Free tile detection → Hint system, Shuffle, Math Mode, Active Mind Mode. Everything downstream of free-tile detection is blocked until that logic is correct.

---

### From ARCHITECTURE.md

**Major components and ownership:**

| Component | Single Responsibility |
|-----------|----------------------|
| `GameplayController` | Input handling, mode lifecycle, win/lose flow — orchestrates only |
| `BoardManager` | TileData array, tile pool, board visual state |
| `FreeTileChecker` | Free-tile algorithm (pure C#, no MonoBehaviour) |
| `MatchValidator` | Pair validation delegated through active IModeStrategy |
| `LevelGenerator` | Guaranteed-solvable board via reverse-generation (pure C#) |
| `ScoreManager` | Score, combo counter; exposes C# events for HUD subscription |
| `HintSystem` | Finds valid free pair; signals AdsManager |
| `AdsManager` | Unity Ads SDK lifecycle only; no game logic |
| `ClassicMode / MathMode / ActiveMindMode` | Each implements IModeStrategy; pure C# |

**Load-bearing design decision — IModeStrategy interface:**
```
IModeStrategy.IsValidMatch(TileData a, TileData b)
IModeStrategy.OnTileSelected(TileData tile)
IModeStrategy.OnMatchConfirmed(TileData a, TileData b)
IModeStrategy.OnLevelStart(LevelConfig config)
IModeStrategy.OnTick(float deltaTime)
IModeStrategy.GetHUDLabel()
```
All mode-specific logic flows through this interface. No-moves detection, hint search, and match validation must all call through `IModeStrategy` — not through mode-specific code paths. Swapping modes = swapping the strategy instance; no scene reload.

**Key patterns:**
- TileData is a pure C# struct (not MonoBehaviour): enables Edit Mode unit tests and avoids 144 MonoBehaviour GC instances
- Object pool for TileView GameObjects: sized to max tile count; zero allocations on level transition
- ScriptableObjects hold static config only (BoardLayoutSO, LevelConfigSO, ThemeConfigSO): no runtime mutable state
- Event-driven HUD: ScoreManager exposes C# events; HUD subscribes; GameplayController never references UI elements directly
- Reverse-generation for solvability: O(n) single-pass, always solvable; requires shuffle pass afterward to break visual pattern predictability

**Dependency graph is a clean DAG** — no circular dependencies. TileData struct and ScriptableObjects are the foundation with no upstream dependencies.

---

### From PITFALLS.md

**Top 5 critical pitfalls:**

**1. Reverse-generation deadlock** (Phase 1 — board generation)
Generator fills all slots before all pairs are placed. Prevention: always place both tiles of a pair in one pass; recompute free slots after each pair placement; cap retries at 50; add post-generation BFS validator before presenting board to player.

**2. Free-tile detection logic inversion** (Phase 1 — before any other mode is built on top)
`hasLeft || hasRight` coded instead of `!hasLeft || !hasRight`. Prevention: unit-test all 8 neighbor combinations exhaustively; name function `IsTileFree` (positive framing); add editor visual debug (green = free, red = blocked); add assertion in tile-selection handler.

**3. No-moves detection missing mode-specific pairing rules** (Phase 2-3)
Dead-end detection written for Classic Mode only; Math Mode and Active Mind Mode boards never correctly detect stuck state. Prevention: route no-moves detection through `IModeStrategy.IsValidMatch` — the same method the match handler calls.

**4. Unity Ads SDK blocking main thread** (Phase 4-5 monetization, but affects bootstrap scene)
Polling `Advertisement.isInitialized` without timeout hangs offline devices. Prevention: use `IUnityAdsInitializationListener` callback; 5-second timeout fallback; preload ads after gameplay starts, not before; test with airplane mode on physical device.

**5. ScriptableObject mutable state corruption** (Phase 1 architecture setup)
Storing current score, tile state, or hints on ScriptableObject fields causes state bleed between editor play sessions and build discrepancies. Prevention: strict rule — SOs hold static config only; enable "Reload Domain" in editor Play Mode Options; use `[NonSerialized]` on any runtime field if one must exist on an SO.

**Additional critical pitfalls to address by phase:**

| Phase | Pitfall | Prevention |
|-------|---------|------------|
| Phase 1 | Tile coordinate system mismatch (z-axis inversion on multi-layer boards) | `BoardCoordinates` static utility class; unit test layer blocking |
| Phase 1 | Double-tap artifact on mid-range Android (single tap fires two frame events) | `inputConsumedThisFrame` boolean; `selectedTileA != selectedTileB` guard |
| Phase 1 | Portrait layout breaking on non-16:9 aspect ratios | `CameraFitter` component; test 5 aspect ratios: 16:9, 18:9, 19.5:9, 20:9, 4:3 |
| Phase 1 | Android back button exits app | Intercept `KeyCode.Escape` from Phase 1 |
| Phase 2 | Tile GameObject GC spikes at level transition | `UnityEngine.Pool.ObjectPool<T>` from Unity 2021+; pool architecture before first level-load feature |
| Phase 2 | Interstitial ad breaking combo/timer state | Gate ads on `GameState.LevelTransition`; save score before ad trigger; `Time.timeScale = 1` on resume |
| Phase 2 | PlayerPrefs concurrent writes causing score corruption | `ScoreRepository` class; flush only at level complete, `OnApplicationPause(true)`, quit |
| Phase 3 | Active Mind Mode face state not reset between phases | Single `TileState` enum (FaceUp/FaceDown/Removed) as sole source of truth |
| Phase 4-5 | Test mode flag left in production build | `#if DEVELOPMENT_BUILD` wrapper; CI grep check pre-release build |

---

## Implications for Roadmap

The dependency graph and pitfall phase-warnings combine to produce a clear build order. Compress or expand phases based on team size, but do not reorder across the dependency boundary.

### Suggested Phase Structure

**Phase 1 — Data Structures and Core Logic Foundation**
Rationale: TileData struct and ScriptableObjects have no upstream dependencies. FreeTileChecker, MatchValidator, and LevelGenerator are pure C# — unit-testable without Unity editor involvement. All Phase 2+ work depends on these being correct. Bugs discovered here are cheap to fix; bugs discovered in Phase 4 require rewrites.

Delivers: TileData struct, IModeStrategy interface, BoardLayoutSO / LevelConfigSO / ThemeConfigSO, FreeTileChecker with full unit test suite (all 8 neighbor combinations), MatchValidator (Classic and Math mode rules), LevelGenerator with BFS post-validation, BoardCoordinates utility class.

Must avoid: Free-tile logic inversion (Pitfall 2), coordinate system mismatch (Pitfall 17), ScriptableObject mutable state (Pitfall 6).

Needs research: Standard patterns; no phase-level research required.

**Phase 2 — Board Core and First Playable Loop**
Rationale: BoardManager, TileView, and a thin GameplayController wired to ClassicMode give a playable loop before any ancillary system exists. Validates board feel, rendering performance, and tile pool architecture on real hardware before committing to final layouts.

Delivers: BoardManager with ObjectPool, TileView MonoBehaviour, GameplayController (Classic Mode only), touch input with `inputConsumedThisFrame` guard, Android back-button interception, portrait lock and CameraFitter for 5 aspect ratios, Undo (1 move).

Must avoid: Tile pooling not in place (Pitfall 8), double-tap artifact (Pitfall 7), layout breaking on non-16:9 (Pitfall 12).

Needs research: No new research; verify touch input behavior on physical mid-range Android device.

**Phase 3 — Remaining Game Modes and Score System**
Rationale: MathMode is a primary differentiator and must ship in v1. ScoreManager and combo logic are low-complexity but feed into HUD and win screen. Active Mind Mode is highest-complexity; build it here if timeline allows but gate shipping on QA pass — defer to v1.1 if not stable.

Delivers: MathMode (sum-to-10 via IModeStrategy), ScoreManager with combo logic and C# events, HUD (timer, score, combo display), Win/Lose state screens, Shuffle (ad-gated), no-moves detection routed through IModeStrategy. Active Mind Mode (face-down concealment, phase transition) — conditional on timeline.

Must avoid: No-moves detection missing mode-specific rules (Pitfall 3), Active Mind Mode state not reset between phases (Pitfall 9).

Needs research: Active Mind Mode UX patterns if building in this phase. Otherwise standard.

**Phase 4 — Meta Systems and Monetization**
Rationale: HintSystem depends on FreeTileChecker and AdsManager. AdsManager must be integrated before either rewarded or interstitial ads can be tested. ScoreRepository must precede leaderboard work. Level progression and menu scenes complete the product shell.

Delivers: HintSystem with lazy recompute pattern, AdsManager with async init and 5-second timeout, rewarded ads for hints, interstitial ads gated on GameState.LevelTransition, ScoreRepository (checkpoint-only flush), local leaderboard per mode, MainMenu / ModeSelect / ResultsScreen scenes, level progression persistence (PlayerPrefs unlock flags).

Must avoid: Ads blocking main thread (Pitfall 4), interstitial breaking combo/score state (Pitfall 5), PlayerPrefs concurrent writes (Pitfall 11), stale hint result (Pitfall 15).

Needs research: Verify current Unity Ads SDK package ID and initialization API against `docs.unity.com/ads` at integration time.

**Phase 5 — Content, Procedural Difficulty, and Platform Compliance**
Rationale: 10+ board layouts are table stakes. Procedural difficulty scaling needs BFS solution-path-length floor validation. Sound/music and cultural theme complete the differentiator identity. Android compliance items (back button already done, UMP consent flow) finalize Play Store readiness.

Delivers: 10-15 BoardLayoutSO assets, procedural difficulty with solution-path-length floor validation per level band, ThemeConfigSO hot-swap for Indian cultural theme (sprites, colors, music references), Indian cultural audio (sitar/tabla ambient tracks), Settings screen (sound/music toggle), UMP consent flow wired to Unity Ads init, `testMode` build define with CI check, AAB build configuration.

Must avoid: Difficulty scaling producing inconsistent curve (Pitfall 10), audio compression defaults bloating APK (Pitfall 20), test mode flag in production (Pitfall 14).

Needs research: Audio licensing for Indian cultural music — needs human decision before this phase starts.

**Phase 6 — Polish and Pre-Launch**
Rationale: Polish is last. Particle effects, haptics, analytics events, and localization infrastructure are non-blocking. Build size audit and performance profile on a mid-range device belong here.

Delivers: Shared particle system for match animations (capped at 50 particles), haptic feedback on match/win, Unity Analytics events (level completion funnel, mode usage), Unity Localization package wired with English string table, audio compression audit (Vorbis 70% music, ADPCM SFX), memory profiler pass on mid-range device (target < 400MB runtime), strip unused shader variants.

Must avoid: Per-tile particle system causing frame drops on combo sequences (Pitfall 18), hardcoded UI strings blocking future localization (Pitfall 19), uncompressed audio bloating APK (Pitfall 20).

Needs research: No additional research required.

---

### Research Flags

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 1 | Standard patterns — skip research | FreeTileChecker, reverse-generation, and SO architecture are well-documented |
| Phase 2 | Standard patterns — skip research | Object pooling, touch input handling are Unity standard guidance |
| Phase 3 | Conditional research needed | If building Active Mind Mode: research memory-game UX patterns for face-down reveal timing |
| Phase 4 | Verify at integration time | Unity Ads SDK package ID and init API must be verified against current docs before integration |
| Phase 5 | Human decision gate | Indian cultural music licensing requires a decision before audio asset work begins |
| Phase 6 | Standard patterns — skip research | Polish and profiling are well-understood |

---

## Confidence Assessment

| Area | Confidence | Basis |
|------|------------|-------|
| Stack selection | HIGH | Unity 6 LTS, URP 2D, IL2CPP/ARM64 mandate are stable and well-documented. Specific package version numbers are MEDIUM — verify in Package Manager at project start. |
| Table stakes features | MEDIUM | Based on genre survey from training data across 10+ comparable titles. Consistent; not live-verified. |
| Differentiator assessment | MEDIUM-HIGH | Math Mode and number tiles rarity in genre is HIGH confidence. Indian theme blue-ocean claim is MEDIUM — not live-verified against current Play Store catalog. |
| Architecture patterns | HIGH | Strategy pattern, object pooling, pure C# logic separation, event-driven HUD are stable Unity best practices independent of version. |
| Pitfall identification | HIGH (logic pitfalls), MEDIUM (SDK pitfalls) | Board logic pitfalls (Pitfalls 1-3) are well-documented puzzle game bugs. Unity Ads behavior (Pitfalls 4-5) tied to SDK version — verify against current docs. |
| Anti-features rationale | MEDIUM | F2P mobile design principles and Indian Android casual market assumptions are from training data; not live market data. |

**Overall confidence: MEDIUM-HIGH.** The architecture and technology decisions are solid. The feature and market assumptions are directionally correct but should be validated against live Play Store data and Unity documentation at project start.

---

## Gaps to Address

1. **Unity Ads SDK package ID and init API** — Verify `com.unity.ads` vs `com.unity.services.ads` and current `IUnityAdsInitializationListener` API before Phase 4. This is the highest-risk unverified technical item.

2. **UI Toolkit scroll view behavior in Unity 6** — Test any settings or level-select screen that uses dynamic-content scroll views early in Phase 2. Known area of partial runtime maturity.

3. **Indian cultural music licensing** — Needs a human decision: license existing sitar/tabla tracks, commission original compositions, or use royalty-free sources. Blocks Phase 5 audio work. Not a research question — a project management gate.

4. **Mid-range Android device testing** — Physical device testing on Snapdragon 450 or MediaTek Helio P22 class hardware (2017-era, 2GB RAM) is required in Phase 2. Emulator and high-end device testing will not reveal double-tap artifacts or GC hitch severity.

5. **Active Mind Mode UX validation** — Memory/recall layer on top of Mahjong Solitaire is novel in the genre. Face-down reveal timing and the observation-to-recall phase transition need playtest validation before committing to v1 scope.

---

## Sources

All research was conducted from training knowledge (cutoff August 2025). WebSearch and WebFetch were unavailable during all four research sessions. No live source verification was possible.

Verify at project start against:
- Unity release notes and Package Manager: https://unity.com/releases/editor/archive
- Unity Ads / LevelPlay docs: https://docs.unity.com/ads/
- Android 64-bit requirement: https://developer.android.com/google/play/requirements/64-bit
- Play Store casual game category (India): Google Play Store live data / AppMagic
- Comparable titles for table stakes validation: Microsoft Mahjong, Mahjong Solitaire Epic (Kristanix), Mahjong Journey (PlaySimple) — review mining on AppFollow or similar
