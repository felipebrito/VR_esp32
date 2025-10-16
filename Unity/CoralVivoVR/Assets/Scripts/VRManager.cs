using UnityEngine;
using UnityEngine.Video;
using System.IO;
using CoralVivoVR.ESP32;

/// <summary>
/// Gerenciador simplificado para modo desktop - apenas o essencial
/// </summary>
public class VRManager : MonoBehaviour
{
    [Header("Referências Essenciais")]
    public Camera mainCamera;
    public Transform videoSphere;
    public VideoPlayer videoPlayer;

    [Header("Configuração")]
    public bool useDesktopMode = true;
    public string videoFileName = "Pierre_Final.mp4";
    
    [Header("Player Configuration")]
    [Range(1, 2)]
    public int playerId = 1; // 1 = Player 1, 2 = Player 2

    [Header("ESP32 Integration")]
    public ESP32WebSocketClient esp32Client;

    [Header("Debug")]
    public bool isPlaying = false;
    public bool isConnected = false;
    public string connectionStatus = "Desconectado";

    private Material videoMaterial;
    private int lastLoggedPercent = -1; // Para controle de log de progresso
    private float lastProgressUpdateTime = 0f; // Para controle de frequência de envio LED

    private void Start()
    {
        ConfigureMode();
        InitializeComponents();
        InitializeESP32();
        LoadVideoForDesktop();
    }

    /// <summary>
    /// Configurar modo Desktop vs VR
    /// </summary>
    private void ConfigureMode()
    {
        if (useDesktopMode)
        {
            Debug.Log("🖥️ MODO DESKTOP ATIVADO - Teste local");
            
            // Configurações para desktop
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0, 0, 5);
                mainCamera.transform.LookAt(Vector3.zero);
            }
            
