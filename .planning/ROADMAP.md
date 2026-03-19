# Roadmap — Vita Mahjong Number

## Overview

Vita Mahjong Number is built in six phases that follow the natural dependency chain of the project: pure data structures and testable logic first, then a playable board loop, then additional game modes, then meta systems, then monetization, then content and platform compliance, and finally polish and pre-launch preparation. Every v1 requirement maps to exactly one phase. No phase completes without observable, testable criteria being satisfied.

Depth: comprehensive. Total v1 requirements: 57.

---

## Progress

| Phase | Name | Goal | Status | Requirements |
|-------|------|------|--------|--------------|
| 1 | Logic Foundation | Pure C# core is correct and fully unit-tested | Pending | CORE-01–08, GEN-01–05, PLATFORM-03 |
| 2 | Board Rendering and First Playable | Classic Mode is playable end-to-end on device | Pending | MODE-C-01–02, THEME-02–04, PLATFORM-01–02 |
| 3 | Additional Modes and Combo | All three game modes are playable; combo system is live | Pending | MODE-M-01–03, MODE-A-01–04, COMBO-01–04 |
| 4 | Meta Systems, Scoring, and UI | Game has a complete product shell with progression, scoring, and menus | Pending | PROG-01–05, SCORE-01–04, HINT-01–03, UI-01–06 |
| 5 | Monetization | Ads are integrated and non-intrusive; hint/extra-move flow is complete | Pending | ADS-01–06 |
| 6 | Content, Platform Compliance, and Polish | Game meets Play Store requirements; Indian theme is complete; performance is verified | Pending | THEME-01, PLATFORM-01–02 (compliance pass) |

---

## Phase 1 — Logic Foundation

**Goal:** Pure C# game logic is provably correct through unit tests before any Unity scene is involved.

**Dependencies:** None. This is the foundation for all other phases.

**Requirements:**
- CORE-01: Free tile visual state is determinable from data alone
- CORE-02: FreeTileChecker correctly identifies a free tile (no tile above AND open on left OR right)
- CORE-03: Tile selection and deselection via tap
- CORE-04: Matched pair removal with match animation trigger signal
- CORE-05: Blocked tile input rejection
- CORE-06: No-moves-left detection
- CORE-07: Win state detection (all tiles cleared)
- CORE-08: Undo last match (restore both tiles)
- GEN-01: Reverse-generation guarantees at least one valid solution
- GEN-02: BFS post-validation pass with retry cap (max 10 retries)
- GEN-03: BoardLayoutSO defines valid tile positions (x, y, z)
- GEN-04: Tile values assigned to free pairs during generation (never random)
- GEN-05: Board generation completes in under 500ms on 2017-era Android
- PLATFORM-03: Unity project uses Unity 6 LTS with URP 2D Renderer

**Success Criteria:**

1. All 8 neighbor combinations for FreeTileChecker pass unit tests: a tile with no above-tile and at least one open side is free; all other combinations return blocked.
2. LevelGenerator produces a board where BFS traversal finds at least one complete solution path for 100 consecutive randomly seeded boards.
3. No-moves-left detection correctly returns true when zero valid free matching pairs exist, using only TileData state and the active matching rule (Classic rules at this phase).
4. Win state detection correctly returns true when the TileData array contains zero non-removed tiles.
5. Undo restores both removed tiles to their exact prior state with the correct free/blocked status recomputed.

---

## Phase 2 — Board Rendering and First Playable

**Goal:** A player can load the app, see a rendered board, select and match tiles in Classic Mode, and complete or lose a level — all on a physical Android device.

**Dependencies:** Phase 1 (FreeTileChecker, LevelGenerator, TileData must be correct before rendering them).

**Requirements:**
- MODE-C-01: Player can match any two free tiles with identical number values
- MODE-C-02: Non-identical free tiles cannot be matched
- THEME-02: Free tiles are visually distinguished from blocked tiles (glow or highlight)
- THEME-03: Match removal animation plays on both tiles (fade out + particle effect)
- THEME-04: Board fits portrait orientation on all common Android screen sizes (16:9 to 20:9)
- PLATFORM-01: Game builds and runs on Android (minSDK 21, ARM64, IL2CPP)
- PLATFORM-02: Game runs at stable 60fps during board interaction on mid-range Android (2GB RAM, ~2017 GPU)

**Success Criteria:**

1. A player can tap a free tile, see it highlighted as selected, tap a second free matching tile, and watch both tiles disappear with a fade-and-particle animation — all without a frame hitch visible to the eye.
2. A player cannot select or match a blocked tile; tapping a blocked tile produces no selection state change.
3. The board renders correctly in portrait mode on five aspect ratios (16:9, 18:9, 19.5:9, 20:9, 4:3) with no tile overlap and no tiles clipped outside the screen boundary.
4. A level can be played from start to win or lose state end-to-end on a physical mid-range Android device (ARM64 IL2CPP build) without a crash.
5. Frame rate measured with Unity Profiler stays at or above 60fps during all tile interaction sequences on the target device.

---

## Phase 3 — Additional Modes and Combo

