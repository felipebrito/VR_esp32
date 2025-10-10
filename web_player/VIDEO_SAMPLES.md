# 🎥 Vídeos 360° para Teste

## 📁 Arquivos de Exemplo

Para testar o player de vídeo 360°, você pode usar estes arquivos de exemplo:

### **Vídeos de Teste Gratuitos:**

1. **Sample Video 360°** (recomendado para teste)
   - **URL**: `https://sample-videos.com/zip/10/mp4/SampleVideo_360x240_1mb.mp4`
   - **Tamanho**: ~1MB
   - **Duração**: ~10 segundos
   - **Formato**: MP4

2. **Big Buck Bunny 360°**
   - **URL**: `https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4`
   - **Tamanho**: ~15MB
   - **Duração**: ~10 minutos
   - **Formato**: MP4

3. **Elephant Dream 360°**
   - **URL**: `https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4`
   - **Tamanho**: ~30MB
   - **Duração**: ~10 minutos
   - **Formato**: MP4

## 🔧 Como Usar

### **Método 1: URL Direta**
1. Abra o player web
2. Clique na área do vídeo
3. Cole uma das URLs acima
4. O vídeo será carregado automaticamente

### **Método 2: Arquivo Local**
1. Baixe um dos vídeos acima
2. Arraste o arquivo para a área do player
3. Ou clique na área e selecione o arquivo

### **Método 3: Seu Próprio Vídeo**
1. Prepare um vídeo 360° no formato MP4/WebM/OGG
2. Carregue usando drag & drop
3. Teste a reprodução e progresso

## 📋 Especificações Recomendadas

### **Para Vídeos 360°:**
- **Resolução**: 1920x1080 ou superior
- **Formato**: MP4 (H.264) ou WebM
- **Frame Rate**: 30fps
- **Duração**: 30 segundos a 5 minutos (para teste)
- **Tamanho**: Máximo 50MB (para carregamento rápido)

### **Projeção:**
- **Equirectangular**: Formato padrão para vídeos 360°
- **Monoscopic**: Adequado para teste
- **Estereoscopic**: Para experiência VR completa

## 🎯 Testes Sugeridos

### **1. Teste Básico:**
1. Carregar vídeo 360°
2. Simular Headset ON
3. Simular Play
4. Verificar progresso nos LEDs
5. Simular Pause
6. Simular Stop

### **2. Teste de Progresso:**
1. Iniciar reprodução
2. Observar LEDs acendendo progressivamente
3. Pausar em diferentes momentos
4. Verificar LEDs mantendo posição
5. Retomar e continuar progresso

### **3. Teste de Dois Players:**
1. Player 1: on1 → play1 → led1:X
2. Player 2: on2 → play2 → led2:X
3. Alternar entre players
4. Testar comandos independentes

## 🔗 Links Úteis

### **Recursos de Vídeo 360°:**
- [360° Video Samples](https://sample-videos.com/)
- [Google VR Samples](https://developers.google.com/vr/concepts/vrview)
- [YouTube 360° Videos](https://www.youtube.com/results?search_query=360+video)

### **Ferramentas de Criação:**
- [Insta360 Studio](https://www.insta360.com/download/insta360-studio)
- [Adobe Premiere Pro](https://www.adobe.com/products/premiere.html)
- [DaVinci Resolve](https://www.blackmagicdesign.com/products/davinciresolve)

## ⚠️ Notas Importantes

1. **CORS**: Vídeos de URLs externas podem ter restrições CORS
2. **Tamanho**: Vídeos grandes podem demorar para carregar
3. **Formato**: Nem todos os formatos são suportados por todos os navegadores
4. **Performance**: Vídeos 360° são mais pesados que vídeos normais
5. **Rede**: Conexão estável recomendada para vídeos online

## 🚀 Dicas de Performance

### **Para Melhor Performance:**
- Use vídeos com resolução adequada (não muito alta)
- Prefira formatos MP4 com H.264
- Mantenha duração curta para testes
- Use vídeos locais quando possível
- Feche outras abas do navegador

### **Para Debug:**
- Monitore o console do navegador (F12)
- Verifique logs do WebSocket
- Teste com diferentes tamanhos de vídeo
- Valide sincronização com ESP32

---

**Com estes recursos, você pode testar completamente o sistema antes de partir para Unity!** 🎯✨
