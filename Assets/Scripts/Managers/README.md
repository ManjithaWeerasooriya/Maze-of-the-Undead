# Managers — Maze of the Undead

This folder contains all singleton and scene-level manager scripts.

| Script | Type | Persists Across Scenes | Responsibility |
|---|---|---|---|
| `GameManager` | Singleton | Optional | Game state, win/lose flow, scene loading |
| `ExitTrigger` | Scene component | No | Detects player reaching the exit and fires events |
| `AudioManager` | Singleton | Yes (always) | Music playback, volume channels, fade transitions |
| `SceneMusic` | Scene component | No | Declares which music track plays in a given scene |
| `AmbientSource` | Scene component | No | Plays looping ambient SFX tied to the SFX volume channel |

---

## GameManager

**Script:** `GameManager.cs`

Central game state owner. Tracks whether the game is `Playing`, `Paused`, `Won`, or `Lost`, and drives the win flow when `ExitTrigger` fires. Can optionally persist across scenes.

### Scene Setup

1. Create an empty GameObject → `_GameManager`
2. Add Component → `Game Manager`
3. Assign `Win Screen` (the UI panel to show on win) in the inspector
4. Enable `Persist Across Scenes` only if you need the same instance across multiple levels — leave it off if each scene has its own `GameManager`

### Inspector Fields

| Field | Default | Description |
|---|---|---|
| Persist Across Scenes | false | Enables `DontDestroyOnLoad` |
| Pause On Win | true | Sets `Time.timeScale = 0` when the player wins |
| Unlock Cursor On Win | true | Shows and unlocks the cursor on win |
| Win Screen | — | Optional UI panel toggled on when the player wins |

### Game States

```
Playing  →  Won   (player reaches exit)
Playing  →  Lost  (future: player dies)
Any      →  Paused (pause menu)
```

### Public API

```csharp
GameManager.Instance.State                  // current GameState enum value
GameManager.Instance.StateChanged           // event Action<GameState> — subscribe to react to state changes
GameManager.Instance.WinLevel();            // trigger win flow manually
GameManager.Instance.RestartLevel();        // reload current scene
GameManager.Instance.LoadNextLevel();       // load next scene in Build Settings order
GameManager.Instance.LoadScene("SceneName"); // load a specific scene by name
```

### How win is triggered

`GameManager` listens to the static `ExitTrigger.ExitReached` event. When the player steps into the exit volume, `ExitTrigger` fires the event, `GameManager` calls `WinLevel()`, shows the win screen, and optionally freezes time. No direct reference between the two components is needed.

---

## ExitTrigger

**Script:** `ExitTrigger.cs`

Detects when the player enters the goal-room exit volume and raises events. Does not handle win logic itself — that is `GameManager`'s responsibility.

### Scene Setup

1. Create a GameObject with a `Collider` component (Box, Sphere, or Capsule)
2. Add Component → `Exit Trigger`
3. Make sure the Collider is marked as **Is Trigger** (the script forces this on Awake if forgotten)
4. Position and size the collider to cover the exit area

### Inspector Fields

| Field | Default | Description |
|---|---|---|
| Detection Mode | By Tag | `ByTag` checks the `playerTag` string; `ByLayer` checks against `playerLayer` |
| Player Tag | `"Player"` | Tag used when Detection Mode is By Tag |
| Player Layer | Layer 6 | Layer mask used when Detection Mode is By Layer |
| One Shot | true | Fires once per scene load; call `ResetTrigger()` to re-arm |
| Retrigger Cooldown | 1s | Minimum seconds between triggers when One Shot is false |
| On Player Exit Reached | — | Inspector `UnityEvent` — wire UI sounds or visual effects here |

### Events

```csharp
// Static event — subscribe without a direct reference (how GameManager uses it)
ExitTrigger.ExitReached += (trigger) => { /* handle win */ };

// Inspector UnityEvent — wire directly in the Unity inspector
trigger.OnPlayerExitReached.AddListener(() => { /* e.g. play sound */ });
```

### Gizmos

The trigger volume is drawn in the Scene view as a green fill with an outlined border — green for Box, Sphere, and Capsule colliders.

### Public API

```csharp
trigger.ResetTrigger(); // re-arms the trigger (useful after scene reload)
```

---

## AudioManager

**Script:** `AudioManager.cs`

