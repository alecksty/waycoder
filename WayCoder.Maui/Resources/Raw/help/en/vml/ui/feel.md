# Sound and haptics

Synthesized tones, audio files, vibration, keeping the screen on.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).
>
> **To hear this whole group of calls in one go**: run `vml run examples/c/audio_all.c` on the Shell page -
> it walks you through them one at a time (12 items, about 25 seconds) and states what each one checks.

### `ui_beep(int freq, int ms)`
**Synthesizes** a tone on the spot (no audio file needed): `freq` in hertz, `ms` in milliseconds.
Note: it is **single-channel** - only one tone sounds at a time, and when you fire several in a row only the last is heard. So express good and bad through **pitch**: 880Hz for one line cleared, 1568Hz for four, and the win sound drowns out everything.
Note: this is its **old semantics and they have not changed**. To sound several notes **at once** (chords / melodies) use `ui_tone_on/off` below.
```c
ui_beep(880, 80);      /* one line cleared */
ui_beep(1568, 160);    /* four lines cleared, higher pitch */
```
### `ui_tone_on(int ch, int note, int vel)` / `ui_tone_off(int ch, int note)`
**Polyphony** - several notes sound **at the same time** (up to 32 voices).
The note number has **real MIDI semantics**: A4 = 69 = 440Hz, middle C = 60. Channels are 0-15 and velocity is 0-127 (**0 is the same as note off**).

A chord is several notes started in a row and **not stopped in between**:
```c
ui_tone_on(0, 60, 100);   /* do  */
ui_tone_on(1, 64, 100);   /* mi  */
ui_tone_on(2, 67, 100);   /* sol - all three sound at once */
...
ui_tone_off(2, 67); ui_tone_off(1, 64); ui_tone_off(0, 60);
```
Note: the arguments are **always clamped, never rejected**: an out-of-range note number is clamped into 0-127 and still sounds, rather than silently going quiet.
Note: `ui_beep` and it **do not interfere with each other** - the beep uses its own dedicated channel, so you can fire sound effects while a chord is held without any problem.

Ready-made examples: `Examples/c/audio_test.c` (scales/chords/melodies) and `Examples/c/piano.c` (a piano).

#### Which one should game sound effects use? - **`ui_beep` single notes** (⚠ not the sequencer)

**The conclusion first**: game event cues always use `ui_beep(freq, ms)`.
The **sequencer** in the standard library (`ui_sfx_add` / `ui_sfx_tick`) **breaks up on a real device**,
and on 2026-09-26 this repository moved the sound effects of **all 28 games** back from it to `ui_beep`.

**Why it breaks up** (both are **configuration** problems, not mechanism problems):

| Cause | What is happening |
|---|---|
| **Several voices sounding at once** | A hit is configured as a three-voice chord and a crash as three low notes blaring together, so any stacked mixing runs straight into **clipping** |
| **Long trailing notes** | The final note of the win/lose cue trails for 12-14 ticks (one tick is 33ms, so about 400ms), which buzzes through the speaker |

`ui_beep` is **single-channel** (the next tone cuts off the previous one), so **there is never more than one tone sounding per event** and clipping is structurally impossible. The price is no chords and no timbre - which is enough for **one-shot cues** such as "hit / explode / win or lose".

```c
void sfx_hit(void)  { ui_beep(523, 264); }   /* hit: bright and clear */
void sfx_boom(void) { ui_beep(131, 198); }   /* crash: one dull thud (the lowest) */
void sfx_win(void)  { ui_beep(1047, 320); }  /* win: highest and longest */
void sfx_lose(void) { ui_beep(131, 320); }   /* lose: lowest and longest */
```

**How to pick the frequencies** (a mechanical rule, do not change them on a whim): take the **first note** of the original motif (MIDI to Hz);
**for the outcome take the extremes at either end** (the win takes the highest note, the lose the lowest - "you can tell win from lose without looking at the screen" rests on exactly this, and both the Gomoku and the chess versions are configured this way: win 1320 / lose 240); the duration is the duration of the whole block, **capped at 320ms**.

