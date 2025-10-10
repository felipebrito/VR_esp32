/**
 * Player de Vídeo 360° com controles integrados
 * Suporta vídeos 360° e integração com ESP32
 */

// Importar Three.js e OrbitControls
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

class Video360Player {
    constructor() {
        this.video = null;
        this.scene = null;
        this.camera = null;
        this.renderer = null;
        this.controls = null;
        this.sphere = null;
        this.videoTexture = null;
        this.isPlaying = false;
        this.isLoaded = false;
        this.duration = 0;
        this.currentTime = 0;
        
        this.init();
    }
    
    init() {
        this.setupEventListeners();
        this.initThreeJS();
        this.log('Video 360° Player inicializado', 'info');
    }
    
    setupEventListeners() {
        // Controles de vídeo
        document.getElementById('playBtn').addEventListener('click', () => this.play());
        document.getElementById('pauseBtn').addEventListener('click', () => this.pause());
        document.getElementById('stopBtn').addEventListener('click', () => this.stop());
        
        // Slider de progresso
        const progressSlider = document.getElementById('progressSlider');
        progressSlider.addEventListener('input', (e) => this.seekTo(e.target.value));
        
        // Integração com Quest Simulator
        if (window.questSimulator) {
            window.questSimulator.onVideoPlay = () => this.play();
            window.questSimulator.onVideoPause = () => this.pause();
            window.questSimulator.onVideoStop = () => this.stop();
            window.questSimulator.onVideoProgress = (progress) => this.setProgress(progress);
        }
        
        // Drag and drop para vídeos
        const videoContainer = document.getElementById('video360');
        videoContainer.addEventListener('dragover', (e) => {
            e.preventDefault();
            videoContainer.classList.add('drag-over');
        });
        
        videoContainer.addEventListener('dragleave', () => {
            videoContainer.classList.remove('drag-over');
        });
        
        videoContainer.addEventListener('drop', (e) => {
            e.preventDefault();
            videoContainer.classList.remove('drag-over');
            this.handleFileDrop(e.dataTransfer.files);
        });
        
        // Clique para carregar vídeo
        videoContainer.addEventListener('click', () => {
            if (!this.isLoaded) {
                this.showVideoSelector();
            }
        });
    }
    
    initThreeJS() {
        const container = document.getElementById('video360');
        
        if (!container) {
            this.log('Erro: Container video360 não encontrado', 'error');
            return;
        }
        
        try {
            // Scene
            this.scene = new THREE.Scene();
            
            // Camera
            this.camera = new THREE.PerspectiveCamera(75, container.clientWidth / container.clientHeight, 0.1, 1000);
            this.camera.position.set(0, 0, 0);
            
            // Renderer
            this.renderer = new THREE.WebGLRenderer({ antialias: true });
            this.renderer.setSize(container.clientWidth, container.clientHeight);
            this.renderer.setPixelRatio(window.devicePixelRatio);
            container.appendChild(this.renderer.domElement);
            
            // Controls
            this.controls = new OrbitControls(this.camera, this.renderer.domElement);
            this.controls.enableDamping = true;
            this.controls.dampingFactor = 0.05;
            this.controls.enableZoom = true;
            this.controls.enablePan = false;
            
            // Sphere para vídeo 360°
            const geometry = new THREE.SphereGeometry(500, 60, 40);
            geometry.scale(-1, 1, 1); // Inverter para vídeo 360°
            
            const material = new THREE.MeshBasicMaterial({
                color: 0xffffff,
                side: THREE.DoubleSide
            });
            
            this.sphere = new THREE.Mesh(geometry, material);
            this.scene.add(this.sphere);
            
            this.log('Three.js inicializado com sucesso', 'success');
        } catch (error) {
            this.log(`Erro ao inicializar Three.js: ${error.message}`, 'error');
            return;
        }
        
        // Adicionar texto inicial
        this.showInitialMessage();
        
        // Render loop
        this.animate();
        
        // Resize handler
        window.addEventListener('resize', () => this.onWindowResize());
    }
    
