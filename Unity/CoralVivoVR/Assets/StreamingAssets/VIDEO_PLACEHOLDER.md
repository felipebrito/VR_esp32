# 🎬 VIDEO PLACEHOLDER

## Como Adicionar Seus Vídeos

### 1. Prepare os Arquivos
- **Formato**: MP4 (recomendado)
- **Resolução**: 1920x1080 ou menor
- **Duração**: Conforme necessário
- **Tamanho**: < 100MB por arquivo

### 2. Estrutura de Pastas
```
StreamingAssets/
├── videos/
│   ├── player1/
│   │   ├── intro.mp4
│   │   └── main_content.mp4
│   └── player2/
│       ├── outro.mp4
│       └── secondary_content.mp4
└── samples/
    └── test_video.mp4
```

### 3. Configuração no Unity
1. **Abra a cena** `passoapasso`
2. **Selecione o GameObject** com ESP32LEDTester
3. **Configure o VideoPlayer**:
   - **Source**: URL
   - **URL**: `file://{Application.streamingAssetsPath}/videos/player1/intro.mp4`

### 4. Teste
1. **Execute a cena**
2. **Pressione Botão 1** da ESP32
3. **Verifique** se o vídeo inicia
4. **Teste pause/resume** com os botões

## 🎮 Controles ESP32

| Botão | Ação | Comportamento |
|-------|------|---------------|
| **Botão 1 (Press)** | Play/Pause | Toggle vídeo |
| **Botão 1 (Long)** | Reset | Volta ao início |
| **Botão 2 (Press)** | Play/Pause | Mesmo que Botão 1 |
| **Botão 2 (Long)** | Reset | Mesmo que Botão 1 |

## 🔄 Estados dos LEDs

| Estado | LEDs | Quando |
|--------|------|--------|
| **PRONTO** | Verde fixo | Início, reset |
| **PLAYING** | Progressão azul/vermelho | Vídeo tocando |
| **PAUSED** | Azul/Vermelho escuro | Vídeo pausado |
| **CHASE** | Efeito chase | Perda de foco |

## ⚠️ Notas Importantes

- **Arquivos grandes** não são versionados no Git
- **Adicione vídeos** manualmente após clonar
- **Teste** com arquivos pequenos primeiro
- **Backup** seus vídeos em local seguro
