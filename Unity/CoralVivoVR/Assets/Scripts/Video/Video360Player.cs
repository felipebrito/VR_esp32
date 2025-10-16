using System.Collections;
using UnityEngine;
using UnityEngine.Video;

namespace CoralVivoVR.Video
{
    /// <summary>
    /// Player de vídeo 360° para Unity VR
    /// Implementa esfera customizada baseada na solução funcionando
    /// </summary>
    public class Video360Player : MonoBehaviour
    {
        [Header("Video Settings")]
        [SerializeField] private string videoPath = "Downloads/Pierre_Final.mov";
        [SerializeField] private float sphereRadius = 10f;
        [SerializeField] private int sphereSegments = 64;
        
        [Header("Rotation Control")]
        [SerializeField] private bool enableRotationControl = true;
        [SerializeField] private float maxRotationAngle = 75f;
        [SerializeField] private float resetSpeed = 2f;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        // Components
        private VideoPlayer videoPlayer;
        private MeshRenderer sphereRenderer;
        private Material videoMaterial;
        private Camera mainCamera;
        
        // State
        private bool isLoaded = false;
        private bool isPlaying = false;
        private bool isRotationLocked = false;
        private Vector3 initialCameraRotation;
        
        // Events
        public static event System.Action OnVideoLoaded;
        public static event System.Action OnVideoStarted;
        public static event System.Action OnVideoPaused;
        public static event System.Action OnVideoStopped;
        public static event System.Action<float> OnProgressChanged;
        
        void Start()
        {
            InitializeComponents();
            CreateSphere();
            SetupVideoPlayer();
            SetupCamera();
            
            LogDebug("Video360Player inicializado");
        }
        
        /// <summary>
        /// Inicializar componentes
        /// </summary>
        private void InitializeComponents()
        {
            // VideoPlayer component
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }
            
            // MeshRenderer
            sphereRenderer = GetComponent<MeshRenderer>();
            if (sphereRenderer == null)
            {
                sphereRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            // Main Camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }
        
        /// <summary>
        /// Criar esfera customizada para vídeo 360°
        /// Baseado na solução funcionando do projeto anterior
        /// </summary>
        private void CreateSphere()
        {
            // Criar mesh customizada
            Mesh sphereMesh = CreateSphereMesh();
            
            // Aplicar mesh
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            meshFilter.mesh = sphereMesh;
            
            // Criar material para vídeo
            CreateVideoMaterial();
            
            LogDebug($"Esfera criada com raio {sphereRadius} e {sphereSegments} segmentos");
        }
        
        /// <summary>
        /// Criar mesh de esfera com UV mapping correto
        /// </summary>
        private Mesh CreateSphereMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Video360Sphere";
            
            // Calcular vértices
            int vertexCount = (sphereSegments + 1) * (sphereSegments + 1);
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[sphereSegments * sphereSegments * 6];
            
            int vertexIndex = 0;
            int triangleIndex = 0;
            
            for (int y = 0; y <= sphereSegments; y++)
            {
                for (int x = 0; x <= sphereSegments; x++)
                {
                    // Calcular posição esférica
                    float xAngle = (float)x / sphereSegments * Mathf.PI * 2;
                    float yAngle = (float)y / sphereSegments * Mathf.PI;
                    
                    Vector3 pos = new Vector3(
                        Mathf.Sin(yAngle) * Mathf.Cos(xAngle),
                        Mathf.Cos(yAngle),
                        Mathf.Sin(yAngle) * Mathf.Sin(xAngle)
                    ) * sphereRadius;
                    
                    // Inverter faces para olhar para dentro
                    vertices[vertexIndex] = -pos;
                    
                    // UV mapping correto para vídeo 360°
                    uvs[vertexIndex] = new Vector2(
                        (float)x / sphereSegments,
                        1.0f - (float)y / sphereSegments // Inverter Y
                    );
                    
                    // Criar triângulos
                    if (x < sphereSegments && y < sphereSegments)
                    {
                        int current = vertexIndex;
                        int next = current + sphereSegments + 1;
                        
                        triangles[triangleIndex] = current;
                        triangles[triangleIndex + 1] = next;
                        triangles[triangleIndex + 2] = current + 1;
                        
                        triangles[triangleIndex + 3] = current + 1;
                        triangles[triangleIndex + 4] = next;
                        triangles[triangleIndex + 5] = next + 1;
                        
                        triangleIndex += 6;
                    }
                    
                    vertexIndex++;
                }
            }
            
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        /// <summary>
        /// Criar material para vídeo
        /// </summary>
        private void CreateVideoMaterial()
        {
            // Usar shader padrão por enquanto
            // TODO: Implementar shader customizado para vídeo 360°
            videoMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            videoMaterial.name = "Video360Material";
            
            sphereRenderer.material = videoMaterial;
            
            LogDebug("Material de vídeo criado");
        }
        
        /// <summary>
        /// Configurar VideoPlayer
        /// </summary>
        private void SetupVideoPlayer()
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            videoPlayer.targetMaterialRenderer = sphereRenderer;
            videoPlayer.targetMaterialProperty = "_MainTex";
            
            // Eventos do vídeo
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.started += OnVideoStartedEvent;
            videoPlayer.loopPointReached += OnVideoLoopPointReached;
            
            LogDebug("VideoPlayer configurado");
        }
        
        /// <summary>
        /// Configurar câmera
        /// </summary>
        private void SetupCamera()
        {
            if (mainCamera != null)
            {
                // Posicionar câmera no centro da esfera
                mainCamera.transform.position = Vector3.zero;
                initialCameraRotation = mainCamera.transform.eulerAngles;
                
                LogDebug($"Câmera posicionada em {mainCamera.transform.position}");
            }
        }
        
