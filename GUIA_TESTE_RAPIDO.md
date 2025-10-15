# 🚀 Guia de Teste Rápido - BIJARI_VR

## ✅ Sistema Funcionando!

### 🌐 **Player Web Ativo**
- **URL**: http://localhost:8000
- **Status**: ✅ Servidor rodando
- **Vídeo**: Pierre_Final [HighRes]-001.mov (2.9GB) ✅ Carregado

### 🎯 **Como Testar Agora:**

#### **1. Abrir Player Web**
```
http://localhost:8000
```

#### **2. Testar Vídeo 360°**
1. **Clique na área do vídeo** no player web
2. **Selecione** `Pierre_Final [HighRes]-001.mov`
3. **Aguarde carregamento** (pode demorar alguns segundos)
4. **Teste controles**: Play, Pause, Seek

#### **3. Navegação 360°**
- **Arrastar**: Rotacionar dentro da esfera côncava
- **🎯 Centralizar**: Botão para voltar ao centro
- **🔄 Reset**: Botão para resetar visualização
- **Graus**: Display em tempo real da rotação e inclinação
- **Esfera Côncava**: Câmera posicionada dentro da esfera para navegação 360°

#### **4. Simular Meta Quest**
- **Headset ON**: `Ctrl + H` ou botão "Headset ON"
- **App Focus**: `Ctrl + F` ou botão "App Focus"  
- **Play**: `Ctrl + P` ou botão "Play"
- **Pause**: `Ctrl + P` ou botão "Pause"
- **Stop**: `Ctrl + S` ou botão "Stop"

#### **5. Testar LEDs Virtuais**
- **Player 1**: LEDs 1-8 (azul)
- **Player 2**: LEDs 9-16 (vermelho)
- **Status**: Verde = Ready, Azul/Vermelho = Playing

### 🔧 **Próximo Passo: ESP32**

Para testar com ESP32 real:
1. **Conecte ESP32** à rede WiFi
2. **Anote o IP** do ESP32
3. **Insira IP** no campo "IP do ESP32"
4. **Clique "Conectar"**
5. **Teste comandos** reais

### 📱 **Comandos Disponíveis**

#### **WebSocket (ESP32)**
- `on1` - Player 1 ready
- `play1` - Player 1 playing
- `led1:50` - Progresso 50%
- `on2` - Player 2 ready
- `play2` - Player 2 playing
- `led2:75` - Progresso 75%

#### **Teclado**
- `Ctrl + H` - Headset ON/OFF
- `Ctrl + F` - App Focus/Unfocus
- `Ctrl + P` - Play/Pause
- `Ctrl + S` - Stop
- `Ctrl + A` - Modo Automático

### 🎥 **Teste do Vídeo Pierre_Final**

1. **Carregue o vídeo** no player
2. **Simule Headset ON**
3. **Inicie reprodução**
4. **Observe LEDs** acendendo progressivamente
5. **Teste pause/resume**
6. **Teste seek** (arrastar slider)

---

## 🚀 **Sistema Pronto para Teste!**

**Player Web**: ✅ Funcionando  
**Vídeo**: ✅ Carregado  
**Simulação VR**: ✅ Ativa  
**Próximo**: ESP32 real

**Acesse**: http://localhost:8000
