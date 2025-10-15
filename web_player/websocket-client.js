/**
 * WebSocket Client para comunicação com ESP32
 * Simula eventos do Meta Quest e controla LEDs
 */
class ESP32WebSocketClient {
    constructor() {
        this.ws = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 3000;
        this.selectedPlayer = 1; // Player selecionado (1, 2, ou 'both')
        this.syncSessionActive = false;
        this.players = {
            1: { headsetOn: false, appFocused: false, state: 'disconnected', progress: 0 },
            2: { headsetOn: false, appFocused: false, state: 'disconnected', progress: 0 }
        };
        this.callbacks = {
            onConnect: [],
            onDisconnect: [],
            onMessage: [],
            onError: []
        };
        
        this.init();
    }
    
    init() {
        this.setupEventListeners();
        this.log('WebSocket Client inicializado', 'info');
    }
    
    setupEventListeners() {
        // Botões de conexão
        document.getElementById('connectBtn').addEventListener('click', () => this.connect());
        document.getElementById('disconnectBtn').addEventListener('click', () => this.disconnect());
        
        // Seleção de Player
        document.getElementById('selectPlayer1').addEventListener('click', () => this.selectPlayer(1));
        document.getElementById('selectPlayer2').addEventListener('click', () => this.selectPlayer(2));
        document.getElementById('selectBothPlayers').addEventListener('click', () => this.selectPlayer('both'));
        
        // Controles de Headset
        document.getElementById('simulateHeadsetOn').addEventListener('click', () => this.simulateHeadsetOn());
        document.getElementById('simulateHeadsetOff').addEventListener('click', () => this.simulateHeadsetOff());
        document.getElementById('simulateFocus').addEventListener('click', () => this.simulateAppFocus());
        document.getElementById('simulateUnfocus').addEventListener('click', () => this.simulateAppUnfocus());
        
        // Sessão Síncrona
        document.getElementById('startSyncSession').addEventListener('click', () => this.startSyncSession());
        document.getElementById('stopSyncSession').addEventListener('click', () => this.stopSyncSession());
        document.getElementById('pauseSyncSession').addEventListener('click', () => this.pauseSyncSession());
        
        // Botões de teste
        document.getElementById('testOn1').addEventListener('click', () => this.sendCommand('on1'));
        document.getElementById('testPlay1').addEventListener('click', () => this.sendCommand('play1'));
        document.getElementById('testLed1').addEventListener('click', () => this.sendCommand('led1:50'));
        document.getElementById('testOn2').addEventListener('click', () => this.sendCommand('on2'));
        document.getElementById('testPlay2').addEventListener('click', () => this.sendCommand('play2'));
        document.getElementById('testLed2').addEventListener('click', () => this.sendCommand('led2:75'));
        
        // Limpar logs
        document.getElementById('clearLogs').addEventListener('click', () => this.clearLogs());
    }
    
    connect() {
        const ip = document.getElementById('esp32IP').value.trim();
        if (!ip) {
            this.log('Por favor, insira o IP do ESP32', 'error');
            return;
        }
        
        const url = `ws://${ip}/ws`;
        this.log(`Tentando conectar ao ESP32: ${url}`, 'info');
        console.log('[DEBUG] Conectando ao WebSocket:', url);
        
        try {
            this.ws = new WebSocket(url);
            this.setupWebSocketEvents();
            this.log('WebSocket criado com sucesso', 'info');
        } catch (error) {
            this.log(`Erro ao criar WebSocket: ${error.message}`, 'error');
            console.error('[DEBUG] Erro ao criar WebSocket:', error);
        }
    }
    
