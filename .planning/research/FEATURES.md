# Feature Landscape: Mahjong Solitaire Mobile

**Domain:** Casual / Puzzle mobile game — Mahjong Solitaire (tile-matching)
**Researched:** 2026-03-19
**Confidence note:** WebSearch and WebFetch were unavailable. All findings are drawn from training data (knowledge cutoff August 2025) covering the Mahjong Solitaire mobile genre, including Microsoft Mahjong, Mahjong Solitaire Epic, Mahjong Journey, Tile Master, and dozens of similar titles on Android and iOS. Confidence is MEDIUM across the board; no external source verification was possible this session.

---

## Table Stakes

Features users expect in every Mahjong Solitaire title. Absence causes 1-star reviews and immediate uninstall.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Solvable board guarantee | Players rage-quit unsolvable boards; genre standard since ~2010 | High | Must use constraint-based generation or backtracking solver. Already planned (procedural solvable). |
| Free-tile highlighting | Without visual cue players cannot tell which tiles are selectable; confusion = quit | Medium | Outline, glow, or color shift on unblocked tiles. Already planned. |
| Undo (at least 1 move) | Players accidentally mis-tap on mobile; no undo = forced restart rage | Low | Single undo is minimum; many games offer 3-5. Plan for 1, let ads unlock more. |
| Shuffle (when stuck) | Players get blocked with no valid moves; shuffle or "reshuffle" is genre rescue mechanic | Low | Can be ad-gated or limited-use. Genre expects it to exist. |
| Hint system | Mobile players expect hand-holding on puzzle games; missing = players quit | Low | Highlight one valid pair. Already planned (rewarded ad gate). |
| Win / Lose state feedback | Clear "You Win" / "No Moves Left" screen with stats | Low | Expected everywhere. |
| Timer / Move counter | Score context; most titles show elapsed time or move count | Low | Both are standard. Score without context feels hollow. |
| Multiple board layouts | Single layout = game feels like a demo, not a product | Medium | Minimum 10-15 layouts for launch credibility. Procedural helps here. |
| Touch-to-select tiles | Responsive tap selection with visual selection state (highlight) | Low | Core interaction. Tap once to select, tap matching to pair. |
| Restart level | Players want to retry; no restart = forced to quit to menu | Low | In-level restart button, standard expectation. |
| Basic sound effects | Tile click, match sound, win sound; silence feels broken | Low | Very low effort, high perceived quality. |
| Settings (sound/music toggle) | Players on commute mute games; no mute = 1-star review | Low | Sound on/off minimum. |
| Score display in-game | Live score visible during play, not just end screen | Low | Players track progress mid-game. |
| Interstitial ads between levels | Monetization norm players now accept (within reason) | Low | Already planned. |
| Android back-button handling | Failure to handle Android back = crash or freeze perception | Low | Unity requires explicit hook. Critical for Android. |
| Landscape/portrait stability | Game should not crash or mislayout on orientation change if portrait-locked | Low | Lock to portrait, handle properly in Unity. |

---

## Differentiators