A singleton that persists across all scene loads. Owns the music `AudioSource`, the three volume channels (Master / Music / SFX), and all fade transitions. All other audio scripts communicate through `AudioManager.Instance`.

### Scene Setup

1. Open `Assets/Scenes/MainMenu.unity` — place AudioManager **only here**
2. Create an empty GameObject → `_AudioManager`
3. Add Component → `Audio Manager`
4. **Uncheck `Play Music On Start`** — `SceneMusic` components control per-scene music
5. Set initial volumes (used on first run; PlayerPrefs takes over after)

> Only place AudioManager in the **first scene that loads**. It calls `DontDestroyOnLoad` on itself and survives into every subsequent scene. Adding it to Level_01 as well will cause the duplicate to self-destruct.

### Inspector Fields

| Field | Default | Description |
|---|---|---|
| Default Music Clip | — | Direct AudioClip assignment (optional) |
| Default Music Resource | `Music/Theme` | Resources path fallback when no clip is assigned |
| Play Music On Start | true | Auto-plays on first scene load — disable when using SceneMusic |
| Loop Music | true | Whether the music source loops |
| Initial Master / Music / SFX Volume | 1 / 0.7 / 1 | Used on first run only |
| Default Fade Duration | 1.0s | Fade time for PlayMusic / StopMusic |
| Cross Fade Duration | 1.5s | Fade time used during scene transitions |
| Music Ignores Game Pause | true | Music keeps playing when `Time.timeScale = 0` |
| Fade Music On Game End | true | Auto-fades music when GameManager reports Won or Lost |
| Debug Logs | true | Console messages confirming audio events |

### Volume Channels

Volume is split into three independent channels. Final output is multiplied together:

```
Music output = MasterVolume × MusicVolume × (IsMusicMuted ? 0 : 1)
SFX output   = MasterVolume × SfxVolume
```

All values are persisted via `PlayerPrefs` and survive between game sessions.

### Public API

```csharp
// Read current values (e.g. to initialise Settings sliders)
AudioManager.Instance.MasterVolume
AudioManager.Instance.MusicVolume
AudioManager.Instance.SfxVolume
AudioManager.Instance.IsMusicMuted
AudioManager.Instance.EffectiveSfxVolume   // Master × SFX — use this in PlayOneShot calls

// Volume setters — called by Settings panel sliders
AudioManager.Instance.SetMasterVolume(float value);
AudioManager.Instance.SetMusicVolume(float value);
AudioManager.Instance.SetSfxVolume(float value);
AudioManager.Instance.SetMusicMuted(bool muted);
AudioManager.Instance.ResetVolumesToDefaults();

// Music playback
AudioManager.Instance.PlayMusic(AudioClip clip, float fadeDuration = -1f);
AudioManager.Instance.PlayMusic(string resourceName, float fadeDuration = -1f);
AudioManager.Instance.PauseMusic();
AudioManager.Instance.ResumeMusic();
AudioManager.Instance.StopMusic(float fadeDuration = -1f);
```

### Volume Change Event

Fires a `VolumeSnapshot` every time any volume changes. Wire the Settings panel to it so sliders stay in sync if volume is changed from elsewhere:

```csharp
AudioManager.Instance.VolumeChanged.AddListener(snapshot =>
{
    masterSlider.SetValueWithoutNotify(snapshot.master);
    musicSlider.SetValueWithoutNotify(snapshot.music);
    sfxSlider.SetValueWithoutNotify(snapshot.sfx);
    muteMusicToggle.SetIsOnWithoutNotify(snapshot.musicMuted);
});
```

### Playing one-shot SFX from other scripts

```csharp
audioSource.PlayOneShot(clip, AudioManager.Instance.EffectiveSfxVolume);
```

---

## SceneMusic

**Script:** `SceneMusic.cs`

Tells `AudioManager` which track to cross-fade into when a scene loads. One per scene. Keeps scene-specific music data inside the scene so `AudioManager` never needs to know about individual scenes.

### Setup (per scene)

1. Create an empty GameObject → `_SceneMusic`
2. Add Component → `Scene Music`
3. Set **Music Resource Name** to the `Resources/` path of the track

| Scene | Music Resource Name |
|---|---|
| MainMenu | `Music/Main_Menu` |
| Level_01 | `Music/Level_Exploration` |

### Inspector Fields

