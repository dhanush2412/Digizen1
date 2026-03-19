# Technology Stack: Vita Mahjong Number

**Project:** Vita Mahjong Number — Unity (C#) Mahjong Solitaire for Android
**Researched:** 2026-03-19
**Research basis:** Training knowledge (cutoff August 2025). WebSearch and WebFetch were unavailable during this session. Confidence levels reflect this limitation — all HIGH-confidence items are stable, well-documented Unity features unlikely to have changed significantly. Version numbers marked MEDIUM should be verified against unity.com and the Unity Package Manager at project start.

---

## Recommended Stack

### Core Engine

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Unity | **6000.0 LTS** (Unity 6) | Game engine | Unity 6 is the current LTS line as of 2025, replacing the 2022.3 LTS cycle. It ships GPU Resident Drawer and improved mobile memory management out of the box. For a mid-range Android target (2GB RAM, ~2017 GPU), you want LTS — not the Tech Stream — for stability and 2-year support. |
| C# | 9 (via Unity 6) | Scripting language | Bundled with Unity 6. Null-coalescing, records, and pattern matching reduce boilerplate in game state code. |
| IL2CPP | Required | AOT compilation backend | **Always use IL2CPP for Android release builds**, not Mono. IL2CPP produces native ARM code — better runtime performance, smaller GC pauses, and required for Google Play's 64-bit mandate. Mono is acceptable for editor/debug iteration only. |

**Confidence:** HIGH — Unity 6 LTS was released in Q4 2024 and is the production-recommended version throughout 2025.

---

### Render Pipeline

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Universal Render Pipeline (URP) | 17.x (bundled with Unity 6) | 2D/3D rendering | URP is the correct choice for this project. Built-in Pipeline is legacy and being phased out. HDRP is GPU-heavy and irrelevant for 2D mobile. URP's 2D Renderer with the 2D Lighting and Sprite Lit shader covers everything Vita Mahjong needs: sprite tiles, UI overlays, subtle Indian-theme decorative lighting if desired. URP also enables GPU Instancing across tile sprites with near-zero overhead. |
| URP 2D Renderer | 17.x | Sprite batching, 2D lighting | Use the 2D Renderer profile (not the Forward Renderer) to keep draw call counts low. Batching identical tile sprites is critical for 2017-era GPUs. |

**NOT:** Do not use the Built-in Render Pipeline. Unity is actively removing Built-in Pipeline features and all new mobile tooling targets URP. Starting on Built-in means a mandatory migration mid-project.

**Confidence:** HIGH — URP for 2D mobile has been Unity's standard recommendation since 2022 and remains current.

---

### UI Framework

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| UI Toolkit | 1.x (Unity 6 built-in) | Menus, HUD, level select, settings screens | UI Toolkit (USS/UXML) is Unity's strategic UI direction. Runtime support is production-ready in Unity 6. For a portrait-orientation puzzle game with relatively simple menus, UI Toolkit's flexbox layout model and style sheets make it trivial to support multiple Android screen ratios without per-device anchoring gymnastics. |
| Unity Canvas (uGUI) | Legacy fallback | Gameplay tile grid overlay only, if needed | The tile board itself may be easier to implement as world-space GameObjects (SpriteRenderer) rather than Canvas UI elements. Canvas/uGUI is an acceptable fallback for any UI screen that proves difficult in UI Toolkit, but prefer UI Toolkit for all overlay/screen UI. |

**NOT:** Do not use TextMesh Pro's legacy font workflow with Canvas for primary UI screens if starting fresh — UI Toolkit handles text natively via its label system.

**TextMesh Pro:** Still use TextMeshPro (via `com.unity.ugui` package, included) for any world-space 3D text labels. For UI Toolkit labels, the built-in text rendering is sufficient.

**Confidence:** MEDIUM — UI Toolkit runtime maturity improved significantly in Unity 6. Verify any edge cases (e.g., scroll views with dynamic content) against Unity 6 release notes before committing to it for complex screens.

---

### Ads SDK

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Unity Ads SDK (LevelPlay / IronSource) | **4.x** (via Package Manager: `com.unity.ads`) | Rewarded ads (hints), interstitial ads between levels | The project spec calls for Unity Ads. In 2024 Unity merged ironSource into the LevelPlay mediation platform. The modern Unity Ads SDK 4.x is distributed via the Package Manager and wraps LevelPlay. It supports both rewarded (hint gating) and interstitial (between-level) placements natively. |

**Integration pattern:**
```csharp
// Rewarded ad for hint — initialize once, show on demand
Advertisement.Initialize(gameId, testMode: false);

// Show rewarded
Advertisement.Show("Rewarded_Android", new ShowAdCallbacks {
    onAdFinished = result => {
        if (result == ShowResult.Finished) HintSystem.GrantHint();
    }
});
```

**NOT:** Do not integrate AdMob or Meta Audience Network unless mediation revenue testing shows it necessary. Adding a second SDK in v1 increases build size and consent-flow complexity. LevelPlay mediation can pull in third-party demand without multiple SDKs in your code.

**GDPR / consent:** Unity Ads 4.x includes the User Messaging Platform (UMP) consent flow adapter. Wire this up on first launch — required for Google Play distribution in EEA.

**Confidence:** MEDIUM — Unity Ads 4.x / LevelPlay integration model was current as of mid-2025. Verify current package ID (`com.unity.ads` vs `com.unity.services.ads`) in Package Manager at project start, as Unity has reorganized Gaming Services packages.

---

### Audio

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Unity Audio (AudioSource / AudioMixer) | Built-in | SFX (tile match, tile select, game over), ambient music | For a puzzle game with a modest audio budget (< 20 sound files, 1-2 music tracks), Unity's built-in audio pipeline is sufficient. No external audio middleware needed. |
| AudioMixer | Built-in | Master / Music / SFX volume channels | Expose AudioMixer parameters to a ScriptableObject-backed AudioSettings so the Settings screen can modify volume without direct AudioMixer references scattered across scripts. |

**NOT:** Do not integrate FMOD or Wwise. Both add 5-15MB to APK size and require licensing for commercial release. The complexity overhead is unjustified for Vita Mahjong's audio scope.

**Audio format recommendations (mobile):**
- Music: `.ogg` (Vorbis, ~128kbps) — best size/quality ratio on Android
- SFX: `.wav` (PCM) for short clips under 1 second; `.ogg` for anything longer
- Set compression in AudioClip import settings: Compressed In Memory for music, Decompress On Load for short SFX

**Confidence:** HIGH — Unity's built-in audio is stable and this guidance has been consistent for years.

---

### ScriptableObject Architecture

ScriptableObjects are the right data layer for this project. The project spec already identifies the correct use cases. Below is a prescriptive pattern:

| ScriptableObject Type | Data It Holds | Notes |
|-----------------------|--------------|-------|
| `BoardLayoutSO` | Tile positions (x,y,z) arrays for preset layouts | One asset per layout variant. Loaded by BoardGenerator at level start. |
| `LevelConfigSO` | Level index, layout reference, mode, time limit, target score | One asset per level. Referenced by a `LevelDatabase` (also a SO). |
| `ThemeSO` | Sprite atlas reference, color palette, audio clip references | One asset per theme (Indian theme v1). Injected into TileRenderer and AudioManager at runtime. |
| `GameModeSO` | Mode enum, matching rule delegate reference, UI label | One per mode (Classic, Math, Active Mind). Drives the matching logic without scene reload. |
| `AudioSettingsSO` | Volume floats for Master/Music/SFX | Persisted via `PlayerPrefs` write on change; read-only at runtime from this SO. |

**Runtime mode-swapping pattern:** The `GameModeSO` asset passed to the GameplayManager determines which matching function is called. Swapping modes = swapping the SO reference. No scene reload needed, no mode enum switch scattered through game logic.

**Confidence:** HIGH — This is a well-established Unity architecture pattern with no version dependency.

---

### Testing Frameworks

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Unity Test Framework (UTF) | 1.4.x (bundled) | Unit + integration tests via NUnit | Built into Unity. Two modes: Edit Mode (pure C# logic — board solver, matching rules, free-tile detection) and Play Mode (scene-driven — board generation, ad callbacks). Edit Mode tests run without spinning up a scene and are fast — ideal for TDD on the procedural board logic. |
| NUnit | 3.x (bundled with UTF) | Test assertions | Standard. No additional package needed. |

**Critical tests to write in Edit Mode:**
- `FreeTileDetector.IsFree(x,y,z)` — the core game rule. Test every adjacency edge case before building UI.
- `BoardGenerator.GenerateSolvableBoard()` — verify output is always solvable by running the solver against generated boards.
- `MathMatcher.IsValidPair(tileA, tileB)` — Math Mode sum-to-10 rule validation.
- `MemoryTracker.CanRecall(tileId)` — Active Mind Mode recall state machine.

**NOT:** Do not add a third-party testing framework (e.g., Moq, FluentAssertions). UTF + NUnit is sufficient for game logic. Moq specifically has IL2CPP incompatibility issues that make it unreliable for mobile test builds.

**Confidence:** HIGH — Unity Test Framework 1.4.x ships with Unity 6 LTS.

---

### Build Pipeline

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Android Build Support module | Unity 6 | Android APK/AAB output | Install via Unity Hub alongside the main editor. |
| Gradle (managed by Unity) | 8.x (Unity 6 managed) | Android build system | Unity manages the Gradle wrapper. Do not upgrade Gradle manually — Unity 6's bundled version is tested against its Android build pipeline. Custom `gradleTemplate.properties` needed only if adding native plugins. |
| Google Play Asset Delivery | Via Addressables | Large asset streaming (optional) | Not needed for v1 if APK < 150MB. Revisit if theme assets grow. |
| Unity Addressables | 2.x | Asset streaming, memory management | Use Addressables from day one for ThemeSO sprite atlases and level assets. This prevents loading all level data into memory at startup, which matters for 2GB RAM targets. |

**Build settings for Android release:**
- Backend: IL2CPP (required, see above)
- Target Architectures: ARM64 only (Google Play requires 64-bit; ARMv7 can be dropped to reduce APK size)
- Managed Stripping Level: Medium (start here; High risks stripping needed reflection-dependent code in Unity Ads SDK)
- Minify: Proguard on Release (Unity 6 enables this by default for IL2CPP builds)
- Split Application Binary: Enable if APK exceeds 100MB (produces AAB for Play Store)

**Confidence:** HIGH for IL2CPP/ARM64 mandate — this has been Google Play policy since 2019. MEDIUM for specific Gradle/Addressables versions — verify in Package Manager at project start.

---

### Additional Packages

| Package | Package ID | Purpose | Notes |
|---------|-----------|---------|-------|
| Input System | `com.unity.inputsystem` 1.7.x | Touch input (tap, select tiles) | Use the new Input System over the legacy `Input.GetTouch()`. Provides clean action-based input that works identically in editor (mouse) and device (touch). Essential for portrait-mode tap handling on tile grid. |
| Unity Localization | `com.unity.localization` 1.5.x | String/sprite localization (future) | Not required for v1 if launching English-only, but add the package from day one. Retrofitting localization into a shipped game is painful. Start with a Locale table even if it only contains English entries initially. |
| Addressables | `com.unity.addressables` 2.x | Asset streaming | See Build Pipeline section above. |
| Unity Analytics (optional) | `com.unity.services.analytics` | Level completion funnels, mode usage | Useful for understanding which game modes retain players. Low integration cost. Requires Privacy Manifest on iOS later. |

**Packages to explicitly avoid:**

| Package | Why Not |
|---------|---------|
| Cinemachine | Overkill for a static 2D puzzle camera. Adds complexity and package weight for zero benefit. |
| Timeline | No cutscenes or sequenced animations needed. |
| Netcode for GameObjects | No multiplayer planned. |
| TextMesh Pro (as primary UI) | Superseded by UI Toolkit for screen-level UI in Unity 6. Still valid for world-space labels. |
| Firebase SDK (full suite) | Heavy (~20MB). Use only Firebase Analytics or Crashlytics if analytics is needed, not the full SDK. Unity Gaming Services Analytics is lighter. |

---

### Version Control & Project Settings

| Tool | Version | Notes |
|------|---------|-------|
| Git | Any current | Use Git with `.gitignore` from gitignore.io (Unity preset). Exclude `Library/`, `Temp/`, `Builds/`, `*.apk`, `*.aab`. |
| Git LFS | 3.x | Required for binary assets: sprite atlases, audio files, Unity `.asset` files over 10MB. Without LFS, repo size balloons quickly with Unity binary assets. |

`.gitattributes` minimum for Unity:
```
*.png filter=lfs diff=lfs merge=lfs -text
*.jpg filter=lfs diff=lfs merge=lfs -text
*.psd filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text
*.ogg filter=lfs diff=lfs merge=lfs -text
*.mp3 filter=lfs diff=lfs merge=lfs -text
*.unity filter=lfs diff=lfs merge=lfs -text
*.asset filter=lfs diff=lfs merge=lfs -text
*.prefab filter=lfs diff=lfs merge=lfs -text
```

---

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| Engine version | Unity 6 LTS (6000.0) | Unity 2022.3 LTS | 2022.3 LTS enters end of support in 2025. Starting a new project on it means a forced migration during active development. Unity 6 is the correct starting point. |
| Render pipeline | URP 2D Renderer | Built-in Pipeline | Built-in is legacy. No new mobile features. Worse batching. Would require migration later. |
| UI framework | UI Toolkit | uGUI (Canvas) | uGUI requires manual anchoring for every screen size variant. UI Toolkit's flexbox model handles screen ratios automatically. uGUI is still acceptable for the tile grid if implemented as world-space sprites. |
| Audio middleware | Unity built-in | FMOD | FMOD adds 10-15MB APK size and commercial license cost. Unjustified for this audio scope. |
| Ads | Unity Ads (LevelPlay) | AdMob standalone | AdMob requires Google's UMP separately. Unity Ads 4.x bundles consent flow. Single SDK is simpler for v1. |
| Scripting backend | IL2CPP | Mono | Mono is for editor/debug only. Never ship Android with Mono — fails Google Play 64-bit requirement and performs worse. |
| Testing | UTF + NUnit | Moq + custom framework | Moq has known IL2CPP stripping issues. UTF NUnit built-in is sufficient. |
| Architecture | ScriptableObject + services | Zenject/VContainer DI | Dependency injection frameworks add learning overhead and are overkill for a single-scene puzzle game. ScriptableObjects as service locators achieve loose coupling without a DI container. |

---

## Installation (Package Manager manifest additions)

The following entries belong in `Packages/manifest.json` alongside Unity's auto-populated entries:

```json
{
  "dependencies": {
    "com.unity.2d.sprite": "1.0.0",
    "com.unity.2d.tilemap": "1.0.0",
    "com.unity.addressables": "2.2.2",
    "com.unity.ads": "4.12.2",
    "com.unity.inputsystem": "1.7.0",
    "com.unity.localization": "1.5.1",
    "com.unity.render-pipelines.universal": "17.0.3",
    "com.unity.services.analytics": "6.0.1",
    "com.unity.test-framework": "1.4.5",
    "com.unity.textmeshpro": "3.0.9"
  }
}
```

**Version note (MEDIUM confidence):** These version numbers were current as of mid-2025. Resolve the latest compatible versions for Unity 6 LTS using Package Manager's "Update" view at project creation. The package IDs themselves are HIGH confidence and stable.

---

## Mobile-Specific Considerations

### Performance for Mid-Range Android (2GB RAM, 2017 GPU)

1. **Draw calls:** Target < 50 draw calls per frame on the board screen. Use a single Sprite Atlas for all tile number sprites and the board background. URP's sprite batching handles this when sprites share material and atlas.

2. **Texture compression:** Use ETC2 for Android (ASTC is better but requires API level 21+ GPU, which 2017 devices inconsistently support). ETC2 is universally supported. Set in Texture Import Settings per-platform.

3. **Memory budget:** Keep total runtime memory under 400MB on a 2GB device (OS + other apps consume significant headroom). Addressables + async loading of level assets keeps the base scene lean. Unload unused assets between levels via `Resources.UnloadUnusedAssets()` or Addressables' release pattern.

4. **GC pressure:** Avoid `new` allocations in `Update()` and tile-matching loops. Pre-pool `List<TileData>` for free-tile queries. Use `struct` over `class` for tile position data passed in hot paths.

5. **Battery:** Fixed timestep at 60fps feels unnecessary for a turn-based puzzle game. Cap frame rate at 30fps for menus and idle states via `Application.targetFrameRate = 30`. The board screen can run at 60fps for responsive tap feedback.

### Build Size

- Target APK under 100MB for v1 to avoid Play Asset Delivery complexity.
- Compress audio aggressively (Vorbis 96kbps for music on Android).
- Strip unused shader variants: enable "Strip Unused Shader Variants" in URP asset settings.
- Use AAB (Android App Bundle) format for Play Store — Google re-compresses per device, saving 20-30% on user download size.

### IL2CPP Build Time

IL2CPP builds are significantly slower than Mono (5-20 minutes vs. 30 seconds). Development workflow:
- Use Mono backend for Play Mode testing in editor (fast iteration).
- Switch to IL2CPP only for device testing and release builds.
- Maintain two build configurations in Build Settings.

---

## Sources

All findings are from training knowledge (cutoff August 2025). No live sources were accessible during this session (WebSearch and WebFetch permissions denied).

Verify at project start against:
- Unity release notes: https://unity.com/releases/editor/archive
- Unity Package Manager: Window > Package Manager > Unity Registry
- Unity Ads / LevelPlay docs: https://docs.unity.com/ads/
- Android developer policies: https://developer.android.com/google/play/requirements/64-bit

**Confidence summary:**
- Unity 6 LTS as current production version: HIGH
- URP 2D Renderer for mobile 2D: HIGH
- IL2CPP + ARM64 mandatory for Google Play: HIGH
- Unity Ads 4.x / LevelPlay integration model: MEDIUM (verify package ID)
- UI Toolkit runtime maturity for all screens: MEDIUM (test scroll/dynamic content)
- Specific package version numbers: MEDIUM (resolve fresh in Package Manager)
- ScriptableObject architecture patterns: HIGH (not version-dependent)
- Audio built-in sufficiency: HIGH
