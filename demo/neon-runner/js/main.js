// =====================================================================
// main.js — Entry point: game state machine, loop, UI wiring
// =====================================================================
import * as THREE from 'three';
import { World } from './world.js';
import { Player } from './player.js';
import { Track } from './track.js';
import { InputManager } from './input.js';
import { AudioSystem } from './audio.js';

// Top-level error display so failures are visible instead of silent.
function showFatalError(msg) {
  const tip = document.getElementById('loadingTip');
  if (tip) {
    tip.innerHTML = '⚠ ' + msg;
    tip.style.color = '#ff3b6b';
  }
  console.error('[Neon Runner] Fatal:', msg);
}
window.addEventListener('error', (e) => {
  if (e.error) showFatalError(e.error.message || String(e.error));
  else showFatalError(e.message);
});
window.addEventListener('unhandledrejection', (e) => {
  showFatalError('Promise rejected: ' + (e.reason && e.reason.message ? e.reason.message : e.reason));
});

// ============= State =============
const STATE = { LOADING: 'loading', MENU: 'menu', PLAYING: 'playing', PAUSED: 'paused', GAMEOVER: 'gameover' };
let state = STATE.LOADING;

const STORAGE_KEY = 'neon-runner-best';

// ============= DOM refs =============
const $ = (id) => document.getElementById(id);
const screens = {
  loading: $('loading'),
  menu: $('menu'),
  hud: $('hud'),
  pause: $('pause'),
  howto: $('howto'),
  gameover: $('gameover'),
  credits: $('credits'),
  notifs: $('notifs')
};

// ============= Game =============
const mount = $('gameMount');
const world = new World(mount);
const player = new Player(world.scene);
const track = new Track(world.scene);
const input = new InputManager();
const audio = new AudioSystem();

// Game stats
const stats = {
  score: 0,
  coins: 0,
  distance: 0,           // meters
  lives: 3,
  maxLives: 3,
  speed: 18,             // current speed (m/s)
  baseSpeed: 18,
  maxSpeed: 48,
  multiplier: 1,
  boostTimer: 0,
  shieldTimer: 0,
  magnetTimer: 0,
  paused: false,
  alive: true,
  startTime: 0,
  elapsedTime: 0
};

// ============= Boot =============
function showScreen(name) {
  for (const [k, el] of Object.entries(screens)) {
    if (k === 'notifs') continue;
    el.classList.toggle('hidden', k !== name);
  }
}

function updateBestScore() {
  const best = parseInt(localStorage.getItem(STORAGE_KEY) || '0', 10);
  $('bestScore').textContent = best;
  return best;
}

function saveBestScore(score) {
  const cur = parseInt(localStorage.getItem(STORAGE_KEY) || '0', 10);
  if (score > cur) {
    localStorage.setItem(STORAGE_KEY, String(score));
    updateBestScore();
    return true;
  }
  return false;
}

function notify(text, kind = '') {
  const el = document.createElement('div');
  el.className = 'notif' + (kind ? ' ' + kind : '');
  el.textContent = text;
  screens.notifs.appendChild(el);
  setTimeout(() => el.remove(), 2200);
}

function setCenterMsg(text, warn = false) {
  const el = $('centerMsg');
  el.textContent = text;
  el.classList.toggle('warn', warn);
  el.classList.remove('hidden');
  clearTimeout(setCenterMsg._t);
  setCenterMsg._t = setTimeout(() => el.classList.add('hidden'), 1400);
}

function updateHud() {
  $('hudScore').textContent = String(Math.floor(stats.score));
  $('hudSpeed').textContent = stats.speed.toFixed(0) + ' m/s';
  $('hudCoins').textContent = String(stats.coins);
  $('speedFill').style.width = Math.min(100, (stats.speed / stats.maxSpeed) * 100) + '%';
  $('livesContainer').textContent = '♥'.repeat(Math.max(0, stats.lives)) + '♡'.repeat(Math.max(0, stats.maxLives - stats.lives));
  const mult = $('multiplier');
  if (stats.multiplier > 1) {
    mult.classList.add('active');
    mult.textContent = '×' + stats.multiplier.toFixed(1);
  } else {
    mult.classList.remove('active');
  }
}

// ============= Game lifecycle =============
function startGame() {
  audio.resume();
  stats.score = 0;
  stats.coins = 0;
  stats.distance = 0;
  stats.lives = stats.maxLives;
  stats.speed = stats.baseSpeed;
  stats.multiplier = 1;
  stats.boostTimer = 0;
  stats.shieldTimer = 0;
  stats.magnetTimer = 0;
  stats.alive = true;
  stats.startTime = performance.now();
  player.reset();
  track.reset();
  input.clearAll();
  showScreen('hud');
  state = STATE.PLAYING;
  audio.startMusic();
  setCenterMsg('GO!');
}

