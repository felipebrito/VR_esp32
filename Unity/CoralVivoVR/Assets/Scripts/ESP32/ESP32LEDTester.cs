using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
        
        // 🎯 Controles de Teste: Space, P, S, R, H, L, ↑/↓
        
        private void Start()
        {
            Debug.Log($"🎮 ESP32LEDTester iniciado - Player {playerID}");
            
            if (autoConnect)
            {
                ConnectToESP32();
            }
        }
        
        private void Update()
        {
            HandleInput();
            UpdateProgress();
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
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao receber mensagens: {e.Message}");
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
            if (!isConnected) return;
            
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            
            // Controles principais
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                SendPlayCommand();
            }
            
            if (keyboard.pKey.wasPressedThisFrame)
            {
                SendPauseCommand();
            }
            
            if (keyboard.sKey.wasPressedThisFrame)
            {
                SendStopCommand();
            }
            
            if (keyboard.rKey.wasPressedThisFrame)
            {
                SendReadyCommand();
            }
            
            if (keyboard.hKey.wasPressedThisFrame)
            {
                SendHeadsetOffCommand();
            }
            
            if (keyboard.lKey.wasPressedThisFrame)
            {
                SendSignalLostCommand();
            }
            
            // Controles de progresso
            if (keyboard.upArrowKey.isPressed)
            {
                progress = Mathf.Min(100f, progress + Time.deltaTime * 50f);
                SendProgressCommand(progress);
            }
            
            if (keyboard.downArrowKey.isPressed)
            {
                progress = Mathf.Max(0f, progress - Time.deltaTime * 50f);
                SendProgressCommand(progress);
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
            // Signal Lost = Rainbow effect
            string command = $"signal_lost{playerID}";
            if (playerID == 1)
            {
                command = $"signal_lost{playerID}"; // Player 1 = Rainbow por padrão
            }
            else
            {
                command = $"signal_lost{playerID}:rainbow"; // Player 2 = Rainbow
            }
            SendCommand(command);
            isReady = false;
            isPlaying = false;
            isPaused = false;
            isHeadsetOff = false;
            isSignalLost = true;
            Debug.Log($"🌈 Player {playerID} - PERDEU CONEXÃO (Rainbow)");
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
        
        [ContextMenu("Test All Commands")]
        public void TestAllCommands()
        {
            if (!isConnected)
            {
                Debug.LogWarning("⚠️ Não conectado ao ESP32");
                return;
            }
            
            StartCoroutine(TestAllCommandsCoroutine());
        }
        
        private IEnumerator TestAllCommandsCoroutine()
        {
            Debug.Log("🧪 Iniciando teste de todos os comandos...");
            
            // Ready
            SendReadyCommand();
            yield return new WaitForSeconds(2f);
            
            // Play
            SendPlayCommand();
            yield return new WaitForSeconds(3f);
            
            // Pause
            SendPauseCommand();
            yield return new WaitForSeconds(2f);
            
            // Headset Off
            SendHeadsetOffCommand();
            yield return new WaitForSeconds(2f);
            
            // Signal Lost
            SendSignalLostCommand();
            yield return new WaitForSeconds(3f);
            
            // Stop (Chase)
            SendStopCommand();
            yield return new WaitForSeconds(3f);
            
            // Reset
            SendReadyCommand();
            
            Debug.Log("✅ Teste de comandos concluído!");
        }
        
        #endregion
        
        #region 🎮 Interface
        
        private void OnGUI()
        {
            if (!isConnected) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            
            GUILayout.Label($"🎮 ESP32 LED Tester - Player {playerID}", GUI.skin.box);
            GUILayout.Space(10);
            
            GUILayout.Label($"🔗 Conectado: {(isConnected ? "✅" : "❌")}");
            GUILayout.Label($"🟢 Pronto (Verde fixo): {(isReady ? "✅" : "❌")}");
            GUILayout.Label($"🔵 Play (Progressão): {(isPlaying ? "✅" : "❌")}");
            GUILayout.Label($"⏸️ Paused: {(isPaused ? "✅" : "❌")}");
            GUILayout.Label($"🏃 Headset Off (Chase): {(isHeadsetOff ? "✅" : "❌")}");
            GUILayout.Label($"🟢 Stop (Verde fixo): {(isStopped ? "✅" : "❌")}");
            GUILayout.Label($"🌈 Perdeu Conexão (Rainbow): {(isSignalLost ? "✅" : "❌")}");
            GUILayout.Space(10);
            
            GUILayout.Label($"📊 Progresso: {progress:F1}%");
            GUILayout.Space(10);
            
            GUILayout.Label("🎮 Controles:", GUI.skin.box);
            GUILayout.Label($"• Space - Play (Progressão azul/vermelho)");
            GUILayout.Label($"• P - Pause");
            GUILayout.Label($"• S - Stop (Verde fixo - pronto)");
            GUILayout.Label($"• R - Pronto (Verde fixo)");
            GUILayout.Label($"• H - Headset Off (Chase)");
            GUILayout.Label($"• L - Perdeu Conexão (Rainbow)");
            GUILayout.Label($"• ↑/↓ - Progresso");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🧪 Testar Todos os Comandos"))
            {
                TestAllCommands();
            }
            
            GUILayout.EndArea();
        }
        
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
                SendHeadsetOffCommand();
            }
            else
            {
                SendReadyCommand();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SendHeadsetOffCommand();
            }
            else
            {
                SendReadyCommand();
            }
        }
        
        #endregion
    }
}
