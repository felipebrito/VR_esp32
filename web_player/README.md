# 🥽 VR Player Web Simulator

Um player web completo que simula eventos do Meta Quest para debug e teste do hardware ESP32 antes do desenvolvimento da aplicação Unity.

## ✨ Funcionalidades

### 🎮 Simulação de Eventos Meta Quest
- **Headset ON/OFF**: Simula quando o usuário coloca/remove o headset
- **App Focus**: Simula quando a aplicação ganha/perde foco
- **Player Status**: Estados DISCONNECTED → CONNECTED → READY → PLAYING → PAUSED
- **Progresso Automático**: Simula progresso de vídeo em tempo real

### 🎥 Player de Vídeo 360°
- **Suporte a vídeos 360°**: Carregamento via drag & drop ou seletor de arquivos
- **Controles completos**: Play, Pause, Stop, Seek
- **Integração com ESP32**: Progresso enviado automaticamente para LEDs
- **Visualização 3D**: Usando Three.js para renderização 360°

### 💡 Visualizador de LEDs ESP32
- **Visualização em tempo real**: Mostra status dos LEDs do ESP32
- **16 LEDs simulados**: 8 para Player 1 (azul) + 8 para Player 2 (vermelho)
- **Estados visuais**: READY (piscar verde), PLAYING (progresso), PAUSED (dim)
- **Sincronização**: LEDs web sincronizados com ESP32 real

### 🔌 Comunicação WebSocket
- **Conexão automática**: Reconexão automática em caso de falha
- **Comandos bidirecionais**: Envio e recebimento de comandos
- **Logs detalhados**: Histórico completo de comunicação
- **Testes integrados**: Botões para testar todos os comandos

## 🚀 Como Usar

### 1. **Abrir o Player**
```bash
# Navegar para a pasta do player
cd web_player

# Abrir index.html em um navegador moderno
# Ou usar um servidor local:
python -m http.server 8000
# Acessar: http://localhost:8000
```

### 2. **Conectar ao ESP32**
1. Insira o IP do ESP32 no campo "IP do ESP32"
2. Clique em "Conectar"
3. Aguarde a confirmação de conexão

### 3. **Simular Eventos Meta Quest**
- **Headset ON**: Clique em "Headset ON" ou `Ctrl + H`
- **App Focus**: Clique em "App Focus" ou `Ctrl + F`
- **Play**: Clique em "Play" ou `Ctrl + P`
- **Pause**: Clique em "Pause" ou `Ctrl + P`
- **Stop**: Clique em "Stop" ou `Ctrl + S`

### 4. **Carregar Vídeo 360°**
1. Clique na área do player de vídeo
2. Selecione um arquivo de vídeo 360°
3. Ou arraste um arquivo diretamente na área

### 5. **Modo Automático**
- Clique em "🤖 Modo Automático" ou `Ctrl + A`
- Sequência automática de eventos para teste completo

## ⌨️ Atalhos de Teclado

| Atalho | Ação |
|--------|------|
| `Ctrl + H` | Headset ON/OFF |
| `Ctrl + F` | App Focus ON/OFF |
| `Ctrl + P` | Play/Pause |
| `Ctrl + S` | Stop |
| `Ctrl + 1` | Player 1 |
| `Ctrl + 2` | Player 2 |
| `Ctrl + A` | Modo Automático |
| `Ctrl + T` | Teste de LEDs |
| `Ctrl + R` | Reset Tudo |

## 🔧 Comandos ESP32 Suportados

### **Enviados para ESP32:**
- `on1` / `on2` - Player ready (LEDs piscam verde)
- `play1` / `play2` - Iniciar reprodução
- `pause1` / `pause2` - Pausar reprodução
- `stop1` / `stop2` - Parar reprodução
- `led1:X` / `led2:X` - Progresso direto (0-100%)

### **Recebidos do ESP32:**
- `play1` / `play2` - ESP32 solicita play
- `pause1` / `pause2` - ESP32 solicita pause
- `stop1` / `stop2` - ESP32 solicita stop

## 📁 Estrutura de Arquivos