function pauseGame() {
  if (state !== STATE.PLAYING) return;
  state = STATE.PAUSED;
  showScreen('pause');
  audio.stopMusic();
}

function resumeGame() {
  if (state !== STATE.PAUSED) return;
  state = STATE.PLAYING;
  showScreen('hud');
  audio.startMusic();
  input.clearAll();
}

function restartGame() {
  startGame();
}

function gameOver(reason) {
  state = STATE.GAMEOVER;
  stats.alive = false;
  audio.crash();
  audio.stopMusic();
  $('goScore').textContent = String(Math.floor(stats.score));
  $('goBest').textContent = String(parseInt(localStorage.getItem(STORAGE_KEY) || '0', 10));
  $('goDistance').textContent = String(Math.floor(stats.distance)) + ' m';
  $('goCoins').textContent = String(stats.coins);
  $('goReason').textContent = reason || 'You crashed!';
  $('gameoverTitle').textContent = stats.score > 5000 ? 'LEGENDARY!' : 'GAME OVER';
  saveBestScore(Math.floor(stats.score));
  showScreen('gameover');
  setTimeout(() => { state = STATE.GAMEOVER; }, 800);
}

function toMenu() {
  state = STATE.MENU;
  showScreen('menu');
  updateBestScore();
}

// ============= Input bindings =============
input.onPress('p', () => { if (state === STATE.PLAYING) pauseGame(); else if (state === STATE.PAUSED) resumeGame(); });
input.onPress('escape', () => { if (state === STATE.PLAYING) pauseGame(); else if (state === STATE.PAUSED) resumeGame(); });
input.onPress('m', () => { const muted = audio.toggleMute(); notify(muted ? 'Muted' : 'Unmuted', 'gold'); });
input.onPress('r', () => { if (state === STATE.GAMEOVER) restartGame(); });

// Menu buttons
$('btnStart').addEventListener('click', () => { audio.click(); startGame(); });
$('btnHowto').addEventListener('click', () => { audio.click(); screens.howto.classList.remove('hidden'); });
$('btnHowtoClose').addEventListener('click', () => { audio.click(); screens.howto.classList.add('hidden'); });
$('btnCredits').addEventListener('click', () => { audio.click(); screens.credits.classList.remove('hidden'); });
$('btnCreditsClose').addEventListener('click', () => { audio.click(); screens.credits.classList.add('hidden'); });
$('btnResume').addEventListener('click', () => { audio.click(); resumeGame(); });
$('btnRestartFromPause').addEventListener('click', () => { audio.click(); restartGame(); });
$('btnQuitToMenu').addEventListener('click', () => { audio.click(); toMenu(); });
$('btnRetry').addEventListener('click', () => { audio.click(); restartGame(); });
$('btnMenu').addEventListener('click', () => { audio.click(); toMenu(); });

// ============= Event handlers =============
function handleCollisions() {
  const events = track.checkCollisions(player);
  for (const e of events) {
    if (e.type === 'coin') {
      stats.coins += 1;
      stats.score += 5 * stats.multiplier;
      audio.coin();
    } else if (e.type === 'pickup') {
      if (e.ref === 'magnet') {
        stats.magnetTimer = 8;
        audio.powerUp();
        notify('🧲 MAGNET (8s)', 'gold');
      } else if (e.ref === 'shield') {
        stats.shieldTimer = 6;
        audio.powerUp();
        notify('🛡️ SHIELD (6s)', 'gold');
      }
    } else if (e.type === 'obstacle') {
      if (stats.shieldTimer > 0) {
        // Shield absorbs the hit
        stats.shieldTimer = Math.max(0, stats.shieldTimer - 2);
        audio.nearMiss();
        notify('🛡️ BLOCKED', 'gold');
        // Push obstacle down (visual feedback)
        if (e.ref && e.ref.mesh) e.ref.mesh.visible = false;
        continue;
      }
      const hit = player.hit();
      if (hit) {
        audio.hit();
        stats.lives -= 1;
        notify('💥 HIT!', 'danger');
        if (stats.lives <= 0) {
          player.kill();
          gameOver('You ran out of lives!');
        }
      }
    }
  }
}

function applyPowerUps(dt) {
  if (stats.magnetTimer > 0) {
    stats.magnetTimer -= dt;
    // Pull nearby coins toward the player
    for (const c of track.coins) {
      if (c.collected || c.z > 0) continue;
      const dx = player.x - c.mesh.position.x;
      const dz = -c.z;
      if (Math.abs(dx) < 3 && Math.abs(dz) < 3) {
        c.mesh.position.x += dx * 0.06;
        c.mesh.position.y += (player.y + 1.2 - c.mesh.position.y) * 0.06;
      }
    }
  }
  if (stats.shieldTimer > 0) stats.shieldTimer -= dt;
}

