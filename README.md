# 🥽 CoralVivoVR - Sistema VR Completo

Sistema completo de realidade virtual para Meta Quest 3S com sincronização ESP32 para LEDs e controles físicos.

## 🎯 **Arquitetura Dual**

### **Versão Desktop (Funcionando)**
- Aplicação web com Three.js
- Player 360° com controles drag
- WebSocket para ESP32
- Teste e desenvolvimento

### **Versão Unity (Em Desenvolvimento)**
- Aplicação Unity nativa para Quest
- APK instalável no Quest 3S
- Performance otimizada
- Controles VR nativos

### **ESP32 (Comum)**
- Rede WiFi: "CoralVivoVR" (senha: 12345678)
- IP fixo: 192.168.0.1
- WebSocket Server para comunicação
- 16 LEDs + 2 botões físicos

## 📁 **Estrutura do Projeto**

```
CoralVivoVR/
├── src/main.cpp              # ESP32 firmware (P2P + WebSocket)
├── platformio.ini            # Configuração PlatformIO
├── Pierre_Final.mov          # Vídeo 360° principal
├── VIDEO_PLACEHOLDER.md      # Instruções do vídeo
├── README.md                 # Este arquivo
└── web_player/               # Versão Desktop (funcionando)
    ├── index.html            # Interface principal
    ├── styles.css            # Estilos
    ├── video-player.js       # Player 360° Three.js
    ├── websocket-client.js   # Cliente WebSocket ESP32
    ├── led-visualizer.js     # Visualização LEDs
    ├── main.js               # Integração principal
    ├── README.md             # Documentação web
    └── VIDEO_SAMPLES.md      # Exemplos de vídeo
```

## 🚀 **Versão Desktop (Pronta)**

### **Como usar:**
1. **Compilar ESP32**: `pio run -t upload`
2. **Servidor local**: `cd web_player && python3 -m http.server 8000`
3. **Acessar**: `http://localhost:8000`
4. **Conectar ESP32**: IP 192.168.0.1
5. **Carregar vídeo**: Pierre_Final.mov

### **Funcionalidades:**
- ✅ **Player 360°** com Three.js
- ✅ **Controles drag** para navegação
- ✅ **WebSocket ESP32** funcionando
- ✅ **LEDs sincronizados** com progresso
- ✅ **Botões físicos** controlando reprodução
- ✅ **Interface completa** de debug

## 🎮 **Versão Unity (Em Desenvolvimento)**

### **Próximos Passos:**
1. **Criar projeto Unity** com template VR
2. **Instalar pacotes XR** (Oculus, XR Interaction Toolkit)
3. **Implementar scripts** para ESP32 e vídeo 360°
4. **Configurar build** para Quest 3S
5. **Gerar APK** instalável

### **Scripts Planejados:**
- `ESP32WebSocketClient.cs` - Comunicação com ESP32
- `Video360Player.cs` - Player de vídeo 360° com esfera customizada
- `CoralVivoVRManager.cs` - Gerenciamento principal do sistema

## 🔧 **ESP32 Firmware**

### **Configuração Atual:**
- **Rede**: "CoralVivoVR" (senha: 12345678)
- **IP**: 192.168.0.1
- **WebSocket**: Porta 80
- **LEDs**: 16 WS2812B (8 para cada player)
- **Botões**: 2 botões físicos (play/pause/stop)

### **Estados dos LEDs:**
- **🟢 READY**: Verde piscando
- **🔵 PLAYING**: Azul progressivo (Player 1) / Vermelho progressivo (Player 2)
- **⏸️ PAUSED**: Azul escuro (Player 1) / Vermelho escuro (Player 2)
- **🔴 HEADSET OFF**: Azul escuro progressivo (Player 1) / Vermelho escuro progressivo (Player 2)
- **🌈 SIGNAL LOST**: Rainbow effect (Player 1) / Chase effect (Player 2)

### **Comandos WebSocket:**
- **String simples**: `on1`/`on2`, `play1`/`play2`, `pause1`/`pause2`, `off1`/`off2`
- **Progresso**: `led1:X`/`led2:X` (X = 0-100)
- **Perda de sinal**: `signal_lost1`/`signal_lost2`, `signal_lost1:chase`/`signal_lost2:rainbow`
- **JSON**: `{"player":1,"status":"playing","progress":0.5}`

### **Compilar e Upload:**
```bash
pio run -t upload
```

### **Teste de LEDs:**
```bash
python3 led_test.py
```

## 🎮 **Funcionalidades Completas**

### **Desktop (Three.js):**
- ✅ Player 360° estereoscópico
- ✅ Controles drag para navegação
- ✅ Detecção automática headset on/off
- ✅ Navegação com mouse/touch

### **Unity (Planejado):**
- ✅ Player 360° estereoscópico
- ✅ Controles Quest nativos
- ✅ Detecção automática headset on/off
- ✅ Navegação com controles Quest

### **ESP32 Integration (Ambas):**
- ✅ Rede P2P dedicada "CoralVivoVR"
- ✅ WebSocket Server (porta 80)
- ✅ 16 LEDs sincronizados com progresso
- ✅ 2 botões físicos (play/pause/stop)
- ✅ Detecção headset via WebSocket

### **Sincronização (Ambas):**
- ✅ LEDs mostram progresso do vídeo
- ✅ Botões ESP32 controlam reprodução
- ✅ Headset on/off pausa automaticamente
- ✅ Comunicação bidirecional WebSocket

## 📋 **TODO List Unity**

### **Fase 1: Setup Unity**
- [ ] Criar projeto Unity VR
- [ ] Instalar pacotes XR/Oculus
- [ ] Configurar para Quest 3S

### **Fase 2: Scripts Core**
- [ ] ESP32WebSocketClient.cs
- [ ] Video360Player.cs
- [ ] CoralVivoVRManager.cs

### **Fase 3: Build e Teste**
- [ ] Configurar build Android
- [ ] Gerar APK para Quest
- [ ] Testar sistema completo

## 🎯 **Status Atual**

- ✅ **Versão Desktop**: Funcionando perfeitamente
- ✅ **ESP32 Firmware**: Funcionando perfeitamente com todos os estados
- ✅ **WebSocket**: Comunicação bidirecional funcionando
- ✅ **LEDs**: Todos os estados implementados (Ready, Playing, Paused, Headset OFF, Signal Lost)
- ✅ **Botões**: Controles físicos funcionando
- ✅ **Teste de LEDs**: Interface Python completa para testar todos os comandos
- 🔄 **Versão Unity**: Em desenvolvimento

## 📄 **Licença**

Projeto desenvolvido para CoralVivoVR - Sistema VR profissional completo.

---

**🎯 Sistema VR dual: Desktop funcionando + Unity em desenvolvimento!** 🚀