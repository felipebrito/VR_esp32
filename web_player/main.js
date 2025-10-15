/**
 * Arquivo principal - Integração de todos os módulos
 * Coordena WebSocket, Quest Simulator, Video Player e LED Visualizer
 */
class VRPlayerApp {
    constructor() {
        this.isInitialized = false;
        this.currentPlayer = 1;
        this.autoMode = false;
        
        this.init();
    }
    
    init() {
        this.setupGlobalEventListeners();
        this.setupKeyboardShortcuts();
        this.setupAutoMode();
        this.showWelcomeMessage();
        this.isInitialized = true;
        
        this.log('VR Player App inicializado com sucesso!', 'success');
    }
    
    setupGlobalEventListeners() {
        // Aguardar todos os módulos estarem prontos
        window.addEventListener('load', () => {
            setTimeout(() => {
                this.integrateModules();
            }, 1000);
        });
        
        // Detectar mudanças de foco da janela (simular eventos Meta Quest)
        window.addEventListener('focus', () => {
            if (window.questSimulator) {
                window.questSimulator.simulateAppFocus();
            }
        });
        
        window.addEventListener('blur', () => {
            if (window.questSimulator) {
                window.questSimulator.simulateAppUnfocus();
            }
        });
        
        // Detectar mudanças de visibilidade da página
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                if (window.questSimulator) {
                    window.questSimulator.simulateAppUnfocus();
                }
            } else {
                if (window.questSimulator) {
                    window.questSimulator.simulateAppFocus();
                }
            }
        });
    }
    
    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (event) => {
            // Ctrl + 1 = Player 1
            if (event.ctrlKey && event.key === '1') {
                event.preventDefault();
                this.setCurrentPlayer(1);
            }
            
            // Ctrl + 2 = Player 2
            if (event.ctrlKey && event.key === '2') {
                event.preventDefault();
                this.setCurrentPlayer(2);
            }
            
            // Ctrl + A = Modo automático
            if (event.ctrlKey && event.key === 'a') {
                event.preventDefault();
                this.toggleAutoMode();
            }
            
            // Ctrl + T = Teste de LEDs
            if (event.ctrlKey && event.key === 't') {
                event.preventDefault();
                this.testLEDs();
            }
            
            // Ctrl + R = Reset tudo
            if (event.ctrlKey && event.key === 'r') {
                event.preventDefault();
                this.resetAll();
            }
        });
    }
    
    setupAutoMode() {
        // Botão para modo automático
        const autoBtn = document.createElement('button');
        autoBtn.id = 'autoModeBtn';
        autoBtn.className = 'btn btn-info';
        autoBtn.textContent = '🤖 Modo Automático';
        autoBtn.style.cssText = 'position: fixed; top: 10px; right: 10px; z-index: 1000;';
        
        autoBtn.addEventListener('click', () => this.toggleAutoMode());
        document.body.appendChild(autoBtn);
    }
    
    integrateModules() {
        this.log('Integrando módulos...', 'info');
        
        // Integrar WebSocket com Quest Simulator
        if (window.esp32Client && window.questSimulator) {
            window.esp32Client.on('onConnect', () => {
                this.log('Módulos integrados - Pronto para uso!', 'success');
                this.showIntegrationStatus(true);
            });
            
            window.esp32Client.on('onDisconnect', () => {
                this.showIntegrationStatus(false);
            });
        }
        
        // Integrar Video Player com LED Visualizer
        if (window.videoPlayer && window.ledVisualizer) {
            // O progresso do vídeo já é enviado automaticamente via WebSocket
            this.log('Player de vídeo integrado com visualizador de LEDs', 'info');
        }
        
        // Integrar comandos da ESP32 com o Video Player
        console.log('[DEBUG] Verificando integração ESP32-VideoPlayer...');
        console.log('[DEBUG] esp32Client:', window.esp32Client);
        console.log('[DEBUG] videoPlayer:', window.videoPlayer);
        
        if (window.esp32Client && window.videoPlayer) {
            console.log('[DEBUG] Registrando callbacks da ESP32...');
            
            window.esp32Client.on('onPlayCommand', (data) => {
                console.log('[DEBUG] Callback onPlayCommand executado:', data);
                this.log('ESP32 solicitou PLAY - executando no player', 'success');
                if (window.videoPlayer.isLoaded) {
                    this.log('Executando play() no videoPlayer', 'info');
                    window.videoPlayer.play();
                } else {
                    this.log('VideoPlayer não está carregado', 'warning');
                }
            });
            
            window.esp32Client.on('onPauseCommand', (data) => {
                console.log('[DEBUG] Callback onPauseCommand executado:', data);
                this.log('ESP32 solicitou PAUSE - executando no player', 'success');
                if (window.videoPlayer.isLoaded) {
                    this.log('Executando pause() no videoPlayer', 'info');
                    window.videoPlayer.pause();
                } else {
                    this.log('VideoPlayer não está carregado', 'warning');
                }
            });
            
            window.esp32Client.on('onStopCommand', (data) => {
                console.log('[DEBUG] Callback onStopCommand executado:', data);
                this.log('ESP32 solicitou STOP - executando no player', 'success');
                if (window.videoPlayer.isLoaded) {
                    this.log('Executando stop() no videoPlayer', 'info');
                    window.videoPlayer.stop();
                } else {
                    this.log('VideoPlayer não está carregado', 'warning');
                }
            });
            
            this.log('Comandos da ESP32 integrados com o player de vídeo', 'success');
        } else {
            console.log('[DEBUG] ESP32 ou VideoPlayer não disponível');
            this.log('ESP32 ou VideoPlayer não disponível para integração', 'warning');
        }
        
        // Adicionar informações de debug
        this.addDebugInfo();
    }
    
    setCurrentPlayer(playerId) {
        this.currentPlayer = playerId;
        
        if (window.esp32Client) {
            window.esp32Client.setPlayerId(playerId);
        }
        
        if (window.questSimulator) {
            window.questSimulator.setPlayerId(playerId);
        }
        
        this.log(`Player atual alterado para: ${playerId}`, 'info');
        this.updatePlayerIndicator();
    }
    
    toggleAutoMode() {
        this.autoMode = !this.autoMode;
        
        const autoBtn = document.getElementById('autoModeBtn');
        if (autoBtn) {
            autoBtn.textContent = this.autoMode ? '🤖 Auto ON' : '🤖 Modo Automático';
            autoBtn.className = this.autoMode ? 'btn btn-success' : 'btn btn-info';
        }
        
        if (this.autoMode) {
            this.log('Modo automático ativado', 'success');
            this.startAutoSequence();
        } else {
            this.log('Modo automático desativado', 'info');
            this.stopAutoSequence();
        }
    }
    
    startAutoSequence() {
        if (!this.autoMode) return;
        
        this.log('Iniciando sequência automática...', 'info');
        
        // Sequência automática de eventos
        setTimeout(() => this.setCurrentPlayer(1), 1000);
        setTimeout(() => {
            if (window.questSimulator) {
                window.questSimulator.simulateHeadsetOn();
            }
        }, 2000);
        setTimeout(() => {
            if (window.questSimulator) {
                window.questSimulator.simulateAppFocus();
            }
        }, 3000);
        setTimeout(() => {
            if (window.questSimulator) {
                window.questSimulator.simulatePlay();
            }
        }, 4000);
        setTimeout(() => {
            if (window.questSimulator) {
                window.questSimulator.simulatePause();
            }
        }, 12000);
        setTimeout(() => {
            if (window.questSimulator) {
                window.questSimulator.simulatePlay();
            }
        }, 14000);
        setTimeout(() => {
            if (window.questSimulator) {
                window.questSimulator.simulateStop();
            }
        }, 20000);
        setTimeout(() => {
            if (window.questSimulator) {
                window.questSimulator.simulateHeadsetOff();
            }
        }, 22000);
        
        // Repetir sequência se modo automático ainda estiver ativo
        setTimeout(() => {
            if (this.autoMode) {
                this.startAutoSequence();
            }
        }, 25000);
    }
    
    stopAutoSequence() {
        // Parar qualquer sequência em andamento
        if (window.questSimulator) {
            window.questSimulator.simulateStop();
        }
    }
    
    testLEDs() {
        this.log('Iniciando teste de LEDs...', 'info');
        
        if (window.ledVisualizer) {
            window.ledVisualizer.testAllLEDs();
        }
    }
    
    resetAll() {
        this.log('Resetando todos os sistemas...', 'warning');
        
        // Parar modo automático
        this.autoMode = false;
        this.stopAutoSequence();
        
        // Resetar Quest Simulator
        if (window.questSimulator) {
            window.questSimulator.simulateStop();
            window.questSimulator.simulateHeadsetOff();
        }
        
        // Resetar Video Player
        if (window.videoPlayer) {
            window.videoPlayer.stop();
        }
        
        // Resetar LEDs
        if (window.ledVisualizer) {
            window.ledVisualizer.clearAllLEDs();
        }
        
        // Limpar logs
        if (window.esp32Client) {
            window.esp32Client.clearLogs();
        }
        
        this.log('Reset completo realizado', 'success');
    }
    
    showWelcomeMessage() {
        const welcome = document.createElement('div');
        welcome.id = 'welcome-message';
        welcome.style.cssText = `
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            background: rgba(0, 0, 0, 0.9);
            color: white;
            padding: 30px;
            border-radius: 15px;
            text-align: center;
            z-index: 2000;
            max-width: 500px;
        `;
        welcome.innerHTML = `
            <h2>🥽 VR Player Simulator</h2>
            <p>Bem-vindo ao simulador de eventos Meta Quest!</p>
            <div style="text-align: left; margin: 20px 0;">
                <h3>🎮 Atalhos de Teclado:</h3>
                <ul>
                    <li><strong>Ctrl + H:</strong> Headset ON/OFF</li>
                    <li><strong>Ctrl + F:</strong> App Focus ON/OFF</li>
                    <li><strong>Ctrl + P:</strong> Play/Pause</li>
                    <li><strong>Ctrl + S:</strong> Stop</li>
                    <li><strong>Ctrl + 1/2:</strong> Trocar Player</li>
                    <li><strong>Ctrl + A:</strong> Modo Automático</li>
                    <li><strong>Ctrl + T:</strong> Teste de LEDs</li>
                    <li><strong>Ctrl + R:</strong> Reset</li>
                </ul>
            </div>
            <button onclick="this.parentElement.remove()" class="btn btn-primary">Começar</button>
        `;
        
        document.body.appendChild(welcome);
        
        // Remover automaticamente após 10 segundos
        setTimeout(() => {
            if (welcome.parentElement) {
                welcome.remove();
            }
        }, 10000);
    }
    
    showIntegrationStatus(connected) {
        const status = document.createElement('div');
        status.id = 'integration-status';
        status.style.cssText = `
            position: fixed;
            top: 60px;
            right: 10px;
            padding: 10px 15px;
            border-radius: 8px;
            color: white;
            font-weight: 600;
            z-index: 1000;
            transition: all 0.3s ease;
        `;
        
        if (connected) {
            status.style.background = '#48bb78';
            status.textContent = '✅ Sistemas Integrados';
        } else {
            status.style.background = '#f56565';
            status.textContent = '❌ Sistemas Desconectados';
        }
        
        // Remover status anterior se existir
        const existingStatus = document.getElementById('integration-status');
        if (existingStatus) {
            existingStatus.remove();
        }
        
        document.body.appendChild(status);
        
        // Remover após 3 segundos
        setTimeout(() => {
            if (status.parentElement) {
                status.remove();
            }
        }, 3000);
    }
    
    updatePlayerIndicator() {
        let indicator = document.getElementById('player-indicator');
        
        if (!indicator) {
            indicator = document.createElement('div');
            indicator.id = 'player-indicator';
            indicator.style.cssText = `
                position: fixed;
                top: 10px;
                left: 10px;
                padding: 8px 15px;
                background: #667eea;
                color: white;
                border-radius: 20px;
                font-weight: 600;
                z-index: 1000;
            `;
            document.body.appendChild(indicator);
        }
        
        indicator.textContent = `Player ${this.currentPlayer}`;
    }
    
    addDebugInfo() {
        const debugInfo = document.createElement('div');
        debugInfo.id = 'debug-info';
        debugInfo.style.cssText = `
            position: fixed;
            bottom: 10px;
            left: 10px;
            background: rgba(0, 0, 0, 0.8);
            color: white;
            padding: 10px;
            border-radius: 8px;
            font-family: monospace;
            font-size: 0.8rem;
            z-index: 1000;
            max-width: 300px;
        `;
        
        debugInfo.innerHTML = `
            <div><strong>Debug Info:</strong></div>
            <div>Player: <span id="debug-player">${this.currentPlayer}</span></div>
            <div>Auto Mode: <span id="debug-auto">${this.autoMode ? 'ON' : 'OFF'}</span></div>
            <div>WebSocket: <span id="debug-ws">Desconectado</span></div>
            <div>Video: <span id="debug-video">Não carregado</span></div>
        `;
        
        document.body.appendChild(debugInfo);
        
        // Atualizar informações de debug periodicamente
        setInterval(() => {
            const playerSpan = document.getElementById('debug-player');
            const autoSpan = document.getElementById('debug-auto');
            const wsSpan = document.getElementById('debug-ws');
            const videoSpan = document.getElementById('debug-video');
            
            if (playerSpan) playerSpan.textContent = this.currentPlayer;
            if (autoSpan) autoSpan.textContent = this.autoMode ? 'ON' : 'OFF';
            
            if (wsSpan) {
                wsSpan.textContent = window.esp32Client && window.esp32Client.isConnected ? 'Conectado' : 'Desconectado';
                wsSpan.style.color = window.esp32Client && window.esp32Client.isConnected ? '#48bb78' : '#f56565';
            }
            
            if (videoSpan) {
                videoSpan.textContent = window.videoPlayer && window.videoPlayer.isLoaded ? 'Carregado' : 'Não carregado';
                videoSpan.style.color = window.videoPlayer && window.videoPlayer.isLoaded ? '#48bb78' : '#f56565';
            }
        }, 1000);
    }
    
    log(message, type = 'info') {
        if (window.esp32Client) {
            window.esp32Client.log(`[VR Player App] ${message}`, type);
        } else {
            console.log(`[VR Player App] ${message}`);
        }
    }
}

// Inicializar a aplicação quando a página carregar
document.addEventListener('DOMContentLoaded', () => {
    window.vrPlayerApp = new VRPlayerApp();
});