    setupWebSocketEvents() {
        this.ws.onopen = (event) => {
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.updateConnectionStatus(true);
            this.log('Conectado ao ESP32!', 'success');
            
            // Aguardar um pouco antes de enviar o status "ready"
            setTimeout(() => {
                this.sendPlayerStatus('ready');
            }, 100);
            
            this.triggerCallbacks('onConnect', event);
        };
        
        this.ws.onmessage = (event) => {
            this.log(`[DEBUG] Recebido do ESP32: ${event.data}`, 'info');
            console.log('[DEBUG] Mensagem completa:', event.data);
            this.handleESP32Message(event.data);
            this.triggerCallbacks('onMessage', event);
        };
        
        this.ws.onclose = (event) => {
            this.isConnected = false;
            this.updateConnectionStatus(false);
            this.log(`Desconectado do ESP32 - Código: ${event.code}, Motivo: ${event.reason}`, 'warning');
            console.log('[DEBUG] WebSocket fechado:', event);
            this.triggerCallbacks('onDisconnect', event);
            
            // Tentar reconectar automaticamente
            if (this.reconnectAttempts < this.maxReconnectAttempts) {
                this.reconnectAttempts++;
                this.log(`Tentativa de reconexão ${this.reconnectAttempts}/${this.maxReconnectAttempts} em ${this.reconnectDelay/1000}s...`, 'info');
                setTimeout(() => this.connect(), this.reconnectDelay);
            } else {
                this.log('Máximo de tentativas de reconexão atingido', 'error');
            }
        };
        
        this.ws.onerror = (error) => {
            this.log(`Erro WebSocket: ${error}`, 'error');
            console.error('[DEBUG] Erro WebSocket:', error);
            this.triggerCallbacks('onError', error);
        };
    }
    
    disconnect() {
        if (this.ws) {
            this.ws.close();
            this.ws = null;
        }
        this.isConnected = false;
        this.updateConnectionStatus(false);
        this.log('Desconectado manualmente', 'info');
    }
    
    sendCommand(command) {
        if (!this.isConnected || !this.ws) {
            this.log('Não conectado ao ESP32', 'error');
            return false;
        }
        
        try {
            this.ws.send(command);
            this.log(`Enviado para ESP32: ${command}`, 'success');
            return true;
        } catch (error) {
            this.log(`Erro ao enviar comando: ${error.message}`, 'error');
            return false;
        }
    }
    
    sendProgress(progress) {
        if (this.selectedPlayer === 'both') {
            // Enviar para ambos os players
            this.sendCommand(`led1:${progress}`);
            this.sendCommand(`led2:${progress}`);
            return true;
        } else {
            const command = `led${this.selectedPlayer}:${progress}`;
            return this.sendCommand(command);
        }
    }
    
    selectPlayer(player) {
        this.selectedPlayer = player;
        
        // Atualizar botões de seleção
        document.getElementById('selectPlayer1').classList.remove('active');
        document.getElementById('selectPlayer2').classList.remove('active');
        document.getElementById('selectBothPlayers').classList.remove('active');
        
        if (player === 1) {
            document.getElementById('selectPlayer1').classList.add('active');
        } else if (player === 2) {
            document.getElementById('selectPlayer2').classList.add('active');
        } else if (player === 'both') {
            document.getElementById('selectBothPlayers').classList.add('active');
        }
        
        this.log(`Player selecionado: ${player === 'both' ? 'Ambos' : `Player ${player}`}`, 'info');
    }
    
    startSyncSession() {
        this.syncSessionActive = true;
        this.log('Iniciando sessão síncrona...', 'success');
        
        // Ativar ambos os players
        this.players[1].state = 'ready';
        this.players[2].state = 'ready';
        
        // Enviar comandos para ESP32
        this.sendCommand('on1');
        this.sendCommand('on2');
        
        // Atualizar interface
        this.updateSyncControls();
        this.updatePlayerStatus(1, 'headset', 'online');
        this.updatePlayerStatus(2, 'headset', 'online');
        this.updateSyncStatus('active');
    }
    
    stopSyncSession() {
        this.syncSessionActive = false;
        this.log('Parando sessão síncrona...', 'info');
        
        // Desativar ambos os players
        this.players[1].state = 'disconnected';
        this.players[2].state = 'disconnected';
        
        // Enviar comandos para ESP32
        this.sendCommand('led1:0');
        this.sendCommand('led2:0');
        
        // Apagar LEDs virtuais
        if (window.ledVisualizer) {
            window.ledVisualizer.clearAllLEDs();
        }
        
        // Atualizar interface
        this.updateSyncControls();
        this.updatePlayerStatus(1, 'headset', 'offline');
        this.updatePlayerStatus(2, 'headset', 'offline');
        this.updateSyncStatus('inactive');
    }
    
