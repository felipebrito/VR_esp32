# 🥽 Guia de Teste no Quest

## 📋 Pré-requisitos

### 1. **ESP32 Configurado:**
- ✅ Firmware atualizado com comunicação bidirecional
- ✅ IP: `192.168.0.1` (padrão)
- ✅ WebSocket na porta 80
- ✅ Botões físicos funcionando

### 2. **Rede WiFi:**
- ✅ Quest e ESP32 na mesma rede
- ✅ ESP32 como hotspot ou ambos conectados ao mesmo router

## 🎮 Como Testar

### **1. Iniciar ESP32:**
```bash
# Compilar e enviar firmware
cd /Users/brito/Desktop/BIJARI_VR
pio run -t upload
```

### **2. Conectar Quest:**
- Conectar Quest ao mesmo WiFi do ESP32
- Abrir aplicação CoralVivoVR no Quest
- Verificar logs no console Unity

### **3. Testar Botões ESP32:**

#### **Botão 1 (Play/Pause):**
- **Press Curto**: Toggle play/pause do vídeo
- **Long Press**: Reset vídeo (volta ao início)

#### **Botão 2 (Effect/Stop):**
- **Press Curto**: Controlar Player 2
- **Long Press**: Reset todos os players

## 🔍 Logs Esperados

### **Conexão:**
```
🎮 ESP32LEDTester iniciado - Player 1
🔌 Conectando ao ESP32: ws://192.168.0.1:80/ws
✅ Conectado ao ESP32!
🟢 Player 1 - PRONTO (Verde fixo)
```

### **Botão Pressionado:**
```
📨 ESP32: button1_short_press
🎮 BOTÃO 1 (ESP32) - Press Curto detectado!
🎬 Vídeo INICIADO
🔵 Player 1 - PLAY (Progressão azul/vermelho)
```

## 🎯 Estados dos LEDs

### **Player 1 (Azul):**
- **Pronto**: Verde fixo
- **Play**: Progressão azul
- **Pause**: Pausado
- **Stop**: Verde fixo
- **Headset Off**: Chase
- **Signal Lost**: Rainbow

### **Player 2 (Vermelho):**
- **Pronto**: Verde fixo
- **Play**: Progressão vermelha
- **Pause**: Pausado
- **Stop**: Verde fixo
- **Headset Off**: Chase
- **Signal Lost**: Chase

## 🚨 Troubleshooting

### **Não Conecta:**
1. Verificar IP do ESP32
2. Verificar rede WiFi
3. Verificar se ESP32 está rodando
4. Verificar logs no console

### **Botões Não Funcionam:**
1. Verificar se ESP32 está enviando eventos
2. Verificar logs de botão no ESP32
3. Verificar se WebSocket está conectado

### **Vídeo Não Controla:**
1. Verificar se VideoPlayer está configurado
2. Verificar se vídeo está na pasta StreamingAssets
3. Verificar logs de vídeo

## 📱 Configuração Quest

### **Build Settings:**
- Platform: Android
- Target Device: Quest
- Scripting Backend: IL2CPP
- Target Architecture: ARM64

### **XR Settings:**
- VR Supported: ✅
- Virtual Reality SDKs: Oculus

## 🎬 Vídeo Configurado

**Vídeo Principal:** `Pierre_Final.mp4`
- **Duração:** 3 minutos e 35 segundos (215 segundos)
- **Localização:** `StreamingAssets/Pierre_Final.mp4`
- **Configuração:** Automática no script ESP32LEDTester
- **Progresso LEDs:** Calculado baseado na duração de 215s

### **Cálculo de Progresso:**
- **0s**: 0% (LEDs apagados)
- **1m07s**: 50% (4 LEDs acesos)
- **2m15s**: 100% (8 LEDs acesos)
- **3m35s**: 100% (Fim do vídeo)

## 🔧 Configuração do Script

No Inspector do ESP32LEDTester:
- **ESP32 IP**: `192.168.0.1`
- **ESP32 Port**: `80`
- **Player ID**: `1` ou `2`
- **Auto Connect**: ✅
- **Video Player**: Linkar o VideoPlayer da cena

---

**Boa sorte com o teste! 🚀**
