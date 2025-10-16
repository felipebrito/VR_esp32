#include <WiFi.h>
#include <DNSServer.h>
#include <ESPAsyncWebServer.h>
#include <ArduinoJson.h>
#include <FastLED.h>

// Hardware Configuration
#define LED_PIN 2
#define BUTTON_PLAY_PAUSE 4
#define BUTTON_EFFECT_STOP 5
#define NUM_LEDS 16
#define PLAYER1_LEDS 8
#define PLAYER2_LEDS 8

// Debug Configuration
#define DEBUG_MODE true
#define DEBUG_WEBSOCKET true
#define DEBUG_BUTTONS true
#define DEBUG_LEDS true

// Debug Macros
#if DEBUG_MODE
  #define DEBUG_PRINT(x) Serial.print(x)
  #define DEBUG_PRINTLN(x) Serial.println(x)
#else
  #define DEBUG_PRINT(x)
  #define DEBUG_PRINTLN(x)
#endif

// Button Configuration
const unsigned long DEBOUNCE_DELAY = 50;
const unsigned long LONG_PRESS_TIME = 1000;

// WiFi Configuration - Access Point P2P
const char* apSSID = "CoralVivoVR";
const char* apPassword = "12345678";
IPAddress apIP(192, 168, 0, 1);

// DNS Server para capturar todas as requisições
DNSServer dnsServer;

// LED Array
CRGB leds[NUM_LEDS];

// Button State Structure
struct ButtonState {
  bool pressed = false;
  bool lastPressed = false;
  unsigned long lastDebounceTime = 0;
  unsigned long pressTime = 0;
  bool longPressDetected = false;
};

// Button States
ButtonState buttonPlayPause;
ButtonState buttonEffectStop;

// Servidor único (porta 80) - HTTP + WebSocket
AsyncWebServer server(80);
AsyncWebSocket ws("/ws");

// Player States
enum PlayerState {
  DISCONNECTED,
  CONNECTED,
  READY,
  PLAYING,
  PAUSED,
  PAUSED_BY_HEADSET
};

struct Player {
  PlayerState state;
  bool connected;
  float progress; // 0.0 to 1.0
  unsigned long lastUpdate;
  AsyncWebSocketClient* client;
};

Player players[2] = {
  {DISCONNECTED, false, 0.0, 0, nullptr},
  {DISCONNECTED, false, 0.0, 0, nullptr}
};


// LED Effects
enum LEDEffect {
  EFFECT_PROGRESS,
  EFFECT_READY_BLINK,
  EFFECT_RAINBOW,
  EFFECT_PULSE,
  EFFECT_CHASE,
  EFFECT_SOLID
};

LEDEffect currentEffect = EFFECT_PROGRESS;
unsigned long lastEffectUpdate = 0;
int effectStep = 0;

// Animation Timing
const unsigned long READY_BLINK_INTERVAL = 500;
const unsigned long EFFECT_UPDATE_INTERVAL = 100;

// Function prototypes
void onWebSocketEvent(AsyncWebSocket *server, AsyncWebSocketClient *client, AwsEventType type, void *arg, uint8_t *data, size_t len);
void handleWebSocketMessage(uint8_t clientNum, char* message);
void handlePlayerDisconnect(uint8_t clientNum);
void sendCommandToPlayer(int playerId, String command);
void handleButtons();
void handleButton(int pin, ButtonState* button, void (*onPress)(), void (*onLongPress)());
void cycleEffect();
void updatePlayerStates();
void updateLEDs();
void updateProgressLEDs();
void updateReadyBlinkLEDs();
void updateRainbowLEDs();
void updatePulseLEDs();
void updateChaseLEDs();
void updateSolidLEDs();
void clearAllLEDs();

