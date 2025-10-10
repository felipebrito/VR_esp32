# 🥽 Guia de Teste Manual - Player Web + ESP32

## ✅ Status Atual
- **ESP32 IP**: `192.168.15.6` ✅
- **Servidor HTTP**: Funcionando ✅
- **WebSocket**: Conectando ✅
- **Comandos**: Enviando (sem resposta de volta) ⚠️

## 🎯 Teste Manual com Player Web

### **1. Abrir Player Web**
```bash
# O servidor já está rodando em:
http://localhost:8000
```

### **2. Conectar ao ESP32**
1. Abra o navegador em `http://localhost:8000`
2. No campo "IP do ESP32", insira: `192.168.15.6`
3. Clique em "Conectar"
4. Aguarde a confirmação "Conectado"

### **3. Testar Comandos Básicos**

#### **Teste 1: Player 1 Ready**
- Clique em "Teste on1"
- **Resultado esperado**: LEDs 1-8 devem piscar verde
- **Observação**: Se não piscar, verifique conexão física dos LEDs

#### **Teste 2: Player 1 Playing**
- Clique em "Teste play1"
- **Resultado esperado**: LEDs 1-8 devem acender azul progressivamente (5s)
- **Observação**: Animação deve durar 5 segundos

#### **Teste 3: Progresso Direto**
- Clique em "Teste led1:50"
- **Resultado esperado**: LEDs 1-4 devem acender azul (50% = 4 LEDs)
- **Observação**: Deve ser instantâneo, sem animação

#### **Teste 4: Player 2**
- Clique em "Teste on2"
- **Resultado esperado**: LEDs 9-16 devem piscar verde
- Clique em "Teste play2"
- **Resultado esperado**: LEDs 9-16 devem acender vermelho progressivamente
- Clique em "Teste led2:75"
- **Resultado esperado**: LEDs 9-15 devem acender vermelho (75% = 6 LEDs)

### **4. Testar Simulação Meta Quest**

#### **Sequência Completa:**
1. **Headset ON**: Clique em "Headset ON" ou `Ctrl + H`
   - LEDs devem piscar verde
2. **App Focus**: Clique em "App Focus" ou `Ctrl + F`
   - Status deve mostrar "FOCUSED"
3. **Play**: Clique em "Play" ou `Ctrl + P`
   - LEDs devem acender progressivamente
4. **Pause**: Clique em "Pause" ou `Ctrl + P`
   - LEDs devem manter posição atual (dim)
5. **Stop**: Clique em "Stop" ou `Ctrl + S`
   - LEDs devem apagar

### **5. Testar Modo Automático**
- Clique em "🤖 Modo Automático" ou `Ctrl + A`
- **Resultado esperado**: Sequência automática de eventos
- **Duração**: ~25 segundos por ciclo

### **6. Testar Player de Vídeo 360°**

#### **Carregar Vídeo:**
1. Clique na área do player de vídeo
2. Selecione um arquivo de vídeo 360°
3. Ou use um dos exemplos do `VIDEO_SAMPLES.md`

#### **Testar Reprodução:**
1. Simule "Headset ON"
2. Clique em "Play" no player de vídeo
3. **Resultado esperado**: LEDs devem acender conforme progresso do vídeo
4. Teste pause/resume
5. Teste seek (arrastar slider)

## 🔧 Troubleshooting

### **Problema: LEDs não acendem**
**Soluções:**
1. Verificar conexão física:
   - LED Strip: GPIO 2 (D2)
   - Alimentação: 5V adequada
   - GND: Conexão comum
2. Verificar logs do ESP32 no monitor serial
3. Testar com comandos Python diretos

### **Problema: WebSocket não conecta**
**Soluções:**
1. Verificar IP do ESP32: `arp -a`
2. Testar ping: `ping 192.168.15.6`
3. Verificar firewall/antivírus
4. Tentar em navegador diferente

### **Problema: Comandos não funcionam**
**Soluções:**
1. Verificar se ESP32 está rodando firmware correto
2. Verificar logs no monitor serial
3. Testar com `curl` ou Python direto
4. Reiniciar ESP32

### **Problema: Vídeo não carrega**
**Soluções:**
1. Usar vídeos em formato MP4
2. Verificar tamanho do arquivo (< 50MB)
3. Testar com vídeos de exemplo
4. Verificar console do navegador (F12)

## 📊 Checklist de Testes

### **Conectividade:**
- [ ] ESP32 responde ao ping
- [ ] Servidor HTTP funciona
- [ ] WebSocket conecta
- [ ] Player web carrega

### **Comandos Básicos:**
- [ ] `on1` - LEDs piscam verde
- [ ] `play1` - Animação 5s azul
- [ ] `led1:X` - Progresso direto
- [ ] `on2` - LEDs piscam verde
- [ ] `play2` - Animação 5s vermelha
- [ ] `led2:X` - Progresso direto

### **Simulação Meta Quest:**
- [ ] Headset ON/OFF
- [ ] App Focus/Unfocus
- [ ] Play/Pause/Stop
- [ ] Modo automático

### **Player de Vídeo:**
- [ ] Carregar vídeo 360°
- [ ] Reprodução com progresso
- [ ] Controles funcionam
- [ ] Sincronização com LEDs

## 🎯 Próximos Passos

1. **Se todos os testes passarem**: ✅ Pronto para Unity!
2. **Se houver problemas**: 🔧 Debug e correção
3. **Se LEDs não funcionarem**: 🔌 Verificar hardware
4. **Se WebSocket falhar**: 🌐 Verificar rede

---

**Agora teste manualmente usando o player web!** 🚀
