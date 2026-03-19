# Vita Mahjong Number

## What This Is

Vita Mahjong Number is a number-based Mahjong Solitaire mobile game for Android (iOS later), built in Unity (C#). Instead of traditional artwork tiles, players match pairs of numbers on a 3D-layered board. The game features three modes — Classic matching, Math Mode (sum-to-10 pairs), and Active Mind Mode (memory/recall) — with an Indian cultural visual theme, procedurally generated boards, and escalating difficulty across levels.

## Core Value

A player should be able to pick up, play a satisfying round of number-based Mahjong Solitaire in under 5 minutes, with every board guaranteed solvable and the challenge growing naturally across levels.

## Requirements

### Validated

(None yet — ship to validate)

### Active

**Core Gameplay**
- [ ] Free tile detection: tile is selectable only if no tile above it AND open on left OR right side
- [ ] Solvable board generation via reverse-generation (place pairs into free slots, guaranteed winnable)
- [ ] Classic Mode: match pairs of identical numbers (1–9)
- [ ] Math Mode: match any two free numbers that sum to 10
- [ ] Active Mind Mode: observation phase (tiles shown face-up) then recall phase (tiles hidden, match from memory)
- [ ] Combo system: consecutive matches within ~3 seconds trigger Super Combo with score bonus and visual effect
- [ ] Hint system: reveal a valid free pair (consumed via rewarded ad)
- [ ] "No moves left" detection with option to restart or use hint

**Level Progression**
- [ ] Procedural level generation with escalating difficulty (more tiles, more layers, fewer hints as level increases)
- [ ] Level number displayed; each completed level unlocks the next
- [ ] Score tracking per level and cumulative across sessions
- [ ] Local leaderboard (high scores stored on device)

**Visual & Theme**
- [ ] Indian cultural theme: tile art, backgrounds, UI decorations inspired by Indian patterns (rangoli, mandala, temple motifs)
- [ ] Free tiles visually distinguished (glow/highlight) from blocked tiles
- [ ] Match animation (tiles disappear with particle effect)
- [ ] Combo visual effect on Super Combo

**Monetization**
- [ ] Rewarded ads: player watches ad to earn a hint or extra moves
- [ ] Interstitial ads: shown between levels (non-intrusive, skippable after delay)
- [ ] Unity Ads SDK integration

**Platform**
- [ ] Android build (minSDK 21+)
- [ ] iOS-ready architecture (no Android-specific code in game logic)
- [ ] Portrait orientation, mobile touch input

### Out of Scope

- Multiplayer — complexity not needed for v1; single-player puzzle focus
- Online leaderboard — requires backend; local leaderboard sufficient for v1
- IAP / remove-ads purchase — deferred to v2 after validating ad revenue
- Sequence Matching mode — deferred; Classic + Math + Active Mind is sufficient for launch
- iCloud/Google Play save sync — deferred to v2
- Hand-crafted levels — all levels procedural; no authored layouts needed

## Context

- Game is a number-based reimagining of Vita Mahjong (a real Mahjong Solitaire app), replacing artwork tiles with numeric values
- Tile-locking logic: position (x, y, z); tile is free if no tile at z+1 AND (no left neighbor OR no right neighbor)
- Reverse-generation ensures solvability: start empty, place pairs in random free slots, repeat until full
- Difficulty scaling via LevelConfig ScriptableObject: increase tile count, layer depth, reduce hint allowance per difficulty band
- Indian cultural theme chosen for artistic identity and cultural resonance with target audience
- Architecture: Unity (C#), ScriptableObjects for data, single Gameplay scene with runtime mode-swapping via strategy pattern

## Constraints

- **Platform**: Android primary, Unity (C#) — must remain iOS-portable; no Android-only APIs in game logic layer
- **Monetization**: Unity Ads SDK — no third-party ad networks for v1; keep it simple
- **Tile range**: Numbers 1–9; board always uses even tile count (pairs only)
- **Board solvability**: Every generated board MUST have at least one solution — reverse-generation is non-negotiable
- **Performance**: Must run smoothly on mid-range Android devices (2GB RAM, ~2017-era GPU)

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Unity (C#) over Pygame/HTML5 | Production-ready, Android + iOS from one codebase, robust touch input | — Pending |
| ScriptableObjects for board/level data | Idiomatic Unity data-driven design; easy to tune difficulty without code changes; testable | — Pending |
| Single Gameplay scene, runtime mode-swap | Avoids duplication; all 3 modes share board rendering and tile interaction | — Pending |
| Reverse-generation for solvability | Guarantees at least one solution exists; simple to implement correctly | — Pending |
| Rewarded ads for hints (not IAP) | Lower friction for v1; validates monetization before building IAP infrastructure | — Pending |
| Indian cultural theme | Strong artistic identity; culturally resonant; differentiates from generic Mahjong apps | — Pending |
| Procedural levels with escalating difficulty | Infinite replayability without content authoring cost | — Pending |

---
*Last updated: 2026-03-19 after initialization*
