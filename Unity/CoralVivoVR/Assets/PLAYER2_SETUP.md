# 🎮 Configuração Player 2 - Unity

## 📋 Passos para criar a cena Player 2:

### 1. **Duplicar a Cena Atual**
- Abra a cena atual (`VR player.unity`)
- Vá em `File > Save Scene As...`
- Salve como `VR player - Player2.unity`

### 2. **Configurar Player ID**
- Na cena duplicada, selecione o GameObject `CoralVivoVRManager`
- No componente `VRManager`, altere:
  - `Player Id` = **2** (em vez de 1)

### 3. **Configurar ESP32WebSocketClient**
- Selecione o GameObject `ESP32Manager`
- No componente `ESP32WebSocketClient`, altere:
  - `Player Id` = **2** (em vez de 1)
  - `Auto Detect Player Id` = **true** (recomendado)

### 4. **Adicionar Script de Configuração (Opcional)**
- Crie um GameObject vazio chamado `Player2Config`
- Adicione o componente `PlayerConfig`
- Configure:
  - `Target Player Id` = **2**
  - `Apply On Start` = **true**

### 5. **Build Settings**
- Vá em `File > Build Settings`
- Adicione a cena `VR player - Player2.unity` ao build
- Configure para Android (Quest)

## 🎯 Comportamento Esperado:

### **Player 1 (Cena Original)**
- **Botão 1**: Play/Pause/Stop
- **LEDs 0-7**: Azul (progresso)
- **Comandos LED**: `led1:X`

### **Player 2 (Cena Duplicada)**
- **Botão 2**: Play/Pause/Stop  
- **LEDs 8-15**: Vermelho (progresso)
- **Comandos LED**: `led2:X`

## 🔧 Teste Rápido:

1. **Abra a cena Player 1** no Unity
2. **Configure `Player Id = 1`** no VRManager
3. **Teste no Desktop Mode** - deve controlar LEDs 0-7 (azul)

4. **Abra a cena Player 2** no Unity  
5. **Configure `Player Id = 2`** no VRManager
6. **Teste no Desktop Mode** - deve controlar LEDs 8-15 (vermelho)

## 📱 Build para Quest:

### **Player 1 APK**
- Build Settings: `VR player.unity`
- Nome do APK: `CoralVivoVR_Player1.apk`

### **Player 2 APK**  
- Build Settings: `VR player - Player2.unity`
- Nome do APK: `CoralVivoVR_Player2.apk`

## 🎮 Controles ESP32:

- **Botão 1**: Controla apenas Player 1
- **Botão 2**: Controla apenas Player 2
- **LEDs 0-7**: Player 1 (azul)
- **LEDs 8-15**: Player 2 (vermelho)
- **LEDs Laranja**: Problema (desconectado/pausado)

## ✅ Verificação Final:

1. **Player 1** conecta e controla LEDs 0-7
2. **Player 2** conecta e controla LEDs 8-15  
3. **Botões** controlam players corretos
4. **LEDs laranja** funcionam para ambos
5. **Builds** geram APKs separados
