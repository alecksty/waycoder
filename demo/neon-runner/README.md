# 🏃 NEON RUNNER — 3D Endless Runner

A complete 3D web game built with vanilla JavaScript and Three.js. No build step, no bundler — just open `index.html` in a browser (or serve the folder over HTTP).

## ✨ Features

- **3D endless runner** with procedurally generated track chunks
- **Three lanes** with smooth switch animation
- **Three obstacle types**:
  - 🔴 Tall barriers — dodge sideways
  - 🟠 Low barriers — jump over
  - 🟢 Overhead beams — slide under
- **Double jump**, **slide**, **boost** (Shift) to build score multiplier
- **Coins** (+5 score), **Magnet** (attracts coins), **Shield** (absorbs one hit)
- **3 lives** with invulnerability frames after a hit
- **Cyberpunk neon visuals**: PBR materials, dynamic lighting, shadows, fog, animated cityscape backdrop, neon glow
- **Procedural audio**: Web Audio API generates all SFX and synthwave music — zero audio files
- **Persistent high score** via localStorage
- **Pause, restart, how-to-play, credits** screens
- **Polished HUD** with speed bar, coin counter, lives, multiplier
- **Floating notifications** + center messages
- **Responsive** layout (works on mobile and desktop)

## 🎮 Controls

| Key | Action |
|---|---|
| `←` `→` or `A` `D` | Switch lane |
| `Space` / `W` / `↑` | Jump (double-tap for double jump) |
| `S` / `↓` | Slide |
| `Shift` | Boost (build score multiplier) |
| `P` / `Esc` | Pause |
| `M` | Mute |
| `R` | Restart (after game over) |

## 🚀 Run it

```bash
cd demo/neon-runner
python3 -m http.server 8000
# then open http://localhost:8000
```

Or just open `index.html` directly in a modern browser (Chrome, Edge, Firefox, Safari).

## 📂 Project structure

```
neon-runner/
├── index.html         # All UI screens, importmap, entry
├── css/style.css      # Cyberpunk neon theme
└── js/
    ├── main.js        # Game loop, state machine, UI wiring
    ├── world.js       # Three.js scene, renderer, lighting, cityscape
    ├── player.js      # Player character: physics, jump, slide, lane switch
    ├── track.js       # Procedural chunk generator, obstacles, coins, pickups
    ├── input.js       # Keyboard input manager
    └── audio.js       # Procedural SFX + synthwave music (Web Audio API)
```

## 🛠 Tech

- **Three.js 0.160** (via CDN / importmap)
- **Pure ES6 modules** — no build step
- **Web Audio API** for all sound
- **CSS custom properties** for theming
- **localStorage** for high score persistence

## 📜 License

MIT — feel free to fork, learn, remix.
