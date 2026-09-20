/**
 * University of Pittsburgh at Bradford - Panther Showcase
 * Landing page logic, WebXR setup, and interactive 2D controls
 */

import { DoubleSide, Group, MeshBasicMaterial } from 'three';
import { GlobalComponent, gltfLoader } from './global';
import { PANTHER_FINISHES, PANTHER_SIZES } from './constants';

import { ARButton } from 'ratk';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import { System } from 'elics';

const CAMERA_ANGULAR_SPEED = Math.PI / 4;

export class InlineSystem extends System {
	init() {
		this.needsSetup = true;
	}

	_setupButtons(renderer, panther) {
		const arButton = document.getElementById('ar-button');
		const webLaunchButton = document.getElementById('web-launch-button');
		if (webLaunchButton) {
			webLaunchButton.style.display = 'none';
		}

		if (arButton) {
			ARButton.convertToARButton(arButton, renderer, {
				ENTER_XR_TEXT: 'View in Mixed Reality',
				requiredFeatures: [],
				optionalFeatures: ['hit-test', 'local-floor', 'bounded-floor', 'layers'],
				onUnsupported: () => {
					arButton.textContent = 'WebXR AR is not available on this device/browser';
					arButton.disabled = true;
					arButton.classList.add('disabled');
					if (webLaunchButton) {
						webLaunchButton.style.display = 'block';
						webLaunchButton.textContent = 'Try a WebXR-compatible browser or headset';
					}
				},
			});
		}

		if (webLaunchButton) {
			webLaunchButton.onclick = () => {
				window.alert('This app requires a WebXR-compatible browser or headset to enter AR.');
			};
		}

		// Size preset buttons
		const sizeOptions = document.querySelector('.size-options');
		const sizeButtons = [...document.querySelectorAll('.size-btn')];
		sizeButtons.forEach((button) => {
			button.onclick = () => {
				sizeButtons.forEach((b) => b.classList.toggle('selected', false));
				button.classList.toggle('selected', true);
				const sizeId = button.dataset.sizeId;
				if (sizeOptions) {
					sizeOptions.dataset.value = sizeId;
				}
				const preset = PANTHER_SIZES.find((s) => s.id === sizeId);
				if (preset) {
					panther.setScale(preset.scale);
					const slider = document.getElementById('panther-scale-slider');
					if (slider) {
						slider.value = preset.scale;
					}
					const scaleValueDisplay = document.getElementById('scale-value-display');
					if (scaleValueDisplay) {
						scaleValueDisplay.innerText = `${(preset.scale * 100).toFixed(0)}% (${preset.heightDisplay})`;
					}
					// Update 3D preview copy if present
					if (this.previewPantherCopy) {
						this.previewPantherCopy.children[0].scale.setScalar(preset.scale);
					}
				}
			};
		});

		// Continuous Scale Slider
		const scaleSlider = document.getElementById('panther-scale-slider');
		const scaleDisplay = document.getElementById('scale-value-display');
		if (scaleSlider) {
			scaleSlider.oninput = () => {
				const val = parseFloat(scaleSlider.value);
				scaleSlider.dataset.userAdjusted = 'true';
				panther.setScale(val);
				if (scaleDisplay) {
					scaleDisplay.innerText = `${(val * 100).toFixed(0)}% (${(val * 2.7).toFixed(1)}m)`;
				}
				// Deselect preset buttons if slider is moved manually
				sizeButtons.forEach((b) => b.classList.toggle('selected', false));
				if (this.previewPantherCopy) {
					this.previewPantherCopy.children[0].scale.setScalar(val);
				}
			};
		}

		// Step buttons
		const btnScaleDown = document.getElementById('btn-scale-down');
		const btnScaleUp = document.getElementById('btn-scale-up');
		if (btnScaleDown && scaleSlider) {
			btnScaleDown.onclick = () => {
				scaleSlider.value = Math.max(0.05, parseFloat(scaleSlider.value) - 0.05);
				scaleSlider.oninput();
			};
		}
		if (btnScaleUp && scaleSlider) {
			btnScaleUp.onclick = () => {
				scaleSlider.value = Math.min(1.5, parseFloat(scaleSlider.value) + 0.05);
				scaleSlider.oninput();
			};
		}

		// Finish selection buttons
		const finishButtons = [...document.querySelectorAll('.finish-btn')];
		finishButtons.forEach((btn) => {
			btn.onclick = () => {
				finishButtons.forEach((b) => b.classList.toggle('selected', false));
				btn.classList.toggle('selected', true);
				const finishId = btn.dataset.finishId;
				panther.setFinish(finishId);
				if (this.previewPantherCopy) {
					this.previewPantherCopy.children[0].material = panther.mesh.material;
				}
				const finishNameDisplay = document.getElementById('current-finish-name');
				if (finishNameDisplay) {
					const f = PANTHER_FINISHES.find((item) => item.id === finishId);
					if (f) finishNameDisplay.innerText = f.name;
				}
			};
		});
	}

	update() {
		const globalEntity = this.getEntities(this.queries.global)[0];
		if (!globalEntity) return;
		const global = globalEntity.getComponent(GlobalComponent);
		const { scene, camera, renderer, panther } = global;

		if (!panther) return;

		if (this.needsSetup) {
			this._setupButtons(renderer, panther);
			this.needsSetup = false;

			this.container = new Group();
			scene.add(this.container);

			// Position desktop preview camera nicely framed around the Panther statue
			camera.position.set(0.55, 0.32, 0.7);

			// Add Panther copy to desktop preview container
			this.previewPantherCopy = panther.createCopy();
			this.previewPantherCopy.position.set(0, 0.0, 0);
			this.container.add(this.previewPantherCopy);

			// Hide the original XR Panther while in inline/desktop mode
			panther.root.visible = false;

			this.orbitControls = new OrbitControls(camera, renderer.domElement);
			this.orbitControls.target.set(0, 0.12, 0);
			this.orbitControls.update();
			this.orbitControls.enableZoom = true;
			this.orbitControls.enablePan = true;
			this.orbitControls.enableDamping = true;
			this.orbitControls.autoRotate = false;
			this.orbitControls.minDistance = 0.2;
			this.orbitControls.maxDistance = 3.0;

			renderer.xr.addEventListener('sessionstart', () => {
				this.container.visible = false;
				panther.root.visible = true;
				const configPanel = document.getElementById('config-panel');
				if (configPanel) configPanel.style.display = 'none';
			});

			renderer.xr.addEventListener('sessionend', () => {
				this.container.visible = true;
				panther.root.visible = false;
				camera.position.set(0, 0.4, 0.8);
				const configPanel = document.getElementById('config-panel');
				if (configPanel) configPanel.style.display = 'flex';
			});
			return;
		}

		if (this.container && this.container.visible && this.orbitControls) {
			this.orbitControls.update();
		}
	}
}

InlineSystem.queries = {
	global: { required: [GlobalComponent] },
};
