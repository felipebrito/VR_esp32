/**
 * Visualizador de LEDs ESP32
 * Mostra o status visual dos LEDs do ESP32 em tempo real
 */
class LEDVisualizer {
    constructor() {
        this.leds = [];
        this.player1LEDs = [];
        this.player2LEDs = [];
        this.isBlinking = false;
        this.blinkInterval = null;
        
        this.init();
    }
    
    init() {
        this.setupLEDs();
        this.setupEventListeners();
        this.log('LED Visualizer inicializado', 'info');
    }
    
    setupLEDs() {
        // Configurar LEDs do Player 1 (1-8)
        for (let i = 1; i <= 8; i++) {
            const led = document.querySelector(`[data-led="${i}"]`);
            if (led) {
                this.player1LEDs.push(led);
                this.leds.push(led);
            }
        }
        
        // Configurar LEDs do Player 2 (9-16)
        for (let i = 9; i <= 16; i++) {
            const led = document.querySelector(`[data-led="${i}"]`);
            if (led) {
                this.player2LEDs.push(led);
                this.leds.push(led);
            }
        }
        
        this.log(`Configurados ${this.leds.length} LEDs (Player 1: ${this.player1LEDs.length}, Player 2: ${this.player2LEDs.length})`, 'info');
    }
    
    setupEventListeners() {
        // Integração com WebSocket client
        if (window.esp32Client) {
            window.esp32Client.on('onMessage', (event) => {
                this.handleESP32Message(event.data);
            });
        }
        
        // Integração com Quest Simulator
        if (window.questSimulator) {
            // Monitorar mudanças de status do player
            this.monitorPlayerStatus();
        }
    }
    
    handleESP32Message(message) {
        // Processar mensagens do ESP32 para atualizar LEDs
        if (message.includes('led1:')) {
            const progress = parseInt(message.split(':')[1]);
            this.updatePlayer1Progress(progress);
        } else if (message.includes('led2:')) {
            const progress = parseInt(message.split(':')[1]);
            this.updatePlayer2Progress(progress);
        } else if (message.includes('on1')) {
            this.startPlayer1Blinking();
        } else if (message.includes('on2')) {
            this.startPlayer2Blinking();
        } else if (message.includes('play1')) {
            this.stopPlayer1Blinking();
        } else if (message.includes('play2')) {
            this.stopPlayer2Blinking();
        } else if (message.includes('off1') || message.includes('stop1')) {
            this.clearPlayer1LEDs();
        } else if (message.includes('off2') || message.includes('stop2')) {
            this.clearPlayer2LEDs();
        }
    }
    
    updatePlayer1Progress(progress) {
        this.stopPlayer1Blinking();
        
        const ledsToLight = Math.round((progress / 100) * this.player1LEDs.length);
        
        this.player1LEDs.forEach((led, index) => {
            if (index < ledsToLight) {
                led.classList.add('active', 'player1');
                led.classList.remove('player2', 'ready');
            } else {
                led.classList.remove('active', 'player1', 'player2', 'ready');
            }
        });
        
        this.log(`Player 1 progresso: ${progress}% (${ledsToLight} LEDs)`, 'info');
    }
    
    updatePlayer2Progress(progress) {
        this.stopPlayer2Blinking();
        
        const ledsToLight = Math.round((progress / 100) * this.player2LEDs.length);
        
        this.player2LEDs.forEach((led, index) => {
            if (index < ledsToLight) {
                led.classList.add('active', 'player2');
                led.classList.remove('player1', 'ready');
            } else {
                led.classList.remove('active', 'player1', 'player2', 'ready');
            }
        });
        
        this.log(`Player 2 progresso: ${progress}% (${ledsToLight} LEDs)`, 'info');
    }
    
    startPlayer1Blinking() {
        this.stopPlayer1Blinking();
        
        // Piscar apenas o LED do meio (LED 4)
        const middleLed = this.player1LEDs[3]; // LED 4 (índice 3)
        if (middleLed) {
            middleLed.classList.add('ready');
            middleLed.classList.remove('player1', 'player2', 'active');
        }
        
        this.log('Player 1 LED do meio piscando verde (READY)', 'info');
    }
    
    startPlayer2Blinking() {
        this.stopPlayer2Blinking();
        
        // Piscar apenas o LED do meio (LED 12)
        const middleLed = this.player2LEDs[3]; // LED 12 (índice 3)
        if (middleLed) {
            middleLed.classList.add('ready');
            middleLed.classList.remove('player1', 'player2', 'active');
        }
        
        this.log('Player 2 LED do meio piscando verde (READY)', 'info');
    }
    
    startPlayer1OfflineBlinking() {
        this.stopPlayer1Blinking();
        
        // Piscar apenas o LED do meio (LED 4) em laranja
        const middleLed = this.player1LEDs[3]; // LED 4 (índice 3)
        if (middleLed) {
            middleLed.classList.add('offline');
            middleLed.classList.remove('player1', 'player2', 'active', 'ready');
        }
        
        this.log('Player 1 LED do meio piscando laranja (OFFLINE)', 'warning');
    }
    
    startPlayer2OfflineBlinking() {
        this.stopPlayer2Blinking();
        
        // Piscar apenas o LED do meio (LED 12) em laranja
        const middleLed = this.player2LEDs[3]; // LED 12 (índice 3)
        if (middleLed) {
            middleLed.classList.add('offline');
            middleLed.classList.remove('player1', 'player2', 'active', 'ready');
        }
        
        this.log('Player 2 LED do meio piscando laranja (OFFLINE)', 'warning');
    }
    