void setup() {
  Serial.begin(115200);
  
  // Initialize LEDs
  FastLED.addLeds<WS2812B, LED_PIN, GRB>(leds, NUM_LEDS);
  FastLED.setBrightness(50);
  clearAllLEDs();
  
  // Initialize buttons with internal pull-up
  pinMode(BUTTON_PLAY_PAUSE, INPUT_PULLUP);
  pinMode(BUTTON_EFFECT_STOP, INPUT_PULLUP);
  
  // Setup Access Point P2P
  WiFi.mode(WIFI_AP);
  WiFi.softAPConfig(apIP, apIP, IPAddress(255, 255, 255, 0));
  WiFi.softAP(apSSID, apPassword, 1, 0, 8); // Canal 1, rede visível, até 8 dispositivos
  delay(100);
  
  Serial.println("Access Point CoralVivoVR iniciado!");
  Serial.print("SSID: ");
  Serial.println(apSSID);
  Serial.print("IP do AP: ");
  Serial.println(WiFi.softAPIP());
  
  // Iniciar DNS Server para capturar todas as requisições
  dnsServer.start(53, "*", apIP);
  
  // Configurar WebSocket
  ws.onEvent(onWebSocketEvent);
  server.addHandler(&ws);
  
  // Servir aplicação WebXR diretamente
  server.on("/", HTTP_GET, [](AsyncWebServerRequest *request){
    Serial.println("[HTML] / - Servindo aplicação WebXR");
    request->send(200, "text/html", "<html><body><h1>CoralVivoVR</h1><p>WebSocket Server Ready</p></body></html>");
  });

  // Servir arquivos estáticos
  server.on("/quest-vr.html", HTTP_GET, [](AsyncWebServerRequest *request){
    Serial.println("[HTML] /quest-vr.html");
    request->send(200, "text/html", "<html><body><h1>CoralVivoVR</h1><p>WebSocket Server Ready</p></body></html>");
  });

  // Qualquer outra rota → 204 (evitar desconexão)
  server.onNotFound([](AsyncWebServerRequest *request){
    String uri = request->url();
    Serial.print("[204] ");
    Serial.println(uri);
    request->send(204, "text/plain", "");
  });
  
  // Iniciar servidor único
  server.begin();
  Serial.println("ESP32 VR LED Controller Ready!");
  Serial.println("HTTP + WebSocket server started on port 80");
}

void loop() {
  // Processar DNS Server para capturar requisições
  dnsServer.processNextRequest();
  
  ws.cleanupClients();
  handleButtons();
  updateLEDs();
  updatePlayerStates();
  
  // Exibir número de dispositivos conectados periodicamente
  static unsigned long lastCheck = 0;
  const unsigned long checkInterval = 5000; // Verificar a cada 5 segundos
  
  if (millis() - lastCheck > checkInterval) {
    int connected = WiFi.softAPgetStationNum();
    Serial.print("Dispositivos conectados: ");
    Serial.println(connected);
    lastCheck = millis();
  }
  
  delay(10);
}

void onWebSocketEvent(AsyncWebSocket *server, AsyncWebSocketClient *client, AwsEventType type, void *arg, uint8_t *data, size_t len) {
  switch(type) {
    case WS_EVT_CONNECT:
      Serial.printf("🔌 Client %u connected from %s\n", client->id(), client->remoteIP().toString().c_str());
      Serial.printf("📊 Total connected clients: %d\n", ws.getClients().size());
      break;
      
    case WS_EVT_DISCONNECT:
      Serial.printf("🔌 Client %u disconnected\n", client->id());
      Serial.printf("📊 Total connected clients: %d\n", ws.getClients().size());
      handlePlayerDisconnect(client->id());
      break;
      
    case WS_EVT_DATA:
      if (len > 0) {
        data[len] = '\0';
        Serial.printf("📨 Received from client %u: %s\n", client->id(), (char*)data);
        handleWebSocketMessage(client->id(), (char*)data);
      }
      break;
      
    default:
      break;
  }
}

