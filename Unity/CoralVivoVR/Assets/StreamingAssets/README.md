# StreamingAssets - Vídeos do Projeto

Esta pasta contém os arquivos de vídeo que são reproduzidos pelo VideoPlayer do Unity.

## 📁 Estrutura Recomendada

```
StreamingAssets/
├── README.md (este arquivo)
├── VIDEO_PLACEHOLDER.md (instruções)
├── videos/
│   ├── player1/
│   │   └── video1.mp4
│   └── player2/
│       └── video2.mp4
└── samples/
    └── sample_video.mp4
```

## 🎬 Formatos Suportados

- **MP4** (recomendado)
- **WebM**
- **AVI**
- **MOV**

## ⚠️ Importante

- **Arquivos grandes** são ignorados pelo Git (.gitignore)
- **Adicione seus vídeos** manualmente após clonar o projeto
- **Mantenha arquivos pequenos** para testes (< 50MB)

## 🔧 Configuração no Unity

1. **Adicione vídeos** nesta pasta
2. **Configure VideoPlayer** para usar `Application.streamingAssetsPath`
3. **Teste** com os botões ESP32

## 📝 Exemplo de Código

```csharp
VideoPlayer videoPlayer = GetComponent<VideoPlayer>();
string videoPath = Path.Combine(Application.streamingAssetsPath, "videos", "player1", "video1.mp4");
videoPlayer.url = videoPath;
```

## 🎮 Controle via ESP32

- **Botão 1 (Press Curto)**: Play/Pause
- **Botão 1 (Long Press)**: Reset
- **Botão 2**: Mesmo comportamento
- **Perda de foco**: Pausa e continua de onde parou