    pauseSyncSession() {
        if (!this.syncSessionActive) return;
        
        this.log('Pausando sessão síncrona...', 'warning');
        
        // Pausar ambos os players
        this.players[1].state = 'paused';
        this.players[2].state = 'paused';
        
        // Enviar comandos para ESP32
        this.sendCommand('pause1');
        this.sendCommand('pause2');
        
        // Atualizar interface
        this.updatePlayerStatus(1, 'player', 'paused');
        this.updatePlayerStatus(2, 'player', 'paused');
    }
    
    updateSyncControls() {
        const startBtn = document.getElementById('startSyncSession');
        const stopBtn = document.getElementById('stopSyncSession');
        const pauseBtn = document.getElementById('pauseSyncSession');
        
        if (this.syncSessionActive) {
            startBtn.disabled = true;
            stopBtn.disabled = false;
            pauseBtn.disabled = false;
        } else {
            startBtn.disabled = false;
            stopBtn.disabled = true;
            pauseBtn.disabled = true;
        }
    }
    
    updateSyncStatus(status) {
        const syncStatusElement = document.getElementById('syncStatus');
        const activePlayersElement = document.getElementById('activePlayers');
        
        if (syncStatusElement) {
            syncStatusElement.textContent = status.toUpperCase();
            syncStatusElement.className = `status-value ${status}`;
        }
        
        if (activePlayersElement) {
            const activeCount = Object.values(this.players).filter(p => p.state !== 'disconnected').length;
            activePlayersElement.textContent = activeCount.toString();
        }
    }
    
    sendPlayerStatus(status) {
        if (this.isConnected && this.ws && this.ws.readyState === WebSocket.OPEN) {
            const message = JSON.stringify({
                player: 1, // Player 1 por padrão
                status: status
            });
            this.ws.send(message);
            this.log(`Enviado status '${status}' para ESP32`, 'info');
        } else {
            this.log(`Não foi possível enviar status '${status}' - WebSocket não está pronto`, 'warning');
        }
    }
    
    handleESP32Message(message) {
        // Processar mensagens recebidas do ESP32
        console.log('[DEBUG] Processando mensagem:', message);
        this.log(`[DEBUG] Processando mensagem: ${message}`, 'info');
        
        try {
            // Tentar parsear como JSON primeiro
            const data = JSON.parse(message);
            const command = data.command;
            const player = data.player;
            
            this.log(`[DEBUG] JSON parseado - Comando: ${command}, Player: ${player}`, 'info');
            console.log('[DEBUG] JSON parseado:', data);
            
            if (command === 'play') {
                this.log('ESP32 solicitou PLAY via JSON', 'success');
                this.triggerCallbacks('onPlayCommand', data);
            } else if (command === 'pause') {
                this.log('ESP32 solicitou PAUSE via JSON', 'success');
                this.triggerCallbacks('onPauseCommand', data);
            } else if (command === 'stop') {
                this.log('ESP32 solicitou STOP via JSON', 'success');
                this.triggerCallbacks('onStopCommand', data);
            } else {
                this.log(`[DEBUG] Comando desconhecido: ${command}`, 'warning');
            }
        } catch (e) {
            this.log(`[DEBUG] Não é JSON, processando como string: ${e.message}`, 'info');
            // Se não for JSON, processar como string
            if (message.includes('play')) {
                this.log('ESP32 solicitou PLAY via string', 'success');
                this.triggerCallbacks('onPlayCommand', message);
            } else if (message.includes('pause')) {
                this.log('ESP32 solicitou PAUSE via string', 'success');
                this.triggerCallbacks('onPauseCommand', message);
            } else if (message.includes('stop')) {
                this.log('ESP32 solicitou STOP via string', 'success');
                this.triggerCallbacks('onStopCommand', message);
            } else {
                this.log(`[DEBUG] Mensagem não reconhecida: ${message}`, 'warning');
            }
        }
    }
    
