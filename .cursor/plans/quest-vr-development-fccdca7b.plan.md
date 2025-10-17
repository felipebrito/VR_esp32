<!-- fccdca7b-09ce-44b3-ab4b-6b0e5ef3f6c3 bcf589f9-5959-463d-856e-c57463ffc198 -->
# Plano: WebXR Standalone para Quest 3S + ESP32

## Arquitetura Final

### Quest 3S (Standalone)

- Aplicação WebXR local (HTML/JS/CSS)
- Vídeo em Downloads/Pierre_Final.mov
- WebSocket Client → ESP32

### ESP32 (Access Point)

- Rede: "CoralVivoVR" (senha: 12345678)
- IP: 192.168.0.1
- WebSocket Server apenas (sem servir HTML)
- 16 LEDs + 2 Botões físicos

## Ajustes Necessários

### 1.1 Adaptar Web Player Existente para WebXR

Modificar `web_player/index.html`:

- Adicionar suporte WebXR Session API
- Criar botão "Enter VR" que ativa modo imersivo
- Manter compatibilidade com desktop browser

Atualizar `web_player/video-player.js`:

- Configurar renderer para WebXR: `renderer.xr.enabled = true`
- Ajustar camera para estereoscópica (dois olhos)
- Remover OrbitControls, usar XRSession tracking nativo
- Implementar controles Quest (trigger/grip buttons)

### 1.2 Adicionar Detecção de Eventos do Headset

Criar `web_player/quest-xr-integration.js`:

- Detectar `visibilitychange` para headset on/off
- Enviar comandos WebSocket: `on1`, `pause1` automaticamente
- Integrar com `websocket-client.js` existente

### 1.3 Configuração e Deploy

Criar `web_player/WEBXR_SETUP.md`:

- Instruções para acessar via Quest Browser
- Configuração HTTPS (necessário para WebXR)
- IP local ou ngrok para acesso remoto
- Testes e troubleshooting

### 1.4 Testar no Quest 3S

- Carregar vídeo Pierre_Final no Quest
- Conectar ao ESP32 via WebSocket
- Validar sincronização LEDs com progresso
- Testar headset on/off events

---

## Fase 2: Unity (Aplicação Nativa)

### 2.1 Setup Projeto Unity

Criar novo projeto Unity:

- Template: VR (Universal Render Pipeline)
- Versão: Unity 2022.3 LTS ou superior
- Plataforma: Android (Meta Quest)

Instalar pacotes via Package Manager:

- XR Plugin Management
- Oculus XR Plugin
- XR Interaction Toolkit
- Unity WebSocket (NuGet ou asset)

### 2.2 Implementar Player 360° Nativo

Criar scripts C#:

`VideoPlayer360.cs`:

```csharp
// Sphere com vídeo texture interno
// VideoPlayer component do Unity
// Escala invertida para projeção côncava
```

`QuestHeadsetEvents.cs`:

```csharp
// Detectar XRInputSubsystem events
// Headset mounted/unmounted
// App focus/unfocus
```

### 2.3 Integração WebSocket ESP32

Criar `ESP32WebSocketClient.cs`:

- Conectar ao IP do ESP32
- Enviar comandos: on1, play1, led1:X
- Receber comandos: play, pause, stop
- Controlar VideoPlayer baseado em mensagens

### 2.4 UI e Controles Quest

Implementar:

- Canvas UI com XR Interaction Toolkit
- Botões para conectar ESP32, carregar vídeo
- Controles de playback com Quest controllers
- Indicador visual de conexão/LEDs

### 2.5 Build e Deploy Android

Configurar:

- Build Settings: Android, IL2CPP, ARM64
- Player Settings: Package name, versões, permissões
- Exportar APK e instalar via SideQuest ou ADB

---

## Entregáveis

### WebXR (Fase 1):

- Web player atualizado com suporte WebXR
- Funciona no Quest Browser sem instalação
- Sincronização ESP32 completa
- Documentação de uso

### Unity (Fase 2):

- Projeto Unity completo
- APK instalável no Quest 3S
- Performance nativa otimizada
- Controles Quest nativos
- Mesma funcionalidade WebSocket

## Arquivos Principais

### WebXR:

- `web_player/index.html` - Adicionar WebXR API
- `web_player/video-player.js` - Adaptar para XR
- `web_player/quest-xr-integration.js` - Novo módulo
- `web_player/WEBXR_SETUP.md` - Documentação

### Unity:

- `unity_quest/Assets/Scripts/VideoPlayer360.cs`
- `unity_quest/Assets/Scripts/ESP32WebSocketClient.cs`
- `unity_quest/Assets/Scripts/QuestHeadsetEvents.cs`
- `unity_quest/UNITY_SETUP.md`

## Ordem de Execução

1. Começar com WebXR (teste rápido, validação conceito)
2. Testar no Quest 3S fisicamente
3. Validar integração ESP32 + vídeo 360°
4. Se aprovado, iniciar Unity para versão nativa
5. Comparar performance e experiência entre ambos

### To-dos

- [ ] Adicionar suporte WebXR API ao index.html
- [ ] Adaptar video-player.js para modo XR estereoscópico
- [ ] Criar quest-xr-integration.js para eventos headset
- [ ] Criar documentação WEBXR_SETUP.md
- [ ] Testar WebXR no Quest 3S com ESP32
- [ ] Criar projeto Unity com pacotes XR/Oculus
- [ ] Implementar VideoPlayer360.cs nativo
- [ ] Implementar ESP32WebSocketClient.cs
- [ ] Implementar QuestHeadsetEvents.cs
- [ ] Criar UI e controles Quest
- [ ] Configurar build Android e gerar APK