```
web_player/
├── index.html              # Interface principal
├── styles.css              # Estilos CSS
├── websocket-client.js     # Cliente WebSocket
├── quest-simulator.js      # Simulador de eventos Meta Quest
├── video-player.js         # Player de vídeo 360°
├── led-visualizer.js       # Visualizador de LEDs
├── main.js                 # Integração de módulos
└── README.md               # Este arquivo
```

## 🎯 Fluxo de Funcionamento

### **1. Simulação Meta Quest:**
```
Headset ON → on1 → ESP32 → LEDs piscam verde
App Focus → on1 → ESP32 → Player READY
Play → play1 → ESP32 → Animação 5s
Progresso → led1:X → ESP32 → LEDs acendem progressivamente
```

### **2. Player de Vídeo:**
```
Carregar vídeo → Criar textura 360° → Three.js renderiza
Play → Enviar progresso → ESP32 → LEDs sincronizados
Pause → Manter progresso → LEDs ficam na posição atual
Stop → led1:0 → ESP32 → LEDs apagam
```

### **3. Visualização LEDs:**
```
ESP32 envia comando → WebSocket recebe → LED Visualizer atualiza
on1 → LEDs piscam verde (READY)
led1:50 → 4 LEDs azuis acendem (50% Player 1)
play1 → Para piscar, inicia progresso
```

## 🐛 Debug e Testes

### **Botões de Teste:**
- **Teste on1**: Simula Player 1 ready
- **Teste play1**: Simula Player 1 playing
- **Teste led1:50**: Simula progresso 50%
- **Teste on2**: Simula Player 2 ready
- **Teste play2**: Simula Player 2 playing
- **Teste led2:75**: Simula progresso 75%

### **Logs Detalhados:**
- Todos os eventos são logados com timestamp
- Tipos: INFO, SUCCESS, WARNING, ERROR
- Histórico dos últimos 100 eventos
- Botão "Limpar Logs" para resetar

### **Informações de Debug:**
- Player atual (1 ou 2)
- Status do modo automático
- Status da conexão WebSocket
- Status do player de vídeo

## 🔗 Integração com ESP32

### **Pré-requisitos:**
1. ESP32 com firmware carregado
2. ESP32 conectado à mesma rede WiFi
3. WebSocket server rodando na porta 80

### **Configuração:**
1. Descobrir IP do ESP32: `arp -a` ou monitor serial
2. Inserir IP no campo "IP do ESP32"
3. Conectar e testar comunicação

### **Comandos de Teste:**
```bash
# Testar conexão HTTP
curl http://192.168.1.100/

# Testar WebSocket (usando wscat)
wscat -c ws://192.168.1.100/ws
```

## 🎨 Personalização

### **Cores dos LEDs:**
```css
.led.player1 {
    background: #4299e1; /* Azul Player 1 */
}

.led.player2 {
    background: #f56565; /* Vermelho Player 2 */
}

.led.ready {
    background: #48bb78; /* Verde READY */
}
```

### **Configurações:**
```javascript
// Alterar player padrão
window.vrPlayerApp.setCurrentPlayer(2);

// Ativar modo automático
window.vrPlayerApp.toggleAutoMode();

// Testar LEDs
window.ledVisualizer.testAllLEDs();
```

## 🚀 Próximos Passos

1. **Testar com ESP32 real** usando este player web
2. **Validar comunicação** WebSocket e comandos
3. **Ajustar timing** e comportamentos conforme necessário
4. **Desenvolver aplicação Unity** baseada nos eventos testados
5. **Integrar Unity** com ESP32 usando os mesmos comandos

## 📝 Notas Importantes

- **Navegador**: Use Chrome/Firefox/Safari moderno
- **HTTPS**: Alguns recursos podem precisar de HTTPS em produção
- **CORS**: Vídeos locais podem ter restrições de CORS
- **Performance**: Vídeos 360° grandes podem impactar performance
- **Rede**: ESP32 e navegador devem estar na mesma rede

---

**Este player web é perfeito para debug do hardware ESP32 antes de partir para o desenvolvimento Unity!** 🎯✨