    stopPlayer1Blinking() {
        this.player1LEDs.forEach(led => {
            led.classList.remove('ready', 'offline');
        });
    }
    
    stopPlayer2Blinking() {
        this.player2LEDs.forEach(led => {
            led.classList.remove('ready', 'offline');
        });
    }
    
    clearPlayer1LEDs() {
        this.stopPlayer1Blinking();
        this.player1LEDs.forEach(led => {
            led.classList.remove('active', 'player1', 'player2', 'ready');
        });
        this.log('Player 1 LEDs apagados', 'info');
    }
    
    clearPlayer2LEDs() {
        this.stopPlayer2Blinking();
        this.player2LEDs.forEach(led => {
            led.classList.remove('active', 'player1', 'player2', 'ready');
        });
        this.log('Player 2 LEDs apagados', 'info');
    }
    
    clearAllLEDs() {
        this.clearPlayer1LEDs();
        this.clearPlayer2LEDs();
        this.log('Todos os LEDs apagados', 'info');
    }
    
    monitorPlayerStatus() {
        // Monitorar mudanças de status do Quest Simulator
        setInterval(() => {
            const playerStatus = document.getElementById('playerStatus');
            if (playerStatus) {
                const status = playerStatus.textContent.toLowerCase();
                
                if (status === 'disconnected') {
                    this.clearAllLEDs();
                } else if (status === 'ready') {
                    // LEDs já devem estar piscando se o comando foi enviado
                } else if (status === 'playing') {
                    // LEDs devem mostrar progresso
                } else if (status === 'paused') {
                    // LEDs devem manter posição atual
                }
            }
        }, 1000);
    }
    
    // Métodos para simulação manual
    simulatePlayer1Ready() {
        this.startPlayer1Blinking();
    }
    
    simulatePlayer2Ready() {
        this.startPlayer2Blinking();
    }
    
    simulatePlayer1Progress(progress) {
        this.updatePlayer1Progress(progress);
    }
    
    simulatePlayer2Progress(progress) {
        this.updatePlayer2Progress(progress);
    }
    
    // Método para testar todos os LEDs
    testAllLEDs() {
        this.log('Testando todos os LEDs...', 'info');
        
        // Teste sequencial
        this.clearAllLEDs();
        
        setTimeout(() => {
            this.player1LEDs.forEach((led, index) => {
                setTimeout(() => {
                    led.classList.add('active', 'player1');
                }, index * 100);
            });
        }, 500);
        
        setTimeout(() => {
            this.player2LEDs.forEach((led, index) => {
                setTimeout(() => {
                    led.classList.add('active', 'player2');
                }, index * 100);
            });
        }, 1500);
        
        setTimeout(() => {
            this.clearAllLEDs();
            this.log('Teste de LEDs concluído', 'success');
        }, 3000);
    }
    
    // Método para mostrar padrão de teste
    showTestPattern() {
        this.clearAllLEDs();
        
        // Padrão alternado
        this.player1LEDs.forEach((led, index) => {
            if (index % 2 === 0) {
                led.classList.add('active', 'player1');
            }
        });
        
        this.player2LEDs.forEach((led, index) => {
            if (index % 2 === 1) {
                led.classList.add('active', 'player2');
            }
        });
        
        this.log('Padrão de teste aplicado', 'info');
    }
    
    log(message, type = 'info') {
        if (window.esp32Client) {
            window.esp32Client.log(`[LED Visualizer] ${message}`, type);
        } else {
            console.log(`[LED Visualizer] ${message}`);
        }
    }
    
    // Método para obter status atual dos LEDs
    getLEDStatus() {
        const status = {
            player1: {
                leds: this.player1LEDs.map(led => ({
                    active: led.classList.contains('active'),
                    player1: led.classList.contains('player1'),
                    player2: led.classList.contains('player2'),
                    ready: led.classList.contains('ready')
                }))
            },
            player2: {
                leds: this.player2LEDs.map(led => ({
                    active: led.classList.contains('active'),
                    player1: led.classList.contains('player1'),
                    player2: led.classList.contains('player2'),
                    ready: led.classList.contains('ready')
                }))
            }
        };
        
        return status;
    }
    
    // Método para aplicar status específico
    applyLEDStatus(status) {
        if (status.player1) {
            status.player1.leds.forEach((ledStatus, index) => {
                if (this.player1LEDs[index]) {
                    const led = this.player1LEDs[index];
                    led.classList.toggle('active', ledStatus.active);
                    led.classList.toggle('player1', ledStatus.player1);
                    led.classList.toggle('player2', ledStatus.player2);
                    led.classList.toggle('ready', ledStatus.ready);
                }
            });
        }
        
        if (status.player2) {
            status.player2.leds.forEach((ledStatus, index) => {
                if (this.player2LEDs[index]) {
                    const led = this.player2LEDs[index];
                    led.classList.toggle('active', ledStatus.active);
                    led.classList.toggle('player1', ledStatus.player1);
                    led.classList.toggle('player2', ledStatus.player2);
                    led.classList.toggle('ready', ledStatus.ready);
                }
            });
        }
    }
}

// Instanciar o visualizador globalmente
window.ledVisualizer = new LEDVisualizer();
