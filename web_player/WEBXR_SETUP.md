# CoralVivoVR - Setup WebXR Standalone

## Arquitetura Standalone

### Quest 3S (Aplicação Local)
- Aplicação WebXR rodando localmente no Quest
- Vídeo armazenado em Downloads/Pierre_Final.mov
- WebSocket Client conecta à ESP32

### ESP32 (Access Point + WebSocket)
- Rede WiFi: "CoralVivoVR" (senha: 12345678)
- IP fixo: 192.168.0.1
- Apenas WebSocket Server (não serve HTML)
- Controla 16 LEDs + 2 botões físicos

## Instalação no Quest 3S

### 1. Transferir Arquivos para Quest

**Opção A: Via USB (Recomendado)**
1. Conecte Quest ao PC via USB
2. Copie pasta `web_player/` para Quest
3. Copie vídeo `Pierre_Final.mov` para Downloads/

**Opção B: Via SideQuest**
1. Instale SideQuest no PC
2. Conecte Quest via USB
3. Use File Manager para transferir arquivos

**Opção C: Via Browser (Temporário)**
1. Quest conecta na rede CoralVivoVR
2. Acessa http://192.168.0.1:8000/quest-vr.html
3. Salva página offline (PWA)

### 2. Configuração da Rede

1. **Ligue a ESP32** - rede "CoralVivoVR" aparecerá
2. **No Quest**: Settings → WiFi → Conectar em "CoralVivoVR"
3. **Senha**: 12345678
4. **Aguarde conexão** - Quest mostrará "Conectado"

### 3. Executar Aplicação

**Se instalado localmente:**
1. Abra Quest Browser
2. File Manager → Local Files
3. Navegue até pasta web_player/
4. Abra quest-vr.html

**Se usando PWA:**
1. Abra Quest Browser
2. Acesse página salva
3. Instale como PWA se disponível

### 4. Conectar ao ESP32

1. **Clique "Conectar ESP32"**
2. **Digite IP**: 192.168.0.1 (já preenchido)
3. **Aguarde**: "Conectado" deve aparecer

### 5. Carregar Vídeo

1. **Clique "Selecionar Vídeo"**
2. **Escolha**: Downloads/Pierre_Final.mov
3. **Aguarde carregamento** (pode demorar)
4. **Vídeo pronto** quando aparecer "Vídeo carregado"

### 6. Entrar no VR

1. **Clique "🥽 Enter VR"**
2. **Permita acesso** quando solicitado pelo Quest
3. **Coloque o headset** - você estará dentro do vídeo 360°
4. **Use controles Quest** para navegar

## Controles VR

### Headset Quest
- **Colocar headset**: Automaticamente detecta e envia `on1` para ESP32
- **Remover headset**: Automaticamente detecta e envia `pause1` para ESP32
- **Foco da aplicação**: Detecta quando app ganha/perde foco

### Controles Quest
- **Trigger**: Interagir com botões na interface VR
- **Grip**: Segurar objetos (se implementado)
- **Joystick**: Navegar na interface VR
- **Botões A/B**: Controles adicionais

### Interface VR
- **Botões flutuantes**: Controles de play/pause/stop
- **Status**: Conexão ESP32 e modo VR
- **Logs**: Informações de debug em tempo real

## Troubleshooting

### WebXR não funciona
- ✅ **Verificar HTTPS**: WebXR só funciona com HTTPS
- ✅ **Verificar navegador**: Use Quest Browser oficial
- ✅ **Verificar suporte**: Nem todos os sites suportam WebXR

### Não conecta ao ESP32
- ✅ **Verificar IP**: ESP32 e Quest na mesma rede
- ✅ **Verificar WebSocket**: ESP32 deve estar rodando
- ✅ **Verificar firewall**: Porta 80 deve estar aberta

### Vídeo não carrega
- ✅ **Verificar formato**: Suporta MP4, MOV, WebM
- ✅ **Verificar tamanho**: Arquivos muito grandes podem demorar
- ✅ **Verificar memória**: Quest tem limitações de RAM

### Performance ruim
- ✅ **Reduzir qualidade**: Use vídeos menores para teste
- ✅ **Fechar apps**: Feche outros apps no Quest
- ✅ **Reiniciar**: Reinicie o Quest se necessário

## Logs e Debug

### Console do Navegador
- **Acesse**: Menu → Developer → Console
- **Veja logs**: Informações detalhadas de debug
- **Erros**: Problemas de conexão e carregamento

### Logs da Aplicação
- **Interface**: Logs aparecem na parte inferior da tela
- **Cores**: Verde (sucesso), Amarelo (aviso), Vermelho (erro)
- **Scroll**: Logs mais recentes aparecem no final

## Funcionalidades

### ✅ Implementado
- Player 360° com Three.js
- Suporte WebXR completo
- Detecção automática de headset on/off
- Integração WebSocket com ESP32
- Controles Quest nativos
- Interface VR otimizada

### 🔄 Em Desenvolvimento
- Controles de vídeo via Quest controllers
- UI flutuante em VR
- Múltiplos vídeos
- Configurações avançadas

### 📋 Planejado
- Gravação de sessões
- Estatísticas de uso
- Modo offline
- Sincronização multi-usuário

## Suporte

Para problemas ou dúvidas:
1. Verifique os logs da aplicação
2. Teste com vídeo menor primeiro
3. Verifique conexão ESP32
4. Reinicie Quest Browser se necessário

## Arquivos Importantes

- `quest-vr.html` - Interface principal VR
- `quest-vr-styles.css` - Estilos otimizados para VR
- `quest-vr-player.js` - Player 360° com WebXR
- `quest-vr-websocket.js` - Comunicação ESP32
- `quest-vr-integration.js` - Detecção de eventos
- `quest-vr-main.js` - Integração principal
