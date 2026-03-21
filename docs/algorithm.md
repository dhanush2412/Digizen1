# How the Board Generator Works

> Plain-English explanation of how Vita Mahjong Number creates levels, guarantees every board is solvable, and makes the game harder as you progress.

---

## The Big Picture

Every time you start a level, the game needs to place numbered tiles on a layered board such that:

1. **You can always win** — there is always at least one way to match all tiles
2. **It feels fair** — tiles aren't locked in a way that makes it impossible mid-game
3. **It gets harder** — later levels have more tiles, deeper stacks, and fewer obvious moves

The challenge: if you just randomly scatter tiles on the board, most boards will be impossible to solve. Our algorithm avoids this completely by working **backwards**.

---

## Part 1 — How a Board is Generated

### The Core Idea: Build It Backwards

Imagine you've already solved a level — all tiles are gone. Now imagine putting tiles **back** onto the board, pair by pair, in reverse. If you do this carefully (only placing tiles in spots that were free at that moment), then the order you placed them in reverse is guaranteed to be a valid solution when played forward.

This is called **Reverse Generation** and it's the heart of the algorithm.

---

### Step-by-Step: How One Board is Built

**Step 1 — Load the Layout**

The game has a blueprint of all the slot positions on the board — like a map of all the spots where a tile *could* go. This blueprint also tells us which slots are on the ground level (layer 0), which are on layer 1 above, and so on.

```
Example: A simple 3-layer pyramid

Layer 2:      [ ]
Layer 1:    [ ][ ]
Layer 0:  [ ][ ][ ]
```

**Step 2 — Find the Starting Free Slots**

We start with an empty board. Every ground-level slot that has no tile above it and has at least one open side is "free" — meaning a tile placed here could theoretically be picked up later.

At the start (empty board), all ground-level slots are free.

```
Free slots at start (marked with ✓):

Layer 0:  [✓][✓][✓]    ← all free, nothing above them
```

**Step 3 — Shuffle the Tile Numbers**

We take all the numbers we need for this level (e.g., 1,1,2,2,3,3,4,4...) and shuffle them randomly — like shuffling a deck of cards. This randomness is what makes every board feel different.

**Step 4 — Place Tiles in Pairs (The Main Loop)**

Now we repeat this until all tiles are placed:

1. **Pick two free slots** — we look at all currently-free slots and pick a smart pair (explained below)
2. **Take the next pair of numbers** from our shuffled deck
3. **Place one number on each slot**
4. **Update which slots are now free** — placing a tile can unblock slots above it (now something can sit on top) or block slots next to it (now that side is closed)

> **Why this guarantees solvability:**
> Every pair we place went onto two slots that were both free at that moment.
> So when you play the game and remove tiles, removing them in the *exact reverse order* of how they were placed is always valid.
> At least one solution always exists — it's baked in by construction.

**Step 5 — Handle Getting Stuck (Retries)**

Sometimes the placement gets stuck — all free slots are used up but there are still tiles left to place. This is called a **deadlock**.

When this happens, the generator simply tries again with a fresh shuffle of the tile numbers. It keeps trying until it succeeds (up to 200 attempts for hard levels, 5 for easy ones). In practice, with smart slot selection, this almost never needs more than a few tries.

---

### How the Generator Picks Slots Smartly

Not all slot choices are equal. A bad choice can close off the board and cause a deadlock. The generator uses a simple scoring rule:

> **Prefer slots that block the fewest neighbors when filled.**

Think of it like parking a car — you prefer a spot that doesn't block the driveway of two other cars. A slot that, when filled, traps several other slots is a bad choice. The generator scores each candidate slot and picks the pair with the lowest "damage" to future options.

---

### Updating Which Slots Are Free (The Careful Part)

This is the trickiest part of the algorithm, and it's easy to get wrong.

**A tile is "free" (removable) if:**
- Nothing is sitting on top of it (the slot directly above is empty)
- **AND** at least one horizontal side is open (left OR right has no neighbor)

```
Examples:

      [ ]          ← this tile has something on top → BLOCKED
      [A]

[B]  [C]  [D]     ← B has D to its right but nothing to its left → FREE
                     C has B on left AND D on right → BLOCKED
                     D has C to its left but nothing to its right → FREE
```

When a tile is placed, the generator **re-checks every neighboring slot** to see if their free status changed. It never assumes — it always recalculates. This prevents the common bug of wrongly thinking a slot is available when it isn't.

---

## Part 2 — How Difficulty Increases Each Level

### The Difficulty Dial

