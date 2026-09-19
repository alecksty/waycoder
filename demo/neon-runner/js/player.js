// =====================================================================
// player.js — The runner character with physics, jump, slide, lane switching
// =====================================================================
import * as THREE from 'three';

const LANE_X = [-2.4, 0, 2.4];   // X positions for 3 lanes
const GRAVITY = -38;
const JUMP_VEL = 14.5;
const DOUBLE_JUMP_VEL = 12.5;
const SLIDE_DURATION = 0.7;
const LANE_SWITCH_DURATION = 0.12;

export class Player {
  constructor(scene) {
    this.scene = scene;
    this.lane = 1;
    this.targetLaneX = LANE_X[1];
    this.x = this.targetLaneX;
    this.y = 0;
    this.z = 0;
    this.vy = 0;
    this.jumpsLeft = 2;
    this.isSliding = false;
    this.slideTimer = 0;
    this.laneSwitchT = 1;       // 1 = idle
    this.laneSwitchFrom = this.targetLaneX;
    this.laneSwitchTo = this.targetLaneX;
    this.boostMode = false;
    this.invulnTimer = 0;
    this.alive = true;
    this._build();
  }

  _build() {
    this.root = new THREE.Group();

    // Torso — capsule-ish using box
    const bodyMat = new THREE.MeshStandardMaterial({
      color: 0x0a1430, emissive: 0x00f0ff, emissiveIntensity: 0.4,
      metalness: 0.7, roughness: 0.25
    });
    const accentMat = new THREE.MeshStandardMaterial({
      color: 0x0a1430, emissive: 0xff00d4, emissiveIntensity: 0.6,
      metalness: 0.7, roughness: 0.25
    });
    const visorMat = new THREE.MeshBasicMaterial({ color: 0x00f0ff });

    const torso = new THREE.Mesh(new THREE.BoxGeometry(0.65, 0.85, 0.4), bodyMat);
    torso.position.y = 1.2;
    torso.castShadow = true;
    this.root.add(torso);
    this.torso = torso;

    // Head
    const head = new THREE.Mesh(new THREE.BoxGeometry(0.55, 0.55, 0.5), bodyMat);
    head.position.y = 1.95;
    head.castShadow = true;
    this.root.add(head);
    this.head = head;

    // Visor
    const visor = new THREE.Mesh(new THREE.PlaneGeometry(0.5, 0.18), visorMat);
    visor.position.set(0, 1.95, 0.26);
    this.root.add(visor);
    this.visor = visor;

    // Backpack / chest accent
    const accent = new THREE.Mesh(new THREE.BoxGeometry(0.7, 0.25, 0.45), accentMat);
    accent.position.set(0, 1.0, -0.05);
    this.root.add(accent);

    // Arms
    const armGeo = new THREE.BoxGeometry(0.18, 0.7, 0.22);
    const armL = new THREE.Mesh(armGeo, bodyMat);
    armL.position.set(-0.42, 1.2, 0);
    armL.castShadow = true;
    this.root.add(armL);
    this.armL = armL;
    const armR = new THREE.Mesh(armGeo, bodyMat);
    armR.position.set(0.42, 1.2, 0);
    armR.castShadow = true;
    this.root.add(armR);
    this.armR = armR;

    // Legs
    const legGeo = new THREE.BoxGeometry(0.22, 0.7, 0.25);
    const legL = new THREE.Mesh(legGeo, bodyMat);
    legL.position.set(-0.18, 0.45, 0);
    legL.castShadow = true;
    this.root.add(legL);
    this.legL = legL;
    const legR = new THREE.Mesh(legGeo, bodyMat);
    legR.position.set(0.18, 0.45, 0);
    legR.castShadow = true;
    this.root.add(legR);
    this.legR = legR;

    // Trail glow plane
    const trailGeo = new THREE.PlaneGeometry(0.8, 0.4);
    const trailMat = new THREE.MeshBasicMaterial({ color: 0x00f0ff, transparent: true, opacity: 0.5 });
    const trail = new THREE.Mesh(trailGeo, trailMat);
    trail.rotation.x = -Math.PI / 2;
    trail.position.y = 0.02;
    this.root.add(trail);
    this.trail = trail;

    this.scene.add(this.root);
  }

  reset() {
    this.lane = 1;
    this.targetLaneX = LANE_X[1];
    this.x = this.targetLaneX;
    this.y = 0;
    this.z = 0;
    this.vy = 0;
    this.jumpsLeft = 2;
    this.isSliding = false;
    this.slideTimer = 0;
    this.laneSwitchT = 1;
    this.invulnTimer = 0;
    this.alive = true;
    this.boostMode = false;
    this._setStandingPose();
    this.root.position.set(this.x, 0, 0);
  }

  setPos(x, y, z) {
    this.x = x; this.y = y; this.z = z;
    this.root.position.set(x, y, z);
  }

