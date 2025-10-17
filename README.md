# 🎬 CoralVivoVR - Sistema de Controle de Vídeo com ESP32

Sistema completo de controle de vídeo VR com LEDs sincronizados via ESP32 e botões físicos.

## 🚀 Funcionalidades

### 🎮 Controle via Botões ESP32
- **Botão 1 (Press Curto)**: Play/Pause do vídeo
- **Botão 1 (Long Press)**: Reset do vídeo
- **Botão 2**: Mesmo comportamento do Botão 1
- **Comunicação bidirecional**: ESP32 ↔ Unity

### 🎬 Controle de Vídeo
- **Play/Pause** sincronizado com LEDs
- **Reset** para início do vídeo
- **Restauração de estado** ao voltar do foco
- **Progresso sincronizado** com LEDs

### 💡 LEDs Sincronizados
- **PRONTO**: Verde fixo (início/reset)
- **PLAYING**: Progressão azul/vermelho (vídeo tocando)
- **PAUSED**: Azul/Vermelho escuro (vídeo pausado)
- **CHASE**: Efeito chase (perda de foco/conexão)

## 📁 Estrutura do Projeto

```
BIJARI_VR/
├── src/                    # Firmware ESP32
│   └── main.cpp           # Código principal ESP32
├── Unity/                 # Projeto Unity
│   └── CoralVivoVR/
│       ├── Assets/
│       │   ├── Scripts/
│       │   │   └── ESP32/
│       │   │       └── ESP32LEDTester.cs  # Script principal
│       │   └── StreamingAssets/          # Vídeos (adicionar manualmente)
│       └── Scenes/
│           └── passoapasso.unity        # Cena de teste
├── platformio.ini         # Configuração PlatformIO
└── README.md             # Este arquivo
```

## 🔧 Configuração

### ESP32 (Firmware)
1. **Conecte ESP32** via USB
2. **Compile e faça upload**:
   ```bash
   pio run --target upload
   ```
3. **ESP32 cria hotspot**: `CoralVivoVR` (senha: `12345678`)

### Unity (Aplicação)
1. **Abra Unity** e carregue o projeto em `Unity/CoralVivoVR/`
2. **Abra a cena** `passoapasso` em `Assets/Scenes/`
3. **Configure VideoPlayer**:
   - Adicione vídeo em `StreamingAssets/`
   - Configure URL no VideoPlayer
4. **Execute a cena**

### Vídeos
1. **Adicione vídeos** em `Unity/CoralVivoVR/Assets/StreamingAssets/`
2. **Formato recomendado**: MP4
3. **Configure URL** no VideoPlayer: `file://{Application.streamingAssetsPath}/seu_video.mp4`

## 🎯 Estados dos LEDs

| Estado | Comando ESP32 | LEDs | Quando |
|--------|---------------|------|--------|
| **DISCONNECTED** | - | Roxo piscando | Sem conexão |
| **READY** | `on1`/`on2` | Verde piscando | Pronto |
| **PLAYING** | `play1`/`play2` | Progressão azul/vermelho | Vídeo tocando |
| **PAUSED** | `pause1`/`pause2` | Azul/Vermelho escuro | Vídeo pausado |
| **SIGNAL_LOST** | `signal_lost1`/`signal_lost2` | Chase | Perda de conexão |

## 🔄 Comandos WebSocket

### Unity → ESP32
- `on1`/`on2`: Estado pronto
- `play1`/`play2`: Iniciar vídeo
- `pause1`/`pause2`: Pausar vídeo
- `led1:X`/`led2:X`: Progresso X%
- `signal_lost1`/`signal_lost2`: Perda de sinal

### ESP32 → Unity
- `button1_short_press`: Botão 1 pressionado
- `button1_long_press`: Botão 1 long press
- `button2_short_press`: Botão 2 pressionado
- `button2_long_press`: Botão 2 long press

## 🎮 Controles de Teste (Unity)

| Tecla | Ação |
|-------|------|
| **Space** | Play |
| **P** | Pause |
| **S** | Stop |
| **R** | Ready |
| **H** | Headset Off |
| **L** | Signal Lost |
| **↑/↓** | Progresso |

## 📊 Logs de Debug

### Unity Console
```
🎮 BOTÃO 1 (ESP32) - Press Curto detectado!
🎬 Vídeo INICIADO
🔵 Player 2 - PLAY (Progressão azul/vermelho)
```

### ESP32 Serial Monitor
```
🎮 BOTÃO 1 (PLAY/PAUSE) - PRESS CURTO
🏃 Player 1 signal lost - chase effect
```

## 🔧 Hardware

### ESP32
- **Pinos LED**: GPIO 2
- **Botão 1**: GPIO 4 (Play/Pause)
- **Botão 2**: GPIO 5 (Effect/Stop)
- **LEDs**: 16 LEDs (8 por player)

### Conexões
- **LEDs**: Pino 2 → Fita LED WS2812B
- **Botões**: GPIO 4,5 → Botões com pull-up interno

## 🚀 Status Atual

- ✅ **ESP32 Firmware**: Completo com todos os estados
- ✅ **Unity Script**: Controle completo de vídeo
- ✅ **Comunicação Bidirecional**: ESP32 ↔ Unity
- ✅ **Controle de Botões**: Play/Pause/Reset
- ✅ **Sincronização LEDs**: Estados sincronizados
- ✅ **Restauração de Estado**: Continua de onde parou
- ✅ **Thread Safety**: Sem erros de thread

## 📝 Próximos Passos

1. **Adicionar vídeos** em StreamingAssets
2. **Configurar VideoPlayer** na cena
3. **Testar** com botões ESP32
4. **Personalizar** comportamentos conforme necessário

## 🎬 Cena de Teste

A cena `passoapasso` foi usada para testar cada etapa do desenvolvimento e serve como base para construção do projeto final.

---

**Desenvolvido para CoralVivoVR** 🐠✨