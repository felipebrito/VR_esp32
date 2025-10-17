using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace CoralVivoVR.ESP32
{
    /// <summary>
    /// Script para testar todos os comandos LED ESP32
    /// Conecta automaticamente e testa todos os estados mapeados
    /// </summary>
    public class ESP32LEDTester : MonoBehaviour
    {
        [Header("🔧 Configuração ESP32")]
        [SerializeField] private string esp32IP = "192.168.0.1";
        [SerializeField] private int esp32Port = 80;
        
        [Header("🎮 Configuração Player")]
        [SerializeField] private int playerID = 1; // 1 ou 2
        [SerializeField] private bool autoConnect = true;
        
        [Header("🎥 Controle de Vídeo")]
        [SerializeField] private VideoPlayer videoPlayer;
        
        [Header("🎨 Estados LED")]
        [SerializeField] private bool isReady = false;
        [SerializeField] private bool isPlaying = false;
        [SerializeField] private bool isPaused = false;
        [SerializeField] private bool isHeadsetOff = false;
        [SerializeField] private bool isSignalLost = false;
        [SerializeField] private bool isStopped = false;
        
        [Header("📊 Progresso")]
        [Range(0, 100)]
        [SerializeField] private float progress = 0f;
        
        [Header("🔗 Conexão")]
        [SerializeField] private bool isConnected = false;
        private ClientWebSocket webSocket;
        
        // Fila de ações para executar na thread principal
        private System.Collections.Generic.Queue<System.Action> mainThreadActions = new System.Collections.Generic.Queue<System.Action>();
        
        // Estado do vídeo antes de perder foco
        private bool wasVideoPlayingBeforeFocusLoss = false;
        private float videoTimeBeforeFocusLoss = 0f;
        
        
        private void Start()
        {
            Debug.Log($"🎮 ESP32LEDTester iniciado - Player {playerID}");
            
            // Configurar VideoPlayer automaticamente
            SetupVideoPlayer();
            
            if (autoConnect)
            {
                ConnectToESP32();
            }
        }
        
        private void SetupVideoPlayer()
        {
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
                if (videoPlayer == null)
                {
                    videoPlayer = gameObject.AddComponent<VideoPlayer>();
                }
            }
            
            // Configurar vídeo Pierre_Final.mp4
            videoPlayer.source = VideoSource.Url;
            
            // Configurar URL correta para Android (Quest)
            string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Pierre_Final.mp4");
            
            #if UNITY_ANDROID && !UNITY_EDITOR
                // No Android (Quest), usar file:///
                videoPlayer.url = "file://" + videoPath;
            #else
                // No Editor, usar caminho direto
                videoPlayer.url = videoPath;
            #endif
            
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            
            // Configurar para reproduzir em tela cheia
            videoPlayer.targetCameraAlpha = 1.0f;
            
            Debug.Log($"🎬 VideoPlayer configurado: {videoPlayer.url}");
            Debug.Log($"🎬 Caminho original: {videoPath}");
            Debug.Log($"🎬 Duração do vídeo: 3m35s (215 segundos)");
            
            // Verificar se o arquivo existe
            if (System.IO.File.Exists(videoPath))
            {
                Debug.Log($"✅ Arquivo de vídeo encontrado: {videoPath}");
            }
            else
            {
                Debug.LogError($"❌ Arquivo de vídeo NÃO encontrado: {videoPath}");
                Debug.LogError($"❌ StreamingAssets path: {Application.streamingAssetsPath}");
            }
        }
        
        private void Update()
        {
            // 🔍 DEBUG: Log periódico do estado
            if (Time.frameCount % 300 == 0) // A cada 5 segundos (60fps * 5s)
            {
                Debug.Log($"🔍 DEBUG - Estado: Conectado={isConnected}, Player={playerID}, WebSocket={webSocket?.State}");
            }
            
            HandleInput();
            UpdateProgress();
            SyncVideoWithLEDs();
            ProcessMainThreadActions();
        }
        
        private void ProcessMainThreadActions()
        {
            // Processar ações da fila na thread principal
            while (mainThreadActions.Count > 0)
            {
                var action = mainThreadActions.Dequeue();
                try
                {
                    action?.Invoke();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Erro ao executar ação na thread principal: {e.Message}");
                }
            }
        }
        
        private void SyncVideoWithLEDs()
        {
            if (videoPlayer != null && isConnected && videoPlayer.isPlaying)
            {
                // Sincronizar progresso do vídeo com LEDs
                // Duração do Pierre_Final.mp4: 3m35s = 215 segundos
                float videoDuration = 215f; // 3 minutos e 35 segundos
                float videoProgress = (float)(videoPlayer.time / videoDuration) * 100f;
                
                // Limitar progresso entre 0 e 100%
                videoProgress = Mathf.Clamp(videoProgress, 0f, 100f);
                
                if (Mathf.Abs(videoProgress - progress) > 1f) // Só atualizar se diferença > 1%
                {
                    progress = videoProgress;
                    SendProgressCommand(progress);
                    Debug.Log($"🎬 Sincronizando vídeo: {progress:F1}% (Tempo: {videoPlayer.time:F1}s / {videoDuration}s)");
                }
            }
        }
        
        #region 🔗 Conexão WebSocket
        
        private async void ConnectToESP32()
        {
            try
            {
                string url = $"ws://{esp32IP}:{esp32Port}/ws";
                Debug.Log($"🔌 Conectando ao ESP32: {url}");
                
                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(new System.Uri(url), CancellationToken.None);
                
                Debug.Log("✅ Conectado ao ESP32!");
                isConnected = true;
                SendReadyCommand();
                
                // Iniciar loop de recebimento de mensagens
                _ = Task.Run(ReceiveMessages);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao conectar: {e.Message}");
                isConnected = false;
            }
        }
        
        private async Task ReceiveMessages()
        {
            var buffer = new byte[1024];
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Debug.Log($"📨 ESP32: {message}");
                        
                        // Processar eventos de botões da ESP32
                        ProcessButtonEvent(message);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao receber mensagens: {e.Message}");
            }
        }
        
        private void ProcessButtonEvent(string message)
        {
            // Processar eventos de botões físicos da ESP32
            switch (message)
            {
                case "button1_short_press":
                    Debug.Log("🎮 BOTÃO 1 (ESP32) - Press Curto detectado!");
                    // Adicionar à fila para executar na thread principal
                    mainThreadActions.Enqueue(() => OnButton1ShortPress());
                    break;
                    
                case "button1_long_press":
                    Debug.Log("🎮 BOTÃO 1 (ESP32) - Long Press detectado!");
                    mainThreadActions.Enqueue(() => OnButton1LongPress());
                    break;
                    
                case "button2_short_press":
                    Debug.Log("🎮 BOTÃO 2 (ESP32) - Press Curto detectado!");
                    mainThreadActions.Enqueue(() => OnButton2ShortPress());
                    break;
                    
                case "button2_long_press":
                    Debug.Log("🎮 BOTÃO 2 (ESP32) - Long Press detectado!");
                    mainThreadActions.Enqueue(() => OnButton2LongPress());
                    break;
                    
                default:
                    // Outras mensagens (comandos normais)
                    break;
            }
        }
        
        private void OnButton1ShortPress()
        {
            // Botão 1 (Play/Pause) - Press Curto
            Debug.Log("🎮 Ação: Toggle Play/Pause para ambos players");
            
            if (videoPlayer == null)
            {
                Debug.LogWarning("⚠️ VideoPlayer não configurado! Configure o VideoPlayer no Inspector.");
                return;
            }
            
            try
            {
                if (videoPlayer.isPlaying)
                {
                    // Se está tocando, pausar
                    videoPlayer.Pause();
                    SendPauseCommand();
                    Debug.Log("🎬 Vídeo PAUSADO");
                }
                else
                {
                    // Se não está tocando, iniciar
                    try
                    {
                        videoPlayer.Play();
                        SendPlayCommand();
                        Debug.Log("🎬 Vídeo INICIADO");
                    }
                    catch (System.Exception playError)
                    {
                        Debug.LogError($"❌ Erro ao iniciar vídeo: {playError.Message}");
                        Debug.LogError($"❌ URL do vídeo: {videoPlayer.url}");
                        Debug.LogError($"❌ Verifique se o arquivo existe e está no formato correto");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao controlar vídeo: {e.Message}");
            }
        }
        
        private void OnButton1LongPress()
        {
            // Botão 1 (Play/Pause) - Long Press
            Debug.Log("🎮 Ação: Resetar todos os players");
            
            if (videoPlayer == null)
            {
                Debug.LogWarning("⚠️ VideoPlayer não configurado! Configure o VideoPlayer no Inspector.");
                return;
            }
            
            try
            {
                // Resetar vídeo para o início
                videoPlayer.Stop();
                videoPlayer.time = 0;
                SendReadyCommand();
                Debug.Log("🎬 Vídeo RESETADO - Volta ao estado PRONTO");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao resetar vídeo: {e.Message}");
            }
        }
        
        private void OnButton2ShortPress()
        {
            // Botão 2 (Effect/Stop) - Press Curto
            Debug.Log("🎮 Ação: Controlar Player 2");
            
            if (videoPlayer == null)
            {
                Debug.LogWarning("⚠️ VideoPlayer não configurado! Configure o VideoPlayer no Inspector.");
                return;
            }
            
            try
            {
                if (videoPlayer.isPlaying)
                {
                    videoPlayer.Pause();
                    SendPauseCommand();
                    Debug.Log("🎬 Player 2 PAUSADO");
                }
                else
                {
                    try
                    {
                        videoPlayer.Play();
                        SendPlayCommand();
                        Debug.Log("🎬 Player 2 INICIADO");
                    }
                    catch (System.Exception playError)
                    {
                        Debug.LogError($"❌ Erro ao iniciar vídeo Player 2: {playError.Message}");
                        Debug.LogError($"❌ URL do vídeo: {videoPlayer.url}");
                        Debug.LogError($"❌ Verifique se o arquivo existe e está no formato correto");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao controlar vídeo: {e.Message}");
            }
        }
        
        private void OnButton2LongPress()
        {
            // Botão 2 (Effect/Stop) - Long Press
            Debug.Log("🎮 Ação: Resetar todos os players");
            
            if (videoPlayer == null)
            {
                Debug.LogWarning("⚠️ VideoPlayer não configurado! Configure o VideoPlayer no Inspector.");
                return;
            }
            
            try
            {
                // Resetar vídeo para o início
                videoPlayer.Stop();
                videoPlayer.time = 0;
                SendReadyCommand();
                Debug.Log("🎬 Player 2 RESETADO - Volta ao estado PRONTO");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao resetar vídeo: {e.Message}");
            }
        }
        
        private async void DisconnectFromESP32()
        {
            if (webSocket != null)
            {
                try
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Erro ao desconectar: {e.Message}");
                }
                finally
                {
                    webSocket.Dispose();
                    webSocket = null;
                    isConnected = false;
                    Debug.Log("🔌 Desconectado do ESP32");
                }
            }
        }
        
        #endregion
        
        #region 🎮 Controles de Input
        
        private void HandleInput()
        {
            if (!isConnected) 
            {
                Debug.Log("⚠️ Input ignorado - não conectado ao ESP32");
                return;
            }
            
            var keyboard = Keyboard.current;
            if (keyboard == null) 
            {
                Debug.Log("⚠️ Input ignorado - teclado não encontrado");
                return;
            }
            
            // 🔍 DEBUG: Log de todas as teclas pressionadas
            if (keyboard.anyKey.wasPressedThisFrame)
            {
                Debug.Log($"🔍 Tecla pressionada detectada - Estado: {isConnected}");
            }
            
            // Controles principais
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("🎮 TECLA: SPACE (Play)");
                SendPlayCommand();
            }
            
            if (keyboard.pKey.wasPressedThisFrame)
            {
                Debug.Log("🎮 TECLA: P (Pause)");
                SendPauseCommand();
            }
            
            if (keyboard.sKey.wasPressedThisFrame)
            {
                Debug.Log("🎮 TECLA: S (Stop)");
                SendStopCommand();
            }
            
            if (keyboard.rKey.wasPressedThisFrame)
            {
                Debug.Log("🎮 TECLA: R (Ready)");
                SendReadyCommand();
            }
            
            if (keyboard.hKey.wasPressedThisFrame)
            {
                Debug.Log("🎮 TECLA: H (Headset Off)");
                SendHeadsetOffCommand();
            }
            
            if (keyboard.lKey.wasPressedThisFrame)
            {
                Debug.Log("🎮 TECLA: L (Signal Lost)");
                SendSignalLostCommand();
            }
            
            // Controles de progresso
            if (keyboard.upArrowKey.isPressed)
            {
                progress = Mathf.Min(100f, progress + Time.deltaTime * 50f);
                SendProgressCommand(progress);
                Debug.Log($"🎮 TECLA: ↑ (Progresso: {progress:F1}%)");
            }
            
            if (keyboard.downArrowKey.isPressed)
            {
                progress = Mathf.Max(0f, progress - Time.deltaTime * 50f);
                SendProgressCommand(progress);
                Debug.Log($"🎮 TECLA: ↓ (Progresso: {progress:F1}%)");
            }
        }
        
        #endregion
        
        #region 📡 Comandos ESP32
        
        private async void SendCommand(string command)
        {
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(command);
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    Debug.Log($"✅ Comando enviado: {command}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Erro ao enviar comando: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Não conectado ao ESP32");
            }
        }
        
        private void SendReadyCommand()
        {
            Debug.Log($"🔍 SendReadyCommand() chamado - Player {playerID}");
            string command = $"on{playerID}";
            SendCommand(command);
            isReady = true;
            isPlaying = false;
            isPaused = false;
            isHeadsetOff = false;
            isSignalLost = false;
            isStopped = false;
            Debug.Log($"🟢 Player {playerID} - PRONTO (Verde fixo)");
        }
        
        private void SendPlayCommand()
        {
            Debug.Log($"🔍 SendPlayCommand() chamado - Player {playerID}");
            string command = $"play{playerID}";
            SendCommand(command);
            isReady = false;
            isPlaying = true;
            isPaused = false;
            isHeadsetOff = false;
            isSignalLost = false;
            progress = 0f;
            Debug.Log($"🔵 Player {playerID} - PLAY (Progressão azul/vermelho)");
        }
        
        private void SendPauseCommand()
        {
            Debug.Log($"🔍 SendPauseCommand() chamado - Player {playerID}");
            string command = $"pause{playerID}";
            SendCommand(command);
            isReady = false;
            isPlaying = false;
            isPaused = true;
            isHeadsetOff = false;
            isSignalLost = false;
            Debug.Log($"⏸️ Player {playerID} - PAUSED (Azul/Vermelho escuro)");
        }
        
        private void SendStopCommand()
        {
            Debug.Log($"🔍 SendStopCommand() chamado - Player {playerID}");
            // Stop (longpress) = Verde fixo (pronto)
            string command = $"on{playerID}";
            SendCommand(command);
            isReady = true;
            isPlaying = false;
            isPaused = false;
            isHeadsetOff = false;
            isSignalLost = false;
            isStopped = false;
            Debug.Log($"🟢 Player {playerID} - STOP (Verde fixo - pronto)");
        }
        
        private void SendHeadsetOffCommand()
        {
            Debug.Log($"🔍 SendHeadsetOffCommand() chamado - Player {playerID}");
            // Headset Off ou parou cena Unity = Chase
            string command = $"signal_lost{playerID}";
            if (playerID == 2)
            {
                command = $"signal_lost{playerID}"; // Player 2 = Chase por padrão
            }
            SendCommand(command);
            isReady = false;
            isPlaying = false;
            isPaused = false;
            isHeadsetOff = true;
            isSignalLost = true;
            Debug.Log($"🏃 Player {playerID} - HEADSET OFF (Chase)");
        }
        
        private void SendSignalLostCommand()
        {
            Debug.Log($"🔍 SendSignalLostCommand() chamado - Player {playerID}");
            // Signal Lost = Chase effect para ambos players quando aplicação para
            string command = $"signal_lost{playerID}";
            // Player 2 = Chase por padrão, Player 1 também = Chase quando aplicação para
            SendCommand(command);
            isReady = false;
            isPlaying = false;
            isPaused = false;
            isHeadsetOff = false;
            isSignalLost = true;
            Debug.Log($"🏃 Player {playerID} - PERDEU CONEXÃO (Chase)");
        }
        
        private void SendProgressCommand(float progressValue)
        {
            string command = $"led{playerID}:{Mathf.RoundToInt(progressValue)}";
            SendCommand(command);
            Debug.Log($"📊 Player {playerID} - Progresso: {progressValue:F1}%");
        }
        
        #endregion
        
        #region 🔄 Atualizações
        
        private void UpdateProgress()
        {
            if (isPlaying)
            {
                // Simular progresso automático durante play
                progress += Time.deltaTime * 20f; // 20% por segundo
                if (progress >= 100f)
                {
                    progress = 100f;
                    // Auto pause quando chegar em 100%
                    SendPauseCommand();
                }
            }
        }
        
        #endregion
        
        #region 🎯 Comandos Especiais
        
        
        #endregion
        
        
        #region 🧹 Cleanup
        
        private void OnDestroy()
        {
            DisconnectFromESP32();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Salvar estado do vídeo antes de pausar
                if (videoPlayer != null)
                {
                    wasVideoPlayingBeforeFocusLoss = videoPlayer.isPlaying;
                    videoTimeBeforeFocusLoss = (float)videoPlayer.time;
                    
                    if (videoPlayer.isPlaying)
                    {
                        videoPlayer.Pause();
                        Debug.Log($"🎬 Vídeo pausado devido à aplicação pausada (tempo: {videoTimeBeforeFocusLoss:F2}s)");
                    }
                }
                
                SendSignalLostCommand();
                Debug.Log("🎮 Aplicação PAUSADA - Enviando comando de perda de sinal (Chase)");
            }
            else
            {
                // Quando a aplicação volta, restaurar estado do vídeo
                RestoreVideoState();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Salvar estado do vídeo antes de perder foco
                if (videoPlayer != null)
                {
                    wasVideoPlayingBeforeFocusLoss = videoPlayer.isPlaying;
                    videoTimeBeforeFocusLoss = (float)videoPlayer.time;
                    
                    if (videoPlayer.isPlaying)
                    {
                        videoPlayer.Pause();
                        Debug.Log($"🎬 Vídeo pausado devido à perda de foco (tempo: {videoTimeBeforeFocusLoss:F2}s)");
                    }
                }
                
                SendSignalLostCommand();
                Debug.Log("🎮 Aplicação PERDEU FOCO - Enviando comando de perda de sinal (Chase)");
            }
            else
            {
                // Quando a aplicação ganha foco, restaurar estado do vídeo
                RestoreVideoState();
            }
        }
        
        private void RestoreVideoState()
        {
            if (videoPlayer == null)
            {
                SendReadyCommand();
                Debug.Log("🎮 Aplicação GANHOU FOCO - VideoPlayer não configurado, voltando ao PRONTO");
                return;
            }
            
            if (wasVideoPlayingBeforeFocusLoss)
            {
                // Se estava tocando, continuar de onde parou
                videoPlayer.time = videoTimeBeforeFocusLoss;
                videoPlayer.Play();
                SendPlayCommand();
                Debug.Log($"🎬 Vídeo RESTAURADO - Continuando de {videoTimeBeforeFocusLoss:F2}s");
            }
            else
            {
                // Se não estava tocando, voltar ao estado pronto
                SendReadyCommand();
                Debug.Log("🎮 Aplicação GANHOU FOCO - Vídeo não estava tocando, voltando ao PRONTO");
            }
            
            // Resetar estado
            wasVideoPlayingBeforeFocusLoss = false;
            videoTimeBeforeFocusLoss = 0f;
        }
        
        #endregion
    }
}