        /// <summary>
        /// Carregar vídeo
        /// </summary>
        public void LoadVideo(string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = videoPath;
            }
            
            LogDebug($"Carregando vídeo: {path}");
            
            // Tentar diferentes caminhos
            string[] possiblePaths = {
                path,
                System.IO.Path.Combine(Application.persistentDataPath, path),
                System.IO.Path.Combine(Application.streamingAssetsPath, path),
                System.IO.Path.Combine(Application.dataPath, path)
            };
            
            foreach (string testPath in possiblePaths)
            {
                if (System.IO.File.Exists(testPath))
                {
                    videoPlayer.url = testPath;
                    videoPlayer.Prepare();
                    LogDebug($"Vídeo encontrado em: {testPath}");
                    return;
                }
            }
            
            LogDebug($"Vídeo não encontrado em nenhum caminho: {path}");
        }
        
        /// <summary>
        /// Reproduzir vídeo
        /// </summary>
        public void Play()
        {
            if (!isLoaded)
            {
                LogDebug("Vídeo não carregado");
                return;
            }
            
            videoPlayer.Play();
            LogDebug("▶️ Reprodução iniciada");
        }
        
        /// <summary>
        /// Pausar vídeo
        /// </summary>
        public void Pause()
        {
            if (!isLoaded)
            {
                LogDebug("Vídeo não carregado");
                return;
            }
            
            videoPlayer.Pause();
            LogDebug("⏸️ Reprodução pausada");
        }
        
        /// <summary>
        /// Parar vídeo
        /// </summary>
        public void Stop()
        {
            if (!isLoaded)
            {
                LogDebug("Vídeo não carregado");
                return;
            }
            
            videoPlayer.Stop();
            LogDebug("⏹️ Reprodução parada");
        }
        
        /// <summary>
        /// Alternar bloqueio de rotação
        /// </summary>
        public void ToggleRotationLock()
        {
            isRotationLocked = !isRotationLocked;
            LogDebug($"Bloqueio de rotação: {(isRotationLocked ? "Ativado" : "Desativado")}");
        }
        
        /// <summary>
        /// Resetar rotação da câmera
        /// </summary>
        public void ResetCameraRotation()
        {
            if (mainCamera != null)
            {
                StartCoroutine(ResetRotationCoroutine());
            }
        }
        
        /// <summary>
        /// Corrotina para resetar rotação suavemente
        /// </summary>
        private IEnumerator ResetRotationCoroutine()
        {
            Vector3 startRotation = mainCamera.transform.eulerAngles;
            float elapsed = 0f;
            
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * resetSpeed;
                mainCamera.transform.eulerAngles = Vector3.Lerp(startRotation, initialCameraRotation, elapsed);
                yield return null;
            }
            
            mainCamera.transform.eulerAngles = initialCameraRotation;
            LogDebug("Rotação da câmera resetada");
        }
        
        // Event handlers
        private void OnVideoPrepared(VideoPlayer vp)
        {
            isLoaded = true;
            LogDebug($"Vídeo carregado: {vp.length:F2}s");
            OnVideoLoaded?.Invoke();
        }
        
        private void OnVideoStartedEvent(VideoPlayer vp)
        {
            isPlaying = true;
            OnVideoStarted?.Invoke();
        }
        
        private void OnVideoLoopPointReached(VideoPlayer vp)
        {
            isPlaying = false;
            OnVideoStopped?.Invoke();
            LogDebug("Vídeo chegou ao fim");
        }
        
        void Update()
        {
            // Detectar mudanças de estado do vídeo
            if (isLoaded)
            {
                bool currentlyPlaying = videoPlayer.isPlaying;
                
                // Detectar mudança de estado
                if (currentlyPlaying != isPlaying)
                {
                    isPlaying = currentlyPlaying;
                    if (isPlaying)
                    {
                        OnVideoStarted?.Invoke();
                    }
                    else
                    {
                        OnVideoPaused?.Invoke();
                    }
                }
                
                // Atualizar progresso do vídeo
                if (isPlaying)
                {
                    float progress = (float)(videoPlayer.time / videoPlayer.length);
                    OnProgressChanged?.Invoke(progress);
                }
            }
            
            // Controle de rotação
            if (enableRotationControl && isRotationLocked && mainCamera != null)
            {
                LimitCameraRotation();
            }
        }
        
        /// <summary>
        /// Limitar rotação da câmera
        /// </summary>
        private void LimitCameraRotation()
        {
            Vector3 currentRotation = mainCamera.transform.eulerAngles;
            
            // Normalizar ângulos
            float x = currentRotation.x > 180 ? currentRotation.x - 360 : currentRotation.x;
            float y = currentRotation.y > 180 ? currentRotation.y - 360 : currentRotation.y;
            
            // Aplicar limites
            x = Mathf.Clamp(x, -maxRotationAngle, maxRotationAngle);
            y = Mathf.Clamp(y, -maxRotationAngle, maxRotationAngle);
            
            mainCamera.transform.eulerAngles = new Vector3(x, y, currentRotation.z);
        }
        
        /// <summary>
        /// Log de debug
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Video360Player] {message}");
            }
        }
        
        // Properties
        public bool IsLoaded => isLoaded;
        public bool IsPlaying => isPlaying;
        public bool IsRotationLocked => isRotationLocked;
        public float Progress => isLoaded ? (float)(videoPlayer.time / videoPlayer.length) : 0f;
        public double Duration => isLoaded ? videoPlayer.length : 0;
        
        void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.started -= OnVideoStartedEvent;
                videoPlayer.loopPointReached -= OnVideoLoopPointReached;
            }
        }
    }
}