    updateConnectionStatus(connected) {
        const statusElement = document.getElementById('connectionStatus');
        const connectBtn = document.getElementById('connectBtn');
        const disconnectBtn = document.getElementById('disconnectBtn');
        
        if (connected) {
            statusElement.textContent = 'Conectado';
            statusElement.className = 'status connected';
            connectBtn.disabled = true;
            disconnectBtn.disabled = false;
        } else {
            statusElement.textContent = 'Desconectado';
            statusElement.className = 'status disconnected';
            connectBtn.disabled = false;
            disconnectBtn.disabled = true;
        }
    }
    
    log(message, type = 'info') {
        const logsContainer = document.getElementById('logs');
        const timestamp = new Date().toLocaleTimeString();
        
        const logEntry = document.createElement('div');
        logEntry.className = 'log-entry';
        logEntry.innerHTML = `
            <span class="log-time">[${timestamp}]</span>
            <span class="log-type ${type}">[${type.toUpperCase()}]</span>
            <span class="log-message">${message}</span>
        `;
        
        logsContainer.appendChild(logEntry);
        logsContainer.scrollTop = logsContainer.scrollHeight;
        
        // Manter apenas os últimos 100 logs
        const logs = logsContainer.querySelectorAll('.log-entry');
        if (logs.length > 100) {
            logs[0].remove();
        }
    }
    
    clearLogs() {
        document.getElementById('logs').innerHTML = '';
        this.log('Logs limpos', 'info');
    }
    
    // Sistema de callbacks para integração com outros módulos
    on(event, callback) {
        console.log(`[DEBUG] Registrando callback para evento: ${event}`);
        if (!this.callbacks[event]) {
            this.callbacks[event] = [];
        }
        this.callbacks[event].push(callback);
        console.log(`[DEBUG] Callback registrado. Total para ${event}: ${this.callbacks[event].length}`);
    }
    
    triggerCallbacks(event, data) {
        console.log(`[DEBUG] triggerCallbacks chamado para evento: ${event}`);
        console.log(`[DEBUG] Callbacks disponíveis:`, Object.keys(this.callbacks));
        if (this.callbacks[event]) {
            console.log(`[DEBUG] Executando ${this.callbacks[event].length} callbacks para ${event}`);
            this.callbacks[event].forEach(callback => callback(data));
        } else {
            console.log(`[DEBUG] Nenhum callback registrado para evento: ${event}`);
        }
    }
    
    // Métodos para simulação de eventos Meta Quest
    simulateHeadsetOn() {
        if (this.selectedPlayer === 'both') {
            this.log('Simulando: Headset colocado (Ambos players)', 'info');
            this.sendCommand('on1');
            this.sendCommand('on2');
            this.updatePlayerStatus(1, 'headset', 'online');
            this.updatePlayerStatus(2, 'headset', 'online');
            this.updatePlayerStatus(1, 'player', 'connected');
            this.updatePlayerStatus(2, 'player', 'connected');
            
            // Atualizar LEDs virtuais
            if (window.ledVisualizer) {
                window.ledVisualizer.startPlayer1Blinking();
                window.ledVisualizer.startPlayer2Blinking();
            }
        } else {
            this.log(`Simulando: Headset colocado (Player ${this.selectedPlayer})`, 'info');
            this.sendCommand(`on${this.selectedPlayer}`);
            this.updatePlayerStatus(this.selectedPlayer, 'headset', 'online');
            this.updatePlayerStatus(this.selectedPlayer, 'player', 'connected');
            
            // Atualizar LEDs virtuais
            if (window.ledVisualizer) {
                if (this.selectedPlayer == 1) {
                    window.ledVisualizer.startPlayer1Blinking();
                } else if (this.selectedPlayer == 2) {
                    window.ledVisualizer.startPlayer2Blinking();
                }
            }
        }
    }
    