Note two more things: **do not write the low notes too low** - a phone speaker rolls off fast below 200Hz, so C2 (65Hz) comes out as a muffled "puff" and the player hears it as **nothing playing** (do not go below C3 = 131Hz); and **win and lose must make a sound**, because at that moment the player is looking at the board, not at the banner you drew.
Examples to copy: `examples/c/gomoku.c`, `chess.c` (the cleanest), `examples/cpp/gorilla.cpp` (the most complete, eight kinds).
The full version is in the developer guide on VML game development under `docs/` (section 4.5).

#### Is the `ui_sfx_*` sequencer still usable?

**Not in games any more.** The mechanism itself is fine (it handles the easily forgotten things for you, such as "an old slot on the same channel has to be stopped first" and "catching up ticks from real elapsed time"), but as soon as you configure **stacked voices** or **long trailing notes** you are back to the two causes of breakup above - and **you cannot verify it on the desktop** (the desktop `vmlcli` does not go through the phone's mixing path). If you really want to use it, configure it as "one voice plus at most 320ms per note" first, and **listen on a real device**.

### `ui_tone_all_off(void)` / `ui_tone_voices(void)`
Turns everything off (with a **fade-out**, not a hard cut); reports how many voices are sounding right now - useful while tuning a chord.
### `ui_tone_wave(int ch, int wave)` / `ui_tone_max_voices(int n)`
Sets the default waveform of **one channel** (`VML_WAVE_SINE`(0) / `SQUARE`(1) / `SAW`(2) / `TRIANGLE`(3)) and
sets **the cap on simultaneous voices** (1-32, default `VML_TONE_MAX_VOICES` = 32).
Different timbres are half of "a chord that does not sound muddy": when several notes sound at once, **give the low voice `TRIANGLE`** (fewer harmonics, so it does not fight) and leave `SQUARE` for the melody (bright) - that is far clearer than using the same waveform for everything.
```c
ui_tone_wave(0, VML_WAVE_TRIANGLE);   /* channel 0 as the low pad */
ui_tone_wave(1, VML_WAVE_SQUARE);     /* channel 1 carries the melody */
ui_tone_max_voices(16);               /* as many voices as you need; too many mask each other */
```
### `ui_tone_panic(void)`
Stops everything **immediately** (**no fade-out**, the sound is cut on the spot). Use it for things like "the user pressed force stop" - the tens of milliseconds it takes a fade-out to go quiet are, under the semantics of an emergency stop, "not stopped".
```c
ui_tone_panic();   /* one-key mute */
```
Note: an emergency stop only **stops the voices**, so your own bookkeeping must be cleared along with it - otherwise the finger that is "still held down" will, when it lifts, decrement a ledger that has already been emptied, and that key will never produce a note again. The "mute" key in `Examples/c/piano.c` does all three things together: `ui_tone_panic()` plus clearing the reference counts plus clearing the slot bookkeeping.
### `ui_audio_play(char* path, int loop)`
Plays an **audio file** (a relative path is resolved against the sandbox root). A non-zero `loop` = repeat (for BGM).
Returns 0 on success / -1 on failure (no such file / illegal path / platform does not support it).
```c
ui_audio_play("bgm.mp3", 1);   /* play in a loop */
```
### `ui_audio_stop(void)`
Stops the audio that is currently playing.
```c
ui_audio_stop();
```
### `ui_audio_volume(int volume)`
Overall volume 0-100 (out-of-range values are clamped). It applies to audio played **afterwards**.
```c
ui_audio_volume(60);
```
### `ui_keep_on(int on)`
The keep-screen-on switch (anyone writing a game should turn it on).
```c
ui_keep_on(1);   /* 1 on, 0 off */
```
### `ui_vibrate(int ms, int strength)`
Vibration: `ms` milliseconds, `strength` intensity (0-255, 0 = use the system default).
```c
ui_vibrate(120, 0);
```
### `ui_vibrate_pattern(int* pattern, int count)`
Vibrates to a **rhythm**: `pattern` is an int array (odd indices = pause, even indices = vibrate, the same semantics as Android),
`count` is the number of segments. Returns 0 on success / -1 on failure.
```c
int pat[4] = { 100, 60, 100, 60 };
ui_vibrate_pattern(pat, 4);   /* buzz-pause-buzz-pause */
```