void handleWebSocketMessage(uint8_t clientNum, char* message) {
  Serial.printf("📨 Raw message received (client %d): %s\n", clientNum, message);
  
  // All commands are now processed via JSON
  // Try to parse as JSON
  StaticJsonDocument<200> doc;
  DeserializationError error = deserializeJson(doc, message);
  
  if (error) {
    Serial.printf("❌ JSON parsing failed: %s\n", error.c_str());
    return;
  }
  
  Serial.printf("✅ JSON parsed successfully\n");
  
  // Handle JSON command messages
  if (doc.containsKey("command")) {
    String command = doc["command"];
    int playerId = doc.containsKey("player") ? doc["player"] : 0; // 0 = broadcast
    
    Serial.printf("🎮 Received JSON command: %s (Player: %d)\n", command.c_str(), playerId);
    
    // Handle heartbeat messages
    if (command == "heartbeat") {
      Serial.printf("💓 Heartbeat received from Player %d\n", playerId);
      return;
    }
    
    // Handle ready status
    if (command == "ready") {
      Serial.printf("✅ Player %d ready\n", playerId);
      if (playerId == 1) {
        players[0].connected = true;
        players[0].state = READY;
        players[0].progress = 0.0;
        players[0].lastUpdate = millis();
      } else if (playerId == 2) {
        players[1].connected = true;
        players[1].state = READY;
        players[1].progress = 0.0;
        players[1].lastUpdate = millis();
      }
      return;
    }
    
    if (command == "on1") {
      players[0].state = READY;
      players[0].connected = true;
      players[0].progress = 0.0;
      Serial.println("Player 1 ready - green blinking");
    }
    else if (command == "play1") {
      players[0].state = PLAYING;
      players[0].progress = 0.0;
      players[0].lastUpdate = millis(); // Set start time for 5s animation
      Serial.println("Player 1 playing - 5s progress animation");
    }
    else if (command == "on2") {
      players[1].state = READY;
      players[1].connected = true;
      players[1].progress = 0.0;
      Serial.println("Player 2 ready - green blinking");
    }
    else if (command == "play2") {
      players[1].state = PLAYING;
      players[1].progress = 0.0;
      players[1].lastUpdate = millis(); // Set start time for 5s animation
      Serial.println("Player 2 playing - 5s progress animation");
    }
    return;
  }
  
  // Handle JSON message field (led1:X, led2:X commands)
  if (doc.containsKey("message")) {
    String message = doc["message"];
    
    Serial.printf("📨 JSON message: %s\n", message.c_str());
    
    // Process led1:X or led2:X commands from JSON message
    if (message.startsWith("led") && message.indexOf(':') > 0) {
      int colonIndex = message.indexOf(':');
      String ledPart = message.substring(3, colonIndex); // Get "1" or "2" from "led1" or "led2"
      String valuePart = message.substring(colonIndex + 1); // Get progress value
      
      int ledPlayerNumber = ledPart.toInt(); // This is 1 or 2
      int progressValue = valuePart.toInt();
      
      Serial.printf("🔍 Parsed: ledPlayerNumber=%d, progressValue=%d\n", ledPlayerNumber, progressValue);
      
      // Use ledPlayerNumber (from led1/led2), NOT playerId from JSON
      if (ledPlayerNumber >= 1 && ledPlayerNumber <= 2 && progressValue >= 0 && progressValue <= 100) {
        int playerIndex = ledPlayerNumber - 1; // 0 for Player 1, 1 for Player 2
        
        players[playerIndex].progress = progressValue / 100.0;
        players[playerIndex].state = PLAYING;
        players[playerIndex].lastUpdate = millis();
        players[playerIndex].connected = true;
        
        Serial.printf("✅ Player %d: Set progress to %d%% (playerIndex=%d, LEDs=%d)\n", 
                     ledPlayerNumber, progressValue, playerIndex,
                     ledPlayerNumber == 1 ? (int)(progressValue * PLAYER1_LEDS / 100) : (int)(progressValue * PLAYER2_LEDS / 100));
        
        return; // IMPORTANT: Return here to prevent double processing
      } else {
        Serial.printf("❌ Invalid LED command: ledPlayerNumber=%d, progressValue=%d\n", ledPlayerNumber, progressValue);
      }
    }
  }
  
  // Handle legacy JSON messages
  int playerId = doc["player"];
  if (playerId < 1 || playerId > 2) {
    Serial.println("Invalid player ID");
    return;
  }
  
  int playerIndex = playerId - 1;
  String status = doc["status"];
  
  Serial.printf("📊 Received JSON status: %s (Player: %d)\n", status.c_str(), playerId);
  
  if (status == "ready") {
    players[playerIndex].state = READY;
    players[playerIndex].connected = true;
    // Find the client by ID
    AsyncWebSocketClient* client = nullptr;
    for (auto& c : ws.getClients()) {
      if (c.id() == clientNum) {
        client = &c;
        break;
      }
    }
    players[playerIndex].client = client;
    Serial.printf("Player %d is ready\n", playerId);
  }
  else if (status == "playing") {
    if (doc.containsKey("progress")) {
      players[playerIndex].progress = doc["progress"];
    }
    players[playerIndex].state = PLAYING;
    Serial.printf("Player %d is playing (progress: %.1f%%)\n", playerId, players[playerIndex].progress * 100);
  }
  else if (status == "paused") {
    players[playerIndex].state = PAUSED;
    Serial.printf("Player %d is paused\n", playerId);
  }
  else if (status == "stopped") {
    players[playerIndex].state = READY;
    players[playerIndex].progress = 0.0;
    Serial.printf("Player %d stopped\n", playerId);
  }
  else if (status == "heartbeat") {
    Serial.printf("💓 Heartbeat received from Player %d\n", playerId);
    // Update last activity timestamp to keep connection alive
    players[playerIndex].lastUpdate = millis();
    return; // Don't update other fields for heartbeat
  }
  
  players[playerIndex].lastUpdate = millis();
}

