using UnityEngine;

namespace CoralVivoVR.Video
{
    /// <summary>
    /// Gerador de mesh customizada para vídeo 360°
    /// Baseado na solução do projeto socket-client original
    /// Resolve problemas de seams/emendas da esfera padrão do Unity
    /// </summary>
    public class Video360MeshGenerator : MonoBehaviour
    {
        [Header("Mesh Settings")]
        [SerializeField] private float radius = 10f;
        [SerializeField] private int segments = 64;
        [SerializeField] private bool generateOnStart = true;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        private Mesh customMesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        
        void Start()
        {
            InitializeComponents();
            
            if (generateOnStart)
            {
                GenerateCustomSphere();
            }
        }
        
        /// <summary>
        /// Inicializar componentes
        /// </summary>
        private void InitializeComponents()
        {
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            LogDebug("Componentes inicializados");
        }
        
        /// <summary>
        /// Gerar esfera customizada para vídeo 360°
        /// </summary>
        public void GenerateCustomSphere()
        {
            LogDebug($"Gerando esfera customizada - Raio: {radius}, Segmentos: {segments}");
            
            customMesh = CreateCustomSphereMesh();
            
            if (meshFilter != null)
            {
                meshFilter.mesh = customMesh;
            }
            
            LogDebug("Esfera customizada gerada com sucesso");
        }
        
        /// <summary>
        /// Criar mesh customizada com UV mapping correto
        /// </summary>
        private Mesh CreateCustomSphereMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Video360CustomSphere";
            
            // Calcular número de vértices
            int vertexCount = (segments + 1) * (segments + 1);
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            int[] triangles = new int[segments * segments * 6];
            
            int vertexIndex = 0;
            int triangleIndex = 0;
            
            // Gerar vértices com UV mapping otimizado para vídeo 360°
            for (int y = 0; y <= segments; y++)
            {
                for (int x = 0; x <= segments; x++)
                {
                    // Coordenadas esféricas
                    float xAngle = (float)x / segments * Mathf.PI * 2;
                    float yAngle = (float)y / segments * Mathf.PI;
                    
                    // Posição esférica
                    Vector3 spherePos = new Vector3(
                        Mathf.Sin(yAngle) * Mathf.Cos(xAngle),
                        Mathf.Cos(yAngle),
                        Mathf.Sin(yAngle) * Mathf.Sin(xAngle)
                    );
                    
                    // Inverter para olhar para dentro (concavo)
                    vertices[vertexIndex] = -spherePos * radius;
                    
                    // UV mapping otimizado para vídeo 360°
                    // Evita seams visíveis
                    uvs[vertexIndex] = new Vector2(
                        (float)x / segments,
                        1.0f - (float)y / segments // Inverter Y para correção
                    );
                    
                    // Normais apontando para dentro
                    normals[vertexIndex] = spherePos;
                    
                    // Gerar triângulos
                    if (x < segments && y < segments)
                    {
                        int current = vertexIndex;
                        int next = current + segments + 1;
                        
                        // Triângulo 1
                        triangles[triangleIndex] = current;
                        triangles[triangleIndex + 1] = next;
                        triangles[triangleIndex + 2] = current + 1;
                        
                        // Triângulo 2
                        triangles[triangleIndex + 3] = current + 1;
                        triangles[triangleIndex + 4] = next;
                        triangles[triangleIndex + 5] = next + 1;
                        
                        triangleIndex += 6;
                    }
                    
                    vertexIndex++;
                }
            }
            
            // Aplicar dados à mesh
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            
            // Otimizar mesh
            mesh.RecalculateBounds();
            mesh.Optimize();
            
            return mesh;
        }
        
        /// <summary>
        /// Aplicar material ao mesh renderer
        /// </summary>
        public void ApplyMaterial(Material material)
        {
            if (meshRenderer != null && material != null)
            {
                meshRenderer.material = material;
                LogDebug($"Material aplicado: {material.name}");
            }
        }
        
        /// <summary>
        /// Configurar propriedades da mesh
        /// </summary>
        public void SetMeshProperties(float newRadius, int newSegments)
        {
            radius = newRadius;
            segments = newSegments;
            
            if (customMesh != null)
            {
                GenerateCustomSphere();
            }
        }
        
        /// <summary>
        /// Log de debug
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Video360MeshGenerator] {message}");
            }
        }
        
        // Properties
        public Mesh CustomMesh => customMesh;
        public float Radius => radius;
        public int Segments => segments;
        
        void OnDestroy()
        {
            if (customMesh != null)
            {
                DestroyImmediate(customMesh);
            }
        }
        
        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (customMesh != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireMesh(customMesh, transform.position, transform.rotation, transform.localScale);
            }
        }
        #endif
    }
}
