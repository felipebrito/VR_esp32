# 🧪 CoralVivoVR - Guia de Teste

## 📋 **Status Atual**
- ✅ Projeto Unity configurado
- ✅ Scripts implementados
- ✅ Cena configurada
- 🔄 **Teste da aplicação** (próximo passo)

## 🎯 **Testes por Etapas**

### **1. Teste no Editor Unity (Desktop)**

#### **Passo 1: Verificar Console**
1. **Play** no Unity Editor
2. **Console** deve mostrar:
   ```
   [ESP32WebSocketClient] ESP32WebSocketClient inicializado
   [CoralVivoVRManager] Inicializando CoralVivoVR...
   [Video360MeshGenerator] Gerando esfera customizada
   [CoralVivoVRManager] CoralVivoVR inicializado com sucesso!
   ```

#### **Passo 2: Verificar Mesh Customizada**
1. **Selecione** Video360Player na Hierarchy
2. **Inspector** → **Mesh Renderer** deve mostrar:
   - **Material**: Video360Material
   - **Mesh**: Video360CustomSphere (gerada automaticamente)

#### **Passo 3: Teste de Vídeo (Simulado)**
1. **Console** deve mostrar:
   ```
   [CoralVivoVRManager] Tentando carregar vídeo: Downloads/Pierre_Final [HighRes]-001.mov
   [CoralVivoVRManager] Testando caminho: /path/to/video
   [CoralVivoVRManager] ❌ Vídeo não encontrado em nenhum caminho
   ```
   *(Normal - vídeo não está no editor)*

### **2. Teste ESP32 (Físico)**

#### **Passo 1: Conectar ESP32**
1. **Conectar ESP32** via USB
2. **Upload firmware** atualizado:
   ```bash
   cd /Users/brito/Desktop/BIJARI_VR
   pio run -t upload
   ```

#### **Passo 2: Verificar Rede**
1. **ESP32** deve criar rede: `CoralVivoVR`
2. **Senha**: `12345678`
3. **IP**: `192.168.0.1`

#### **Passo 3: Teste de Conexão**
1. **Console Unity** deve mostrar:
   ```
   [ESP32WebSocketClient] Tentando conectar ao ESP32: ws://192.168.0.1/ws
   [ESP32WebSocketClient] Conectado ao ESP32!
   ```

### **3. Teste Quest 3S (Build APK)**

#### **Passo 1: Preparar Build**
1. **File** → **Build Settings**
2. **Platform**: Android
3. **Target Device**: Quest 3S
4. **Build**

#### **Passo 2: Instalar no Quest**
1. **Transferir APK** para Quest
2. **Instalar** via SideQuest ou ADB
3. **Executar** aplicação

#### **Passo 3: Teste Completo**
1. **Conectar Quest** à rede `CoralVivoVR`
2. **Executar** CoralVivoVR
3. **Testar botões** ESP32 (Play/Pause/Stop)
4. **Verificar LEDs** sincronizados

## 🔧 **Troubleshooting**

### **Problemas Comuns:**

#### **❌ "Vídeo não encontrado"**
- **Causa**: Arquivo não está no Quest
- **Solução**: Copiar `Pierre_Final [HighRes]-001.mov` para `/sdcard/Download/`

#### **❌ "ESP32 não conecta"**
- **Causa**: Rede não configurada
- **Solução**: Verificar SSID `CoralVivoVR` e IP `192.168.0.1`

#### **❌ "Mesh não aparece"**
- **Causa**: Video360MeshGenerator não executou
- **Solução**: Verificar `Generate On Start = true`

#### **❌ "Seams visíveis"**
- **Causa**: Usando Sphere padrão
- **Solução**: Remover Mesh Filter, usar Video360MeshGenerator

## 📱 **Teste de Vídeo Real**

### **Passo 1: Preparar Vídeo**
1. **Copiar** `Pierre_Final [HighRes]-001.mov` para Quest
2. **Localização**: `/sdcard/Download/Pierre_Final [HighRes]-001.mov`

### **Passo 2: Teste de Reprodução**
1. **Executar** aplicação no Quest
2. **Console** deve mostrar:
   ```
   [CoralVivoVRManager] ✅ Vídeo encontrado em: /sdcard/Download/Pierre_Final [HighRes]-001.mov
   [CoralVivoVRManager] Vídeo configurado: /sdcard/Download/Pierre_Final [HighRes]-001.mov
   ```

### **Passo 3: Teste de Controles**
1. **Botão 1 ESP32**: Play/Pause
2. **Botão 2 ESP32**: Play/Pause Player 2
3. **Long Press**: Stop
4. **LEDs**: Sincronizados com progresso

## 🎮 **Teste de VR**

### **Passo 1: Verificar Tracking**
1. **Movimento da cabeça** deve rotacionar a câmera
2. **Sem seams** visíveis no vídeo
3. **Projeção 360°** correta

### **Passo 2: Teste de Performance**
1. **FPS estável** (72fps Quest 3S)
2. **Sem stuttering** no vídeo
3. **LEDs responsivos**

## ✅ **Checklist de Teste**

- [ ] **Unity Editor**: Console sem erros
- [ ] **Mesh Customizada**: Gerada automaticamente
- [ ] **ESP32**: Conecta via WebSocket
- [ ] **Quest Build**: APK instalado
- [ ] **Vídeo**: Carrega do Downloads
- [ ] **Controles**: Botões ESP32 funcionam
- [ ] **LEDs**: Sincronizados com vídeo
- [ ] **VR**: Tracking suave, sem seams

## 🚀 **Próximo Passo**

Após testes bem-sucedidos:
1. **Otimizar performance**
2. **Ajustar configurações**
3. **Teste final completo**

---
**Status**: 🧪 Pronto para teste
**Próximo**: Executar testes por etapas
