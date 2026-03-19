# Domain Pitfalls: Unity Mahjong Solitaire Mobile

**Domain:** Puzzle mobile game — Mahjong Solitaire, Unity C#, Android
**Researched:** 2026-03-19
**Confidence note:** WebSearch unavailable. All findings are from training knowledge (cutoff Aug 2025) of Unity mobile dev, Mahjong Solitaire game logic, and Android performance patterns. Confidence levels reflect this.

---

## Critical Pitfalls

Mistakes that cause rewrites, unsolvable boards at runtime, or app store rejections.

---

### Pitfall 1: Reverse-Generation Deadlock — Board Fills Before All Pairs Are Placed

**What goes wrong:** The reverse-generation algorithm (place pairs into free slots, fill the board) can reach a state where no free slots remain yet the tile count target has not been met. This happens when the algorithm greedily fills slots without reserving "opening paths" for later pairs. The result: an underfilled board, a crash, or an infinite retry loop.

**Why it happens:** Free slot availability is dynamic — placing a tile can block formerly free slots on adjacent positions. A greedy random-slot selection does not look ahead. Late in the fill pass, the only open slots may be isolated positions that cannot form a pair (no matching free slot exists for the second tile of a pair). The algorithm stalls with an odd number of remaining slots.

**Consequences:** Board generation fails silently and the game either crashes, presents a half-empty board, or hangs. If you add a "retry if failed" fallback without a cap, the retry loop becomes an infinite freeze on certain seeds.

**Prevention:**
- During reverse-generation, always place BOTH tiles of a pair in the same pass before moving to the next pair. Never place one tile and defer its partner.
- After placing each pair, immediately recompute the free-slot list. Do not cache free slots across pair placements.
- Add a generation attempt counter (max 50 retries per board). On failure, fall back to a simpler known-solvable seed rather than retrying infinitely.
- Write a post-generation validator: walk the board state forward using a BFS solver. If no valid path to empty board exists, the board is broken — reject and regenerate before presenting to the player.

**Detection / Warning signs:**
- Board sometimes appears with fewer tiles than expected in testing
- Occasional hang on level load (especially higher levels with more layers)
- Generation logs show retry counts climbing above 5 on certain level configs

**Phase:** Core gameplay (board generation), Phase 1 or 2.

---

### Pitfall 2: Free-Tile Detection Logic Inversion — "Blocked" Tiles Become Selectable

**What goes wrong:** The free-tile rule is: tile at (x, y, z) is free if (1) no tile at z+1 directly above it AND (2) no left neighbor OR no right neighbor. Condition 2 is a logical OR on the "empty" side, which is easy to invert. A common mistake is coding `hasLeft || hasRight` (blocked if either side is occupied) instead of `!hasLeft || !hasRight` (free if either side is open). Another mistake: "above" checks only the directly overhead position but forgets that large tiles or offset grids may have multiple overhead positions.

**Why it happens:** The natural-language description ("open on left OR right") maps awkwardly to boolean logic. Developers write the positive form and invert it incorrectly.

**Consequences:** Players can select tiles they should not be able to, or valid tiles are permanently un-selectable. Solvable boards become unsolvable at runtime. This is one of the hardest bugs to catch manually because many boards appear to work fine until a specific tile configuration triggers the inversion.

**Prevention:**
- Write the free-tile predicate as a pure function with a dedicated unit test suite. Test all 8 neighbor combinations exhaustively (no left/no right/both/neither, no above/above present).
- Name the function `IsTileFree(x, y, z)` — not `IsTileBlocked`. Positive framing reduces logic inversion errors.
- Visual debugging: in the editor, render free tiles with a green tint and blocked tiles with a red tint. Any playthrough where a "red" tile accepts input is a bug.
- Add an assertion: `Debug.Assert(IsTileFree(tile) == true)` inside the tile-selection handler. Assertions fire in editor builds and catch the inversion early.

**Detection / Warning signs:**
- Boards that should be solvable end with "no moves" prematurely
- Tiles in the middle of a stack are selectable before the top is cleared
- The hint system suggests a tile the player cannot visually identify as free

**Phase:** Core gameplay (tile logic), Phase 1. Fix before building any mode on top of it.

