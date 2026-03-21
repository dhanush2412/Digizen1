# Vita Mahjong Number

A **number-based Mahjong Solitaire** game for Android, built with Unity 6 LTS and C#. Features an Indian cultural theme and a procedural board generator that guarantees **every board is 100% solvable**.

---

## How We Built This (RuFlo / Claude-Flow)

This game was built entirely using **RuFlo** (the [claude-flow](https://github.com/ruvnet/claude-code-flow) multi-agent framework) orchestrated through Claude Code.

### Build Process

```
/gsd:new-project       → Created ROADMAP.md with 6 phases, 57 requirements
/gsd:plan-phase        → Planned Phase 1 (board generation algorithm)
/claude-flow-swarm     → Deployed parallel agents: coder, tester, reviewer,
                         documenter, architect — all working concurrently
/claude-mem:do         → Saved all session work to persistent semantic memory
```

### Multi-Agent Swarm — How the 100% Solvability Was Achieved

The guarantee that every board is solvable came directly from a **swarm of specialized agents working in parallel**, each owning a distinct responsibility:

| Agent | Role | What It Built |
|-------|------|---------------|
| `architect` | Algorithm design | Designed the reverse-generation approach and the `IsFree` formula (`!blockedLeft \|\| !blockedRight`) — the mathematical foundation of solvability |
| `coder` | Implementation | Wrote `BoardGenerator.cs`, `LayoutTemplate.cs`, `TilePosition.cs`, `BoardResult`, and all 8 strategy/helper classes |
| `reviewer` | Bug hunting | Identified **3 critical bugs** in the first pass (wrong free-set seeding, skipped unoccupied neighbors, missing z+1 check) — without these fixes, boards were unsolvable |
| `tester` | Test coverage | Wrote **5 test files / ~50 tests**: unit, integration, stress, and mode-specific. Stress suite validates 1,300+ boards using O(N) solution-sequence replay |
| `documenter` | Verification | Wrote `algorithm.md` and ported the entire algorithm to JavaScript as `game_test.html` — a browser game that lets you click Auto Solve to prove every generated board is completeable |

**The agents ran concurrently** via `/claude-flow-swarm`, so algorithm design, implementation, and testing happened simultaneously — not sequentially. The reviewer agent's bug report fed directly back to the coder agent in the same session.

#### The 3 Bugs the Reviewer Agent Caught

Without the reviewer agent, these would have shipped silently broken:

```
Bug 1 (seeding)    — Only z=0 tiles were ever seeded as free.
                     Multi-layer boards never placed tiles above ground level.
                     Fix: seed ALL layout positions at start.

Bug 2 (neighbors)  — PlaceTile skipped unoccupied neighbors with an early continue.
                     Covered tiles stayed in the free set forever.
                     Fix: evaluate ALL layout neighbors, add/remove based on IsFree.

Bug 3 (z+1)        — GetAffectedNeighbors never checked the layer above.
                     Placing a tile never updated what was now free above it.
                     Fix: add EncodePos(x, y, z+1) to the neighbor scratch list.
```

With all three fixed, the reverse-generation algorithm is **mathematically proven correct** — the stored `SolutionSequence` is not a guess or an approximation, it is the exact removal path that was used to build the board in reverse.

The entire Phase 1 (board logic + tests + HTML verification game) was built in a single session with no manual code writing.

---

## Game Modes

| Mode | Match Rule | Tile Values |
|------|-----------|-------------|
| **Classic** | Same value | 1–9 |
| **Math Mode** | Values sum to 10 | 1–9 (symmetric pool) |
| **Active Mind** | TBD (Phase 3) | — |

---

## How the Game Works

Vita Mahjong Number is **Mahjong Solitaire** with numbers instead of traditional tiles.

### Gameplay
1. A board of stacked tiles is generated (36-tile practice or 144-tile standard layout)
2. Players click two matching **free** tiles to remove them as a pair
3. A tile is **free** if it has no tile on top AND at least one open side (left or right)
4. The goal: remove all tiles from the board

### What Makes a Tile "Free"?
```
A tile at (x, y, z) is FREE when:
  • Nothing is on top of it (z+1 layer is empty above it), AND
  • It has at least one open horizontal side:
      left side is clear  (no tile at x-1)
      OR
      right side is clear (no tile at x+2)
```

The critical formula: `(!blockedLeft || !blockedRight) && !coveredAbove`
NOT `blockedLeft || blockedRight` — that would require *both* sides free, which is wrong.

---

## The Algorithm: Guaranteed Solvable Boards

### The Problem
Random tile placement almost always creates unsolvable boards. Classic retry approaches are slow and probabilistic.

### Our Solution: Reverse-Generation

Instead of placing tiles randomly and checking solvability, we **build the solution first** and then reverse it:

```
STEP 1  Start with ALL layout positions in a "free" set
STEP 2  Pick two free tiles that match (same value or sum=10)
STEP 3  Place them as a pair → record in placement order list
STEP 4  Update the free set: tiles newly exposed become free,
        tiles now blocked become unfree
STEP 5  Repeat until every position is filled
STEP 6  SolutionSequence = REVERSE of placement order
```

**Why this guarantees solvability:**
The placement order is valid by construction — we only placed pairs that were free at that moment. Reversing it gives a valid *removal* order. Every pair in the sequence will be free when it is the player's turn to remove it.

### The Math

```
Placement step N:  tiles A and B are free  →  placed as pair N
Removal step (Total-N):  tiles A and B are free  →  can be removed

This is proven because:
  • When we placed pair N, A and B were in _freeTiles (verified by IsFree)
  • All pairs placed AFTER N only covered positions that were empty at step N
  • Therefore, when pairs Total..N+1 have been removed, A and B are in
    exactly the same state as when they were placed → still free
```

### Complexity
- Generation: O(N log N) — N tile placements, free-set operations O(log N)
- Validation: O(N) — linear replay of solution sequence
- No BFS, no backtracking, no retries needed (retry logic exists only for edge cases)

### Pool Symmetry (Math Mode)
For Math Mode (pairs sum to 10), the tile pool must satisfy:
```
count(v) == count(10-v)  for all v in 1..9
count(5) must be even     (5 pairs with itself)
```
`TileDistribution.BuildMathPool(n)` produces `10×n` tiles with these properties guaranteed.

---

## Architecture

```
src/Board/
├── TilePosition.cs          — 3D position encoding (5-bit x, y, z packed into int)
├── TileDistribution.cs      — Tile pool construction and pair-balance validation
├── BoardGenerator.cs        — Core reverse-generation algorithm + BoardResult
├── LayoutTemplate.cs        — Board shape definitions (36-tile, 144-tile layouts)
├── BFSValidator.cs          — O(N) solution-sequence replay validator
├── LevelConfig.cs           — Difficulty curve (sigmoid progression)
├── GameMode.cs              — GameMode enum + ToMatchMode() extension
├── TileData.cs              — Immutable tile struct (Position + Value)
├── IModeStrategy.cs         — Strategy interface for tile matching
├── ClassicModeStrategy.cs   — Implements: valueA == valueB
├── MathModeStrategy.cs      — Implements: valueA + valueB == 10
├── BoardState.cs            — Runtime board state + no-moves detection
└── FreeTileChecker.cs       — Convenience wrapper for IsFree checks

tests/Board/
├── FreeTileTests.cs         — IsFree formula verification (all neighbor configs)
├── GeneratorTests.cs        — Board generation: solvability, perf, tile count
├── MathModeTests.cs         — Math Mode pool symmetry + matching rules
├── DifficultyTests.cs       — Sigmoid difficulty curve correctness
└── StressTests.cs           — 1000 Classic + 200 Math + 100 Standard-144 boards,
                               all verified with O(N) solution-sequence replay

docs/
├── algorithm.md             — Plain-English explanation of the algorithm
├── game_test.html           — Self-contained HTML5 browser game for verification
└── README.md                — This file
```

### Key Design Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Solvability approach | Reverse-generation | O(N), 100% guarantee vs exponential BFS retry |
| Validation | Solution-sequence replay | O(N) exact vs bounded BFS (500 states ≈ 0% coverage) |
| Game mode routing | Strategy pattern | No hardcoded `if Classic / if Math` in core engine |
| Board layer | Pure C# (no Unity) | Testable with NUnit, no editor dependency |
| Position encoding | 5-bit packed int | Compact, O(1) hash/compare for HashSet<int> |
| x coordinate | Always ≥ 0 | 5-bit mask: negative x aliases to 31, 30 etc. |

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Game engine | Unity 6 LTS |
| Rendering | URP 2D Renderer |
| Language | C# (.NET Standard 2.1) |
| Board logic | Pure C# (no Unity dependencies) |
| Tests | NUnit 3.x (Unity Edit Mode test runner) |
| Build orchestration | RuFlo (claude-flow) multi-agent swarm |
| AI orchestrator | Claude Code (claude-sonnet-4-6) |
| Algorithm verification | HTML5 Canvas + JavaScript (docs/game_test.html) |
| Memory/continuity | claude-mem persistent semantic memory |
| Platform target | Android (Unity IL2CPP) |

---

## Verification

Open `docs/game_test.html` in any browser (no server needed):

1. Click **New Game** → board generates instantly
2. Check browser console → `✅ VERIFIED SOLVABLE` confirms O(N) replay passed
3. Click **Auto Solve** → watch every pair removed in guaranteed order
4. Change the **Seed** and regenerate → same seed = same board (deterministic)
5. Switch between **Classic / Math** and **36 / 144 tiles** to stress-test all paths

---

## Running Tests (Unity)

```
Window → General → Test Runner → Edit Mode → Run All
```

Expected: **~50 tests pass**, including:
- 1,000 Classic boards all solvable (30s stress test)
- 200 Math Mode boards all solvable
- 100 Standard 144-tile boards all solvable
- Solution sequence structural integrity for 200 boards

---

## What's Next (Phase 2+)

- Unity project setup with URP 2D Renderer
- Tile visual design (Indian cultural aesthetic)
- LayoutTemplate ScriptableObjects for designer-friendly level editing
- Active Mind mode (third game variant)
- Sound design and haptics
- Google Play publishing pipeline

---

*Built with [claude-flow](https://github.com/ruvnet/claude-code-flow) — AI-powered multi-agent development.*