    simulateHeadsetOff() {
        if (this.selectedPlayer === 'both') {
            this.log('Simulando: Headset removido (Ambos players)', 'info');
            this.sendCommand('off1');
            this.sendCommand('off2');
            this.updatePlayerStatus(1, 'headset', 'offline');
            this.updatePlayerStatus(2, 'headset', 'offline');
            this.updatePlayerStatus(1, 'player', 'paused');
            this.updatePlayerStatus(2, 'player', 'paused');
            
            // Atualizar LEDs virtuais - mostrar laranja (pausado por headset)
            if (window.ledVisualizer) {
                window.ledVisualizer.startPlayer1OfflineBlinking();
                window.ledVisualizer.startPlayer2OfflineBlinking();
            }
        } else {
            this.log(`Simulando: Headset removido (Player ${this.selectedPlayer})`, 'info');
            this.sendCommand(`off${this.selectedPlayer}`);
            this.updatePlayerStatus(this.selectedPlayer, 'headset', 'offline');
            this.updatePlayerStatus(this.selectedPlayer, 'player', 'paused');
            
            // Atualizar LEDs virtuais - mostrar laranja (pausado por headset)
            if (window.ledVisualizer) {
                if (this.selectedPlayer == 1) {
                    window.ledVisualizer.startPlayer1OfflineBlinking();
                } else if (this.selectedPlayer == 2) {
                    window.ledVisualizer.startPlayer2OfflineBlinking();
                }
            }
        }
    }
    
    simulateAppFocus() {
        if (this.selectedPlayer === 'both') {
            this.log('Simulando: App ganhou foco (Ambos players)', 'info');
            this.updatePlayerStatus(1, 'focus', 'focused');
            this.updatePlayerStatus(2, 'focus', 'focused');
            this.sendCommand('on1');
            this.sendCommand('on2');
        } else {
            this.log(`Simulando: App ganhou foco (Player ${this.selectedPlayer})`, 'info');
            this.updatePlayerStatus(this.selectedPlayer, 'focus', 'focused');
            this.sendCommand(`on${this.selectedPlayer}`);
        }
    }
    
    simulateAppUnfocus() {
        if (this.selectedPlayer === 'both') {
            this.log('Simulando: App perdeu foco (Ambos players)', 'info');
            this.updatePlayerStatus(1, 'focus', 'unfocused');
            this.updatePlayerStatus(2, 'focus', 'unfocused');
            this.sendCommand('pause1');
            this.sendCommand('pause2');
        } else {
            this.log(`Simulando: App perdeu foco (Player ${this.selectedPlayer})`, 'info');
            this.updatePlayerStatus(this.selectedPlayer, 'focus', 'unfocused');
            this.sendCommand(`pause${this.selectedPlayer}`);
        }
    }
    
    updatePlayerStatus(playerId, type, status) {
        const statusElement = document.getElementById(`${type}Status${playerId}`);
        if (statusElement) {
            statusElement.textContent = status.toUpperCase();
            statusElement.className = `status-value ${status}`;
        }
        
        // Atualizar estado interno
        if (this.players[playerId]) {
            if (type === 'headset') {
                this.players[playerId].headsetOn = (status === 'online');
            } else if (type === 'focus') {
                this.players[playerId].appFocused = (status === 'focused');
            } else if (type === 'player') {
                this.players[playerId].state = status;
            }
        }
        
        // Atualizar contador de players ativos
        this.updateSyncStatus(this.syncSessionActive ? 'active' : 'inactive');
    }
    
    updateProgress(progress) {
        if (this.selectedPlayer === 'both') {
            const progressElement1 = document.getElementById('progressStatus1');
            const progressElement2 = document.getElementById('progressStatus2');
            if (progressElement1) progressElement1.textContent = `${progress}%`;
            if (progressElement2) progressElement2.textContent = `${progress}%`;
        } else {
            const progressElement = document.getElementById(`progressStatus${this.selectedPlayer}`);
            if (progressElement) {
                progressElement.textContent = `${progress}%`;
            }
        }
        this.sendProgress(progress);
    }
}

// Instanciar o cliente WebSocket globalmente
window.esp32Client = new ESP32WebSocketClient();