    showInitialMessage() {
        const container = document.getElementById('video360');
        const message = document.createElement('div');
        message.id = 'initial-message';
        message.style.cssText = `
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            background: rgba(0, 0, 0, 0.8);
            color: white;
            padding: 20px;
            border-radius: 10px;
            text-align: center;
            cursor: pointer;
            z-index: 1000;
        `;
        message.innerHTML = `
            <h3>🎥 Player de Vídeo 360°</h3>
            <p>Clique aqui para carregar um vídeo</p>
            <p>ou arraste um arquivo de vídeo</p>
            <small>Formatos suportados: MP4, WebM, OGG</small>
        `;
        container.appendChild(message);
    }
    
    showVideoSelector() {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'video/*';
        input.style.display = 'none';
        
        input.addEventListener('change', (e) => {
            if (e.target.files.length > 0) {
                this.loadVideo(e.target.files[0]);
            }
        });
        
        document.body.appendChild(input);
        input.click();
        document.body.removeChild(input);
    }
    
    handleFileDrop(files) {
        if (files.length > 0) {
            const file = files[0];
            if (file.type.startsWith('video/')) {
                this.loadVideo(file);
            } else {
                this.log('Arquivo não é um vídeo válido', 'error');
            }
        }
    }
    
    loadVideo(file) {
        this.log(`Carregando vídeo: ${file.name}`, 'info');
        
        // Criar URL do vídeo
        const videoURL = URL.createObjectURL(file);
        
        // Criar elemento de vídeo
        this.video = document.createElement('video');
        this.video.crossOrigin = 'anonymous';
        this.video.loop = true;
        this.video.muted = true; // Necessário para autoplay
        
        this.video.addEventListener('loadedmetadata', () => {
            this.duration = this.video.duration;
            this.log(`Vídeo carregado: ${this.duration.toFixed(2)}s`, 'success');
            this.updateTimeDisplay();
        });
        
        this.video.addEventListener('timeupdate', () => {
            this.currentTime = this.video.currentTime;
            const progress = (this.currentTime / this.duration) * 100;
            this.updateProgress(progress);
            this.updateTimeDisplay();
        });
        
        this.video.addEventListener('ended', () => {
            this.stop();
        });
        
        this.video.src = videoURL;
        this.video.load();
        
        // Criar textura do vídeo
        this.videoTexture = new THREE.VideoTexture(this.video);
        this.videoTexture.minFilter = THREE.LinearFilter;
        this.videoTexture.magFilter = THREE.LinearFilter;
        
        // Aplicar textura à esfera se ela existir
        if (this.sphere && this.sphere.material) {
            this.sphere.material.map = this.videoTexture;
            this.sphere.material.needsUpdate = true;
        } else {
            this.log('Erro: Esfera não foi criada corretamente', 'error');
            return;
        }
        
        // Remover mensagem inicial
        const initialMessage = document.getElementById('initial-message');
        if (initialMessage) {
            initialMessage.remove();
        }
        
        this.isLoaded = true;
        this.log('Vídeo 360° pronto para reprodução', 'success');
    }
    
    play() {
        if (!this.isLoaded || !this.video) {
            this.log('Nenhum vídeo carregado', 'error');
            return;
        }
        
        this.video.play();
        this.isPlaying = true;
        this.updatePlaybackControls();
        this.log('▶️ Reprodução iniciada', 'success');
        
        // Integrar com Quest Simulator
        if (window.questSimulator) {
            window.questSimulator.onVideoPlay();
        }
    }
    
    pause() {
        if (!this.isLoaded || !this.video) {
            return;
        }
        
        this.video.pause();
        this.isPlaying = false;
        this.updatePlaybackControls();
        this.log('⏸️ Reprodução pausada', 'info');
        
        // Integrar com Quest Simulator
        if (window.questSimulator) {
            window.questSimulator.onVideoPause();
        }
    }
    
    stop() {
        if (!this.isLoaded || !this.video) {
            return;
        }
        
        this.video.pause();
        this.video.currentTime = 0;
        this.isPlaying = false;
        this.currentTime = 0;
        this.updatePlaybackControls();
        this.updateProgress(0);
        this.log('⏹️ Reprodução parada', 'info');
        
        // Integrar com Quest Simulator
        if (window.questSimulator) {
            window.questSimulator.onVideoStop();
        }
    }
    
