using UnityEngine;
using UnityEngine.Video;
using CoralVivoVR.ESP32;
using System.Collections;
using System.Collections.Generic;

namespace CoralVivoVR.VR
{
    /// <summary>
    /// Gerenciador principal do CoralVivoVR
    /// Integra ESP32, vídeo 360° e controles VR
    /// </summary>
    public class CoralVivoVRManager : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private ESP32WebSocketClient esp32Client;
        [SerializeField] private VideoPlayer videoPlayer;
        [Header("VR Components")]
        [SerializeField] private Camera vrCamera;
        [SerializeField] private Transform xrOrigin;
        
        [Header("Settings")]
        [SerializeField] private string videoPath = "Pierre_Final.mp4";
        [SerializeField] private bool autoLoadVideo = true;
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("LED Simulation")]
        [SerializeField] private bool simulateLEDs = true;
        [SerializeField] private float ledUpdateInterval = 0.1f;
        
        [Header("Android Permissions")]
        [SerializeField] private bool enablePermissionDebug = true;
        
        // State
        private bool isInitialized = false;
        private float lastLEDUpdate = 0f;
        
        // Events
        public static event System.Action OnSystemReady;
        public static event System.Action<string> OnSystemError;
        
        void Start()
        {
            InitializeSystem();
        }
        
        /// <summary>
        /// Inicializar sistema completo
        /// </summary>
        private void InitializeSystem()
        {
            LogDebug("Inicializando CoralVivoVR...");
            
            // Verificar componentes
            if (!ValidateComponents())
            {
                OnSystemError?.Invoke("Componentes essenciais não encontrados");
                return;
            }
            
            // Configurar eventos
            SetupEventListeners();
            
            // Inicializar componentes
            InitializeComponents();
            
            isInitialized = true;
            LogDebug("CoralVivoVR inicializado com sucesso!");
            OnSystemReady?.Invoke();
        }
        
        /// <summary>
        /// Validar componentes essenciais
        /// </summary>
        private bool ValidateComponents()
        {
            bool isValid = true;
            
            if (esp32Client == null)
            {
                esp32Client = FindObjectOfType<ESP32WebSocketClient>();
                if (esp32Client == null)
                {
                    LogDebug("ESP32WebSocketClient não encontrado");
                    isValid = false;
                }
            }
            
            if (videoPlayer == null)
            {
                videoPlayer = FindObjectOfType<VideoPlayer>();
                if (videoPlayer == null)
                {
                    LogDebug("VideoPlayer não encontrado");
                    isValid = false;
                }
            }
            
            // Configurar VR Components
            InitializeVRComponents();
            
            return isValid;
        }
        
        /// <summary>
        /// Inicializar componentes VR
        /// </summary>
        private void InitializeVRComponents()
        {
            // Configurar VR Camera
            if (vrCamera == null)
            {
                vrCamera = Camera.main;
                if (vrCamera == null)
                {
                    vrCamera = FindObjectOfType<Camera>();
                }
            }
            
            // Configurar XR Origin
            if (xrOrigin == null)
            {
                GameObject xrOriginObj = GameObject.Find("XR Origin");
                if (xrOriginObj != null)
                {
                    xrOrigin = xrOriginObj.transform;
                }
            }
            
            // Verificar se XR está ativo
            if (UnityEngine.XR.XRSettings.enabled)
            {
                LogDebug("XR está ativo - modo VR");
            }
            else
            {
                LogDebug("⚠️ XR não está ativo - verificar configurações");
            }
        }
        
        /// <summary>
        /// Configurar listeners de eventos
        /// </summary>
        private void SetupEventListeners()
        {
            // ESP32 Events
            ESP32WebSocketClient.OnConnected += OnESP32Connected;
            ESP32WebSocketClient.OnDisconnected += OnESP32Disconnected;
            ESP32WebSocketClient.OnPlayCommand += OnPlayCommand;
            ESP32WebSocketClient.OnPauseCommand += OnPauseCommand;
            ESP32WebSocketClient.OnStopCommand += OnStopCommand;
            ESP32WebSocketClient.OnError += OnESP32Error;
            
            // Video Events
            videoPlayer.prepareCompleted += OnVideoLoaded;
            videoPlayer.started += OnVideoStarted;
            videoPlayer.loopPointReached += OnVideoStopped;
            
            LogDebug("Event listeners configurados");
        }
        
        /// <summary>
        /// Inicializar componentes
        /// </summary>
        private void InitializeComponents()
        {
            // Carregar vídeo diretamente (sem permissões complexas)
            if (autoLoadVideo)
            {
                LoadVideo();
            }
            
            LogDebug("Componentes inicializados");
        }
        
        
        // ESP32 Event Handlers
        private void OnESP32Connected()
        {
            LogDebug("ESP32 conectado");
            
            // Enviar status ready
            esp32Client.SendPlayerStatus("ready");
        }
        
        private void OnESP32Disconnected()
        {
            LogDebug("ESP32 desconectado");
        }
        
        private void OnESP32Error(string error)
        {
            LogDebug($"Erro ESP32: {error}");
            OnSystemError?.Invoke($"ESP32 Error: {error}");
        }
        
        private void OnPlayCommand()
        {
            LogDebug("Comando PLAY recebido do ESP32 (Player 1)");
            videoPlayer.Play();
        }
        
        private void OnPauseCommand()
        {
            LogDebug("Comando PAUSE recebido do ESP32 (Player 1)");
            videoPlayer.Pause();
        }
        
        private void OnStopCommand()
        {
            LogDebug("Comando STOP recebido do ESP32 (Player 1)");
            videoPlayer.Stop();
        }
        
        // Video Event Handlers
        private void OnVideoLoaded(VideoPlayer vp)
        {
            LogDebug("Vídeo carregado com sucesso");
            
            // Enviar status para ESP32
            if (esp32Client.IsConnected)
            {
                esp32Client.SendPlayerStatus("loaded");
            }
            
            // REPRODUZIR AUTOMATICAMENTE PARA TESTE
            LogDebug("🎬 Iniciando reprodução automática para teste...");
            videoPlayer.Play();
        }
        
        private void OnVideoStarted(VideoPlayer vp)
        {
            LogDebug("Vídeo iniciado");
            
            // Enviar status para ESP32
            if (esp32Client.IsConnected)
            {
                esp32Client.SendPlayerStatus("playing");
            }
        }
        
        private void OnVideoStopped(VideoPlayer vp)
        {
            LogDebug("Vídeo parado");
            
            // Enviar status para ESP32
            if (esp32Client.IsConnected)
            {
                esp32Client.SendPlayerStatus("stopped");
            }
        }
        
        void Update()
        {
            if (!isInitialized) return;
            
            // Detectar pause do vídeo
            if (videoPlayer != null && videoPlayer.isPrepared)
            {
                bool isPlaying = videoPlayer.isPlaying;
                
                // Detectar mudança de estado (pause)
                if (!isPlaying && videoPlayer.time > 0)
                {
                    // Vídeo pausado
                    if (esp32Client.IsConnected)
                    {
                        esp32Client.SendPlayerStatus("paused");
                    }
                }
                
                // Atualizar progresso para LEDs
                if (isPlaying && simulateLEDs && esp32Client.IsConnected)
                {
                    float progress = (float)(videoPlayer.time / videoPlayer.length);
                    esp32Client.SendLEDCommand(1, progress);
                }
            }
            
            // Atualizar LEDs periodicamente
            if (simulateLEDs && Time.time - lastLEDUpdate > ledUpdateInterval)
            {
                UpdateLEDs();
                lastLEDUpdate = Time.time;
            }
        }
        
        /// <summary>
        /// Atualizar LEDs baseado no progresso do vídeo
        /// </summary>
        private void UpdateLEDs()
        {
            if (esp32Client.IsConnected && videoPlayer != null && videoPlayer.isPrepared)
            {
                float progress = (float)(videoPlayer.time / videoPlayer.length);
                esp32Client.SendLEDCommand(1, progress);
            }
        }
        
        /// <summary>
        /// Conectar ao ESP32
        /// </summary>
        public void ConnectToESP32()
        {
            if (esp32Client != null)
            {
                esp32Client.Connect();
            }
        }
        
        /// <summary>
        /// Desconectar do ESP32
        /// </summary>
        public void DisconnectFromESP32()
        {
            if (esp32Client != null)
            {
                esp32Client.Disconnect();
            }
        }
        
        /// <summary>
        /// Carregar vídeo - Implementação simplificada baseada no socket-client
        /// </summary>
        public void LoadVideo(string path = null)
        {
            if (videoPlayer != null)
            {
                if (string.IsNullOrEmpty(path))
                {
                    path = videoPath;
                }
                
                LogDebug($"Carregando vídeo: {path}");
                
                // Usar apenas StreamingAssets (como socket-client)
                string fullVideoPath = System.IO.Path.Combine(Application.streamingAssetsPath, path);
                
                if (!System.IO.File.Exists(fullVideoPath))
                {
                    LogDebug($"❌ Vídeo não encontrado: {fullVideoPath}");
                    UseFallbackVideo();
                    return;
                }
                
                LogDebug($"✅ Vídeo encontrado: {fullVideoPath}");
                ConfigureVideoPlayer(fullVideoPath);
            }
        }
        
        /// <summary>
        /// Reproduzir vídeo
        /// </summary>
        public void PlayVideo()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Play();
            }
        }
        
        /// <summary>
        /// Pausar vídeo
        /// </summary>
        public void PauseVideo()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Pause();
            }
        }
        
        /// <summary>
        /// Parar vídeo
        /// </summary>
        public void StopVideo()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
        }
        
        /// <summary>
        /// Resetar rotação da câmera
        /// </summary>
        public void ResetCamera()
        {
            if (vrCamera != null)
            {
                vrCamera.transform.rotation = Quaternion.identity;
                LogDebug("Rotação da câmera resetada");
            }
        }
        
        /// <summary>
        /// Configurar VideoPlayer com configurações otimizadas
        /// </summary>
        private void ConfigureVideoPlayer(string url)
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            videoPlayer.targetMaterialRenderer = GetComponent<Renderer>();
            videoPlayer.skipOnDrop = true;
            videoPlayer.playbackSpeed = 1.0f;
            
            LogDebug($"🎬 Vídeo configurado: {url}");
            LogDebug($"🎬 URL do VideoPlayer: {videoPlayer.url}");
            
            videoPlayer.Prepare();
        }
        
        /// <summary>
        /// Usar vídeo de fallback quando não conseguir carregar o principal
        /// </summary>
        private void UseFallbackVideo()
        {
            LogDebug("🎬 Usando fallback - criando esfera colorida de teste");
            
            // Criar uma esfera colorida como fallback visual
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Material fallbackMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                fallbackMaterial.color = Color.blue;
                renderer.material = fallbackMaterial;
                
                LogDebug("✅ Fallback visual aplicado - esfera azul");
            }
        }
        
        
        /// <summary>
        /// Log de debug
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[CoralVivoVRManager] {message}");
            }
        }
        
        // Properties
        public bool IsInitialized => isInitialized;
        public bool IsESP32Connected => esp32Client != null && esp32Client.IsConnected;
        public bool IsVideoLoaded => videoPlayer != null && videoPlayer.isPrepared;
        public bool IsVideoPlaying => videoPlayer != null && videoPlayer.isPlaying;
        public float VideoProgress => videoPlayer != null && videoPlayer.isPrepared ? (float)(videoPlayer.time / videoPlayer.length) : 0f;
        
        void OnDestroy()
        {
            // Remover event listeners
            ESP32WebSocketClient.OnConnected -= OnESP32Connected;
            ESP32WebSocketClient.OnDisconnected -= OnESP32Disconnected;
            ESP32WebSocketClient.OnPlayCommand -= OnPlayCommand;
            ESP32WebSocketClient.OnPauseCommand -= OnPauseCommand;
            ESP32WebSocketClient.OnStopCommand -= OnStopCommand;
            ESP32WebSocketClient.OnError -= OnESP32Error;
            
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnVideoLoaded;
                videoPlayer.started -= OnVideoStarted;
                videoPlayer.loopPointReached -= OnVideoStopped;
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                LogDebug("App pausado");
                PauseVideo();
            }
            else
            {
                LogDebug("App retomado");
            }
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                LogDebug("App perdeu foco");
                PauseVideo();
            }
            else
            {
                LogDebug("App ganhou foco");
            }
        }
    }
}
