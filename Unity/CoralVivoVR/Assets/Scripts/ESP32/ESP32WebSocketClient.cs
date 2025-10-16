using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Certificate handler para aceitar conexões inseguras (ESP32 local)
/// </summary>
public class AcceptAllCertificatesSignedWithASpecificKeyPublicKey : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true; // Aceitar todos os certificados para ESP32 local
    }
}

namespace CoralVivoVR.ESP32
{
    /// <summary>
    /// Cliente WebSocket para comunicação com ESP32
    /// Baseado na lógica funcionando da versão desktop
    /// </summary>
    public class ESP32WebSocketClient : MonoBehaviour
    {
        [Header("ESP32 Connection Settings")]
        [SerializeField] private string esp32IP = "192.168.0.1";
        [SerializeField] private int esp32Port = 80; // WebSocket na porta 80
        [SerializeField] private float reconnectDelay = 3f;
        
        [Header("Player Configuration")]
        [SerializeField] private int playerId = 1; // 1 ou 2
        [SerializeField] private bool useRealConnection = true; // true para Quest, false para Editor
        [SerializeField] private bool forceSimulation = false; // Forçar simulação para teste
        [SerializeField] private bool autoDetectPlayerId = true; // Auto-detectar Player ID do VRManager
        
        [Header("Connection Management")]
        [SerializeField] private float heartbeatInterval = 5f; // Intervalo do heartbeat
        [SerializeField] private float connectionTimeout = 10f; // Timeout de conexão
        [SerializeField] private bool autoReconnect = true; // Reconexão automática
        
        // Debug Configuration
        [SerializeField] private bool enableDebugLogs = false; // Desabilitado para reduzir spam
        [SerializeField] private bool enableWebSocketDebug = false; // Desabilitado para reduzir spam
        [SerializeField] private bool enableConnectionDebug = true; // Apenas logs de conexão
        [SerializeField] private int maxReconnectAttempts = 5;
        
        // Events
        public static event Action OnConnected;
        public static event Action OnDisconnected;
        public static event Action<string> OnMessageReceived;
        public static event Action<string> OnError;
        
        // Commands
        public static event Action OnPlayCommand;
        public static event Action OnPauseCommand;
        public static event Action OnStopCommand;
        
        // Connection state
        private bool isConnected = false;
        private bool isConnecting = false;
        private int reconnectAttempts = 0;
        private Coroutine reconnectCoroutine;
        private Coroutine heartbeatCoroutine;
        private Coroutine connectionMonitorCoroutine;
        
        // WebSocket simulation using UnityWebRequest
        private string wsUrl;
        private float lastHeartbeatTime = 0f;
        private float lastMessageTime = 0f;
        
        // Real WebSocket
        private ClientWebSocket webSocket;
        private CancellationTokenSource cancellationTokenSource;
        
        void Start()
        {
            LogDebug("=== ESP32WebSocketClient Start() chamado ===");
            
            // Verificar se o componente está habilitado
            if (!enabled)
            {
                LogDebug("ESP32WebSocketClient desabilitado - não conectando");
                return;
            }
            
            // Auto-detectar Player ID do VRManager se habilitado
            if (autoDetectPlayerId)
            {
                SyncPlayerIdFromVRManager();
            }
            
            wsUrl = $"ws://{esp32IP}:{esp32Port}/ws";
            LogDebug($"ESP32WebSocketClient inicializado - Player {playerId}");
            LogDebug($"ESP32 HTTP: http://{esp32IP}:{esp32Port}/");
            LogDebug($"ESP32 WebSocket: {wsUrl}");
            
            if (useRealConnection && !forceSimulation)
            {
                LogDebug("🔌 Usando conexão REAL com ESP32");
                StartPersistentConnection();
            }
            else
            {
                LogDebug("🔌 Modo Simulação - Simulando conexão ESP32");
                SimulateConnection();
            }
        }
        
        /// <summary>
        /// Sincronizar Player ID com VRManager
        /// </summary>
        private void SyncPlayerIdFromVRManager()
        {
            VRManager vrManager = FindObjectOfType<VRManager>();
            if (vrManager != null)
            {
                int newPlayerId = vrManager.playerId;
                if (newPlayerId != playerId)
                {
                    LogDebug($"🔄 Sincronizando Player ID: {playerId} → {newPlayerId}");
                    playerId = newPlayerId;
                }
            }
            else
            {
                LogDebug("⚠️ VRManager não encontrado - usando Player ID padrão");
            }
        }
        