    seekTo(progress) {
        if (!this.isLoaded || !this.video) {
            return;
        }
        
        const time = (progress / 100) * this.duration;
        this.video.currentTime = time;
        this.currentTime = time;
        this.updateTimeDisplay();
        
        // Enviar progresso para ESP32
        if (window.esp32Client && window.esp32Client.isConnected) {
            window.esp32Client.sendProgress(Math.round(progress));
        }
    }
    
    setProgress(progress) {
        const progressSlider = document.getElementById('progressSlider');
        if (progressSlider) {
            progressSlider.value = progress;
        }
        
        if (this.isLoaded && this.video) {
            const time = (progress / 100) * this.duration;
            this.video.currentTime = time;
            this.currentTime = time;
            this.updateTimeDisplay();
        }
    }
    
    updateProgress(progress) {
        const progressSlider = document.getElementById('progressSlider');
        if (progressSlider) {
            progressSlider.value = progress;
        }
        
        // Enviar progresso para ESP32
        if (window.esp32Client && window.esp32Client.isConnected) {
            window.esp32Client.sendProgress(Math.round(progress));
        }
    }
    
    updatePlaybackControls() {
        const playBtn = document.getElementById('playBtn');
        const pauseBtn = document.getElementById('pauseBtn');
        
        if (playBtn && pauseBtn) {
            playBtn.disabled = this.isPlaying;
            pauseBtn.disabled = !this.isPlaying;
        }
    }
    
    updateTimeDisplay() {
        const timeDisplay = document.getElementById('timeDisplay');
        if (timeDisplay) {
            const current = this.formatTime(this.currentTime);
            const total = this.formatTime(this.duration);
            timeDisplay.textContent = `${current} / ${total}`;
        }
    }
    
    formatTime(seconds) {
        if (isNaN(seconds)) return '00:00';
        
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }
    
    onWindowResize() {
        const container = document.getElementById('video360');
        const width = container.clientWidth;
        const height = container.clientHeight;
        
        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(width, height);
    }
    
    animate() {
        requestAnimationFrame(() => this.animate());
        
        if (this.controls) {
            this.controls.update();
        }
        
        if (this.videoTexture) {
            this.videoTexture.needsUpdate = true;
        }
        
        this.renderer.render(this.scene, this.camera);
    }
    
    log(message, type = 'info') {
        if (window.esp32Client) {
            window.esp32Client.log(`[Video Player] ${message}`, type);
        } else {
            console.log(`[Video Player] ${message}`);
        }
    }
    
    // Método para carregar vídeo de URL
    loadVideoFromURL(url) {
        this.log(`Carregando vídeo de URL: ${url}`, 'info');
        
        this.video = document.createElement('video');
        this.video.crossOrigin = 'anonymous';
        this.video.loop = true;
        this.video.muted = true;
        
        this.video.addEventListener('loadedmetadata', () => {
            this.duration = this.video.duration;
            this.log(`Vídeo carregado: ${this.duration.toFixed(2)}s`, 'success');
            this.updateTimeDisplay();
        });
        
        this.video.addEventListener('timeupdate', () => {
            this.currentTime = this.video.currentTime;
            const progress = (this.currentTime / this.duration) * 100;
            this.updateProgress(progress);
            this.updateTimeDisplay();
        });
        
        this.video.addEventListener('ended', () => {
            this.stop();
        });
        
        this.video.src = url;
        this.video.load();
        
        this.videoTexture = new THREE.VideoTexture(this.video);
        this.videoTexture.minFilter = THREE.LinearFilter;
        this.videoTexture.magFilter = THREE.LinearFilter;
        
        this.sphere.material.map = this.videoTexture;
        this.sphere.material.needsUpdate = true;
        
        const initialMessage = document.getElementById('initial-message');
        if (initialMessage) {
            initialMessage.remove();
        }
        
        this.isLoaded = true;
        this.log('Vídeo 360° pronto para reprodução', 'success');
    }
}

// Instanciar o player globalmente
window.videoPlayer = new Video360Player();
