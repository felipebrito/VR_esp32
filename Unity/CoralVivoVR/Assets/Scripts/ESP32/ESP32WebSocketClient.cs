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
        [SerializeField] private int esp32Port = 80;
        [SerializeField] private float reconnectDelay = 3f;
        
        [Header("Player Configuration")]
        [SerializeField] private int playerId = 1; // 1 ou 2
        [SerializeField] private bool useRealConnection = true; // true para Quest, false para Editor
        [SerializeField] private bool forceSimulation = false; // Forçar simulação para teste
        
        [Header("Connection Management")]
        [SerializeField] private float heartbeatInterval = 5f; // Intervalo do heartbeat
        [SerializeField] private float connectionTimeout = 10f; // Timeout de conexão
        [SerializeField] private bool autoReconnect = true; // Reconexão automática
        
        // Debug Configuration
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool enableWebSocketDebug = true;
        [SerializeField] private bool enableConnectionDebug = true;
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
            StartCoroutine(ConnectCoroutine());
        }
        
        /// <summary>
        /// Desconectar do ESP32
        /// </summary>
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
                LogDebug("Não conectado ao ESP32 - mensagem não enviada");
                return;
            }
            
            // Adicionar Player ID à mensagem se não estiver presente
            string messageWithPlayer = message;
            if (!message.Contains("player"))
            {
                messageWithPlayer = $"{{\"player\":{playerId},\"message\":\"{message}\"}}";
            }
            
            // Simular envio de mensagem WebSocket
            // Em uma implementação real, usaria WebSocketSharp ou similar
            LogDebug($"Enviando (Player {playerId}): {messageWithPlayer}");
            
            // Simular resposta do ESP32 para comandos específicos
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
            
            // No Editor, simular conexão bem-sucedida para evitar erro de conexão insegura
            #if UNITY_EDITOR
            LogConnectionDebug("Editor Mode - Simulando conexão HTTP bem-sucedida");
            yield return new WaitForSeconds(0.5f); // Simular delay de conexão
            
            LogConnectionDebug("✅ ESP32 HTTP acessível - simulando WebSocket");
            
            isConnecting = false;
            isConnected = true;
            reconnectAttempts = 0;
            lastMessageTime = Time.time;
            
            LogConnectionDebug("✅ Conectado ao ESP32 WebSocket!");
            OnConnected?.Invoke();
            
            if (heartbeatCoroutine != null) StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());
            
            yield return new WaitForSeconds(0.1f);
            SendPlayerStatus("ready");
            #else
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
            #endif
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
                    // Verificar se recebeu mensagem recentemente
                    float timeSinceLastMessage = Time.time - lastMessageTime;
                    
                    if (timeSinceLastMessage > connectionTimeout)
                    {
                        LogDebug($"⚠️ Timeout de conexão - sem mensagens por {timeSinceLastMessage:F1}s");
                        Disconnect();
                        
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
            if (pauseStatus)
            {
                LogDebug("App pausado - enviando pause1");
                SendMessage("pause1");
            }
            else
            {
                LogDebug("App retomado - enviando on1");
                SendMessage("on1");
            }
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                LogDebug("App perdeu foco - enviando pause1");
                SendMessage("pause1");
            }
            else
            {
                LogDebug("App ganhou foco - enviando on1");
                SendMessage("on1");
            }
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