**Goal:** All three game modes (Classic, Math Mode, Active Mind) are fully playable, and the combo system is active across all modes.

**Dependencies:** Phase 2 (board rendering and tile interaction must be working before layering mode logic on top).

**Requirements:**
- MODE-M-01: Player can match any two free tiles whose values sum to exactly 10
- MODE-M-02: Two free tiles that do not sum to 10 cannot be matched
- MODE-M-03: Math Mode uses the same board generation and free-tile logic as Classic Mode
- MODE-A-01: Observation phase shows all tiles face-up for a configurable period (default 5 seconds)
- MODE-A-02: Recall phase flips tiles face-down after observation
- MODE-A-03: Matched tiles are removed normally; unmatched attempts provide error feedback
- MODE-A-04: Active Mind Mode uses Classic matching rules during recall phase
- COMBO-01: Consecutive matches within 3 seconds increment a combo counter
- COMBO-02: Combo counter resets if 3 seconds pass without a match
- COMBO-03: Combo multiplier applies to score per match
- COMBO-04: Super Combo visual effect triggers at combo count >= 3

**Success Criteria:**

1. In Math Mode, a player can successfully match tile pairs that sum to 10 (e.g., 3+7, 4+6) and cannot match pairs that do not sum to 10 — no-moves detection correctly identifies deadlock using Math Mode pairing rules, not Classic rules.
2. In Active Mind Mode, tiles are face-up during the observation phase for the full configured duration, then flip face-down; matched tiles are removed and incorrect matches show error feedback.
3. The active mode can be swapped at runtime (via IModeStrategy substitution) without reloading the scene and without state leaking between modes.
4. The combo counter increments on back-to-back matches within 3 seconds and resets correctly after a 3-second pause; the Super Combo particle effect triggers when combo reaches 3.
5. No-moves detection is verified for Math Mode boards: a board with only tiles whose remaining values cannot sum to 10 correctly triggers the no-moves state.

---

## Phase 4 — Meta Systems, Scoring, and UI

**Goal:** The game has a complete product shell — menus, level progression, scoring, hints, win/lose screens — and a player can navigate the full game loop from main menu to level completion and back.

**Dependencies:** Phase 3 (all modes must be playable before meta systems can be wired to them).

**Requirements:**
- PROG-01: Levels procedurally generated with LevelConfig ScriptableObject (tile count, layer depth, hint count)
- PROG-02: Difficulty increases with each level (more tiles, more layers, fewer hints)
- PROG-03: Completing a level unlocks the next level
- PROG-04: Current level number displayed on HUD during gameplay
- PROG-05: Player's highest completed level persisted locally (PlayerPrefs)
- SCORE-01: Player earns points per match; score displayed on HUD
- SCORE-02: Final score shown on win/lose screen
- SCORE-03: High score per level stored locally and shown on menu/level-select
- SCORE-04: Cumulative total score tracked across sessions
- HINT-01: Player can request a hint that highlights one valid free matching pair
- HINT-02: Each level starts with a limited number of free hints (defined by LevelConfig)
- HINT-03: Additional hints unlocked by watching a rewarded ad (ad integration wired in Phase 5; stub here)
- UI-01: Main menu with mode selection (Classic, Math Mode, Active Mind)
- UI-02: HUD displays score, combo counter, level number, hint count, timer
- UI-03: Win screen with final score, stars earned, next level button
- UI-04: Lose screen with current score, restart button, hint ad button
- UI-05: Android back button pauses the game and shows a pause menu
- UI-06: Pause menu has resume and quit options

**Success Criteria:**

1. A player can navigate from the main menu, select a game mode, play a level to completion, see their score on the win screen, and have the next level unlocked — across a full app restart (progression persists in PlayerPrefs).
2. The HUD displays the correct score (updated after each match), current combo counter, level number, and remaining hint count at all times during gameplay.
3. The hint system highlights exactly one valid free matching pair when requested; hint count decrements by one; requesting a hint at zero free hints shows a prompt that will later trigger a rewarded ad (stub behavior acceptable in this phase).
4. Difficulty scaling is observable: Level 1 boards have fewer tiles and more available hints than Level 10 boards, with values determined by LevelConfig ScriptableObject.
5. The Android back button during gameplay pauses the game (Time.timeScale = 0) and shows a pause menu with working resume and quit buttons.

---

## Phase 5 — Monetization

**Goal:** Unity Ads are fully integrated; rewarded ads gate hints and extra moves; interstitial ads play between levels; GDPR consent flow is displayed on first launch.

**Dependencies:** Phase 4 (hint system stub and level transition flow must exist before ads can be wired to them).

**Requirements:**
- ADS-01: Unity Ads SDK initialized asynchronously at app launch with 5-second timeout and graceful degradation
- ADS-02: Rewarded ad plays when player requests a hint beyond free allowance
- ADS-03: Rewarded ad plays when player requests extra moves after "no moves left"
- ADS-04: Interstitial ad shown between levels (after win/lose screen, before next level loads)
- ADS-05: Ads never shown mid-gameplay (only on transition screens)
- ADS-06: UMP consent dialog displayed on first launch (GDPR compliance)