            // Configurar controles de mouse para desktop
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Desabilitar EventSystem para evitar conflitos
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null)
            {
                eventSystem.enabled = false;
                Debug.Log("🖥️ EventSystem desabilitado no modo desktop");
            }
        }
        else
        {
            Debug.Log("🥽 MODO VR ATIVADO - Quest 3S");
            
            // Configurações para VR
            if (mainCamera != null)
            {
                mainCamera.transform.position = Vector3.zero;
            }
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Inicializar componentes essenciais
    /// </summary>
    private void InitializeComponents()
    {
        // Encontrar referências necessárias
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (videoPlayer == null)
            videoPlayer = GetComponentInChildren<VideoPlayer>();

        if (videoSphere == null && videoPlayer != null)
            videoSphere = videoPlayer.transform;

        // Configurar VideoPlayer
        if (videoPlayer != null && videoSphere != null)
        {
            videoPlayer.started += OnVideoStarted;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            
            // Configurar renderização
            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
            
            // Configurar material do vídeo
            var videoRenderer = videoSphere.GetComponent<MeshRenderer>();
            if (videoRenderer != null)
            {
                videoMaterial = new Material(Shader.Find("Custom/Video360"));
                if (videoMaterial != null)
                {
                    videoRenderer.material = videoMaterial;
                    videoPlayer.targetMaterialRenderer = videoRenderer;
                    videoPlayer.targetMaterialProperty = "_MainTex";
                    Debug.Log("Material do vídeo configurado");
                }
                else
                {
                    Debug.LogError("Shader Custom/Video360 não encontrado!");
                }
            }
            
            Debug.Log("VideoPlayer configurado com sucesso");
                }
                else
                {
            Debug.LogError("VideoPlayer ou VideoSphere não encontrados!");
        }
    }

    /// <summary>
    /// Inicializar ESP32 para testes
    /// </summary>
    private void InitializeESP32()
    {
        // Encontrar ESP32Client se não estiver atribuído
        if (esp32Client == null)
        {
            esp32Client = FindObjectOfType<ESP32WebSocketClient>();
        }
        
        // Apenas se inscrever nos eventos ESP32 (o ESP32Manager já cuida da conexão)
        ESP32WebSocketClient.OnConnected += OnESP32Connected;
        ESP32WebSocketClient.OnDisconnected += OnESP32Disconnected;
        ESP32WebSocketClient.OnMessageReceived += OnESP32MessageReceived;
        ESP32WebSocketClient.OnPlayCommand += OnPlayCommand;
        ESP32WebSocketClient.OnPauseCommand += OnPauseCommand;
        ESP32WebSocketClient.OnStopCommand += OnStopCommand;
        
        Debug.Log("🔌 ESP32 eventos configurados - ESP32Manager cuida da conexão");
    }

    /// <summary>
    /// Carregar vídeo para modo desktop
    /// </summary>
    private void LoadVideoForDesktop()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer não encontrado!");
            return;
        }
        
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
        
        Debug.Log($"🖥️ Carregando vídeo: {videoPath}");
        
        if (System.IO.File.Exists(videoPath))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoPath;
            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            videoPlayer.targetMaterialRenderer = videoSphere.GetComponent<MeshRenderer>();
            videoPlayer.targetMaterialProperty = "_MainTex";
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
            
            Debug.Log($"✅ Vídeo configurado: {videoPath}");
            
            // Preparar o vídeo
            videoPlayer.Prepare();
        }
        else
        {
            Debug.LogError($"❌ Vídeo não encontrado: {videoPath}");
        }
    }

    /// <summary>
    /// Evento quando vídeo é preparado com sucesso
    /// </summary>
    private void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("🎬 Vídeo preparado com sucesso!");
        Debug.Log($"📁 URL: {vp.url}");
        Debug.Log($"📏 Dimensões: {vp.width}x{vp.height}");
        Debug.Log($"⏱️ Duração: {vp.length:F2}s");
        
        // Não reproduzir automaticamente - aguardar comando do ESP32
        Debug.Log("🖥️ Vídeo pronto - aguardando comando do ESP32");
    }

    /// <summary>
    /// Evento quando vídeo inicia
    /// </summary>
    private void OnVideoStarted(VideoPlayer vp)
    {
        isPlaying = true;
        Debug.Log("🎬 Vídeo iniciado");
    }

    private void Update()
    {
        // Atualizar estado de reprodução
        if (videoPlayer != null)
        {
            isPlaying = videoPlayer.isPlaying;
            
            // Enviar progresso LED para ESP32
            if (isPlaying && esp32Client != null && esp32Client.IsConnected)
            {
                SendVideoProgressToESP32();
            }
        }

        // Controles de teclado para teste desktop
        if (useDesktopMode)
        {
            HandleDesktopInput();
            HandleESP32TestInput(); // Adicionar controles de teste ESP32
        }
    }

    /// <summary>
    /// Controles de teclado para teste desktop
    /// </summary>
    private void HandleDesktopInput()
    {
        // Verificar se Input System está ativo
        #if ENABLE_INPUT_SYSTEM
        // Usar Input System (novo)
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        
        if (keyboard != null)
        {
            // Controles de vídeo
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                if (isPlaying)
                    PauseVideo();
                else
                    PlayVideo();
                Debug.Log($"🖥️ Desktop: {(isPlaying ? "Play" : "Pause")}");
            }
            
            if (keyboard.sKey.wasPressedThisFrame)
            {
                StopVideo();
                Debug.Log("🖥️ Desktop: Stop");
            }
            
            // Reset câmera
            if (keyboard.rKey.wasPressedThisFrame)
            {
                if (mainCamera != null)
                {
                    mainCamera.transform.position = new Vector3(0, 0, 5);
                    mainCamera.transform.LookAt(Vector3.zero);
                    Debug.Log("🖥️ Desktop: Câmera resetada");
                }
            }
            
            // Toggle modo
            if (keyboard.vKey.wasPressedThisFrame)
            {
                useDesktopMode = !useDesktopMode;
                ConfigureMode();
                Debug.Log($"🖥️ Desktop: Modo alterado para {(useDesktopMode ? "Desktop" : "VR")}");
            }
        }
        
        // Controles de rotação da câmera
        if (mouse != null && mouse.leftButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            float mouseX = delta.x * 0.1f;
            float mouseY = delta.y * 0.1f;
            
            if (mainCamera != null)
            {
                mainCamera.transform.RotateAround(Vector3.zero, Vector3.up, mouseX);
                mainCamera.transform.RotateAround(Vector3.zero, mainCamera.transform.right, -mouseY);
            }
        }
        
        #else
        // Usar Input Legacy (antigo)
        // Controles de vídeo
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isPlaying)
                PauseVideo();
            else
                PlayVideo();
            Debug.Log($"🖥️ Desktop: {(isPlaying ? "Play" : "Pause")}");
        }
        
        if (Input.GetKeyDown(KeyCode.S))
        {
            StopVideo();
            Debug.Log("🖥️ Desktop: Stop");
        }
        
        // Controles de rotação da câmera
        if (Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            if (mainCamera != null)
            {
                mainCamera.transform.RotateAround(Vector3.zero, Vector3.up, mouseX * 2f);
                mainCamera.transform.RotateAround(Vector3.zero, mainCamera.transform.right, -mouseY * 2f);
            }
        }
        
        // Reset câmera
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0, 0, 5);
                mainCamera.transform.LookAt(Vector3.zero);
                Debug.Log("🖥️ Desktop: Câmera resetada");
            }
        }
        
        // Toggle modo
        if (Input.GetKeyDown(KeyCode.V))
        {
            useDesktopMode = !useDesktopMode;
            ConfigureMode();
            Debug.Log($"🖥️ Desktop: Modo alterado para {(useDesktopMode ? "Desktop" : "VR")}");
        }
        #endif
    }

    /// <summary>
    /// Controles de teste ESP32 (simular botões físicos)
    /// </summary>
    private void HandleESP32TestInput()
    {
        #if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        
        if (keyboard != null)
        {
            // Simular Botão 1 ESP32 (Play/Pause) - Player 1
            if (keyboard.pKey.wasPressedThisFrame)
            {
                Debug.Log("🎮 Simulando Botão 1 ESP32 (Play/Pause) - Player 1");
                OnPlayCommand();
            }
            
            // Simular Botão 2 ESP32 (Stop) - Player 2
            if (keyboard.oKey.wasPressedThisFrame)
            {
                Debug.Log("🎮 Simulando Botão 2 ESP32 (Stop) - Player 2");
                OnStopCommand();
            }
            
            // Teste específico Player 1
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                Debug.Log("🧪 Teste Player 1 - Play");
                if (esp32Client != null)
                    esp32Client.TestESP32Message("play", 1);
            }
            
            // Teste específico Player 2
            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("🧪 Teste Player 2 - Play");
                if (esp32Client != null)
                    esp32Client.TestESP32Message("play", 2);
            }
        }
        #else
        // Usar Input Legacy
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("🎮 Simulando Botão 1 ESP32 (Play/Pause) - Player 1");
            OnPlayCommand();
        }
        
        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log("🎮 Simulando Botão 2 ESP32 (Stop) - Player 2");
            OnStopCommand();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("🧪 Teste Player 1 - Play");
            if (esp32Client != null)
                esp32Client.TestESP32Message("play", 1);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("🧪 Teste Player 2 - Play");
            if (esp32Client != null)
                esp32Client.TestESP32Message("play", 2);
        }
        #endif
    }

    /// <summary>
    /// Reproduzir vídeo
    /// </summary>
    public void PlayVideo()
    {
        if (videoPlayer != null && !videoPlayer.isPlaying)
        {
            videoPlayer.Play();
            isPlaying = true;
            Debug.Log("Vídeo iniciado");

            // Enviar comando LED inicial para mostrar estado reproduzindo
            if (esp32Client != null && esp32Client.IsConnected)
            {
                esp32Client.SendLEDCommand($"led{playerId}:0");
                Debug.Log($"🟢 LED progresso iniciado (Player {playerId})");
            }
        }
    }

    /// <summary>
    /// Pausar vídeo
    /// </summary>
    public void PauseVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            isPlaying = false;
            Debug.Log("Vídeo pausado");
            
            // Não enviar comando LED - o progresso para automaticamente
            Debug.Log("🟡 Vídeo pausado - progresso LED para automaticamente");
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
            videoPlayer.time = 0; // Reset to beginning
            isPlaying = false;
            Debug.Log("Vídeo parado e resetado para início");
            
            // Enviar comando LED para desligar completamente
            if (esp32Client != null && esp32Client.IsConnected)
            {
                esp32Client.SendLEDCommand($"off{playerId}");
                Debug.Log($"🔴 LED desligado enviado para ESP32 (Player {playerId})");
            }
            
            // Preparar vídeo para reiniciar do início
            videoPlayer.Prepare();
            Debug.Log("🔄 Vídeo preparado para reiniciar do início");
        }
    }

    /// <summary>
    /// Enviar progresso do vídeo para ESP32 (LEDs)
    /// </summary>
    private void SendVideoProgressToESP32()
    {
        if (videoPlayer == null || esp32Client == null) return;
        
        // Só enviar progresso se o vídeo estiver realmente reproduzindo
        if (!videoPlayer.isPlaying) return;
        
        // Só enviar se ESP32 estiver conectado
        if (!esp32Client.IsConnected) return;
        
        // Enviar apenas a cada 3 segundos para evitar spam
        if (Time.time - lastProgressUpdateTime < 3.0f) return;
        
        // Calcular progresso (0.0 a 1.0)
        float progress = (float)videoPlayer.time / (float)videoPlayer.length;
        progress = Mathf.Clamp01(progress);
        
        // Converter para porcentagem (0-100)
        int progressPercent = Mathf.RoundToInt(progress * 100f);
        
        // Enviar comando LED para ESP32 via WebSocket (usar playerId correto)
        string ledCommand = $"led{playerId}:{progressPercent}";
        esp32Client.SendLEDCommand(ledCommand);
        
        // Atualizar timestamp
        lastProgressUpdateTime = Time.time;
        
        // Log apenas a cada 10% para não spam
        if (progressPercent % 10 == 0 && progressPercent != lastLoggedPercent)
        {
            Debug.Log($"📊 Progresso vídeo: {progressPercent}%");
            lastLoggedPercent = progressPercent;
        }
    }

    // ESP32 Event Handlers
    private void OnESP32Connected()
    {
            isConnected = true;
        connectionStatus = "Conectado ao ESP32";
        Debug.Log("🔌 ESP32 conectado!");
    }
    
    private void OnESP32Disconnected()
    {
        isConnected = false;
        connectionStatus = "Desconectado do ESP32";
        Debug.Log("🔌 ESP32 desconectado!");
    }
    
    private void OnESP32MessageReceived(string message)
    {
        Debug.Log($"📨 Mensagem ESP32: {message}");
    }
    
    private void OnPlayCommand()
    {
        Debug.Log("▶️ Comando PLAY recebido do ESP32");
        PlayVideo();
    }
    
    private void OnPauseCommand()
    {
        Debug.Log("⏸️ Comando PAUSE recebido do ESP32");
        PauseVideo();
    }
    
    private void OnStopCommand()
    {
        Debug.Log("⏹️ Comando STOP recebido do ESP32");
        StopVideo();
    }

    private void OnDestroy()
    {
        // Limpar eventos
        if (videoPlayer != null)
        {
            videoPlayer.started -= OnVideoStarted;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
        
        // Limpar eventos ESP32
        if (esp32Client != null)
        {
            ESP32WebSocketClient.OnConnected -= OnESP32Connected;
            ESP32WebSocketClient.OnDisconnected -= OnESP32Disconnected;
            ESP32WebSocketClient.OnMessageReceived -= OnESP32MessageReceived;
            ESP32WebSocketClient.OnPlayCommand -= OnPlayCommand;
            ESP32WebSocketClient.OnPauseCommand -= OnPauseCommand;
            ESP32WebSocketClient.OnStopCommand -= OnStopCommand;
        }
    }
} 