// ============= Game loop =============
let lastT = 0;
let loadingT = 0;
let cameraShake = 0;
let frame = 0;

function tick(t) {
  requestAnimationFrame(tick);
  const dt = Math.min(0.05, (t - lastT) / 1000 || 0);
  lastT = t;
  frame++;

  // Loading → Menu transition
  if (state === STATE.LOADING) {
    loadingT += dt;
    $('loaderFill').style.width = Math.min(100, loadingT * 80) + '%';
    if (loadingT > 1.2) {
      state = STATE.MENU;
      showScreen('menu');
      updateBestScore();
    }
    world.render();
    return;
  }

  // Camera always renders (background visible behind menu)
  if (state === STATE.MENU || state === STATE.PAUSED || state === STATE.GAMEOVER) {
    // Slow backdrop scroll
    if (world.backdrop) world.backdrop.rotation.y = Math.sin(t * 0.0001) * 0.02;
    world.render();
    if (state === STATE.PLAYING) { /* shouldn't happen */ }
    input.endFrame();
    return;
  }

  if (state !== STATE.PLAYING) {
    input.endFrame();
    return;
  }

  // ---- In-game update ----
  // Speed ramp with distance
  stats.distance += stats.speed * dt;
  stats.elapsedTime = (performance.now() - stats.startTime) / 1000;
  const targetSpeed = Math.min(stats.maxSpeed, stats.baseSpeed + stats.distance * 0.0008);
  // Boost mode slows but builds multiplier
  if (player.boostMode) {
    stats.boostTimer += dt;
    if (stats.boostTimer > 0.5) {
      stats.multiplier = Math.min(5, stats.multiplier + 0.1);
      stats.boostTimer = 0;
    }
    stats.speed += (targetSpeed * 0.6 - stats.speed) * 0.1;
  } else {
    stats.multiplier = Math.max(1, stats.multiplier - dt * 0.4);
    stats.speed += (targetSpeed - stats.speed) * 0.1;
  }
  track.setDifficulty(Math.min(1.2, stats.distance / 4000));

  // Update player
  const playerEvents = player.update(dt, input, stats.speed);

  // Update world
  track.update(dt, stats.speed);

  // Apply power-ups
  applyPowerUps(dt);

  // Handle collisions
  handleCollisions();

  // Player event SFX
  for (const ev of playerEvents) {
    if (ev.type === 'jump') { audio.jump(); setCenterMsg(player.jumpsLeft === 1 ? 'JUMP!' : 'DOUBLE!'); }
    if (ev.type === 'doubleJump') { audio.doubleJump(); setCenterMsg('AIR!'); }
    if (ev.type === 'slide') { audio.slide(); }
  }

  // Score
  stats.score += (stats.speed * dt) * 0.4 * stats.multiplier;
  if (player.boostMode) stats.score += dt * 5; // bonus

  // Camera shake on hit
  if (player.invulnTimer > 0) cameraShake = 0.15;
  if (cameraShake > 0) {
    world.camera.position.x = (Math.random() - 0.5) * cameraShake;
    world.camera.position.y = 6.5 + (Math.random() - 0.5) * cameraShake;
    cameraShake *= 0.85;
  } else {
    world.camera.position.x = player.x * 0.4;
    world.camera.position.y = 6.5 + player.y * 0.2;
  }
  world.camera.lookAt(player.x * 0.6, 1.5 + player.y * 0.2, -6);

  // Speed milestone announcements
  const milestone = Math.floor(stats.distance / 500);
  if (milestone > 0 && milestone !== (window._lastMilestone || 0)) {
    window._lastMilestone = milestone;
    if (milestone % 2 === 0) {
      notify(`${milestone * 500}m reached!`, 'gold');
      audio.levelUp();
    }
  }

  // HUD
  if (frame % 3 === 0) updateHud();

  // Render
  world.render();
  input.endFrame();
}

// ============= Boot sequence =============
function boot() {
  // Initial warm-up: render a few frames in loading so first paint is clean
  world.render();
  setTimeout(() => {
    updateBestScore();
    showScreen('loading');
    state = STATE.LOADING;
    requestAnimationFrame(tick);
  }, 50);

  // ?autostart=1 — skip menu (for demos / testing)
  const params = new URLSearchParams(location.search);
  if (params.get('autostart') === '1') {
    setTimeout(() => startGame(), 200);
  }
}

boot();
