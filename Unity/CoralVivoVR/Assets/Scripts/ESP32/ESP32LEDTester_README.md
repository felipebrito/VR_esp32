# 🎮 ESP32LEDTester - Script de Teste Unity

Script Unity completo para testar todos os comandos LED ESP32 mapeados.

## 🎯 **Funcionalidades**

### **🔧 Configuração Automática**
- **Conexão automática** ao ESP32 ao dar play na cena
- **Player ID configurável** (1 ou 2) via Inspector
- **IP e Porta** configuráveis (padrão: 192.168.0.1:80)

### **🎮 Controles de Teste**
- **Space** - Play (Azul/Vermelho progressivo automático)
- **P** - Pause (Azul/Vermelho escuro)
- **S** - Stop (Chase effect - perda de sinal)
- **R** - Ready (Verde piscando)
- **H** - Headset Off (Azul/Vermelho escuro progressivo)
- **L** - Signal Lost (Rainbow effect)
- **↑/↓** - Controle manual de progresso

### **📊 Estados LED Testados**
- **🟢 READY**: Verde piscando
- **🔵 PLAYING**: Azul progressivo (P1) / Vermelho progressivo (P2)
- **⏸️ PAUSED**: Azul escuro (P1) / Vermelho escuro (P2)
- **🔴 HEADSET OFF**: Azul escuro progressivo (P1) / Vermelho escuro progressivo (P2)
- **🌈 SIGNAL LOST**: Rainbow effect (P1) / Chase effect (P2)

## 🚀 **Como Usar**

### **1. Configuração**
1. Adicione o script `ESP32LEDTester` a um GameObject na cena
2. Configure o **Player ID** (1 ou 2) no Inspector
3. Ajuste **ESP32 IP** se necessário (padrão: 192.168.0.1)

### **2. Teste Manual**
1. **Play na cena** → Conecta automaticamente ao ESP32
2. **Use as teclas** para testar cada comando
3. **Observe os logs** no Console Unity
4. **Veja os LEDs** físicos respondendo

### **3. Teste Automático**
1. **Clique no botão** "🧪 Testar Todos os Comandos" na interface
2. **Ou use o Context Menu** → "Test All Commands"
3. **Script executa** todos os comandos sequencialmente

## 🎯 **Lógica Implementada**

### **🔄 Ciclo de Estados**
```
Ready → Play → Pause → Headset Off → Signal Lost → Stop → Ready
```

### **🎮 Comportamento Específico**
- **Stop (S)**: Ativa Chase effect (perda de sinal)
- **Play (Space)**: Fecha o ciclo, volta ao estado normal
- **Progresso automático**: Durante Play, simula progresso de 0-100%
- **Auto pause**: Quando progresso chega em 100%

### **📱 Detecção de Foco**
- **OnApplicationPause**: Headset removido → Headset Off
- **OnApplicationFocus**: Headset colocado → Ready

## 🎨 **Interface Visual**

### **📊 HUD na Tela**
- **Status da conexão** ESP32
- **Estado atual** de cada LED
- **Progresso** em tempo real
- **Lista de controles** disponíveis
- **Botão de teste** automático

### **📝 Logs no Console**
- **Conexão**: Status de conexão WebSocket
- **Comandos**: Cada comando enviado ao ESP32
- **Respostas**: Mensagens recebidas do ESP32
- **Estados**: Mudanças de estado dos LEDs

## 🔧 **Configurações Avançadas**

### **🎮 Teclas Personalizáveis**
```csharp
[SerializeField] private KeyCode playKey = KeyCode.Space;
[SerializeField] private KeyCode pauseKey = KeyCode.P;
[SerializeField] private KeyCode stopKey = KeyCode.S;
// ... outras teclas
```

### **⚙️ Parâmetros Ajustáveis**
```csharp
[SerializeField] private string esp32IP = "192.168.0.1";
[SerializeField] private int esp32Port = 80;
[SerializeField] private int playerID = 1;
[SerializeField] private bool autoConnect = true;
```

## 🎯 **Comandos ESP32 Mapeados**

### **🔤 String Simples**
- `on1`/`on2` → Ready (Verde piscando)
- `play1`/`play2` → Playing (Azul/Vermelho progressivo)
- `pause1`/`pause2` → Paused (Azul/Vermelho escuro)
- `off1`/`off2` → Headset Off (Azul/Vermelho escuro progressivo)

### **🎨 Perda de Sinal**
- `signal_lost1` → Rainbow effect (Player 1)
- `signal_lost2` → Chase effect (Player 2)
- `signal_lost1:chase` → Chase effect (Player 1)
- `signal_lost2:rainbow` → Rainbow effect (Player 2)

### **📊 Progresso**
- `led1:X`/`led2:X` → Progresso manual (X = 0-100)

## 🚀 **Próximos Passos**

1. **Teste o script** com o ESP32 funcionando
2. **Integre com VRManager** para controle automático
3. **Adicione feedback visual** na interface VR
4. **Implemente sincronização** com vídeo 360°

---

**🎮 Script pronto para testar todos os comandos LED ESP32!** ✨
