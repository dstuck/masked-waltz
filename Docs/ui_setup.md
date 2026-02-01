# UI Setup (Beat Input Frames)

This UI is a clear **3-frame beat input display** that shows, per beat in the current measure:
- `A` = **Away** (`Apart`)
- `T` = **Together**
- `_` = nothing yet
- `X` = multiple inputs in the same bucket (invalid)

The display is driven by `BeatInputUI` (a small UI script) which reads rhythm state from `PlayerController` and relies on a running `BeatClock`.

## BeatClock setup (required)

You should create `BeatClock` once in the scene and wire references explicitly.

### 1) Add `BeatClock` to the scene
- Select the `BeatManager` GameObject (the one that has the music `AudioSource`).
- Add `BeatClock` (`Assets/Scripts/Music/BeatClock.cs`) as a component on that same GameObject.

### 2) Configure `BeatClock`
In the `BeatClock` inspector:
- `_beatsPerMeasure`: set to **3**
- `_beatManager`: drag the `BeatManager` component (same GameObject if you added `BeatClock` onto `BeatManager`)
- `_beatIndexOffset`: set to **0** normally. If your UI/measure feels shifted (Frame1 corresponds to beat 2), set this to **-1** to make Frame1 line up with the downbeat.

### 2a) Configure BPM (single source of truth)
Set BPM and audio source **only** in `BeatManager`:
- `BeatManager._bpm`: set to your song BPM (e.g. **150**)
- `BeatManager._audioSource`: drag the `AudioSource` playing the music

### 3) Hook up the player’s rhythm references
Select the `Player` GameObject (has `PlayerController`) and assign:
- `PlayerController._beatClock`: drag the `BeatClock` component you added above
- `PlayerController._playerMarker`: drag the `PlayerMarker` object (so invalid patterns can flicker)

If these references are not assigned, the UI will stay as `_ _ _` and rhythm input bucketing won’t run.

## Beat input frames (required)

We display the current measure’s 3 beat buckets using **three framed UI slots**.

### 1) Create the 3 frames
- Under your `Canvas`, create an empty `GameObject` named `BeatInputFrames`.
- Create three children: `Frame1`, `Frame2`, `Frame3`.
- On each frame GameObject:
  - Add an `Image`
  - Assign your **`UIFrame`** sprite to that Image
  - Size/position them (e.g. a small horizontal row near the bottom)

### 2) Add text to each frame
- Under each frame, create a child **TextMeshPro** text (UI).
  - Center it in the frame (anchors + alignment)
  - Pick a readable font size
  - Initial text can be `_`

### 3) Add `BeatInputUI` and hook up the slots (recommended)
- Select `BeatInputFrames` and add `BeatInputUI` (`Assets/Scripts/UI/BeatInputUI.cs`).
- Assign references:
  - `_player`: drag the `Player` (or the `PlayerController` component)
  - `_beat1`: drag `Frame1/Text (TMP)`
  - `_beat2`: drag `Frame2/Text (TMP)`
  - `_beat3`: drag `Frame3/Text (TMP)`

Note: invalid 3-beat patterns still trigger `PlayerMarker.Flicker()` at measure boundaries.

