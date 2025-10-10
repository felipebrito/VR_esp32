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
        
        try {
            this.ws = new WebSocket(url);
            this.setupWebSocketEvents();
        } catch (error) {
            this.log(`Erro ao criar WebSocket: ${error.message}`, 'error');
        }
    }
    
    setupWebSocketEvents() {
        this.ws.onopen = (event) => {
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.updateConnectionStatus(true);
            this.log('Conectado ao ESP32!', 'success');
            this.triggerCallbacks('onConnect', event);
        };
        
        this.ws.onmessage = (event) => {
            this.log(`Recebido do ESP32: ${event.data}`, 'info');
            this.handleESP32Message(event.data);
            this.triggerCallbacks('onMessage', event);
        };
        
        this.ws.onclose = (event) => {
            this.isConnected = false;
            this.updateConnectionStatus(false);
            this.log('Desconectado do ESP32', 'warning');
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
    
    handleESP32Message(message) {
        // Processar mensagens recebidas do ESP32
        if (message.includes('play')) {
            this.log('ESP32 solicitou PLAY', 'info');
            // Aqui você pode integrar com o player de vídeo
            this.triggerCallbacks('onPlayCommand', message);
        } else if (message.includes('pause')) {
            this.log('ESP32 solicitou PAUSE', 'info');
            this.triggerCallbacks('onPauseCommand', message);
        } else if (message.includes('stop')) {
            this.log('ESP32 solicitou STOP', 'info');
            this.triggerCallbacks('onStopCommand', message);
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
        if (this.callbacks[event]) {
            this.callbacks[event].push(callback);
        }
    }
    
    triggerCallbacks(event, data) {
        if (this.callbacks[event]) {
            this.callbacks[event].forEach(callback => callback(data));
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
        } else {
            this.log(`Simulando: Headset colocado (Player ${this.selectedPlayer})`, 'info');
            this.sendCommand(`on${this.selectedPlayer}`);
            this.updatePlayerStatus(this.selectedPlayer, 'headset', 'online');
            this.updatePlayerStatus(this.selectedPlayer, 'player', 'connected');
        }
    }
    
    simulateHeadsetOff() {
        if (this.selectedPlayer === 'both') {
            this.log('Simulando: Headset removido (Ambos players)', 'info');
            this.sendCommand('led1:0');
            this.sendCommand('led2:0');
            this.updatePlayerStatus(1, 'headset', 'offline');
            this.updatePlayerStatus(2, 'headset', 'offline');
            this.updatePlayerStatus(1, 'player', 'disconnected');
            this.updatePlayerStatus(2, 'player', 'disconnected');
        } else {
            this.log(`Simulando: Headset removido (Player ${this.selectedPlayer})`, 'info');
            this.sendCommand(`led${this.selectedPlayer}:0`);
            this.updatePlayerStatus(this.selectedPlayer, 'headset', 'offline');
            this.updatePlayerStatus(this.selectedPlayer, 'player', 'disconnected');
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