---

### Pitfall 3: "No Moves" Detection Missed Because Free-Pair Check Is Incomplete

**What goes wrong:** The "no moves left" detection iterates free tiles looking for a matching pair. In Math Mode (sum-to-10), every pair (1+9, 2+8, 3+7, 4+6, 5+5) must be checked. In Active Mind Mode, the hidden-tile state must be excluded from the free pair check during the recall phase. If the detection only checks Classic Mode pairs (identical numbers), Math Mode and Active Mind Mode boards will never correctly detect a dead end — the game will never offer the player a restart or hint option.

**Why it happens:** No-moves detection is written once for Classic Mode and assumed to work for all modes. The mode-specific pairing rules are not injected into the check.

**Consequences:** Math Mode or Active Mind Mode boards lock up with no input possible and no UI prompt. Players cannot progress. The only escape is force-quitting.

**Prevention:**
- Route the "is this a valid pair?" logic through the same strategy interface used for matching. The no-moves detector should call `GameMode.IsValidPair(tileA, tileB)` — the same method the match handler calls.
- Write one no-moves integration test per game mode that constructs a board state with zero valid moves and asserts the detection fires correctly.

**Detection / Warning signs:**
- Math Mode play sessions that appear to "freeze" with tiles still on the board
- QA testers reporting they cannot restart after getting stuck in Mode 2 or Mode 3

**Phase:** Game modes (Phase 2–3). Flag for testing in each mode.

---

### Pitfall 4: Unity Ads SDK Blocking the Main Thread on Load

**What goes wrong:** Unity Ads SDK initialization and ad preloading involve network calls. If `Advertisement.Initialize()` is called synchronously on startup and the network is slow or absent, it blocks or delays game startup. In Unity 2022+, the initialization is async, but if you poll `Advertisement.isInitialized` in a coroutine tight loop (`while (!isInitialized) yield return null;`) without a timeout, the game stalls indefinitely on offline devices.

**Why it happens:** Tutorial examples show simple synchronous-style polling. Developers copy the pattern without adding a timeout or fallback.

**Consequences:** Players on slow connections or offline devices experience a multi-second black screen or indefinite hang on launch. On some Android devices with strict doze mode, the initialization callback never fires.

**Prevention:**
- Always initialize ads with a timeout: if initialization has not completed within 5 seconds, proceed without ads (degrade gracefully — hide the "watch ad" button, do not block gameplay).
- Use the `IUnityAdsInitializationListener` callback interface rather than polling.
- Preload interstitial and rewarded ads after gameplay starts, not before. Players should be in the menu or mid-game before ads are prefetched — not waiting on a loading screen for it.
- Test explicitly with airplane mode enabled on a physical Android device.

**Detection / Warning signs:**
- Long black screen on first launch
- Game loads instantly in Unity Editor but hangs 3–5 seconds on device
- Crash reports showing null-reference exceptions in ad callback handlers

**Phase:** Monetization integration (Phase 4–5). Also affects Phase 1 if ads are initialized in the bootstrap scene.

---

### Pitfall 5: Interstitial Ads Shown During Active Gameplay, Destroying Combo / Timer State

**What goes wrong:** Interstitial ads are intended between levels. If the ad display trigger is tied to a loose event (e.g., "on level complete") and the level-complete event fires before combo timers and score animations finish, the ad interrupts mid-sequence. On Android, returning from an ad (Activity resume) does not restore Unity's `Time.timeScale` if it was modified by the combo timer pause logic. The combo timer continues counting during the ad.

**Why it happens:** Ad triggers are wired to high-level events without considering game-state at the moment of display. `Time.timeScale` bugs are common when pause/resume logic is spread across multiple systems.

**Consequences:** Players return from an ad to find their combo streak broken, timer expired, or score not properly tallied. Score persistence may record an incorrect value if the save also happens in the level-complete flow.

