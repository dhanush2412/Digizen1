# Requirements — Vita Mahjong Number

## v1 Requirements

### CORE — Core Game Logic
- [ ] **CORE-01**: Player can see which tiles are "free" (highlighted/glowing) vs blocked at all times
- [ ] **CORE-02**: System correctly identifies a free tile: no tile directly above it AND (no left neighbor OR no right neighbor)
- [ ] **CORE-03**: Player can tap a free tile to select it; tapping again deselects it
- [ ] **CORE-04**: When two matching free tiles are selected, both are removed from the board with a match animation
- [ ] **CORE-05**: Player cannot select a blocked tile (input ignored or error feedback shown)
- [ ] **CORE-06**: System detects "no moves left" state and presents restart or hint option
- [ ] **CORE-07**: System detects win state (all tiles cleared) and shows win screen
- [ ] **CORE-08**: Player can undo the last match (restores both tiles)

### GEN — Board Generation
- [ ] **GEN-01**: Every generated board is guaranteed to have at least one valid solution (reverse-generation algorithm)
- [ ] **GEN-02**: Board generation includes a BFS post-validation pass with a retry cap (max 10 retries) to handle deadlock edge cases
- [ ] **GEN-03**: Board layout is defined by a ScriptableObject (BoardLayoutSO) specifying valid tile positions (x, y, z)
- [ ] **GEN-04**: Tile values (numbers 1–9) are assigned to free position pairs during generation, never placed randomly
- [ ] **GEN-05**: Board generation completes in under 500ms on a 2017-era Android device

### MODE-C — Classic Mode
- [ ] **MODE-C-01**: Player can match any two free tiles with identical number values
- [ ] **MODE-C-02**: Non-identical free tiles cannot be matched (input rejected)

### MODE-M — Math Mode
- [ ] **MODE-M-01**: Player can match any two free tiles whose values sum to exactly 10 (e.g., 3+7, 4+6, 1+9)
- [ ] **MODE-M-02**: Two free tiles that do not sum to 10 cannot be matched
- [ ] **MODE-M-03**: Math Mode uses the same board generation and free-tile logic as Classic Mode

### MODE-A — Active Mind Mode
- [ ] **MODE-A-01**: Observation phase: all tiles are shown face-up for a configurable period (default 5 seconds)
- [ ] **MODE-A-02**: Recall phase: tiles flip face-down after observation; player must match from memory
- [ ] **MODE-A-03**: Matched tiles are removed normally; unmatched attempts provide error feedback
- [ ] **MODE-A-04**: Active Mind Mode uses Classic matching rules (identical numbers) during recall phase

### COMBO — Combo System
- [ ] **COMBO-01**: Consecutive matches within 3 seconds of each other increment a combo counter
- [ ] **COMBO-02**: Combo counter resets if 3 seconds pass without a match
- [ ] **COMBO-03**: Combo multiplier applies to score per match (combo × base score)
- [ ] **COMBO-04**: "Super Combo" visual effect (particle burst + screen flash) triggers at combo ≥ 3

### PROG — Level Progression
- [ ] **PROG-01**: Levels are procedurally generated with a LevelConfig ScriptableObject (tile count, layer depth, hint count)
- [ ] **PROG-02**: Difficulty increases with each level: more tiles, more layers, fewer available hints
- [ ] **PROG-03**: Completing a level unlocks the next level
- [ ] **PROG-04**: Current level number is displayed on the HUD during gameplay
- [ ] **PROG-05**: Player's highest completed level is persisted locally (PlayerPrefs)

### SCORE — Scoring and Leaderboard
- [ ] **SCORE-01**: Player earns points per match; score is displayed on HUD
- [ ] **SCORE-02**: Final score is shown on the win/lose screen at end of each level
- [ ] **SCORE-03**: High score per level is stored locally and shown on the level select/menu screen
- [ ] **SCORE-04**: Cumulative total score is tracked across sessions

### HINT — Hint System
- [ ] **HINT-01**: Player can request a hint that highlights one valid free matching pair
- [ ] **HINT-02**: Each level starts with a limited number of free hints (defined by LevelConfig)
- [ ] **HINT-03**: Additional hints are unlocked by watching a rewarded ad

### ADS — Monetization
- [ ] **ADS-01**: Unity Ads SDK is initialized asynchronously at app launch with a 5-second timeout and graceful degradation
- [ ] **ADS-02**: Rewarded ad plays when player requests a hint beyond their free allowance
- [ ] **ADS-03**: Rewarded ad plays when player requests extra moves after "no moves left"
- [ ] **ADS-04**: Interstitial ad is shown between levels (after win/lose screen, before next level loads)
- [ ] **ADS-05**: Ads are never shown mid-gameplay (only on transition screens)
- [ ] **ADS-06**: UMP consent dialog is displayed on first launch (GDPR compliance)

### UI — User Interface
- [ ] **UI-01**: Main menu with mode selection (Classic, Math Mode, Active Mind)
- [ ] **UI-02**: HUD displays: current score, combo counter, level number, hint count, timer (if applicable)
- [ ] **UI-03**: Win screen displays: final score, stars earned, next level button
- [ ] **UI-04**: Lose screen (no moves left) displays: current score, restart button, hint ad button
- [ ] **UI-05**: Android back button pauses the game and shows a pause menu
- [ ] **UI-06**: Pause menu has resume and quit options