void handlePlayerDisconnect(uint8_t clientNum) {
  for (int i = 0; i < 2; i++) {
    if (players[i].client && players[i].client->id() == clientNum) {
      players[i].state = DISCONNECTED;
      players[i].connected = false;
      players[i].client = nullptr;
      players[i].progress = 0.0;
      Serial.printf("Player %d disconnected\n", i + 1);
      break;
    }
  }
}

void sendCommandToPlayer(int playerId, String command) {
  Serial.println("=== SENDCOMMANDTOPLAYER CHAMADA ===");
  Serial.printf("SENDCMD: Tentando enviar comando '%s' para player %d\n", command.c_str(), playerId);
  
  int playerIndex = playerId - 1;
  Serial.printf("Player %d - connected: %s, client: %s\n", playerId, 
                players[playerIndex].connected ? "true" : "false",
                players[playerIndex].client ? "true" : "false");
  
  // Verificar clientes conectados
  Serial.printf("Total de clientes WebSocket conectados: %d\n", ws.getClients().size());
  
  // DEBUG: Listar todos os clientes
  Serial.println("=== CLIENTES CONECTADOS ===");
  for (auto& client : ws.getClients()) {
    Serial.printf("Cliente ID: %u, IP: %s\n", client.id(), client.remoteIP().toString().c_str());
  }
  
  // Enviar comando se houver pelo menos um cliente conectado
  if (ws.getClients().size() > 0) {
    StaticJsonDocument<100> doc;
    doc["command"] = command;
    doc["player"] = playerId;
    
    String message;
    serializeJson(doc, message);
    
    Serial.printf("Enviando mensagem: %s\n", message.c_str());
    
    // Enviar para todos os clientes conectados
    for (auto& client : ws.getClients()) {
      ws.text(client.id(), message);
      Serial.printf("Enviado para cliente %u\n", client.id());
    }
    Serial.printf("✅ Comando '%s' enviado para player %d (broadcast para %d clientes)\n", command.c_str(), playerId, ws.getClients().size());
  } else {
    Serial.printf("❌ ERRO: Nenhum cliente WebSocket conectado\n");
  }
}

void handleButtons() {
  // Debug: verificar estado dos pinos
  static unsigned long lastDebugTime = 0;
  if (millis() - lastDebugTime > 5000) { // A cada 5 segundos
    Serial.printf("🔍 DEBUG: Pino %d = %d, Pino %d = %d\n", 
                  BUTTON_PLAY_PAUSE, digitalRead(BUTTON_PLAY_PAUSE),
                  BUTTON_EFFECT_STOP, digitalRead(BUTTON_EFFECT_STOP));
    lastDebugTime = millis();
  }
  
  handleButton(BUTTON_PLAY_PAUSE, &buttonPlayPause, []() {
    // Short press: toggle play/pause for both players
    DEBUG_PRINTLN("=== BUTTON 1 PRESSED ===");
    DEBUG_PRINTLN("Button 1 pressed - toggling play/pause");
    Serial.printf("Current time: %lu\n", millis());
    
    for (int i = 0; i < 2; i++) {
      Serial.printf("Player %d state: %d\n", i + 1, players[i].state);
      
      if (players[i].state == PLAYING) {
        players[i].state = PAUSED;
        sendCommandToPlayer(i + 1, "pause");
        Serial.printf("Player %d paused\n", i + 1);
      } else if (players[i].state == PAUSED) {
        // Resume from where it was paused - Unity controla progresso via led1:X
        players[i].state = PLAYING;
        players[i].lastUpdate = millis(); // Atualizar timestamp para modo manual
        sendCommandToPlayer(i + 1, "play");
        Serial.printf("Player %d resumed\n", i + 1);
      } else if (players[i].state == READY || players[i].state == DISCONNECTED) {
        // Start new playback - Unity controla progresso via led1:X
        players[i].state = PLAYING;
        players[i].progress = 0.0;
        players[i].lastUpdate = millis(); // Atualizar timestamp para modo manual
        sendCommandToPlayer(i + 1, "play");
        Serial.printf("Player %d started\n", i + 1);
      }
    }
    Serial.println("=== BUTTON 1 PROCESSED ===");
  }, []() {
    // Long press: stop all players and turn off LEDs
    Serial.println("=== BUTTON 1 LONG PRESS ===");
    Serial.println("Button 1 long press - stopping all players");
    for (int i = 0; i < 2; i++) {
      players[i].state = READY; // Voltar para READY para permitir reinício
      players[i].progress = 0.0;
      players[i].lastUpdate = millis(); // Atualizar timestamp
      sendCommandToPlayer(i + 1, "stop");
    }
    Serial.println("All players stopped and ready to restart");
    Serial.println("=== BUTTON 1 LONG PRESS PROCESSED ===");
  });
  
  handleButton(BUTTON_EFFECT_STOP, &buttonEffectStop, []() {
    // Short press: control Player 2 (play/pause)
    Serial.println("=== BUTTON 2 PRESSED ===");
    Serial.println("Button 2 pressed - controlling Player 2");
    if (players[1].state == PLAYING) {
      players[1].state = PAUSED;
      sendCommandToPlayer(2, "pause");
      Serial.println("Player 2 paused");
    } else if (players[1].state == PAUSED) {
      // Resume from where it was paused - usar modo manual (lastUpdate = 0)
      players[1].state = PLAYING;
      players[1].lastUpdate = 0; // Modo manual - Unity controla progresso
      sendCommandToPlayer(2, "play");
      Serial.println("Player 2 resumed");
    } else if (players[1].state == READY || players[1].state == DISCONNECTED) {
      // Start new playback - usar modo manual (lastUpdate = 0)
      players[1].state = PLAYING;
      players[1].progress = 0.0;
      players[1].lastUpdate = 0; // Modo manual - Unity controla progresso
      sendCommandToPlayer(2, "play");
      Serial.println("Player 2 started");
    }
    Serial.println("=== BUTTON 2 PROCESSED ===");
  }, []() {
    // Long press: reset all players and turn off LEDs
    Serial.println("=== BUTTON 2 LONG PRESS ===");
    Serial.println("Button 2 long press - resetting all players");
    for (int i = 0; i < 2; i++) {
      players[i].state = READY; // Voltar para READY para permitir reinício
      players[i].progress = 0.0;
      players[i].lastUpdate = 0; // Modo manual
      sendCommandToPlayer(i + 1, "stop");
    }
    Serial.println("All players reset and ready to restart");
    Serial.println("=== BUTTON 2 LONG PRESS PROCESSED ===");
  });
}