  // Called when input is consumed; returns events for SFX/HUD
  update(dt, input, speed) {
    if (!this.alive) {
      // Tumble after death
      this.root.rotation.x += dt * 6;
      this.root.rotation.z += dt * 4;
      this.vy += GRAVITY * dt;
      this.y += this.vy * dt;
      if (this.y < -2) this.y = -2;
      this.root.position.y = this.y;
      this.root.position.x = this.x;
      return [];
    }
    const events = [];

    // ---- Lane switching ----
    if (input.wasPressed('arrowleft') || input.wasPressed('a')) {
      if (this.lane > 0) {
        this.lane--;
        this._beginLaneSwitch(LANE_X[this.lane]);
      }
    }
    if (input.wasPressed('arrowright') || input.wasPressed('d')) {
      if (this.lane < 2) {
        this.lane++;
        this._beginLaneSwitch(LANE_X[this.lane]);
      }
    }

    if (this.laneSwitchT < 1) {
      this.laneSwitchT = Math.min(1, this.laneSwitchT + dt / LANE_SWITCH_DURATION);
      const t = 1 - Math.pow(1 - this.laneSwitchT, 3); // ease out cubic
      this.x = this.laneSwitchFrom + (this.laneSwitchTo - this.laneSwitchFrom) * t;
    }

    // ---- Jump ----
    if (input.wasPressed(' ') || input.wasPressed('arrowup') || input.wasPressed('w')) {
      if (this.isSliding) {
        // Cancel slide and jump
        this.isSliding = false;
        this.slideTimer = 0;
        this._setStandingPose();
        this.vy = JUMP_VEL * 0.85;
        this.jumpsLeft = 1;
        events.push({ type: 'jump' });
      } else if (this.jumpsLeft > 0) {
        this.vy = this.jumpsLeft === 2 ? JUMP_VEL : DOUBLE_JUMP_VEL;
        this.jumpsLeft--;
        events.push({ type: this.jumpsLeft === 1 ? 'doubleJump' : 'jump' });
        this._setStandingPose();
      }
    }

    // ---- Slide ----
    if ((input.wasPressed('arrowdown') || input.wasPressed('s')) && !this.isSliding && this.y <= 0.01) {
      this.isSliding = true;
      this.slideTimer = SLIDE_DURATION;
      this._setSlidePose();
      events.push({ type: 'slide' });
    }
    if (this.isSliding) {
      this.slideTimer -= dt;
      if (this.slideTimer <= 0) {
        this.isSliding = false;
        this._setStandingPose();
      }
    }

    // ---- Boost (slows, builds multiplier) ----
    this.boostMode = input.isDown('shift');

    // ---- Gravity ----
    this.vy += GRAVITY * dt;
    this.y += this.vy * dt;
    if (this.y <= 0) {
      this.y = 0;
      this.vy = 0;
      this.jumpsLeft = 2;
    }

    // ---- Animation: leg swing ----
    const animSpeed = (this.isSliding ? 0 : speed * 0.6) + 4;
    const t = performance.now() / 1000;
    if (!this.isSliding) {
      const swing = Math.sin(t * animSpeed) * 0.7;
      this.legL.rotation.x = swing;
      this.legR.rotation.x = -swing;
      this.armL.rotation.x = -swing * 0.5;
      this.armR.rotation.x = swing * 0.5;
    }

    // ---- Subtle bobbing ----
    const bob = (this.y > 0.01) ? 0 : Math.sin(t * animSpeed) * 0.04;

    // ---- Invuln blink ----
    if (this.invulnTimer > 0) {
      this.invulnTimer -= dt;
      this.torso.material.emissiveIntensity = Math.sin(t * 30) > 0 ? 1.2 : 0.1;
    } else {
      this.torso.material.emissiveIntensity = 0.4;
    }

    // ---- Visor color shift when boosting ----
    this.visor.material.color.set(this.boostMode ? 0xffd54a : 0x00f0ff);

    // ---- Trail intensity ----
    this.trail.material.opacity = 0.3 + Math.min(0.4, speed * 0.05);

    this.root.position.set(this.x, this.y + bob, this.z);
    this.root.rotation.y = (this.laneSwitchTo - this.x) * 0.12; // lean into turn
    return events;
  }

  _beginLaneSwitch(toX) {
    this.laneSwitchFrom = this.x;
    this.laneSwitchTo = toX;
    this.laneSwitchT = 0;
  }

  _setStandingPose() {
    this.torso.scale.set(1, 1, 1);
    this.torso.position.y = 1.2;
    this.head.scale.set(1, 1, 1);
    this.head.position.y = 1.95;
    this.visor.position.y = 1.95;
    this.legL.scale.set(1, 1, 1);
    this.legR.scale.set(1, 1, 1);
    this.legL.position.y = 0.45;
    this.legR.position.y = 0.45;
    this.armL.scale.set(1, 1, 1);
    this.armR.scale.set(1, 1, 1);
    this.armL.position.y = 1.2;
    this.armR.position.y = 1.2;
    this.root.rotation.x = 0;
  }

  _setSlidePose() {
    this.torso.scale.set(1, 0.4, 1.4);
    this.torso.position.y = 0.55;
    this.head.scale.set(1, 0.6, 1.2);
    this.head.position.y = 0.95;
    this.visor.position.y = 0.95;
    this.legL.scale.set(1, 0.5, 1.3);
    this.legR.scale.set(1, 0.5, 1.3);
    this.legL.position.y = 0.18;
    this.legR.position.y = 0.18;
    this.legL.rotation.x = -1.2;
    this.legR.rotation.x = -1.2;
    this.armL.scale.set(1, 0.5, 1.2);
    this.armR.scale.set(1, 0.5, 1.2);
    this.armL.position.y = 0.6;
    this.armR.position.y = 0.6;
  }

  hit() {
    if (this.invulnTimer > 0) return false;
    this.invulnTimer = 1.4;
    return true;
  }

  kill() {
    this.alive = false;
    this.vy = 8;
  }
}

export const LANES = LANE_X;