Every level has a "difficulty value" (called `d`) that goes from nearly 0 at Level 1 to nearly 1 at Level 100. It follows an S-shaped curve:

```
Difficulty (d)
1.0 |                                    ████████
    |                              ██████
0.5 |                        ██████
    |                  ██████
0.0 | ████████████████
    +----+----+----+----+----+----+----+----+----+-- Level
    1   10   20   30   40   50   60   70   80   90  100
```

- Levels **1–20**: nearly flat and easy — a gentle introduction
- Levels **20–80**: the main difficulty ramp — where most of the challenge lives
- Levels **80–100**: expert territory — nearly maxed out

---

### What Changes as Difficulty Increases

Three things change as `d` grows from 0 to 1:

---

#### 1. Tile Count — More Tiles = Longer Game

| Level | Tiles on Board |
|-------|----------------|
| 1     | ~38 tiles      |
| 10    | ~42 tiles      |
| 25    | ~60 tiles      |
| 50    | ~90 tiles      |
| 75    | ~126 tiles     |
| 100   | ~144 tiles     |

More tiles = more decisions = longer game.

**Formula:** `Tiles = 36 + round(108 × d)`
(Always rounded to an even number, since tiles come in pairs)

---

#### 2. Layer Depth — Taller Stacks = Fewer Free Tiles

| Level | Max Layers |
|-------|------------|
| 1–12  | 1–2 layers |
| 13–35 | 2–3 layers |
| 36–62 | 3–4 layers |
| 63–100| 4–5 layers |

With only 1 layer, almost every tile is free to pick up. With 5 layers, most tiles are buried — you have to carefully uncover lower tiles before you can reach them.

**Formula:** `Layers = 1 + floor(4 × d)`

---

#### 3. Branching Factor — How Many Choices You Have

The "branching factor" is the average number of valid matching pairs available at any point during the game.

- **High branching (easy):** Lots of free tiles match — you have many options, hard to get stuck
- **Low branching (hard):** Only a few pairs are available at any time — one wrong move and you're stuck

| Level | Target Free Pairs Available |
|-------|----------------------------|
| 1     | ~8 pairs visible           |
| 50    | ~5 pairs visible           |
| 100   | ~2 pairs visible           |

This is controlled by the layout template chosen for the level — denser, taller layouts naturally produce lower branching.

---

### The Retry Budget Also Scales

Hard levels are naturally more prone to the generator getting stuck during construction (more tiles, tighter constraints). So the generator is given more retry attempts for harder levels:

| Level Range | Max Retries |
|-------------|-------------|
| 1–25        | 5 attempts  |
| 26–60       | 20 attempts |
| 61–100      | 200 attempts|

---

## Part 3 — Math Mode: How "Sum to 10" Changes Things

In Classic Mode, you match two tiles with the **same number** (e.g., 7 and 7).

In Math Mode, you match two tiles whose numbers **add up to 10** (e.g., 3 and 7, or 4 and 6).

### Valid pairs in Math Mode:

| Pair    | Sum |
|---------|-----|
| 1 + 9   | 10  |
| 2 + 8   | 10  |
| 3 + 7   | 10  |
| 4 + 6   | 10  |
| 5 + 5   | 10  |

### How the generator adapts:

The placement loop still works the same way — the only change is **what counts as a valid pair** when drawing from the tile pool.

One special rule: the number **5** can only pair with another **5**. So the generator always makes sure there's an **even number of 5s** in the pool. If there were an odd number of 5s, one 5 would have no partner — game over, unsolvable.

The generator checks this before even starting:
- For every value 1–4: count of that value must equal count of its complement (e.g., count of 3s must equal count of 7s)
- For 5s: must be an even count

If these rules aren't satisfied, the generator refuses to start and reports an error.

---

## Summary: Why Every Board Is Guaranteed Solvable

Here's the one-line proof, in plain English:

> **We built the board by removing tiles (in our imagination) one valid pair at a time, from a solved board down to empty. The reverse of that removal sequence is always a valid solution.**

The player might find a *different* solution — or might make wrong moves that lead to a dead end — but there is always *at least one* way to win. The game never cheats you.

---

## Quick Reference: Key Numbers

| Parameter | Level 1 | Level 50 | Level 100 |
|-----------|---------|----------|-----------|
| Tiles     | ~38     | ~90      | ~144      |
| Max Layers| 1       | 3        | 5         |
| Retries   | 5       | 20       | 200       |
| Difficulty d | 0.02 | 0.50  | 0.98      |

---

*Last updated: 2026-03-20*
