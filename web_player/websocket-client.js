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
        this.playerId = 1; // Player padrão
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
        const command = `led${this.playerId}:${progress}`;
        return this.sendCommand(command);
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
        this.log('Simulando: Headset colocado', 'info');
        this.sendCommand(`on${this.playerId}`);
        this.updatePlayerStatus('headset', 'online');
        this.updatePlayerStatus('player', 'connected');
    }
    
    simulateHeadsetOff() {
        this.log('Simulando: Headset removido', 'info');
        this.sendCommand(`off${this.playerId}`);
        this.updatePlayerStatus('headset', 'offline');
        this.updatePlayerStatus('player', 'disconnected');
    }
    
    simulateAppFocus() {
        this.log('Simulando: App ganhou foco', 'info');
        this.updatePlayerStatus('focus', 'focused');
        this.sendCommand(`on${this.playerId}`);
    }
    
    simulateAppUnfocus() {
        this.log('Simulando: App perdeu foco', 'info');
        this.updatePlayerStatus('focus', 'unfocused');
        this.sendCommand(`pause${this.playerId}`);
    }
    
    updatePlayerStatus(type, status) {
        const statusElement = document.getElementById(`${type}Status`);
        if (statusElement) {
            statusElement.textContent = status.toUpperCase();
            statusElement.className = `status-value ${status}`;
        }
    }
    
    updateProgress(progress) {
        const progressElement = document.getElementById('progressStatus');
        if (progressElement) {
            progressElement.textContent = `${progress}%`;
        }
        this.sendProgress(progress);
    }
    
    setPlayerId(playerId) {
        this.playerId = playerId;
        this.log(`Player ID alterado para: ${playerId}`, 'info');
    }
}

// Instanciar o cliente WebSocket globalmente
window.esp32Client = new ESP32WebSocketClient();