        /// <summary>
        /// Iniciar conexão persistente com ESP32
        /// </summary>
        private void StartPersistentConnection()
        {
            LogDebug("🚀 Iniciando conexão persistente com ESP32");
            Connect();
            
            // Iniciar monitoramento de conexão
            if (connectionMonitorCoroutine != null)
                StopCoroutine(connectionMonitorCoroutine);
            connectionMonitorCoroutine = StartCoroutine(ConnectionMonitor());
        }
        
        /// <summary>
        /// Simular conexão bem-sucedida para teste
        /// </summary>
        private void SimulateConnection()
        {
            isConnected = true;
            reconnectAttempts = 0;
            lastMessageTime = Time.time;
            LogDebug("🔌 Simulando conexão ESP32 bem-sucedida para teste");
            OnConnected?.Invoke();
            
            // Iniciar heartbeat simulado
            if (heartbeatCoroutine != null)
                StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());
            
            // Enviar status ready
            SendMessage("ready");
        }
        
        /// <summary>
        /// Conectar ao ESP32
        /// </summary>
        public void Connect()
        {
            if (!enabled)
            {
                LogDebug("ESP32WebSocketClient desabilitado - não conectando");
                return;
            }
            
            if (isConnected || isConnecting)
            {
                LogDebug("Já conectado ou conectando ao ESP32");
                return;
            }
            
            isConnecting = true;
            LogDebug($"Tentando conectar ao ESP32: {wsUrl}");
            
            if (useRealConnection && !forceSimulation)
            {
                StartCoroutine(ConnectRealWebSocket());
            }
            else
            {
                StartCoroutine(ConnectCoroutine());
            }
        }
        