### THEME — Visual Theme
- [ ] **THEME-01**: All tile art, backgrounds, and UI decorations use an Indian cultural aesthetic (rangoli, mandala, temple motifs)
- [ ] **THEME-02**: Free tiles are visually distinguished from blocked tiles (glow or highlight effect)
- [ ] **THEME-03**: Match removal animation plays on both tiles (fade out + particle effect)
- [ ] **THEME-04**: Board fits portrait orientation on all common Android screen sizes (16:9 to 20:9)

### PLATFORM — Platform
- [ ] **PLATFORM-01**: Game builds and runs on Android (minSDK 21, ARM64, IL2CPP)
- [ ] **PLATFORM-02**: Game runs at a stable 60fps during board interaction on mid-range Android (2GB RAM, ~2017 GPU)
- [ ] **PLATFORM-03**: Unity project uses Unity 6 LTS with URP 2D Renderer

---

## v2 Requirements (Deferred)

- IAP (remove ads, hint packs) — deferred; validate ad revenue in v1 first
- Google Play Games Services / online leaderboard — requires backend; local is sufficient for v1
- iCloud/Google Play save sync — deferred to v2
- Sequence Matching mode (clear in order 1→9) — deferred; three modes sufficient for launch
- Undo history (more than 1 step) — single undo sufficient for v1
- iOS build — architecture is iOS-ready; actual build deferred until Android is validated

---

## Out of Scope

- Multiplayer — not a Mahjong Solitaire genre feature; adds complexity with no casual puzzle value
- Online accounts / profiles — infrastructure cost dwarfs v1 value
- Level editor — procedural generation removes need for hand-authored levels
- Landscape orientation — portrait is standard for casual mobile puzzle genre
- Platform: PC/Mac/Web — Android focus for v1; Unity architecture supports later

---

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CORE-01 | Phase 1 — Logic Foundation | Pending |
| CORE-02 | Phase 1 — Logic Foundation | Pending |
| CORE-03 | Phase 1 — Logic Foundation | Pending |
| CORE-04 | Phase 1 — Logic Foundation | Pending |
| CORE-05 | Phase 1 — Logic Foundation | Pending |
| CORE-06 | Phase 1 — Logic Foundation | Pending |
| CORE-07 | Phase 1 — Logic Foundation | Pending |
| CORE-08 | Phase 1 — Logic Foundation | Pending |
| GEN-01 | Phase 1 — Logic Foundation | Pending |
| GEN-02 | Phase 1 — Logic Foundation | Pending |
| GEN-03 | Phase 1 — Logic Foundation | Pending |
| GEN-04 | Phase 1 — Logic Foundation | Pending |
| GEN-05 | Phase 1 — Logic Foundation | Pending |
| PLATFORM-03 | Phase 1 — Logic Foundation | Pending |
| MODE-C-01 | Phase 2 — Board Rendering and First Playable | Pending |
| MODE-C-02 | Phase 2 — Board Rendering and First Playable | Pending |
| THEME-02 | Phase 2 — Board Rendering and First Playable | Pending |
| THEME-03 | Phase 2 — Board Rendering and First Playable | Pending |
| THEME-04 | Phase 2 — Board Rendering and First Playable | Pending |
| PLATFORM-01 | Phase 2 — Board Rendering and First Playable | Pending |
| PLATFORM-02 | Phase 2 — Board Rendering and First Playable | Pending |
| MODE-M-01 | Phase 3 — Additional Modes and Combo | Pending |
| MODE-M-02 | Phase 3 — Additional Modes and Combo | Pending |
| MODE-M-03 | Phase 3 — Additional Modes and Combo | Pending |
| MODE-A-01 | Phase 3 — Additional Modes and Combo | Pending |
| MODE-A-02 | Phase 3 — Additional Modes and Combo | Pending |
| MODE-A-03 | Phase 3 — Additional Modes and Combo | Pending |
| MODE-A-04 | Phase 3 — Additional Modes and Combo | Pending |
| COMBO-01 | Phase 3 — Additional Modes and Combo | Pending |
| COMBO-02 | Phase 3 — Additional Modes and Combo | Pending |
| COMBO-03 | Phase 3 — Additional Modes and Combo | Pending |
| COMBO-04 | Phase 3 — Additional Modes and Combo | Pending |
| PROG-01 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| PROG-02 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| PROG-03 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| PROG-04 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| PROG-05 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| SCORE-01 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| SCORE-02 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| SCORE-03 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| SCORE-04 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| HINT-01 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| HINT-02 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| HINT-03 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| UI-01 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| UI-02 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| UI-03 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| UI-04 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| UI-05 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| UI-06 | Phase 4 — Meta Systems, Scoring, and UI | Pending |
| ADS-01 | Phase 5 — Monetization | Pending |
| ADS-02 | Phase 5 — Monetization | Pending |
| ADS-03 | Phase 5 — Monetization | Pending |
| ADS-04 | Phase 5 — Monetization | Pending |
| ADS-05 | Phase 5 — Monetization | Pending |
| ADS-06 | Phase 5 — Monetization | Pending |
| THEME-01 | Phase 6 — Content, Platform Compliance, and Polish | Pending |

**Total: 57 / 57 requirements mapped.**
