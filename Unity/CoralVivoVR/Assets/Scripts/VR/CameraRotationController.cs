using UnityEngine;

namespace CoralVivoVR.VR
{
    /// <summary>
    /// Controlador de rotação da câmera para VR
    /// Limita rotação e fornece controles de reset
    /// </summary>
    public class CameraRotationController : MonoBehaviour
    {
        [Header("Rotation Limits")]
        [SerializeField] private bool enableRotationLimits = true;
        [SerializeField] private float maxPitchAngle = 75f;
        [SerializeField] private float maxYawAngle = 180f;
        
        [Header("Reset Settings")]
        [SerializeField] private bool enableReset = true;
        [SerializeField] private KeyCode resetKey = KeyCode.R;
        [SerializeField] private float resetSpeed = 2f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool enableDebugLogs = true;
        
        // Components
        private Camera targetCamera;
        private Transform cameraTransform;
        
        // State
        private Vector3 initialRotation;
        private bool isResetting = false;
        private Coroutine resetCoroutine;
        
        // Input
        private Vector2 lastMousePosition;
        private bool isDragging = false;
        
        void Start()
        {
            InitializeCamera();
            StoreInitialRotation();
        }
        
        /// <summary>
        /// Inicializar câmera
        /// </summary>
        private void InitializeCamera()
        {
            targetCamera = GetComponent<Camera>();
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
            
            if (targetCamera != null)
            {
                cameraTransform = targetCamera.transform;
                LogDebug($"Câmera inicializada: {targetCamera.name}");
            }
            else
            {
                LogDebug("Nenhuma câmera encontrada");
            }
        }
        
        /// <summary>
        /// Armazenar rotação inicial
        /// </summary>
        private void StoreInitialRotation()
        {
            if (cameraTransform != null)
            {
                initialRotation = cameraTransform.eulerAngles;
                LogDebug($"Rotação inicial armazenada: {initialRotation}");
            }
        }
        
        void Update()
        {
            if (cameraTransform == null) return;
            
            HandleInput();
            ApplyRotationLimits();
            UpdateDebugInfo();
        }
        
        /// <summary>
        /// Processar input do usuário
        /// </summary>
        private void HandleInput()
        {
            // Reset com teclado
            if (enableReset && Input.GetKeyDown(resetKey))
            {
                ResetRotation();
            }
            
            // Controle de mouse/touch para rotação
            HandleRotationInput();
        }
        
        /// <summary>
        /// Processar input de rotação
        /// </summary>
        private void HandleRotationInput()
        {
            Vector2 currentMousePosition = Input.mousePosition;
            
            if (Input.GetMouseButtonDown(0))
            {
                lastMousePosition = currentMousePosition;
                isDragging = true;
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                Vector2 deltaPosition = currentMousePosition - lastMousePosition;
                
                // Aplicar rotação baseada no movimento do mouse
                float sensitivity = 0.5f;
                float yaw = deltaPosition.x * sensitivity;
                float pitch = -deltaPosition.y * sensitivity;
                
                RotateCamera(yaw, pitch);
                
                lastMousePosition = currentMousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
        }
        
        /// <summary>
        /// Rotacionar câmera
        /// </summary>
        private void RotateCamera(float yaw, float pitch)
        {
            if (cameraTransform == null) return;
            
            // Aplicar rotação Y (yaw)
            cameraTransform.Rotate(0, yaw, 0, Space.World);
            
            // Aplicar rotação X (pitch)
            cameraTransform.Rotate(pitch, 0, 0, Space.Self);
        }
        
        /// <summary>
        /// Aplicar limites de rotação
        /// </summary>
        private void ApplyRotationLimits()
        {
            if (!enableRotationLimits || cameraTransform == null) return;
            
            Vector3 currentRotation = cameraTransform.eulerAngles;
            
            // Normalizar ângulos para -180 a 180
            float pitch = NormalizeAngle(currentRotation.x);
            float yaw = NormalizeAngle(currentRotation.y);
            
            // Aplicar limites
            pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);
            yaw = Mathf.Clamp(yaw, -maxYawAngle, maxYawAngle);
            
            // Aplicar rotação limitada
            cameraTransform.eulerAngles = new Vector3(pitch, yaw, currentRotation.z);
        }
        
        /// <summary>
        /// Normalizar ângulo para -180 a 180
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            while (angle > 180) angle -= 360;
            while (angle < -180) angle += 360;
            return angle;
        }
        
        /// <summary>
        /// Resetar rotação da câmera
        /// </summary>
        public void ResetRotation()
        {
            if (cameraTransform == null || isResetting) return;
            
            LogDebug("Resetando rotação da câmera");
            
            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
            }
            
            resetCoroutine = StartCoroutine(ResetRotationCoroutine());
        }
        
        /// <summary>
        /// Corrotina para resetar rotação suavemente
        /// </summary>
        private System.Collections.IEnumerator ResetRotationCoroutine()
        {
            isResetting = true;
            Vector3 startRotation = cameraTransform.eulerAngles;
            float elapsed = 0f;
            
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * resetSpeed;
                
                // Interpolar entre rotação atual e inicial
                Vector3 targetRotation = Vector3.Lerp(startRotation, initialRotation, elapsed);
                cameraTransform.eulerAngles = targetRotation;
                
                yield return null;
            }
            
            // Garantir rotação exata
            cameraTransform.eulerAngles = initialRotation;
            isResetting = false;
            
            LogDebug("Rotação resetada");
        }
        
        /// <summary>
        /// Atualizar informações de debug
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (!showDebugInfo || cameraTransform == null) return;
            
            Vector3 currentRotation = cameraTransform.eulerAngles;
            float pitch = NormalizeAngle(currentRotation.x);
            float yaw = NormalizeAngle(currentRotation.y);
            
            // Mostrar informações na tela (opcional)
            if (enableDebugLogs && Time.frameCount % 60 == 0) // A cada segundo
            {
                LogDebug($"Rotação - Pitch: {pitch:F1}°, Yaw: {yaw:F1}°");
            }
        }
        
        /// <summary>
        /// Definir rotação inicial
        /// </summary>
        public void SetInitialRotation(Vector3 rotation)
        {
            initialRotation = rotation;
            LogDebug($"Nova rotação inicial definida: {rotation}");
        }
        
        /// <summary>
        /// Habilitar/desabilitar limites de rotação
        /// </summary>
        public void SetRotationLimits(bool enabled)
        {
            enableRotationLimits = enabled;
            LogDebug($"Limites de rotação: {(enabled ? "Ativados" : "Desativados")}");
        }
        
        /// <summary>
        /// Definir ângulos máximos
        /// </summary>
        public void SetMaxAngles(float pitch, float yaw)
        {
            maxPitchAngle = pitch;
            maxYawAngle = yaw;
            LogDebug($"Novos ângulos máximos - Pitch: {pitch}°, Yaw: {yaw}°");
        }
        
        /// <summary>
        /// Log de debug
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[CameraRotationController] {message}");
            }
        }
        
        // Properties
        public bool IsResetting => isResetting;
        public Vector3 CurrentRotation => cameraTransform != null ? cameraTransform.eulerAngles : Vector3.zero;
        public Vector3 InitialRotation => initialRotation;
        public bool IsDragging => isDragging;
        
        void OnDestroy()
        {
            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
            }
        }
        
        void OnDrawGizmos()
        {
            if (!showDebugInfo || cameraTransform == null) return;
            
            // Desenhar limites de rotação (opcional)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraTransform.position, 1f);
        }
    }
}