        /// <summary>
        /// Conectar usando WebSocket real
        /// </summary>
        private IEnumerator ConnectRealWebSocket()
        {
            LogConnectionDebug("Iniciando conexão WebSocket real...");
            
            webSocket = new ClientWebSocket();
            cancellationTokenSource = new CancellationTokenSource();
            
            LogConnectionDebug($"Conectando ao WebSocket: {wsUrl}");
            
            // Converter para Task e aguardar
            var connectTask = webSocket.ConnectAsync(new Uri(wsUrl), cancellationTokenSource.Token);
            
            // Aguardar conexão
            while (!connectTask.IsCompleted)
            {
                yield return null;
            }
            
            if (connectTask.IsFaulted)
            {
                LogConnectionDebug($"❌ Erro na conexão WebSocket: {connectTask.Exception?.GetBaseException().Message}");
                OnError?.Invoke(connectTask.Exception?.GetBaseException().Message ?? "Erro desconhecido");
                isConnecting = false;
                TryReconnect();
                yield break;
            }
            
            if (webSocket.State == WebSocketState.Open)
            {
                LogConnectionDebug("✅ Conectado ao ESP32 WebSocket!");
                isConnecting = false;
                isConnected = true;
                reconnectAttempts = 0;
                lastMessageTime = Time.time;
                
                OnConnected?.Invoke();
                
                // Iniciar heartbeat
                if (heartbeatCoroutine != null) StopCoroutine(heartbeatCoroutine);
                heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());
                
                // Iniciar listener de mensagens
                StartCoroutine(ListenForMessages());
                
                yield return new WaitForSeconds(0.1f);
                SendPlayerStatus("ready");
            }
            else
            {
                LogConnectionDebug($"❌ WebSocket não conectado. Estado: {webSocket.State}");
                OnError?.Invoke($"WebSocket não conectado. Estado: {webSocket.State}");
                isConnecting = false;
                TryReconnect();
            }
        }
        
        /// <summary>
        /// Escutar mensagens do WebSocket
        /// </summary>
        private IEnumerator ListenForMessages()
        {
            var buffer = new byte[1024];
            
            while (isConnected && webSocket?.State == WebSocketState.Open)
            {
                var receiveTask = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);
                
                // Aguardar mensagem
                while (!receiveTask.IsCompleted)
                {
                    yield return null;
                }
                
                if (receiveTask.IsFaulted)
                {
                    LogDebug($"❌ Erro ao receber mensagem: {receiveTask.Exception?.GetBaseException().Message}");
                    break;
                }
                
                var result = receiveTask.Result;
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    LogDebug($"📨 Mensagem recebida: {message}");
                    lastMessageTime = Time.time;
                    HandleESP32Message(message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    LogDebug("🔌 WebSocket fechado pelo servidor");
                    break;
                }
                
                yield return null;
            }
            
            LogDebug("🔌 Listener de mensagens finalizado");
        }
        
        public void Disconnect()
        {
            if (!isConnected)
            {
                LogDebug("Não está conectado ao ESP32");
                return;
            }
            
            LogDebug("Desconectando do ESP32");
            isConnected = false;
            isConnecting = false;
            
            // Fechar WebSocket real
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                }
                catch (Exception e)
                {
                    LogDebug($"Erro ao fechar WebSocket: {e.Message}");
                }
            }
            
            // Cancelar operações
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            
            // Parar corrotinas
            if (reconnectCoroutine != null)
            {
                StopCoroutine(reconnectCoroutine);
                reconnectCoroutine = null;
            }
            
            if (heartbeatCoroutine != null)
            {
                StopCoroutine(heartbeatCoroutine);
                heartbeatCoroutine = null;
            }
            
            OnDisconnected?.Invoke();
        }
        
        /// <summary>
        /// Enviar status do player para ESP32
        /// </summary>
        public void SendPlayerStatus(string status)
        {
            if (!isConnected)
            {
                LogDebug($"Não foi possível enviar status '{status}' - não conectado");
                return;
            }
            
            string message = $"{{\"player\":{playerId},\"status\":\"{status}\"}}";
            SendMessage(message);
            LogDebug($"Enviado status '{status}' para ESP32");
        }
        
        /// <summary>
        /// Enviar comando LED para ESP32
        /// </summary>
        public void SendLEDCommand(string ledCommand)
        {
            if (!isConnected)
            {
                LogDebug($"Não foi possível enviar comando LED '{ledCommand}' - não conectado");
                return;
            }
            
            SendMessage(ledCommand);
            LogDebug($"Enviado comando LED: {ledCommand}");
        }
        
        /// <summary>
        /// Enviar comando LED para ESP32
        /// </summary>
        public void SendLEDCommand(int playerId, float progress)
        {
            if (!isConnected)
            {
                LogDebug($"Não foi possível enviar LED - não conectado");
                return;
            }
            
            int progressPercent = Mathf.RoundToInt(progress * 100);
            string message = $"led{playerId}:{progressPercent}";
            SendMessage(message);
            LogDebug($"Enviado: {message}");
        }
        
        /// <summary>
        /// Enviar mensagem para ESP32
        /// </summary>
        private void SendMessage(string message)
        {
            if (!isConnected)
            {
                // Não logar para evitar spam - apenas retornar silenciosamente
                return;
            }
            
            // Verificar estado do WebSocket antes de enviar
            if (useRealConnection && !forceSimulation && webSocket?.State != WebSocketState.Open)
            {
                // WebSocket não está aberto - não enviar
                return;
            }
            
            // Adicionar Player ID à mensagem se não estiver presente
            string messageWithPlayer = message;
            if (!message.Contains("player"))
            {
                messageWithPlayer = $"{{\"player\":{playerId},\"message\":\"{message}\"}}";
            }
            
            LogDebug($"Enviando (Player {playerId}): {messageWithPlayer}");
            
            if (useRealConnection && !forceSimulation && webSocket?.State == WebSocketState.Open)
            {
                // Enviar via WebSocket real
                try
                {
                    var buffer = Encoding.UTF8.GetBytes(messageWithPlayer);
                    var sendTask = webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationTokenSource.Token);
                    
                    if (sendTask.IsFaulted)
                    {
                        LogDebug($"❌ Erro ao enviar mensagem: {sendTask.Exception?.GetBaseException().Message}");
                    }
                }
                catch (Exception e)
                {
                    LogDebug($"❌ Exceção ao enviar mensagem: {e.Message}");
                    isConnected = false; // Marcar como desconectado se envio falhar
                }
            }
            else
            {
                // Simular envio para modo de teste
                LogDebug($"Simulando envio: {messageWithPlayer}");
            }
            
            if (message.Contains("ready"))
            {
                LogDebug($"Player {playerId} pronto - aguardando comandos do ESP32");
            }
        }
        
        /// <summary>
        /// Conectar ao ESP32 via WebSocket
        /// </summary>
        private IEnumerator ConnectCoroutine()
        {
            LogConnectionDebug("Iniciando tentativa de conexão WebSocket...");
            LogConnectionDebug($"Tentando conectar ao ESP32: {wsUrl}");
            
            // Teste de conectividade HTTP primeiro usando WWW (mais compatível)
            string testUrl = $"http://{esp32IP}:{esp32Port}/";
            LogConnectionDebug($"Testando conectividade HTTP: {testUrl}");
            
            LogConnectionDebug("Enviando requisição HTTP...");
            using (WWW www = new WWW(testUrl))
            {
                yield return www;
                
                LogConnectionDebug($"Resposta HTTP: {(www.responseHeaders.Count > 0 ? "Success" : "Failed")}");
                LogConnectionDebug($"Erro: {www.error}");
                LogConnectionDebug($"Response Text: {www.text}");
                
                if (string.IsNullOrEmpty(www.error))
                {
                    // HTTP OK - agora simular WebSocket
                    LogConnectionDebug("✅ ESP32 HTTP acessível - simulando WebSocket");
                    
                    isConnecting = false;
                    isConnected = true;
                    reconnectAttempts = 0;
                    lastMessageTime = Time.time;
                    
                    LogConnectionDebug("✅ Conectado ao ESP32 WebSocket!");
                    OnConnected?.Invoke();
                    
                    // Iniciar heartbeat
                    if (heartbeatCoroutine != null)
                        StopCoroutine(heartbeatCoroutine);
                    heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());
                    
                    // Enviar status ready
                    yield return new WaitForSeconds(0.1f);
                    SendPlayerStatus("ready");
                }
                else
                {
                    LogConnectionDebug($"❌ Falha ao conectar ao ESP32: {www.error}");
                    OnError?.Invoke(www.error);
                    
                    isConnecting = false;
                    if (autoReconnect)
                        TryReconnect();
                }
            }
        }
        
        /// <summary>
        /// Tentar reconectar automaticamente
        /// </summary>
        private void TryReconnect()
        {
            if (reconnectAttempts >= maxReconnectAttempts)
            {
                LogDebug("Máximo de tentativas de reconexão atingido");
                return;
            }
            
            reconnectAttempts++;
            LogDebug($"Tentando reconectar... ({reconnectAttempts}/{maxReconnectAttempts})");
            
            reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
        }
        
        /// <summary>
        /// Corrotina de reconexão
        /// </summary>
        private IEnumerator ReconnectCoroutine()
        {
            yield return new WaitForSeconds(reconnectDelay);
            Connect();
        }
        
        /// <summary>
        /// Corrotina de heartbeat para manter conexão ativa
        /// </summary>
        private IEnumerator HeartbeatCoroutine()
        {
            while (isConnected)
            {
                yield return new WaitForSeconds(heartbeatInterval);
                
                if (isConnected)
                {
                    lastHeartbeatTime = Time.time;
                    SendPlayerStatus("heartbeat");
                    LogDebug($"💓 Heartbeat enviado (Player {playerId})");
                }
            }
        }
        
        /// <summary>
        /// Monitor de conexão - verifica se ainda está conectado
        /// </summary>
        private IEnumerator ConnectionMonitor()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                
                if (isConnected)
                {
                    // Verificar estado do WebSocket diretamente
                    if (useRealConnection && !forceSimulation && webSocket?.State != WebSocketState.Open)
                    {
                        LogDebug($"⚠️ WebSocket não está mais aberto - Estado: {webSocket?.State}");
                        isConnected = false;
                        
                        if (autoReconnect)
                        {
                            LogDebug("🔄 Iniciando reconexão automática...");
                            yield return new WaitForSeconds(reconnectDelay);
                            Connect();
                        }
                    }
                }
                else if (!isConnecting && autoReconnect)
                {
                    // Não conectado e não tentando conectar - tentar reconectar
                    LogDebug("🔄 Conexão perdida - tentando reconectar...");
                    Connect();
                }
            }
        }
        
        /// <summary>
        /// Simular comandos do ESP32 para teste
        /// </summary>
        private IEnumerator PollESP32Messages()
        {
            yield return new WaitForSeconds(3f); // Aguardar vídeo carregar
            
            while (true)
            {
                if (isConnected)
                {
                    // Simular comandos do ESP32 baseado nos logs reais
                    yield return StartCoroutine(SimulateESP32Commands());
                }
                yield return new WaitForSeconds(2f); // Verificar a cada 2 segundos
            }
        }
        
        /// <summary>
        /// Simular comandos reais do ESP32
        /// </summary>
        private IEnumerator SimulateESP32Commands()
        {
            // Simular botão 1 (Play/Pause) para Player 1
            if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
            {
                string message = $"{{\"command\":\"play\",\"player\":{playerId}}}";
                LogDebug($"🎮 Simulando Botão 1 (Play/Pause) para Player {playerId}");
                HandleESP32Message(message);
                yield return new WaitForSeconds(1f);
            }
            
            // Simular botão 2 (Stop) para Player 2
            if (UnityEngine.Random.Range(0f, 1f) < 0.3f)
            {
                string message = $"{{\"command\":\"stop\",\"player\":{playerId}}}";
                LogDebug($"🎮 Simulando Botão 2 (Stop) para Player {playerId}");
                HandleESP32Message(message);
                yield return new WaitForSeconds(1f);
            }
        }
        
        /// <summary>
        /// Método público para testar mensagens ESP32 manualmente
        /// </summary>
        public void TestESP32Message(string command, int targetPlayerId = 0)
        {
            if (targetPlayerId == 0) targetPlayerId = playerId;
            string message = $"{{\"command\":\"{command}\",\"player\":{targetPlayerId}}}";
            LogDebug($"🧪 Teste manual: {message}");
            HandleESP32Message(message);
        }
        
        /// <summary>
        /// Processar mensagem recebida do ESP32
        /// </summary>
        private void HandleESP32Message(string message)
        {
            LogDebug($"Recebido do ESP32: {message}");
            lastMessageTime = Time.time; // Atualizar timestamp da última mensagem
            
            try
            {
                // Tentar parsear como JSON
                if (message.Contains("command") && message.Contains("player"))
                {
                    // Extrair player ID da mensagem
                    int messagePlayerId = ExtractPlayerId(message);
                    
                    // Verificar se a mensagem é para este player
                    if (messagePlayerId == playerId || messagePlayerId == 0) // 0 = broadcast
                    {
                        LogDebug($"✅ Mensagem para Player {playerId} - Processando");
                        OnMessageReceived?.Invoke(message);
                        
                        if (message.Contains("\"command\":\"play\""))
                        {
                            LogDebug($"ESP32 solicitou PLAY para Player {playerId}");
                            OnPlayCommand?.Invoke();
                        }
                        else if (message.Contains("\"command\":\"pause\""))
                        {
                            LogDebug($"ESP32 solicitou PAUSE para Player {playerId}");
                            OnPauseCommand?.Invoke();
                        }
                        else if (message.Contains("\"command\":\"stop\""))
                        {
                            LogDebug($"ESP32 solicitou STOP para Player {playerId}");
                            OnStopCommand?.Invoke();
                        }
                    }
                    else
                    {
                        LogDebug($"❌ Mensagem para Player {messagePlayerId} - Ignorando (sou Player {playerId})");
                    }
                }
                else
                {
                    // Mensagem sem player específico - processar para todos
                    LogDebug("📢 Mensagem broadcast - Processando");
                    OnMessageReceived?.Invoke(message);
                }
            }
            catch (Exception e)
            {
                LogDebug($"Erro ao processar mensagem: {e.Message}");
            }
        }
        
        /// <summary>
        /// Extrair Player ID da mensagem JSON
        /// </summary>
        private int ExtractPlayerId(string message)
        {
            try
            {
                // Buscar "player":X na mensagem
                int playerIndex = message.IndexOf("\"player\":");
                if (playerIndex != -1)
                {
                    int startIndex = playerIndex + 9; // Após "player":
                    int endIndex = message.IndexOf(",", startIndex);
                    if (endIndex == -1) endIndex = message.IndexOf("}", startIndex);
                    
                    if (endIndex != -1)
                    {
                        string playerStr = message.Substring(startIndex, endIndex - startIndex).Trim();
                        if (int.TryParse(playerStr, out int playerId))
                        {
                            return playerId;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogDebug($"Erro ao extrair Player ID: {e.Message}");
            }
            
            return 0; // Broadcast se não conseguir extrair
        }
        
        /// <summary>
        /// Log de debug
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ESP32WebSocket] {message}");
            }
        }
        
        private void LogWebSocketDebug(string message)
        {
            if (enableWebSocketDebug)
            {
                Debug.Log($"[ESP32WebSocket] {message}");
            }
        }
        
        private void LogConnectionDebug(string message)
        {
            if (enableConnectionDebug)
            {
                Debug.Log($"[ESP32Connection] {message}");
            }
        }
        
        /// <summary>
        /// Verificar se está conectado
        /// </summary>
        public bool IsConnected => isConnected;
        
        /// <summary>
        /// Obter URL do WebSocket
        /// </summary>
        public string WebSocketURL => wsUrl;
        
        void OnApplicationPause(bool pauseStatus)
        {
            // Desabilitado para evitar comandos desnecessários
            LogDebug($"App pause status: {pauseStatus} - comandos desabilitados");
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            // Desabilitado para evitar comandos desnecessários
            LogDebug($"App focus status: {hasFocus} - comandos desabilitados");
        }
        
        private void OnDestroy()
        {
            // Limpar todas as corrotinas
            if (heartbeatCoroutine != null)
                StopCoroutine(heartbeatCoroutine);
            if (connectionMonitorCoroutine != null)
                StopCoroutine(connectionMonitorCoroutine);
            if (reconnectCoroutine != null)
                StopCoroutine(reconnectCoroutine);
                
            // Desconectar se estiver conectado
            if (isConnected)
                Disconnect();
        }
    }
}