**Prevention:**
- Gate interstitial display: only show if `gameState == GameState.LevelTransition` and all score animations are complete. Use a simple state machine — not raw events — to control when ads are permissible.
- On `OnApplicationPause(false)` (Unity's resume callback), always reset `Time.timeScale = 1` explicitly.
- Save score to PlayerPrefs (or your persistence layer) before triggering the ad. Do not rely on post-ad callbacks to complete the save.

**Detection / Warning signs:**
- Score shows 0 after returning from an ad
- Combo timer is negative or reports "time out" immediately on ad dismiss
- Level unlocked but no score recorded in leaderboard

**Phase:** Monetization + progression integration (Phase 4–5).

---

### Pitfall 6: ScriptableObject Data Shared Across Play Sessions (Mutable Runtime State on Assets)

**What goes wrong:** Unity ScriptableObjects are asset files. If you store mutable runtime state — current score, tile board state, remaining hints — directly on a ScriptableObject field, those values persist between Play Mode sessions in the editor AND can corrupt the asset on disk in certain Unity versions. In a build, the values are reset on cold launch, but mid-session they accumulate state correctly — creating an editor/build discrepancy that is hard to reproduce.

**Why it happens:** ScriptableObjects are convenient shared containers. Developers add a `currentScore` field to a `LevelConfig` SO because it's already referenced everywhere. The first few tests pass because the value starts at 0.

**Consequences:** Level 1 shows a score from the previous editor playtest. Hint counts start at wrong values. Difficulty settings drift. The bug is intermittent and hard to reproduce because it only manifests after multiple editor runs without domain reload.

**Prevention:**
- Strict rule: ScriptableObjects hold only **static configuration** (tile counts, difficulty bands, ad placement settings). All **runtime state** lives in a separate runtime data class (plain C# class or MonoBehaviour) that is created fresh on scene load.
- Use `[NonSerialized]` on any field that represents runtime state if it must live on an SO for architectural reasons.
- Enable "Enter Play Mode Options > Reload Domain" in editor settings to catch this class of bug during development.

**Detection / Warning signs:**
- Score shows a non-zero value on a fresh game start in the editor
- Level difficulty appears wrong on first playthrough after opening Unity
- Hint count decrements from a number other than the configured maximum

**Phase:** Architecture setup (Phase 1). The rule must be established before any feature work starts.

---

### Pitfall 7: Touch Input Registering Multiple Tile Selections on a Single Tap (Double-Tap Artifact)

**What goes wrong:** On Android, `Input.GetMouseButtonDown(0)` (or `Input.touchCount` + `Touch.phase == TouchPhase.Began`) can fire on two consecutive frames for a single physical tap on certain mid-range devices with aggressive touch sampling. If tile selection logic runs on `Update()` without a per-frame consumed flag, one tap registers two tile selections: the first selects a tile, the second deselects it (or worse, matches it with itself).

**Why it happens:** PC testing uses mouse input which fires cleanly once. Android touch events are reported differently by the OS driver. Budget/mid-range chipsets (MediaTek, 2017-era Qualcomm) are more prone to multi-frame touch events.

**Consequences:** Tapping a tile appears to do nothing (selected then immediately deselected in the same logical frame). Players report unresponsive controls. In worst case, a tile "matches with itself" if match validation does not check `tileA != tileB`.

**Prevention:**
- Use `Touch.phase == TouchPhase.Began` only (not `Stationary` or `Moved`) and process exactly one selection event per `Update()` call using a boolean flag `inputConsumedThisFrame`.
- In the match handler, assert `selectedTileA != selectedTileB` before accepting a match. This is both a correctness guard and a direct prevention of self-match bugs.
- Test input on a physical mid-range Android device (not just emulator or high-end device) in Phase 1.

**Detection / Warning signs:**
- Tap on a tile does nothing visually (no selection highlight appears)
- Works correctly in Unity Editor but unreliable on device
- Two selection sounds play in rapid succession on a single tap

**Phase:** Input system (Phase 1). Must be solved before touch-dependent features are built.

---

### Pitfall 8: Memory Pressure from Tile GameObjects Not Being Pooled

**What goes wrong:** Each Mahjong board contains 72–144+ tile GameObjects. If tiles are destroyed and re-instantiated on every level load (`Destroy()` old board, `Instantiate()` new board), the garbage collector spikes on mid-range devices (2GB RAM targets). On Android, GC spikes manifest as 100–300ms hitches visible as frame drops at level transition. Over many levels, heap fragmentation accumulates.

**Why it happens:** `Instantiate/Destroy` is the natural Unity pattern. Pooling feels like premature optimization. It isn't for tile-heavy puzzle games.

**Consequences:** Visible stutter at level load. On devices with 2GB RAM and background processes, the stutter can be severe enough to cause an ANR (Application Not Responding) dialog if it exceeds 5 seconds — which triggers a Play Store rating penalty.

**Prevention:**
- Use an object pool for tile GameObjects from the start. Unity 2021+ has `UnityEngine.Pool.ObjectPool<T>` built in — use it. The pool is sized to the maximum tile count for any level config.
- On level load, release all tiles back to the pool (deactivate + reset state), then acquire from pool for the new board. Zero allocations, zero GC pressure.
- Profile with Unity's Memory Profiler on a mid-range device before shipping Phase 2 content.

**Detection / Warning signs:**
- Unity Profiler shows `GC.Collect` spikes above 10ms at level transitions
- Framerate drops to <30fps during level load on test device
- Increasing heap size in Memory Profiler across level transitions

**Phase:** Performance optimization. Pool architecture should be in place by Phase 2 (when level transitions first exist). Retrofitting pooling after full game is built is costly.

---

### Pitfall 9: Active Mind Mode — Tile Face-Up/Face-Down State Not Properly Reset Between Phases

**What goes wrong:** Active Mind Mode has two phases: observation (all tiles face-up) then recall (tiles face-down, player matches from memory). The face-up/face-down state is typically a material swap or sprite swap on the tile renderer. If the phase transition resets tile visual state but not the underlying data state (e.g., a `bool isFaceUp` field), the free-tile detection or match validation may still operate on the wrong state. Alternatively, if a tile is removed during the observation phase (due to a bug), its absence is not reflected in the recall phase board.

**Why it happens:** Visual state and data state are separate and the reset logic updates only one. Observation-to-recall is a mode-specific transition that is easy to miss in general-purpose reset methods.

**Consequences:** Tiles that were removed in observation phase appear as face-down tiles in recall phase. Players "match" non-existent tiles. Score accumulates incorrectly. Board ends in an inconsistent state.

**Prevention:**
- Maintain a single source of truth: `TileState` enum (`FaceUp`, `FaceDown`, `Removed`). The renderer reads from this enum — it never holds independent state.
- The phase transition method must iterate all tiles and call `tile.SetState(TileState.FaceDown)` — this updates both data and visual in one call.
- In recall phase, `Removed` tiles must be invisible AND non-interactable. Add an assertion: no `Removed` tile should ever receive a touch event.

**Detection / Warning signs:**
- Clicking where a removed tile was in observation phase produces a match in recall phase
- Board clears in recall phase faster than the number of visible tiles suggests is possible
- Score totals for Active Mind Mode are higher than mathematically possible

**Phase:** Active Mind Mode implementation (Phase 3).

---

### Pitfall 10: Procedural Difficulty Scaling Produces Unsolvable or Trivially Easy Boards at Extreme Levels

**What goes wrong:** LevelConfig ScriptableObjects define difficulty bands (tile count, layer depth, hint count). As levels escalate, increasing layer depth dramatically increases the probability that the reverse-generation algorithm produces boards with very long required solution chains (high difficulty) or trivially flat boards at low levels. Without a "solvability depth" metric, the procedural generator cannot distinguish between a board that is solvable in 5 moves versus one that requires a precise 70-step sequence.

**Why it happens:** Reverse-generation guarantees solvability but does not control difficulty. Tile count + layer depth are proxies for difficulty, not direct measures of it.

**Consequences:** Level 10 feels easier than Level 5 because the random seed produced a shallow board. Players complain of inconsistent difficulty curve. Retention drops.

**Prevention:**
- After generation, compute a "solution path length" using a greedy BFS solver. If the shortest solution path is shorter than a minimum threshold for that level band, regenerate (up to the retry limit). This adds a measurable difficulty floor.
- Do not use layer count alone as the difficulty signal. Also vary the ratio of "exposed" pairs at generation start — fewer initially exposed pairs forces longer sequences.
- Cap maximum level-specific retry count at 100 to prevent hangs on extreme configurations.

**Detection / Warning signs:**
- Playtesters report Level 15 is easier than Level 8
- Average moves-to-solve does not increase monotonically across level bands in testing
- Generation retry count is high (>10) for certain level configs, indicating the config is over-constrained

**Phase:** Procedural generation + difficulty (Phase 2). Needs dedicated playtesting milestone.

---

### Pitfall 11: PlayerPrefs Score Persistence Corrupted by Concurrent Writes

**What goes wrong:** PlayerPrefs is Unity's simple key-value store. It is not thread-safe and is not designed for frequent writes. In Vita Mahjong Number, score updates happen during combo sequences (potentially several per second), and `PlayerPrefs.Save()` is a synchronous disk write. Calling `PlayerPrefs.Save()` on every score increment causes micro-stutters on Android where the write blocks the main thread (typically 2–8ms per call on flash storage). On some Android versions, concurrent writes (e.g., ad callback and game logic both calling Save) can corrupt the file.

**Why it happens:** `PlayerPrefs.Set*` is easy to call anywhere. Developers call `Save()` immediately after each set "to be safe."

**Consequences:** Score micro-stutters during combos. On rare occasions (device low-storage, write collision), PlayerPrefs data becomes unreadable and all scores reset to zero.

**Prevention:**
- Keep score in-memory during a session. Call `PlayerPrefs.Save()` only at well-defined checkpoints: level complete, app pause (`OnApplicationPause(true)`), app quit.
- Write a thin `ScoreRepository` class that queues writes and flushes on those checkpoints. The game never touches PlayerPrefs directly.
- For the leaderboard, store it as a JSON blob under a single PlayerPrefs key rather than N individual keys (reduces write surface area).

**Detection / Warning signs:**
- Frame spikes visible in Profiler during combo scoring
- Score resets to 0 after a device storage warning notification
- Android Logcat shows I/O exceptions during PlayerPrefs write

**Phase:** Score persistence (Phase 2). The `ScoreRepository` pattern should be in place before leaderboard work begins.

---

### Pitfall 12: Portrait Layout Breaking on Non-Standard Aspect Ratios

**What goes wrong:** The Mahjong board is a fixed 2D/3D grid. Unity's Canvas Scaler set to "Scale With Screen Size" handles common aspect ratios, but the board render area does not automatically reflow. On 19.5:9 (modern tall phones), the board may be clipped at the bottom. On 4:3 (older Android tablets that match your minSDK), the board may overflow the screen width. Many developers test only on 16:9 and 18:9 during development.

**Why it happens:** Canvas Scaler handles UI elements but the game board (a camera-rendered world-space grid) needs its own aspect-aware positioning logic. These are two separate layout systems.

**Consequences:** On non-16:9 devices, tiles are partially off-screen and un-tappable. Game is unshippable on a significant portion of Android devices.

**Prevention:**
- Use a `CameraFitter` component that adjusts orthographic camera size based on `Screen.safeArea` and the board's world-space bounding box. Recalculate on `Start()` and on `OnRectTransformDimensionsChange()`.
- Test against Unity's Game View with at least 5 aspect ratios: 16:9, 18:9, 19.5:9, 20:9, 4:3. Add them to Unity's Game View aspect list as a project standard.
- Keep the board's world-space width fixed; scale height to fit. Tile tap areas must be computed from world-space positions, not screen-space constants.

**Detection / Warning signs:**
- Bottom row of tiles is partially clipped in any non-16:9 Game View test
- Tiles in top-right quadrant do not respond to tap on certain screen sizes
- UI score panel overlaps the top tile row on tall phones

**Phase:** Rendering / layout (Phase 1 — catch before building any game content on top of broken layout).

---

## Moderate Pitfalls

---

### Pitfall 13: Combo Timer Using `Time.deltaTime` Accumulation Instead of Wall-Clock Elapsed

**What goes wrong:** If the combo timer uses `timer -= Time.deltaTime` in `Update()` and the game is paused (ad shown, home button pressed), `Time.timeScale = 0` pauses `Time.deltaTime`. When the game resumes, the timer picks up exactly where it left off — meaning a player who watches a rewarded ad gets the combo timer "frozen" for free, gaining an unfair advantage. Alternatively, if pause is implemented with a different mechanism that does NOT freeze `Time.deltaTime`, the combo expires during an ad the player was forced to watch.

**Prevention:**
- Use `Time.unscaledDeltaTime` for the combo timer. It is not affected by `timeScale`.
- On `OnApplicationPause(true)`, snapshot `Time.unscaledTime`. On resume, compute elapsed and deduct from combo timer.

**Phase:** Combo system (Phase 2).

---

### Pitfall 14: Unity Ads Test Mode Left Enabled in Production Build

**What goes wrong:** Unity Ads has a test mode flag. If `testMode: true` is left in the `Advertisement.Initialize()` call for the production build, real ad impressions are never served — revenue is zero. The game works perfectly in QA but earns nothing after launch.

**Prevention:**
- Use a build scripting define: `#if DEVELOPMENT_BUILD` wraps `testMode = true`. In Release builds, `testMode = false` is hardcoded.
- Add a pre-build checklist item: verify `testMode == false` in production. Or use a CI build step that greps for `testMode = true` and fails the production build.

**Phase:** Monetization (Phase 4–5).

---

### Pitfall 15: Hint System Revealing a Tile Pair That Is No Longer Valid After Board State Change

**What goes wrong:** The hint system computes a valid free pair, then displays a highlight. If the player taps a different tile between hint computation and highlight display (possible in Active Mind Mode or with fast input), the board state may have changed. The highlighted tiles may no longer be free or may no longer form a valid pair (especially in Math Mode where removing one tile changes available sums).

**Prevention:**
- Compute hints lazily: do not store a hint result across frames. Recompute on the frame the hint is displayed.
- Lock input for one frame after a match completes to allow board state to settle before a new hint is requested.

**Phase:** Hint system (Phase 3).

---

### Pitfall 16: Android Back Button Not Handled — Instant App Exit During Gameplay

**What goes wrong:** On Android, the hardware/gesture Back button triggers `Application.Quit()` by default in Unity if not intercepted. Players accidentally exit mid-game with no save prompt. This is a common Play Store review complaint.

**Prevention:**
- Override back button in `Update()` with `Input.GetKeyDown(KeyCode.Escape)`. Show a "Pause / Quit?" dialog instead of quitting immediately.
- On quit confirmation, flush score persistence before `Application.Quit()`.

**Phase:** Platform integration (Phase 1 or 2 — add before any QA testing begins).

---

### Pitfall 17: Tile Coordinate System Mismatch Between Generator and Renderer

**What goes wrong:** The board generator uses a logical coordinate system (column x, row y, layer z). The renderer positions tiles in Unity world space. If the coordinate mapping function has an off-by-one error (e.g., layer 0 is the top visual layer but the generator treats layer 0 as the bottom), the visual rendering is correct but the free-tile detection is inverted on the z-axis. The game appears to work for flat boards (z=0 only) but breaks on any multi-layer board.

**Prevention:**
- Define coordinate conventions once, in a static `BoardCoordinates` utility class, with XML doc comments. All systems (generator, renderer, free-tile checker) import from this single source of truth.
- Unit test: place a tile at layer 0 and one at layer 1 directly above. Assert the layer-0 tile is blocked. This test will catch the inversion immediately.

**Phase:** Architecture setup (Phase 1). Must be nailed before any content is built.

---

## Minor Pitfalls

---

### Pitfall 18: Particle Effects Causing Frame Drops on Match Animation

**What goes wrong:** Each tile match triggers a particle effect. With 10+ quick matches (combo sequence), 20+ simultaneous particle systems are active. On 2017-era mid-range GPUs, this causes visible frame drops from 60fps to 35fps.

**Prevention:**
- Use a single shared Particle System with `Emit(count)` rather than per-tile particle system instances. Cap active particles at 50 total.
- Profile particle GPU cost on a mid-range device (Snapdragon 450 or MediaTek Helio P22 class) before finalizing the effect.

**Phase:** Polish (Phase 3–4). Don't optimize prematurely, but budget for it.

---

### Pitfall 19: Localization-Hardcoded Strings in UI

**What goes wrong:** Indian cultural theme may lead to including Hindi/regional language strings in the UI. If strings are hardcoded in TextMeshPro components rather than in a localization table, adding any second language requires hunting through every UI prefab.

**Prevention:**
- Even for a single-language v1, route all UI strings through a `StringTable` dictionary keyed by ID. TextMeshPro components call `LocalizationManager.Get("score_label")`. Cost is minimal; future language support becomes trivial.

**Phase:** UI setup (Phase 1 or 2).

---

### Pitfall 20: Build Size Bloat from Uncompressed Audio

**What goes wrong:** Unity imports audio as uncompressed PCM by default unless Audio Import settings are changed. Background music tracks at uncompressed quality can be 10–20MB each. A game with 3 music tracks + 15 SFX can ship with 50MB of audio alone — significant for a casual mobile game targeting users on slow data connections.

**Prevention:**
- Set background music to `Vorbis` compression, quality 70%. Set SFX to `ADPCM` (low latency, acceptable quality).
- Check: every audio asset's import settings before the first production build. Use Unity's Audio Profiler to confirm memory footprint.

**Phase:** Production build preparation (Phase 5).

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Board generation | Deadlock on dense boards (Pitfall 1) | Post-generation BFS validator + retry cap |
| Tile interaction | Free-tile logic inversion (Pitfall 2) | Unit test all 8 neighbor combinations before Phase 2 |
| No-moves detection | Mode-specific pairing rules missing (Pitfall 3) | Wire through GameMode strategy interface |
| Multi-layer boards | Coordinate system mismatch (Pitfall 17) | BoardCoordinates utility class in Phase 1 |
| Input system | Double-tap artifact on mid-range Android (Pitfall 7) | Test on physical device in Phase 1 |
| ScriptableObjects | Mutable runtime state on assets (Pitfall 6) | Enforce config-only SO rule from Day 1 |
| Level transitions | Tile pooling not in place (Pitfall 8) | Pool architecture before first level-load feature |
| Score persistence | PlayerPrefs concurrent writes (Pitfall 11) | ScoreRepository pattern before leaderboard |
| Active Mind Mode | Face state not reset between phases (Pitfall 9) | Single TileState enum; Phase 3 |
| Combo system | Timer behavior across pause/ad (Pitfall 13) | unscaledDeltaTime from start |
| Unity Ads init | Main thread block on slow network (Pitfall 4) | Async init + 5-second timeout |
| Ads between levels | Interstitial breaks combo/score state (Pitfall 5) | State machine gate on ad display |
| Ads production build | Test mode left on (Pitfall 14) | Build define + CI grep check |
| Hint system | Stale hint result (Pitfall 15) | Lazy recompute on display frame |
| Android platform | Back button exits app (Pitfall 16) | Intercept KeyCode.Escape from Phase 1 |
| Aspect ratio | Board clipped on non-16:9 (Pitfall 12) | CameraFitter + 5 aspect ratio test matrix |
| Difficulty scaling | Inconsistent curve from reverse-generation (Pitfall 10) | Solution-path-length floor per level band |

---

## Sources

All findings are from training knowledge (cutoff Aug 2025). No external URLs available due to WebSearch being unavailable in this session. Confidence levels:

- Pitfalls 1–3 (board logic): HIGH — well-documented class of puzzle game bugs
- Pitfalls 4–5 (Unity Ads): MEDIUM — Unity Ads SDK behavior; verify against current Unity Ads documentation
- Pitfall 6 (ScriptableObject mutability): HIGH — documented Unity engine behavior
- Pitfall 7 (double-tap): MEDIUM — device-specific; verify on actual mid-range Android hardware
- Pitfalls 8, 11 (performance/persistence): HIGH — standard Unity mobile optimization knowledge
- Pitfalls 9–10 (Active Mind Mode, difficulty): MEDIUM — game-design reasoning; validate with playtests
- Pitfalls 12–20 (misc): MEDIUM — standard Unity mobile development knowledge

**Recommend verifying:** Unity Ads SDK initialization API (Pitfall 4) against current Unity Ads documentation at `docs.unity.com/ads` as the SDK has changed significantly between versions 3.x, 4.x, and the current mediation SDK.