Features that set a title apart. Players do not expect them, but they drive retention, ratings, and word-of-mouth when done well.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Math Mode (sum-to-10 pairs) | Unique mechanic not found in standard Mahjong Solitaire; appeals to brain-training market | Medium | Core differentiator for this project. Requires modified match logic. |
| Active Mind Mode (memory/recall) | Memory layer on top of tile-matching is very rare in genre; taps cognitive wellness trend | High | Tiles flip face-down after reveal; player must recall positions. High risk, high reward. |
| Indian cultural visual theme | Most Mahjong games use East Asian aesthetics; Indian theme is a blue ocean differentiator | Medium | Tile art, backgrounds, music. Needs cultural authenticity care. |
| Number-based tiles instead of Chinese symbols | Accessibility win for players unfamiliar with Mahjong iconography; lowers entry barrier | Low | Numbers are universally legible. Core differentiator vs traditional tiles. |
| Combo / Super Combo system | Adds skill expression and replayability beyond basic matching | Medium | Consecutive matches multiply score. Already planned. Increases session engagement. |
| Escalating difficulty across procedural levels | Prevents game from feeling trivial after level 5; keeps players returning | Medium | Difficulty parameters: board density, tile count, more blocked tiles. |
| Rewarded ads for hints (opt-in) | Players prefer watching an ad over paying; opt-in rewarded ads have highest acceptance | Low | Already planned. Industry best practice for F2P casual. |
| Cultural music / ambient audio | Sitar, tabla, or Indian classical ambient tracks strongly reinforce theme identity | Medium | Outsource or license; can be added post-launch but at-launch audio sells theme. |
| Brain-training positioning | "Sharpen your mind" framing appeals to 35-65 demographic on Android India | Low | Marketing/ASO differentiator, not a feature per se, but features must support claim. |
| Local leaderboard (score) | Motivation for replayability; simple high-score table keeps casual players coming back | Low | Already planned. Global leaderboard is NOT needed for v1 (see anti-features). |
| Session length indicator (level progress bar) | Helps players plan breaks; increases completion rates vs abandonment mid-session | Low | Simple progress bar showing tiles remaining / pairs found. |
| Daily puzzle / Daily challenge | High retention driver; players open app daily for unique challenge | Medium | Single hand-crafted or seeded-random board per calendar day. Consider for v1.1. |

---

## Anti-Features

Features to deliberately NOT build for v1. Each has a reason and a suggested alternative.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Online multiplayer | Enormous infrastructure cost, server ops, latency handling; irrelevant for casual solo puzzle genre | Ship solo-only; add async challenge sharing in v2 if metrics justify |
| Global leaderboard (real-time) | Requires backend, auth, anti-cheat, ongoing ops; cheaters ruin experience immediately | Ship local leaderboard; consider Game Services integration only after launch validation |
| IAP consumables (hint packs, shuffle packs) | Complex payment flow, Play Store policy compliance, tax handling across regions; too heavy for v1 | Rewarded ads cover the same unlock need with zero IAP overhead |
| Subscription monetization | Inappropriate for this genre and demographic; casual players reject subscriptions in puzzle games | Ad-supported F2P is correct for Indian Android casual market |
| Account / login system | Auth adds friction at install; casual puzzle games with login requirements see 40-60% drop at onboarding | Anonymous local play only; no account required |
| Story mode / narrative | No precedent in Mahjong Solitaire genre for story; production cost vastly exceeds return | Level progression with visual theme evolution (backgrounds change) achieves same retention with 5% effort |
| Social sharing of scores | Screenshot share buttons add dev time; social graph for puzzle games rarely drives installs | Skip for v1; ASO and ads drive installs cheaper |
| Custom tile editors (user-created boards) | Content creator tools are a product-within-a-product; niche audience | Procedural generation serves the same "always fresh" need |
| PvP tile-racing modes | Incompatible with solitaire genre identity; confuses positioning | Stick to solo puzzle identity |
| Achievement system (Play Games) | Play Games Services integration is a non-trivial Android setup; achievement design takes time | Local score milestones give same dopamine hit without Google Play Games dependency |
| Cloud save / cross-device sync | Firebase or Play Games sync adds ops complexity; single-device Android casual players rarely need it | Local SQLite/PlayerPrefs save is sufficient for v1 |
| Animated tile 3D models | 2D flat tile art is genre standard; 3D tiles add render cost with no gameplay benefit on low-end Android | 2D sprite tiles with subtle shadow/bevel look polished without GPU cost |
| More than 3 game modes at launch | Each mode is a product decision; too many modes dilutes messaging and increases QA scope | Launch Classic + Math Mode; hold Active Mind Mode for v1.1 unless already built and stable |

---

## Feature Dependencies

