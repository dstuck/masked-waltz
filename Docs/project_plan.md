# Masked Waltz

## Overview

Find your partner in the masquerade ball. You switch partners and positions until you find your true partner.
Dancers flow around and you need to listen/watch for feedback when near target and move between dancers while keeping rhythm

Flow patterns marked on the ground showing which direction dancers will move. They overlap allowing you to move between flows.

## Controls
Press alternating buttons on waltz beat: A B B, A B B
Directional inputs control how fast or slow you move within the flow

Missing the beats causes you to stumble and not act for the next measure
You can swap masks with your partner or swap partners with nearby dancers based on which inputs you use, A A A to swap masks and B B B to swap partners in the direction you’re pressing

Handling beat inputs: inputs span from half beat to half beat. Multiple or missed inputs during the same beat cycle causes the dancers to break and miss control for a cycle. One input moves together and one moves away and the down beat has twice as much movement so you either end back up in the starting position, you change mask or partners or you miss and get reset to middle position

## Implementation Plan

v0.1 - sync music
- [ ] Play song
- [ ] Screen pulse on beat

v0.2 - dancers
- [ ] Dancers travel in a circle
- [ ] Red square on one dancer specifies player
- [ ] Movement input can shift the player's pair relative to circular dance

v0.3 - rhythm inputs
- [ ] Player stores 1-2-3 waltz beat
- [ ] Player tracks 3 input pattern
- [ ] Player flickers if not a valid ATT, TAA, AAA, or TTT input

v0.4 - partner swapping
- [ ] AAA pattern changes partners shifting the mask to the nearest dancing pair if within distance
- [ ] BBB pattern between leader and follower
- [ ] Add 3 overlapping circles of dancers

v0.5 - true partner
- [ ] True partner is selected at start of game from a distant dancer
- [ ] The UI beat pulse changes based on how close you are to the target
- [ ] Response when you find your partner

v0.6 - cleanup
- [ ] Dancing pair return back to their standard spots when not player controlled
- [ ] ...additional cleanups