void handleButton(int pin, ButtonState* button, void (*onPress)(), void (*onLongPress)()) {
  bool reading = !digitalRead(pin); // Inverted because of pull-up
  
  if (reading != button->lastPressed) {
    button->lastDebounceTime = millis();
  }
  
  if ((millis() - button->lastDebounceTime) > DEBOUNCE_DELAY) {
    if (reading != button->pressed) {
      button->pressed = reading;
      
      if (button->pressed) {
        button->pressTime = millis();
        button->longPressDetected = false;
        onPress();
      } else {
        button->longPressDetected = false;
      }
    }
  }
  
  // Check for long press
  if (button->pressed && !button->longPressDetected && onLongPress) {
    if ((millis() - button->pressTime) > LONG_PRESS_TIME) {
      button->longPressDetected = true;
      onLongPress();
    }
  }
  
  button->lastPressed = reading;
}

void cycleEffect() {
  currentEffect = (LEDEffect)((currentEffect + 1) % 6);
  effectStep = 0;
  Serial.printf("Changed effect to: %d\n", currentEffect);
}

void updatePlayerStates() {
  unsigned long now = millis();
  
  for (int i = 0; i < 2; i++) {
    // Check for timeout (5 seconds without update)
    if (players[i].connected && (now - players[i].lastUpdate) > 5000) {
      players[i].state = DISCONNECTED;
      players[i].connected = false;
      players[i].client = nullptr;
      Serial.printf("Player %d timed out\n", i + 1);
    }
  }
}

void updateLEDs() {
  unsigned long now = millis();
  
  if (now - lastEffectUpdate < EFFECT_UPDATE_INTERVAL) {
    return;
  }
  lastEffectUpdate = now;
  
  clearAllLEDs();
  
  // Update based on current effect
  switch (currentEffect) {
    case EFFECT_PROGRESS:
      updateProgressLEDs();
      break;
    case EFFECT_READY_BLINK:
      updateReadyBlinkLEDs();
      break;
    case EFFECT_RAINBOW:
      updateRainbowLEDs();
      break;
    case EFFECT_PULSE:
      updatePulseLEDs();
      break;
    case EFFECT_CHASE:
      updateChaseLEDs();
      break;
    case EFFECT_SOLID:
      updateSolidLEDs();
      break;
  }
  
  FastLED.show();
}

