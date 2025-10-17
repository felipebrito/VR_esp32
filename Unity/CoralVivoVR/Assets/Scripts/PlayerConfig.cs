using UnityEngine;
using CoralVivoVR.ESP32;

/// <summary>
/// Script para configurar rapidamente Player 1 ou Player 2
/// </summary>
public class PlayerConfig : MonoBehaviour
{
    [Header("Player Configuration")]
    [Range(1, 2)]
    public int targetPlayerId = 1;
    
    [Header("Auto-Apply on Start")]
    public bool applyOnStart = true;
    
    void Start()
    {
        if (applyOnStart)
        {
            ApplyPlayerConfiguration();
        }
    }
    
    /// <summary>
    /// Aplicar configuração do Player
    /// </summary>
    [ContextMenu("Apply Player Configuration")]
    public void ApplyPlayerConfiguration()
    {
        // Configurar VRManager
        VRManager vrManager = FindObjectOfType<VRManager>();
        if (vrManager != null)
        {
            vrManager.playerId = targetPlayerId;
            Debug.Log($"✅ VRManager configurado para Player {targetPlayerId}");
        }
        else
        {
            Debug.LogError("❌ VRManager não encontrado na cena!");
        }
        
        // Configurar ESP32WebSocketClient
        ESP32WebSocketClient esp32Client = FindObjectOfType<ESP32WebSocketClient>();
        if (esp32Client != null)
        {
            // Usar reflexão para acessar o campo privado playerId
            var field = typeof(ESP32WebSocketClient).GetField("playerId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(esp32Client, targetPlayerId);
                Debug.Log($"✅ ESP32WebSocketClient configurado para Player {targetPlayerId}");
            }
        }
        else
        {
            Debug.LogError("❌ ESP32WebSocketClient não encontrado na cena!");
        }
        
        // Configurar nome do GameObject para identificação
        gameObject.name = $"Player{targetPlayerId}_Config";
        
        Debug.Log($"🎮 Configuração Player {targetPlayerId} aplicada com sucesso!");
    }
    
    /// <summary>
    /// Configurar como Player 1
    /// </summary>
    [ContextMenu("Set as Player 1")]
    public void SetAsPlayer1()
    {
        targetPlayerId = 1;
        ApplyPlayerConfiguration();
    }
    
    /// <summary>
    /// Configurar como Player 2
    /// </summary>
    [ContextMenu("Set as Player 2")]
    public void SetAsPlayer2()
    {
        targetPlayerId = 2;
        ApplyPlayerConfiguration();
    }
}


