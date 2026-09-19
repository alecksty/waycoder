// =====================================================================
// world.js — Three.js scene, renderer, lighting, environment
// =====================================================================
import * as THREE from 'three';

export class World {
  constructor(mount) {
    this.mount = mount;
    this.size = { w: mount.clientWidth, h: mount.clientHeight };

    // ---- Renderer ----
    this.renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.setSize(this.size.w, this.size.h);
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.renderer.toneMappingExposure = 1.15;
    mount.appendChild(this.renderer.domElement);

    // ---- Scene ----
    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x05060f);
    this.scene.fog = new THREE.Fog(0x05060f, 35, 150);

    // ---- Camera ----
    this.camera = new THREE.PerspectiveCamera(72, this.size.w / this.size.h, 0.1, 500);
    this.camera.position.set(0, 6.5, 9);
    this.camera.lookAt(0, 1.5, -6);

    // ---- Lighting ----
    this.ambient = new THREE.AmbientLight(0x4a5cff, 0.4);
    this.scene.add(this.ambient);

    this.moonLight = new THREE.DirectionalLight(0x8aa4ff, 0.7);
    this.moonLight.position.set(-20, 30, -10);
    this.moonLight.castShadow = true;
    this.moonLight.shadow.mapSize.set(2048, 2048);
    this.moonLight.shadow.camera.near = 0.5;
    this.moonLight.shadow.camera.far = 120;
    this.moonLight.shadow.camera.left = -25;
    this.moonLight.shadow.camera.right = 25;
    this.moonLight.shadow.camera.top = 25;
    this.moonLight.shadow.camera.bottom = -25;
    this.moonLight.shadow.bias = -0.0005;
    this.scene.add(this.moonLight);

    this.fillLight = new THREE.PointLight(0xff00d4, 0.8, 30);
    this.fillLight.position.set(8, 4, 4);
    this.scene.add(this.fillLight);

    this.cyanLight = new THREE.PointLight(0x00f0ff, 0.7, 35);
    this.cyanLight.position.set(-8, 5, -10);
    this.scene.add(this.cyanLight);

    // ---- Distant cityscape backdrop ----
    this._buildBackdrop();

    // ---- Resize ----
    this._onResize = () => this.resize();
    window.addEventListener('resize', this._onResize);
  }

  _rng(seedStr) {
    let h = 0x9e3779b9;
    for (let i = 0; i < seedStr.length; i++) h = ((h ^ seedStr.charCodeAt(i)) * 0x85ebca6b) >>> 0;
    return () => {
      h = ((h ^ (h << 13)) >>> 0);
      h = ((h ^ (h >>> 17)) >>> 0);
      h = ((h ^ (h << 5)) >>> 0);
      return (h % 100000) / 10000;
    };
  }

  _buildBackdrop() {
    const grp = new THREE.Group();
    const cityGeo = new THREE.BoxGeometry(1, 1, 1);
    const cityMat = new THREE.MeshStandardMaterial({
      color: 0x0a1432, emissive: 0x0a1a4a, emissiveIntensity: 0.6,
      metalness: 0.4, roughness: 0.7
    });
    const r = this._rng('neon-skyline');
    for (let side = -1; side <= 1; side += 2) {
      for (let i = 0; i < 45; i++) {
        const w = 2 + r() * 4;
        const h = 8 + r() * 32;
        const d = 2 + r() * 4;
        const x = side * (28 + r() * 35);
        const z = -60 + i * 4 + (r() - 0.5) * 2;
        const m = new THREE.Mesh(cityGeo, cityMat.clone());
        m.position.set(x, h / 2 - 1, z);
        m.scale.set(w, h, d);
        grp.add(m);
        // Window glow strip
        const win = new THREE.Mesh(
          new THREE.PlaneGeometry(w * 0.6, h * 0.7),
          new THREE.MeshBasicMaterial({
            color: new THREE.Color().setHSL(0.55 + r() * 0.3, 0.9, 0.55),
            transparent: true,
            opacity: 0.08 + r() * 0.18
          })
        );
        win.position.set(x - side * (d / 2 + 0.01), h / 2 - 1, z);
        win.rotation.y = side === -1 ? -Math.PI / 2 : Math.PI / 2;
        grp.add(win);
      }
    }
    this.scene.add(grp);
    this.backdrop = grp;
  }

  resize() {
    this.size = { w: this.mount.clientWidth, h: this.mount.clientHeight };
    this.camera.aspect = this.size.w / this.size.h;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(this.size.w, this.size.h);
  }

  render() { this.renderer.render(this.scene, this.camera); }
}