**Success Criteria:**

1. With the device in airplane mode, the app launches, the ads SDK initialization times out gracefully within 5 seconds, and the game proceeds to the main menu without hanging or crashing; no ad placements are shown.
2. When a player requests a hint beyond their free allowance, a rewarded ad plays to completion and the hint is granted; if the ad is skipped or unavailable, the hint is not granted.
3. When a player hits the "no moves left" state and requests extra moves, a rewarded ad plays and extra moves are granted on completion.
4. An interstitial ad is shown after the win/lose screen exactly once per level transition; no ad is shown mid-gameplay during active tile interaction.
5. On a fresh install (cleared app data), the UMP consent dialog appears before any ad is requested and before the main menu is navigable.

---

## Phase 6 — Content, Platform Compliance, and Polish

**Goal:** The game is release-ready: Indian cultural art assets are applied across all surfaces, the Android build passes Play Store technical requirements, performance is verified on a target device, and final polish (particles, haptics, audio, localization strings) is complete.

**Dependencies:** Phase 5 (full game loop including ads must be stable before final content and profiling pass).

**Requirements:**
- THEME-01: All tile art, backgrounds, and UI decorations use an Indian cultural aesthetic (rangoli, mandala, temple motifs)

*Note: PLATFORM-01 (Android build) is first achieved in Phase 2 as a development target; this phase delivers the release-configuration Android build (AAB, signed, UMP wired, test flags stripped). PLATFORM-02 (60fps on mid-range) is first measured in Phase 2; this phase delivers the final profiling pass and confirmation.*

**Success Criteria:**

1. Every tile, background, and UI decoration surface in the game uses Indian cultural visual assets (rangoli, mandala, or temple motifs); no placeholder or generic assets remain in the release build.
2. A signed Android App Bundle (AAB) builds successfully with ARM64 IL2CPP, minSDK 21, targeting SDK 34+, with all test-mode flags confirmed absent (via `#if DEVELOPMENT_BUILD` guard verified in CI or manual grep).
3. Unity Profiler on a mid-range device (Snapdragon 450 or equivalent, 2GB RAM) shows sustained 60fps across board interaction, combo particle effects, and level transitions with peak memory usage below 400MB.
4. Match removal particle effects, Super Combo screen flash, and tap haptics (if implemented) play correctly during a full level session without causing a visible frame drop.
5. All player-visible UI strings are sourced from a localization string table (English locale), with no hardcoded text in scene objects — enabling future locale additions without code changes.

---

## Coverage Map

| Requirement | Phase |
|-------------|-------|
| CORE-01 | Phase 1 |
| CORE-02 | Phase 1 |
| CORE-03 | Phase 1 |
| CORE-04 | Phase 1 |
| CORE-05 | Phase 1 |
| CORE-06 | Phase 1 |
| CORE-07 | Phase 1 |
| CORE-08 | Phase 1 |
| GEN-01 | Phase 1 |
| GEN-02 | Phase 1 |
| GEN-03 | Phase 1 |
| GEN-04 | Phase 1 |
| GEN-05 | Phase 1 |
| PLATFORM-03 | Phase 1 |
| MODE-C-01 | Phase 2 |
| MODE-C-02 | Phase 2 |
| THEME-02 | Phase 2 |
| THEME-03 | Phase 2 |
| THEME-04 | Phase 2 |
| PLATFORM-01 | Phase 2 |
| PLATFORM-02 | Phase 2 |
| MODE-M-01 | Phase 3 |
| MODE-M-02 | Phase 3 |
| MODE-M-03 | Phase 3 |
| MODE-A-01 | Phase 3 |
| MODE-A-02 | Phase 3 |
| MODE-A-03 | Phase 3 |
| MODE-A-04 | Phase 3 |
| COMBO-01 | Phase 3 |
| COMBO-02 | Phase 3 |
| COMBO-03 | Phase 3 |
| COMBO-04 | Phase 3 |
| PROG-01 | Phase 4 |
| PROG-02 | Phase 4 |
| PROG-03 | Phase 4 |
| PROG-04 | Phase 4 |
| PROG-05 | Phase 4 |
| SCORE-01 | Phase 4 |
| SCORE-02 | Phase 4 |
| SCORE-03 | Phase 4 |
| SCORE-04 | Phase 4 |
| HINT-01 | Phase 4 |
| HINT-02 | Phase 4 |
| HINT-03 | Phase 4 |
| UI-01 | Phase 4 |
| UI-02 | Phase 4 |
| UI-03 | Phase 4 |
| UI-04 | Phase 4 |
| UI-05 | Phase 4 |
| UI-06 | Phase 4 |
| ADS-01 | Phase 5 |
| ADS-02 | Phase 5 |
| ADS-03 | Phase 5 |
| ADS-04 | Phase 5 |
| ADS-05 | Phase 5 |
| ADS-06 | Phase 5 |
| THEME-01 | Phase 6 |

**Total mapped: 57 / 57 — 100% coverage.**

---

*Last updated: 2026-03-19*
