# Masked Waltz

## Overview

Find your partner in the masquerade ball. You switch partners and positions until you find your true partner.
Dancers flow around and you need to listen/watch for feedback when near target and move between dancers while keeping rhythm

Flow patterns marked on the ground showing which direction dancers will move. They overlap allowing you to move between flows.

## Dance Movement
Dancers are in pairs and each pair is in a rotating dance circle. There will be multiple overlapping dance circles so that dancers end up crossing paths. The circles will rotate and the dance pairs position should move with the parent but their rotation should not alter

## Controls
Press alternating buttons on waltz beat: A B B, A B B
Directional inputs control how fast or slow you move within the flow

Missing the beats causes you to stumble and not act for the next measure
You can swap masks with your partner or swap partners with nearby dancers based on which inputs you use

Handling beat inputs: inputs span from half beat to half beat. Multiple or missed inputs during the same beat cycle causes the dancers to break and miss control for a cycle. One input moves together and one moves away and the down beat has twice as much movement so you either end back up in the starting position, you change mask or partners or you miss and get reset to middle position

## Implementation Plan

v0.1 - sync music
- [x] Play song
- [x] Screen pulse on beat

v0.2 - dancers
- [x] Dancers travel in a circle but they do not rotate
- [x] Red square on one dancer specifies player
- [x] Movement input can shift the player's pair relative to circular dance

v0.3 - rhythm inputs
- [x] Beat clock tracks **beats** and **half-beats** from the music `AudioSource` (sample-time based)
- [x] Use half-beats as the **bucket boundaries** (inputs are captured from half-beat to half-beat, centered on each beat)
- [x] Track **downbeat** (every 3 beats) and expose `beatInMeasure` as **1-2-3**
- [x] Player stores current waltz step (1-2-3) and the **last 3 beat inputs** for the current measure
- [x] Record `Apart` / `Together` presses into the current beat bucket; **multiple presses in one bucket** marks that beat invalid
- [x] At the end of each measure, validate pattern ∈ {`ATT`, `TAA`, `AAA`, `TTT`}; otherwise **player flickers**

v0.4 - partner swapping
- [x] `AAA` pattern transfers player control to the nearest `DancePair` **in the held movement direction** (within distance / cone)
- [x] `TTT` pattern swaps between **leader** and **follower** (moves the player marker between them)
- [x] Player movement remains **simple world-space movement** from input, gated by **stumble lockout**; `Apart`/`Together` inputs move the **dancers within the pair** (towards/away, downbeat 2×) and **in-pair** offsets reset on invalid patterns and at every measure boundary (no world teleport)
- [x] Non-player pairs automatically step **`ATT`** (Away, Together, Together) each measure

v0.5 - true partner
- [ ] True partner is selected at start of game from a distant dancer
- [ ] The UI beat pulse changes based on how close you are to the target
- [ ] Response when you find your partner

v0.6 - cleanup
- [ ] Dancing pair return back to their standard spots when not player controlled
- [ ] ...additional cleanups