void updateProgressLEDs() {
  unsigned long now = millis();
  
  // Verificar perda de conexão com Unity (Player 1)
  if (players[0].connected && players[0].state == PLAYING && (now - players[0].lastUpdate) > 15000) { // 15 segundos sem comunicação
    // Perda de conexão detectada - piscar LED laranja/vermelho
    static unsigned long lastBlink = 0;
    static bool blinkState = false;
    
    if (now - lastBlink > 500) { // Piscar a cada 500ms
      blinkState = !blinkState;
      lastBlink = now;
      
      if (blinkState) {
        // LED laranja/vermelho para indicar perda de conexão
        for (int i = 0; i < PLAYER1_LEDS; i++) {
          leds[i] = CRGB(255, 100, 0); // Laranja
        }
      } else {
        // Apagar LEDs
        for (int i = 0; i < PLAYER1_LEDS; i++) {
          leds[i] = CRGB::Black;
        }
      }
    }
    return; // Não processar progresso normal durante perda de conexão
  }
  
  // Player 1 progress (LEDs 1-8, left to right)
  if (players[0].state == PLAYING) {
    // Check if using manual progress control (Unity sends led1:X commands)
    // Se lastUpdate foi atualizado recentemente (< 5 segundos), é modo manual
    if ((now - players[0].lastUpdate) < 5000) {
      // Manual control via led1:X commands - show exact progress with gradual intensity
      float totalLEDs = players[0].progress * PLAYER1_LEDS;
      int fullLEDs = (int)totalLEDs;
      float partialLED = totalLEDs - fullLEDs;
      
      // SEMPRE acender pelo menos o primeiro LED quando há progresso > 0
      if (players[0].progress > 0) {
        Serial.printf("🔵 PLAYER 1 LED UPDATE: Progresso %.2f, totalLEDs %.2f, fullLEDs %d\n", 
                     players[0].progress, totalLEDs, fullLEDs);
        
        // Acender LEDs completos com intensidade baseada no progresso
        for (int i = 0; i < fullLEDs; i++) {
          // Calcular intensidade baseada no progresso do LED atual
          float ledProgress = (i + 1) / (float)PLAYER1_LEDS; // Progresso deste LED (0.125, 0.25, etc.)
          float intensity = 0.2 + (0.8 * ledProgress); // 20% a 100% de intensidade
          int brightness = (int)(255 * intensity);
          leds[i] = CRGB(0, 0, brightness);
          Serial.printf("🔵 LED %d acendido com intensidade %d\n", i, brightness);
        }
        
        // Acender LED parcial com luminosidade proporcional
        if (fullLEDs < PLAYER1_LEDS && partialLED > 0) {
          float ledProgress = (fullLEDs + 1) / (float)PLAYER1_LEDS; // Progresso deste LED
          float intensity = 0.2 + (0.8 * ledProgress); // 20% a 100% de intensidade
          int brightness = (int)(255 * intensity * partialLED);
          leds[fullLEDs] = CRGB(0, 0, brightness);
          Serial.printf("🔵 LED %d parcial acendido com intensidade %d\n", fullLEDs, brightness);
        }
        
        // Se não há LEDs completos mas há progresso, acender primeiro LED com intensidade mínima
        if (fullLEDs == 0 && players[0].progress > 0) {
          int brightness = (int)(255 * 0.2); // 20% de intensidade mínima
          leds[0] = CRGB(0, 0, brightness);
          Serial.printf("🔵 LED 0 acendido com intensidade mínima %d\n", brightness);
        }
      }
    } else {
      // Automatic animation mode
      unsigned long playTime = now - players[0].lastUpdate;
      if (playTime < 6000) { // Only animate for first 6 seconds after play command
        float progress = min(1.0, playTime / 5000.0); // 5 seconds = 5000ms
        
        // Progresso suave com luminosidade gradual
        float totalLEDs = progress * PLAYER1_LEDS;
        int fullLEDs = (int)totalLEDs;
        float partialLED = totalLEDs - fullLEDs; // Parte fracionária do próximo LED
        
        // Acender LEDs completos
        for (int i = 0; i < fullLEDs; i++) {
          leds[i] = CRGB::Blue;
        }
        
        // Acender LED parcial com luminosidade proporcional
        if (fullLEDs < PLAYER1_LEDS && partialLED > 0) {
          int brightness = (int)(255 * partialLED);
          leds[fullLEDs] = CRGB(0, 0, brightness); // Azul com luminosidade variável
        }
        
        // Update progress for display
        players[0].progress = progress;
      } else {
        // Show current progress without animation - também com progresso suave
        float totalLEDs = players[0].progress * PLAYER1_LEDS;
        int fullLEDs = (int)totalLEDs;
        float partialLED = totalLEDs - fullLEDs;
        
        // Acender LEDs completos
        for (int i = 0; i < fullLEDs; i++) {
          leds[i] = CRGB::Blue;
        }
        
        // Acender LED parcial com luminosidade proporcional
        if (fullLEDs < PLAYER1_LEDS && partialLED > 0) {
          int brightness = (int)(255 * partialLED);
          leds[fullLEDs] = CRGB(0, 0, brightness);
        }
      }
    }
  }
  else if (players[0].state == PAUSED) {
    // Show paused state with dimmer blue LEDs - progresso suave
    float totalLEDs = players[0].progress * PLAYER1_LEDS;
    int fullLEDs = (int)totalLEDs;
    float partialLED = totalLEDs - fullLEDs;
    
    for (int i = 0; i < fullLEDs; i++) {
      leds[i] = CRGB(0, 0, 64); // Dim blue for paused
    }
    
    if (fullLEDs < PLAYER1_LEDS && partialLED > 0) {
      int brightness = (int)(64 * partialLED);
      leds[fullLEDs] = CRGB(0, 0, brightness);
    }
  }
  else if (players[0].state == PAUSED_BY_HEADSET) {
    // Show paused by headset state with dimmer blue LEDs - progresso suave
    float totalLEDs = players[0].progress * PLAYER1_LEDS;
    int fullLEDs = (int)totalLEDs;
    float partialLED = totalLEDs - fullLEDs;
    
    for (int i = 0; i < fullLEDs; i++) {
      leds[i] = CRGB(0, 0, 64); // Dim blue for paused by headset
    }
    
    if (fullLEDs < PLAYER1_LEDS && partialLED > 0) {
      int brightness = (int)(64 * partialLED);
      leds[fullLEDs] = CRGB(0, 0, brightness);
    }
  }
  
  // Player 2 progress (LEDs 9-16, right to left)
  if (players[1].state == PLAYING) {
    // Check if using manual progress control (lastUpdate == 0)
    if (players[1].lastUpdate == 0) {
      // Manual control via led2:X commands - show exact progress
      float totalLEDs = players[1].progress * PLAYER2_LEDS;
      int fullLEDs = (int)totalLEDs;
      float partialLED = totalLEDs - fullLEDs;
      
      // Acender LEDs completos
      for (int i = 0; i < fullLEDs; i++) {
        leds[NUM_LEDS - 1 - i] = CRGB::Red;
      }
      
      // Acender LED parcial com luminosidade proporcional
      if (fullLEDs < PLAYER2_LEDS && partialLED > 0) {
        int brightness = (int)(255 * partialLED);
        leds[NUM_LEDS - 1 - fullLEDs] = CRGB(brightness, 0, 0);
      }
    } else {
      // Automatic animation mode
      unsigned long playTime = now - players[1].lastUpdate;
      if (playTime < 6000) { // Only animate for first 6 seconds after play command
        float progress = min(1.0, playTime / 5000.0); // 5 seconds = 5000ms
        
        // Progresso suave com luminosidade gradual
        float totalLEDs = progress * PLAYER2_LEDS;
        int fullLEDs = (int)totalLEDs;
        float partialLED = totalLEDs - fullLEDs; // Parte fracionária do próximo LED
        
        // Acender LEDs completos
        for (int i = 0; i < fullLEDs; i++) {
          leds[NUM_LEDS - 1 - i] = CRGB::Red;
        }
        
        // Acender LED parcial com luminosidade proporcional
        if (fullLEDs < PLAYER2_LEDS && partialLED > 0) {
          int brightness = (int)(255 * partialLED);
          leds[NUM_LEDS - 1 - fullLEDs] = CRGB(brightness, 0, 0); // Vermelho com luminosidade variável
        }
        
        // Update progress for display
        players[1].progress = progress;
      } else {
        // Show current progress without animation - também com progresso suave
        float totalLEDs = players[1].progress * PLAYER2_LEDS;
        int fullLEDs = (int)totalLEDs;
        float partialLED = totalLEDs - fullLEDs;
        
        // Acender LEDs completos
        for (int i = 0; i < fullLEDs; i++) {
          leds[NUM_LEDS - 1 - i] = CRGB::Red;
        }
        
        // Acender LED parcial com luminosidade proporcional
        if (fullLEDs < PLAYER2_LEDS && partialLED > 0) {
          int brightness = (int)(255 * partialLED);
          leds[NUM_LEDS - 1 - fullLEDs] = CRGB(brightness, 0, 0);
        }
      }
    }
  }
  else if (players[1].state == PAUSED) {
    // Show paused state with dimmer red LEDs - progresso suave
    float totalLEDs = players[1].progress * PLAYER2_LEDS;
    int fullLEDs = (int)totalLEDs;
    float partialLED = totalLEDs - fullLEDs;
    
    for (int i = 0; i < fullLEDs; i++) {
      leds[NUM_LEDS - 1 - i] = CRGB(64, 0, 0); // Dim red for paused
    }
    
    if (fullLEDs < PLAYER2_LEDS && partialLED > 0) {
      int brightness = (int)(64 * partialLED);
      leds[NUM_LEDS - 1 - fullLEDs] = CRGB(brightness, 0, 0);
    }
  }
  else if (players[1].state == PAUSED_BY_HEADSET) {
    // Show paused by headset state with dimmer red LEDs - progresso suave
    float totalLEDs = players[1].progress * PLAYER2_LEDS;
    int fullLEDs = (int)totalLEDs;
    float partialLED = totalLEDs - fullLEDs;
    
    for (int i = 0; i < fullLEDs; i++) {
      leds[NUM_LEDS - 1 - i] = CRGB(64, 0, 0); // Dim red for paused by headset
    }
    
    if (fullLEDs < PLAYER2_LEDS && partialLED > 0) {
      int brightness = (int)(64 * partialLED);
      leds[NUM_LEDS - 1 - fullLEDs] = CRGB(brightness, 0, 0);
    }
  }
  
  // Ready state blinking (green)
  bool blinkState = (now / READY_BLINK_INTERVAL) % 2;
  
  if (players[0].state == READY && blinkState) {
    for (int i = 0; i < PLAYER1_LEDS; i++) {
      leds[i] = CRGB::Green;
    }
  }
  
  if (players[1].state == READY && blinkState) {
    for (int i = 0; i < PLAYER2_LEDS; i++) {
      leds[NUM_LEDS - 1 - i] = CRGB::Green;
    }
  }
}

