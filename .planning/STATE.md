# State — Vita Mahjong Number

## Project Reference

**Name:** Vita Mahjong Number
**Core Value:** A player should be able to pick up, play a satisfying round of number-based Mahjong Solitaire in under 5 minutes, with every board guaranteed solvable and the challenge growing naturally across levels.
**Platform:** Android (primary), Unity 6 LTS / C#
**Architecture:** Single Gameplay scene, runtime mode-swap via IModeStrategy, ScriptableObjects for data, URP 2D Renderer, IL2CPP + ARM64

---

## Current Position

**Phase:** 1 — Logic Foundation
**Plan:** Not started
**Status:** Planning complete — ready to begin Phase 1
**Progress:**

```
[Phase 1] [ ] Logic Foundation
[Phase 2] [ ] Board Rendering and First Playable
[Phase 3] [ ] Additional Modes and Combo
[Phase 4] [ ] Meta Systems, Scoring, and UI
[Phase 5] [ ] Monetization
[Phase 6] [ ] Content, Platform Compliance, and Polish
```

**Overall:** 0 / 6 phases complete

---

## Performance Metrics

| Metric | Target | Current |
|--------|--------|---------|
| Requirements coverage | 57 / 57 | 57 / 57 (mapped) |
| Phases complete | 6 | 0 |
| Unit tests passing | TBD in Phase 1 | — |
| Build target | ARM64 IL2CPP APK/AAB | Not yet created |
| Frame rate (mid-range device) | 60fps | Not yet measured |
| Peak memory (mid-range device) | < 400MB | Not yet measured |
| Board generation time | < 500ms | Not yet measured |

---

## Accumulated Context

### Architecture Decisions (Locked)

- **TileData is a pure C# struct**, not MonoBehaviour — enables Edit Mode unit tests, avoids GC on 144 tile instances.
- **IModeStrategy interface** is the single routing point for all mode-specific logic: `IsValidMatch`, `OnTileSelected`, `OnMatchConfirmed`, `OnLevelStart`, `OnTick`, `GetHUDLabel`. No-moves detection and hint search MUST call through this interface.
- **ScriptableObjects hold static config only** — BoardLayoutSO, LevelConfigSO, ThemeConfigSO. Zero runtime mutable state on SOs. Enable "Reload Domain" in Editor Play Mode Options.
- **Object pool for TileView GameObjects** — sized to max tile count; zero allocations on level transition.
- **Event-driven HUD** — ScoreManager exposes C# events; HUD subscribes; GameplayController never references UI elements directly.
- **Reverse-generation for solvability** — place pairs into free slots iteratively; BFS post-validation with retry cap.

### Critical Pitfalls to Avoid (from research)

1. Free-tile detection logic inversion: `!hasLeft || !hasRight` NOT `hasLeft || hasRight` — unit test all 8 neighbor combinations before writing any downstream code.
2. Reverse-generation deadlock: always place both tiles of a pair in one pass; recompute free slots after each pair; cap retries at 50.
3. No-moves detection must route through `IModeStrategy.IsValidMatch` — not hardcoded Classic rules.
4. Unity Ads SDK: use `IUnityAdsInitializationListener` callback with 5-second timeout; never poll `Advertisement.isInitialized`.
5. ScriptableObject mutable state: use `[NonSerialized]` on any runtime field; prefer pure C# runtime state classes.

### Open Decisions / Gates

| Decision | Status | Blocks |
|----------|--------|--------|
| Unity Ads SDK package ID (`com.unity.ads` vs `com.unity.services.ads`) | Unverified — check Package Manager at project start | Phase 5 |
| Indian cultural music licensing (license, commission, or royalty-free) | Requires human decision | Phase 6 audio |
| Active Mind Mode scope for v1 | Included in Phase 3 — validate via QA pass; defer to v1.1 if unstable | Phase 3 gate |
| UI Toolkit scroll view behavior in Unity 6 | Verify in Phase 2 before committing to settings/level-select | Phase 4 |

### Todos

- [ ] Set up Unity 6 LTS project with URP 2D Renderer before Phase 1 work begins
- [ ] Verify Unity Ads SDK package ID against Package Manager at project start
- [ ] Acquire or commission Indian cultural music tracks before Phase 6
- [ ] Procure mid-range Android test device (Snapdragon 450 / Helio P22, 2GB RAM) for Phase 2 validation

### Blockers

None currently.

---

## Session Continuity

**Last updated:** 2026-03-19
**Last action:** Roadmap and STATE.md created by gsd-roadmapper. Requirements traceability table updated.
**Next action:** Begin Phase 1 — run `/gsd:plan-phase 1`

### Resuming Context

When resuming after a break, read in this order:
1. This file (STATE.md) — current position and open decisions
2. `.planning/ROADMAP.md` — phase goals and success criteria for current phase
3. `.planning/REQUIREMENTS.md` — requirement IDs and descriptions for current phase
4. Any plan file at `.planning/plans/phase-1-plan.md` (created by plan-phase)

---

*Last updated: 2026-03-19*
