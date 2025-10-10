/**
 * Simulador de Eventos Meta Quest
 * Simula os eventos que aconteceriam no Meta Quest para debug
 */
class QuestEventSimulator {
    constructor() {
        this.isHeadsetOn = false;
        this.isAppFocused = false;
        this.isPlaying = false;
        this.currentProgress = 0;
        this.playerId = 1;
        
        this.init();
    }
    
    init() {
        this.setupEventListeners();
        this.setupKeyboardShortcuts();
        this.log('Quest Event Simulator inicializado', 'info');
    }
    
    setupEventListeners() {
        // Botões de simulação
        document.getElementById('simulateHeadsetOn').addEventListener('click', () => this.simulateHeadsetOn());
        document.getElementById('simulateHeadsetOff').addEventListener('click', () => this.simulateHeadsetOff());
        document.getElementById('simulateFocus').addEventListener('click', () => this.simulateAppFocus());
        document.getElementById('simulateUnfocus').addEventListener('click', () => this.simulateAppUnfocus());
        
        // Integração com WebSocket client
        if (window.esp32Client) {
            window.esp32Client.on('onConnect', () => {
                this.log('WebSocket conectado - pronto para simular eventos', 'success');
            });
            
            window.esp32Client.on('onDisconnect', () => {
                this.log('WebSocket desconectado - eventos suspensos', 'warning');
            });
        }
    }
    
    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (event) => {
            // Ctrl + H = Headset ON/OFF
            if (event.ctrlKey && event.key === 'h') {
                event.preventDefault();
                if (this.isHeadsetOn) {
                    this.simulateHeadsetOff();
                } else {
                    this.simulateHeadsetOn();
                }
            }
            
            // Ctrl + F = Focus ON/OFF
            if (event.ctrlKey && event.key === 'f') {
                event.preventDefault();
                if (this.isAppFocused) {
                    this.simulateAppUnfocus();
                } else {
                    this.simulateAppFocus();
                }
            }
            
            // Ctrl + P = Play/Pause
            if (event.ctrlKey && event.key === 'p') {
                event.preventDefault();
                if (this.isPlaying) {
                    this.simulatePause();
                } else {
                    this.simulatePlay();
                }
            }
            
            // Ctrl + S = Stop
            if (event.ctrlKey && event.key === 's') {
                event.preventDefault();
                this.simulateStop();
            }
        });
    }
    
    simulateHeadsetOn() {
        this.isHeadsetOn = true;
        this.log('🥽 Headset colocado - Usuário ONLINE', 'success');
        
        // Atualizar interface
        this.updateHeadsetStatus(true);
        
        // Enviar comando para ESP32
        if (window.esp32Client && window.esp32Client.isConnected) {
            window.esp32Client.simulateHeadsetOn();
        }
        
        // Simular delay de inicialização
        setTimeout(() => {
            this.log('Headset inicializado - Pronto para reprodução', 'info');
            this.updatePlayerStatus('ready');
        }, 1000);
    }
    
    simulateHeadsetOff() {
        this.isHeadsetOn = false;
        this.log('🥽 Headset removido - Usuário OFFLINE', 'warning');
        
        // Parar reprodução se estiver rodando
        if (this.isPlaying) {
            this.simulateStop();
        }
        
        // Atualizar interface
        this.updateHeadsetStatus(false);
        
        // Enviar comando para ESP32
        if (window.esp32Client && window.esp32Client.isConnected) {
            window.esp32Client.simulateHeadsetOff();
        }
        
        this.updatePlayerStatus('disconnected');
    }
    
    simulateAppFocus() {
        this.isAppFocused = true;
        this.log('📱 App ganhou foco - Usuário ATIVO', 'info');
        
        // Atualizar interface
        this.updateFocusStatus(true);
        
        // Enviar comando para ESP32
        if (window.esp32Client && window.esp32Client.isConnected) {
            window.esp32Client.simulateAppFocus();
        }
        
        // Se headset estiver on, mostrar como ready
        if (this.isHeadsetOn) {
            this.updatePlayerStatus('ready');
        }
    }
    
    simulateAppUnfocus() {
        this.isAppFocused = false;
        this.log('📱 App perdeu foco - Usuário INATIVO', 'warning');
        
        // Pausar reprodução se estiver rodando
        if (this.isPlaying) {
            this.simulatePause();
        }
        
        // Atualizar interface
        this.updateFocusStatus(false);
        
        // Enviar comando para ESP32
        if (window.esp32Client && window.esp32Client.isConnected) {
            window.esp32Client.simulateAppUnfocus();
        }
    }
    
    simulatePlay() {
        if (!this.isHeadsetOn) {
            this.log('❌ Não é possível reproduzir - Headset não está colocado', 'error');
            return;
        }
        
        this.isPlaying = true;
        this.log('▶️ Iniciando reprodução', 'success');
        
        // Atualizar interface
        this.updatePlaybackStatus(true);
        
        // Enviar comando para ESP32
        if (window.esp32Client && window.esp32Client.isConnected) {
            window.esp32Client.sendCommand(`play${this.playerId}`);
        }
        
        this.updatePlayerStatus('playing');
        
        // Iniciar simulação de progresso
        this.startProgressSimulation();
    }
    
    simulatePause() {
        if (!this.isPlaying) {
            this.log('❌ Não há reprodução para pausar', 'warning');
            return;
        }
        
        this.isPlaying = false;
        this.log('⏸️ Reprodução pausada', 'info');
        
        // Atualizar interface
        this.updatePlaybackStatus(false);
        
        // Enviar comando para ESP32
        if (window.esp32Client && window.esp32Client.isConnected) {
            window.esp32Client.sendCommand(`pause${this.playerId}`);
        }
        
        this.updatePlayerStatus('paused');
        
        // Parar simulação de progresso
        this.stopProgressSimulation();
    }
    
    simulateStop() {
        this.isPlaying = false;
        this.currentProgress = 0;
        this.log('⏹️ Reprodução parada - Modo IDLE', 'info');
        
        // Atualizar interface
        this.updatePlaybackStatus(false);
        this.updateProgress(0);
        
        // Enviar comando para ESP32
        if (window.esp32Client && window.esp32Client.isConnected) {
            window.esp32Client.sendCommand(`stop${this.playerId}`);
            window.esp32Client.sendCommand(`led${this.playerId}:0`);
        }
        
        this.updatePlayerStatus('ready');
        
        // Parar simulação de progresso
        this.stopProgressSimulation();
    }
    
    startProgressSimulation() {
        this.progressInterval = setInterval(() => {
            if (this.isPlaying) {
                this.currentProgress += 0.5; // Incremento de 0.5% por vez
                
                if (this.currentProgress > 100) {
                    this.currentProgress = 100;
                    this.simulateStop(); // Parar quando chegar ao fim
                    return;
                }
                
                this.updateProgress(this.currentProgress);
                
                // Enviar progresso para ESP32
                if (window.esp32Client && window.esp32Client.isConnected) {
                    window.esp32Client.sendProgress(Math.round(this.currentProgress));
                }
            }
        }, 100); // Atualizar a cada 100ms
    }
    
    stopProgressSimulation() {
        if (this.progressInterval) {
            clearInterval(this.progressInterval);
            this.progressInterval = null;
        }
    }
    
    updateHeadsetStatus(isOn) {
        const statusElement = document.getElementById('headsetStatus');
        if (statusElement) {
            statusElement.textContent = isOn ? 'ONLINE' : 'OFFLINE';
            statusElement.className = `status-value ${isOn ? 'online' : 'offline'}`;
        }
    }
    
    updateFocusStatus(isFocused) {
        const statusElement = document.getElementById('focusStatus');
        if (statusElement) {
            statusElement.textContent = isFocused ? 'FOCUSED' : 'UNFOCUSED';
            statusElement.className = `status-value ${isFocused ? 'focused' : 'unfocused'}`;
        }
    }
    
    updatePlaybackStatus(isPlaying) {
        const playBtn = document.getElementById('playBtn');
        const pauseBtn = document.getElementById('pauseBtn');
        
        if (playBtn && pauseBtn) {
            playBtn.disabled = isPlaying;
            pauseBtn.disabled = !isPlaying;
        }
    }
    
    updatePlayerStatus(status) {
        const statusElement = document.getElementById('playerStatus');
        if (statusElement) {
            statusElement.textContent = status.toUpperCase();
            statusElement.className = `status-value ${status}`;
        }
    }
    
    updateProgress(progress) {
        const progressElement = document.getElementById('progressStatus');
        const progressSlider = document.getElementById('progressSlider');
        
        if (progressElement) {
            progressElement.textContent = `${Math.round(progress)}%`;
        }
        
        if (progressSlider) {
            progressSlider.value = progress;
        }
    }
    
    setPlayerId(playerId) {
        this.playerId = playerId;
        this.log(`Player ID alterado para: ${playerId}`, 'info');
    }
    
    // Método para simular eventos automáticos (útil para testes)
    simulateAutomaticSequence() {
        this.log('🤖 Iniciando sequência automática de eventos', 'info');
        
        setTimeout(() => this.simulateHeadsetOn(), 1000);
        setTimeout(() => this.simulateAppFocus(), 2000);
        setTimeout(() => this.simulatePlay(), 3000);
        setTimeout(() => this.simulatePause(), 8000);
        setTimeout(() => this.simulatePlay(), 10000);
        setTimeout(() => this.simulateStop(), 15000);
        setTimeout(() => this.simulateHeadsetOff(), 18000);
    }
    
    log(message, type = 'info') {
        if (window.esp32Client) {
            window.esp32Client.log(`[Quest Simulator] ${message}`, type);
        } else {
            console.log(`[Quest Simulator] ${message}`);
        }
    }
    
    // Métodos para integração com player de vídeo
    onVideoPlay() {
        this.simulatePlay();
    }
    
    onVideoPause() {
        this.simulatePause();
    }
    
    onVideoStop() {
        this.simulateStop();
    }
    
    onVideoProgress(progress) {
        this.currentProgress = progress;
        this.updateProgress(progress);
        
        if (window.esp32Client && window.esp32Client.isConnected) {
            window.esp32Client.sendProgress(Math.round(progress));
        }
    }
}

// Instanciar o simulador globalmente
window.questSimulator = new QuestEventSimulator();