void updateReadyBlinkLEDs() {
  unsigned long now = millis();
  bool blinkState = (now / READY_BLINK_INTERVAL) % 2;
  
  // Clear all LEDs first
  for (int i = 0; i < NUM_LEDS; i++) {
    leds[i] = CRGB::Black;
  }
  
  if (blinkState) {
    // Player 1 ready (green blinking on LED 4 - middle of Player 1)
    if (players[0].state == READY) {
      leds[3] = CRGB::Green; // LED 4 (index 3)
    }
    // Player 1 paused by headset (purple blinking on LED 4)
    else if (players[0].state == PAUSED_BY_HEADSET) {
      leds[3] = CRGB(128, 0, 128); // Purple
    }
    // Player 1 offline (purple blinking on LED 4)
    else if (players[0].state == DISCONNECTED) {
      leds[3] = CRGB(128, 0, 128); // Purple
    }
    
    // Player 2 ready (green blinking on LED 12 - middle of Player 2)
    if (players[1].state == READY) {
      leds[11] = CRGB::Green; // LED 12 (index 11)
    }
    // Player 2 paused by headset (purple blinking on LED 12)
    else if (players[1].state == PAUSED_BY_HEADSET) {
      leds[11] = CRGB(128, 0, 128); // Purple
    }
    // Player 2 offline (purple blinking on LED 12)
    else if (players[1].state == DISCONNECTED) {
      leds[11] = CRGB(128, 0, 128); // Purple
    }
  }
}

void updateRainbowLEDs() {
  for (int i = 0; i < NUM_LEDS; i++) {
    leds[i] = CHSV((effectStep + i * 16) % 256, 255, 255);
  }
  effectStep = (effectStep + 2) % 256;
}

void updatePulseLEDs() {
  int brightness = 128 + 127 * sin(effectStep * 0.1);
  for (int i = 0; i < NUM_LEDS; i++) {
    leds[i] = CRGB(brightness, brightness, brightness);
  }
  effectStep++;
}

void updateChaseLEDs() {
  for (int i = 0; i < NUM_LEDS; i++) {
    if (i == (effectStep % NUM_LEDS)) {
      leds[i] = CRGB::White;
    } else {
      leds[i] = CRGB::Black;
    }
  }
  effectStep++;
}

void updateSolidLEDs() {
  for (int i = 0; i < NUM_LEDS; i++) {
    leds[i] = CRGB::White;
  }
}

void clearAllLEDs() {
  for (int i = 0; i < NUM_LEDS; i++) {
    leds[i] = CRGB::Black;
  }
}