```
Solvable board generation
  └─> Free tile detection logic         (detection IS the validity check during generation)
  └─> Hint system                       (hints need free-tile detection to suggest pairs)
  └─> Shuffle                           (shuffle needs re-evaluation of free tiles after)
  └─> Math Mode matching                (match rule change; generation still needs solvability check with new rule)
  └─> Active Mind Mode                  (adds face-down layer ON TOP of solvable board; board gen unchanged)

Free tile detection
  └─> Touch-to-select validation        (only allow selecting free tiles)
  └─> Win/Lose state detection          (no free tiles left AND board not empty = stuck)
  └─> Hint system                       (scan free tiles to suggest match)

Combo system
  └─> Score display                     (combo multiplier must be shown visually)
  └─> Win screen stats                  (report max combo reached)

Rewarded ads (hint gate)
  └─> AdMob SDK integration            (Unity AdMob package)
  └─> Hint system                       (reward delivers hint execution)

Interstitial ads
  └─> AdMob SDK integration            (same SDK, different ad unit)
  └─> Level transition flow             (ad fires at level completion, before next level load)

Score tracking / local leaderboard
  └─> PlayerPrefs or SQLite persistence (save scores across sessions)
  └─> Level completion event            (score finalized at win state)

Multiple board layouts / procedural difficulty
  └─> Solvable board generation         (layouts are templates fed into generator)
  └─> Difficulty parameters             (tile count, blocked ratio, min free tiles at start)
```

---

## MVP Recommendation

### Must ship in v1 (game is not reviewable without these)

1. Solvable board generation with free-tile detection
2. Touch selection + tile matching (Classic mode: identical number pairs)
3. Free-tile visual highlight (outline or glow)
4. Hint system (rewarded ad gate)
5. Shuffle (limited use or ad gate)
6. Undo (1 move minimum)
7. Win / Lose state with score display
8. Timer or move counter (pick one; timer is simpler)
9. 10+ board layouts (procedural templates)
10. Restart level button
11. Sound effects (match, win, tap)
12. Settings screen (sound/music toggle)
13. Interstitial ads between levels (AdMob)
14. Local high-score leaderboard (per mode)
15. Android back-button handling

### Should ship in v1 (strong differentiators already planned)

16. Math Mode (sum-to-10 matching) — unique mechanic, core to positioning
17. Combo / Super Combo system — low complexity, high engagement impact
18. Indian cultural visual theme — core identity, low extra cost once art assets exist
19. Number-based tiles — already the premise; no extra work

### Defer to v1.1

- Active Mind Mode (memory/recall) — High complexity, high QA risk; validate v1 first
- Daily puzzle mode — Medium complexity; needs seeded RNG system
- Background / theme variation per difficulty tier

### Do not build

- Everything in the Anti-Features table above

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Table stakes | MEDIUM | Based on genre survey from training data (Microsoft Mahjong, Mahjong Solitaire Epic, Mahjong Journey, Tile Master, 1010! analogues). No live source verification possible this session. Features listed are consistent across 10+ genre titles. |
| Differentiators | MEDIUM | Assessment is based on genre comparison; Indian theme and number tiles are observably rare in training data corpus. Math Mode / memory mode rarity is HIGH confidence — these mechanics genuinely do not appear in standard genre titles. |
| Anti-features | MEDIUM | Rationale grounded in F2P mobile game design principles (AARRR model, IAP friction data, server ops cost). IAP/multiplayer avoidance for casual Indian Android market is well-documented in training data. |
| Dependencies | HIGH | Dependencies are logical/technical and do not depend on external sources. |

---

## Sources

Note: No external sources were accessible this session (WebSearch and WebFetch denied). All findings derive from training data knowledge of the Mahjong Solitaire mobile genre as of August 2025. This is noted as a gap. Recommend manual verification against:

- Google Play Store reviews for: Microsoft Mahjong, Mahjong Solitaire Epic (Kristanix), Mahjong Journey (PlaySimple)
- AppFollow or AppMagic review analysis for "mahjong solitaire" category
- SimilarWeb / data.ai rankings for casual puzzle, India Android market Q1 2025
