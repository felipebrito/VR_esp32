using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NativeWebSocket;

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
        
        [Header("📊 Progresso")]
        [Range(0, 100)]
        [SerializeField] private float progress = 0f;
        
        [Header("🔗 Conexão")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private WebSocket webSocket;
        
        [Header("🎯 Controles de Teste")]
        [SerializeField] private KeyCode playKey = KeyCode.Space;
        [SerializeField] private KeyCode pauseKey = KeyCode.P;
        [SerializeField] private KeyCode stopKey = KeyCode.S;
        [SerializeField] private KeyCode readyKey = KeyCode.R;
        [SerializeField] private KeyCode headsetOffKey = KeyCode.H;
        [SerializeField] private KeyCode signalLostKey = KeyCode.L;
        
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
                
                webSocket = new WebSocket(url);
                
                webSocket.OnOpen += () =>
                {
                    Debug.Log("✅ Conectado ao ESP32!");
                    isConnected = true;
                    SendReadyCommand();
                };
                
                webSocket.OnMessage += (bytes) =>
                {
                    string message = System.Text.Encoding.UTF8.GetString(bytes);
                    Debug.Log($"📨 ESP32: {message}");
                };
                
                webSocket.OnError += (e) =>
                {
                    Debug.LogError($"❌ Erro WebSocket: {e}");
                    isConnected = false;
                };
                
                webSocket.OnClose += (e) =>
                {
                    Debug.Log($"🔌 Desconectado do ESP32: {e}");
                    isConnected = false;
                };
                
                await webSocket.Connect();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao conectar: {e.Message}");
            }
        }
        
        private async void DisconnectFromESP32()
        {
            if (webSocket != null)
            {
                await webSocket.Close();
                webSocket = null;
                isConnected = false;
                Debug.Log("🔌 Desconectado do ESP32");
            }
        }
        
        #endregion
        
        #region 🎮 Controles de Input
        
        private void HandleInput()
        {
            if (!isConnected) return;
            
            // Controles principais
            if (Input.GetKeyDown(playKey))
            {
                SendPlayCommand();
            }
            
            if (Input.GetKeyDown(pauseKey))
            {
                SendPauseCommand();
            }
            
            if (Input.GetKeyDown(stopKey))
            {
                SendStopCommand();
            }
            
            if (Input.GetKeyDown(readyKey))
            {
                SendReadyCommand();
            }
            
            if (Input.GetKeyDown(headsetOffKey))
            {
                SendHeadsetOffCommand();
            }
            
            if (Input.GetKeyDown(signalLostKey))
            {
                SendSignalLostCommand();
            }
            
            // Controles de progresso
            if (Input.GetKey(KeyCode.UpArrow))
            {
                progress = Mathf.Min(100f, progress + Time.deltaTime * 50f);
                SendProgressCommand(progress);
            }
            
            if (Input.GetKey(KeyCode.DownArrow))
            {
                progress = Mathf.Max(0f, progress - Time.deltaTime * 50f);
                SendProgressCommand(progress);
            }
        }
        
        #endregion
        
        #region 📡 Comandos ESP32
        
        private async void SendCommand(string command)
        {
            if (webSocket != null && isConnected)
            {
                try
                {
                    await webSocket.SendText(command);
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
            Debug.Log($"🟢 Player {playerID} - READY (Verde piscando)");
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
            Debug.Log($"🔵 Player {playerID} - PLAYING (Azul/Vermelho progressivo automático)");
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
            // Stop = Signal Lost (Chase effect)
            string command = $"signal_lost{playerID}";
            if (playerID == 2)
            {
                command = $"signal_lost{playerID}"; // Player 2 = Chase por padrão
            }
            SendCommand(command);
            isReady = false;
            isPlaying = false;
            isPaused = false;
            isHeadsetOff = false;
            isSignalLost = true;
            Debug.Log($"🏃 Player {playerID} - SIGNAL LOST (Chase effect)");
        }
        
        private void SendHeadsetOffCommand()
        {
            string command = $"off{playerID}";
            SendCommand(command);
            isReady = false;
            isPlaying = false;
            isPaused = false;
            isHeadsetOff = true;
            isSignalLost = false;
            Debug.Log($"🔴 Player {playerID} - HEADSET OFF (Azul/Vermelho escuro progressivo)");
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
            Debug.Log($"🌈 Player {playerID} - SIGNAL LOST (Rainbow effect)");
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
            GUILayout.Label($"🟢 Ready: {(isReady ? "✅" : "❌")}");
            GUILayout.Label($"🔵 Playing: {(isPlaying ? "✅" : "❌")}");
            GUILayout.Label($"⏸️ Paused: {(isPaused ? "✅" : "❌")}");
            GUILayout.Label($"🔴 Headset Off: {(isHeadsetOff ? "✅" : "❌")}");
            GUILayout.Label($"🌈 Signal Lost: {(isSignalLost ? "✅" : "❌")}");
            GUILayout.Space(10);
            
            GUILayout.Label($"📊 Progresso: {progress:F1}%");
            GUILayout.Space(10);
            
            GUILayout.Label("🎮 Controles:", GUI.skin.box);
            GUILayout.Label($"• {playKey} - Play");
            GUILayout.Label($"• {pauseKey} - Pause");
            GUILayout.Label($"• {stopKey} - Stop (Chase)");
            GUILayout.Label($"• {readyKey} - Ready");
            GUILayout.Label($"• {headsetOffKey} - Headset Off");
            GUILayout.Label($"• {signalLostKey} - Signal Lost (Rainbow)");
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