| Field | Description |
|---|---|
| Music Clip | Direct AudioClip assignment — takes priority over resource name |
| Music Resource Name | Resources path, e.g. `Music/Level_Exploration` |
| Fade Duration | `-1` uses AudioManager's default; positive value overrides per scene |

### Why music doesn't play when opening Level_01 directly in the editor

`AudioManager` only exists if it was created in a previously loaded scene. Playing Level_01 directly skips Main Menu, so `AudioManager.Instance` is null when `SceneMusic.Start()` runs.

**Fix:** In `File > Build Settings`, drag `MainMenu.unity` to position 0. For a more robust solution, create a `Bootstrap.unity` scene (index 0) containing only `_AudioManager` that immediately loads `MainMenu`.

---

## AmbientSource

**Script:** `AmbientSource.cs`

Plays looping ambient audio (wind, dripping, hum) and keeps its volume tied to the `AudioManager` SFX channel. Supports a random clip pool and an intermittent playback mode so ambient sounds feel natural rather than mechanically looped.

### Setup

1. Create an empty GameObject → e.g. `_Ambience_Wind`
2. Add Component → `Ambient Source` (adds `Audio Source` automatically)
3. Assign clips to the **Clips** array — or use **Resource Names** for Resources-based loading
4. Tune Playback Mode, interval, and Base Volume

### Inspector Fields

| Field | Description |
|---|---|
| Clips | Pool of AudioClips — one is chosen at random each play |
| Resource Names | Same as Clips but loaded by Resources path at runtime |
| Playback Mode | `RandomInterval` — plays, pauses, repeats with gaps. `Loop` — one clip loops forever |
| Min Interval | Shortest silence gap between clips (seconds) — `RandomInterval` only |
| Max Interval | Longest silence gap between clips (seconds) — `RandomInterval` only |
| Base Volume | Per-source volume before AudioManager scaling |
| Play On Enable | Start playing as soon as the GameObject activates |

### Recommended settings for wind ambience in Level_01

| Field | Value |
|---|---|
| Clips | `Wind.ogg`, `Wind2.ogg`, `Wind3.ogg` |
| Playback Mode | Random Interval |
| Min Interval | `5` |
| Max Interval | `15` |
| Base Volume | `0.35` |
| AudioSource → Spatial Blend | `0` (2D — heard everywhere in the level) |

For positional sounds (e.g. dripping near a wall), set **Spatial Blend = 1** on the `Audio Source` and place the GameObject at the sound's world position.

---

## Audio File Structure

All audio assets must live inside `Assets/Resources/` so they can be loaded at runtime by name.

```
Assets/Resources/
├── Music/
│   ├── Main_Menu.ogg
│   ├── Level_Exploration.mp3
│   ├── Game_Over.ogg
│   └── Victory.ogg
├── SFX/
│   └── Ambience/
│       ├── Wind.ogg
│       ├── Wind2.ogg
│       └── Wind3.ogg
├── UI/
│   └── ButtonClick.mp3
└── Zombie/
    ├── zombie-moan1.mp3 … zombie-moan5.mp3
    └── ZombieAttack1.mp3 … ZombieAttack4.mp3
```

---

## Settings Panel Integration

The Settings panel has no direct dependency on `AudioManager`'s internals. It only calls public methods and reads public properties. Wire it like this in the Settings panel's `Start` or `OnEnable`:

```csharp
// Populate sliders with saved values on open
masterSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
muteMusicToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsMusicMuted);

// Hook slider/toggle events to AudioManager methods
masterSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSfxVolume);
muteMusicToggle.onValueChanged.AddListener(AudioManager.Instance.SetMusicMuted);

// Optional: reset button
resetButton.onClick.AddListener(AudioManager.Instance.ResetVolumesToDefaults);
```

All four `Set*` methods can also be wired in the **inspector** — drag the `_AudioManager` GameObject into the slider's `OnValueChanged` event and pick the method from the dropdown.

---

## Verification Checklist

- Play from Main Menu → console: `[AudioManager] Now playing music: Main_Menu`
- Click Start → console: `[AudioManager] Now playing music: Level_Exploration` with a cross-fade
- Wind ambience starts in Level_01, plays intermittently with natural gaps
- Reach the exit → `ExitTrigger` fires → `GameManager` calls `WinLevel()` → win screen appears → music fades out
- Adjusting volumes in Settings updates music and ambient levels in real time
