#include <WiFi.h>
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

// Button Configuration
const unsigned long DEBOUNCE_DELAY = 50;
const unsigned long LONG_PRESS_TIME = 1000;

// WiFi Configuration
const char* ssid = "VIVOFIBRA-WIFI6-2D81";
const char* password = "xgsxJmdzjFNro5q";

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

// WebSocket Server
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
  
  // Connect to WiFi
  WiFi.begin(ssid, password);
  Serial.print("Connecting to WiFi");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println();
  Serial.print("Connected! IP address: ");
  Serial.println(WiFi.localIP());
  
  // Start WebSocket server
  ws.onEvent(onWebSocketEvent);
  server.addHandler(&ws);
  
  // Add root handler for testing
  server.on("/", HTTP_GET, [](AsyncWebServerRequest *request){
    request->send(200, "text/plain", "ESP32 VR LED Controller - WebSocket Server Running");
  });
  
  server.begin();
  
  Serial.println("ESP32 VR LED Controller Ready!");
  Serial.println("WebSocket server started on port 80");
}

void loop() {
  ws.cleanupClients();
  handleButtons();
  updateLEDs();
  updatePlayerStates();
  delay(10);
}

void onWebSocketEvent(AsyncWebSocket *server, AsyncWebSocketClient *client, AwsEventType type, void *arg, uint8_t *data, size_t len) {
  switch(type) {
    case WS_EVT_CONNECT:
      Serial.printf("Client %u connected from %s\n", client->id(), client->remoteIP().toString().c_str());
      break;
      
    case WS_EVT_DISCONNECT:
      Serial.printf("Client %u disconnected\n", client->id());
      handlePlayerDisconnect(client->id());
      break;
      
    case WS_EVT_DATA:
      if (len > 0) {
        data[len] = '\0';
        Serial.printf("Received from client %u: %s\n", client->id(), (char*)data);
        handleWebSocketMessage(client->id(), (char*)data);
      }
      break;
      
    default:
      break;
  }
}

void handleWebSocketMessage(uint8_t clientNum, char* message) {
  Serial.printf("Raw message received: %s\n", message);
  
  // Handle simple string commands (on1, play1, on2, play2, led1:0, led1:1, etc.)
  String msgStr = String(message);
  msgStr.trim();
  
      // Handle LED progress commands (led1:0-100, led2:0-100)
      if (msgStr.startsWith("led")) {
        int colonIndex = msgStr.indexOf(':');
        if (colonIndex > 0) {
          String ledPart = msgStr.substring(3, colonIndex); // Get number after "led"
          String valuePart = msgStr.substring(colonIndex + 1); // Get value after ":"
          
          int playerNumber = ledPart.toInt();
          int progressValue = valuePart.toInt();
          
          if (playerNumber >= 1 && playerNumber <= 2 && progressValue >= 0 && progressValue <= 100) {
            int playerIndex = playerNumber - 1;
            
            // Set player progress and state - let updateProgressLEDs() handle the display
            players[playerIndex].progress = progressValue / 100.0; // Convert to 0.0-1.0
            players[playerIndex].state = PLAYING; // Set to playing state to show progress
            players[playerIndex].lastUpdate = millis() + 10000; // Set future timestamp to prevent automatic animation
            
            Serial.printf("Player %d progress set to %d%% (%d LEDs)\n", playerNumber, progressValue, 
                         playerNumber == 1 ? (int)(progressValue * PLAYER1_LEDS / 100) : (int)(progressValue * PLAYER2_LEDS / 100));
            return;
          } else {
            Serial.printf("Invalid LED command: %s (Player %d, Progress %d)\n", msgStr.c_str(), playerNumber, progressValue);
            return;
          }
        }
      }
  
  if (msgStr == "on1") {
    if (players[0].state == PAUSED_BY_HEADSET) {
      // Resume from where it was paused
      players[0].state = PLAYING;
      Serial.printf("Player 1 headset back on - resuming from %.1f%%\n", players[0].progress * 100);
    } else {
      // Normal ready state
      players[0].state = READY;
      players[0].connected = true;
      players[0].progress = 0.0;
      players[0].lastUpdate = millis(); // Reset timer for blinking
      Serial.println("Player 1 ready - green blinking");
    }
    return;
  }
  else if (msgStr == "play1") {
    players[0].state = PLAYING;
    players[0].progress = 0.0;
    players[0].lastUpdate = millis(); // Set start time for 5s animation
    Serial.println("Player 1 playing - 5s progress animation");
    return;
  }
  else if (msgStr == "on2") {
    if (players[1].state == PAUSED_BY_HEADSET) {
      // Resume from where it was paused
      players[1].state = PLAYING;
      Serial.printf("Player 2 headset back on - resuming from %.1f%%\n", players[1].progress * 100);
    } else {
      // Normal ready state
      players[1].state = READY;
      players[1].connected = true;
      players[1].progress = 0.0;
      players[1].lastUpdate = millis(); // Reset timer for blinking
      Serial.println("Player 2 ready - green blinking");
    }
    return;
  }
  else if (msgStr == "play2") {
    players[1].state = PLAYING;
    players[1].progress = 0.0;
    players[1].lastUpdate = millis(); // Set start time for 5s animation
    Serial.println("Player 2 playing - 5s progress animation");
    return;
  }
  else if (msgStr == "off1") {
    players[0].state = PAUSED_BY_HEADSET;
    players[0].connected = true; // Still connected, just headset removed
    // Keep progress and lastUpdate for resuming
    Serial.println("Player 1 headset removed - paused by headset (orange blinking)");
    return;
  }
  else if (msgStr == "off2") {
    players[1].state = PAUSED_BY_HEADSET;
    players[1].connected = true; // Still connected, just headset removed
    // Keep progress and lastUpdate for resuming
    Serial.println("Player 2 headset removed - paused by headset (orange blinking)");
    return;
  }
  else if (msgStr == "pause1") {
    if (players[0].state == PLAYING) {
      players[0].state = PAUSED;
      Serial.println("Player 1 paused - LEDs dimmed");
    }
    return;
  }
  else if (msgStr == "pause2") {
    if (players[1].state == PLAYING) {
      players[1].state = PAUSED;
      Serial.println("Player 2 paused - LEDs dimmed");
    }
    return;
  }
  
  // Try to parse as JSON
  StaticJsonDocument<200> doc;
  DeserializationError error = deserializeJson(doc, message);
  
  if (error) {
    Serial.print("JSON parsing failed: ");
    Serial.println(error.c_str());
    return;
  }
  
  // Handle JSON command messages
  if (doc.containsKey("command")) {
    String command = doc["command"];
    Serial.printf("Received JSON command: %s\n", command.c_str());
    
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
  
  // Handle legacy JSON messages
  int playerId = doc["player"];
  if (playerId < 1 || playerId > 2) {
    Serial.println("Invalid player ID");
    return;
  }
  
  int playerIndex = playerId - 1;
  String status = doc["status"];
  
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
  Serial.print("SENDCMD:");
  int playerIndex = playerId - 1;
  Serial.printf("DEBUG: Tentando enviar comando '%s' para player %d\n", command.c_str(), playerId);
  Serial.printf("DEBUG: Player %d - connected: %s, client: %s\n", playerId, 
                players[playerIndex].connected ? "true" : "false",
                players[playerIndex].client ? "true" : "false");
  
  // Enviar comando se houver pelo menos um cliente conectado
  if (ws.getClients().size() > 0) {
    StaticJsonDocument<100> doc;
    doc["command"] = command;
    doc["player"] = playerId;
    
    String message;
    serializeJson(doc, message);
    
    // Enviar para todos os clientes conectados
    for (auto& client : ws.getClients()) {
      ws.text(client.id(), message);
    }
    Serial.printf("Sent command '%s' to player %d (broadcast to %d clients)\n", command.c_str(), playerId, ws.getClients().size());
  } else {
    Serial.printf("ERRO: Nenhum cliente WebSocket conectado\n");
  }
}

void handleButtons() {
  handleButton(BUTTON_PLAY_PAUSE, &buttonPlayPause, []() {
    // Short press: toggle play/pause for both players
    Serial.println("=== BUTTON 1 PRESSED ===");
    Serial.println("Button 1 pressed - toggling play/pause");
    for (int i = 0; i < 2; i++) {
      if (players[i].state == PLAYING) {
        players[i].state = PAUSED;
        sendCommandToPlayer(i + 1, "pause");
        Serial.printf("Player %d paused\n", i + 1);
      } else if (players[i].state == PAUSED) {
        // Resume from where it was paused
        players[i].state = PLAYING;
        players[i].lastUpdate = millis() - (players[i].progress * 5000); // Adjust timer to continue from paused position
        sendCommandToPlayer(i + 1, "play");
        Serial.printf("Player %d resumed\n", i + 1);
      } else if (players[i].state == READY) {
        // Start new playback
        players[i].state = PLAYING;
        players[i].progress = 0.0;
        players[i].lastUpdate = millis();
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
      players[i].state = DISCONNECTED;
      players[i].connected = false;
      players[i].progress = 0.0;
      sendCommandToPlayer(i + 1, "stop");
    }
    Serial.println("All players stopped and LEDs turned off");
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
      // Resume from where it was paused
      players[1].state = PLAYING;
      players[1].lastUpdate = millis() - (players[1].progress * 5000);
      sendCommandToPlayer(2, "play");
      Serial.println("Player 2 resumed");
    } else if (players[1].state == READY) {
      // Start new playback
      players[1].state = PLAYING;
      players[1].progress = 0.0;
      players[1].lastUpdate = millis();
      sendCommandToPlayer(2, "play");
      Serial.println("Player 2 started");
    }
    Serial.println("=== BUTTON 2 PROCESSED ===");
  }, []() {
    // Long press: reset all players and turn off LEDs
    Serial.println("=== BUTTON 2 LONG PRESS ===");
    Serial.println("Button 2 long press - resetting all players");
    for (int i = 0; i < 2; i++) {
      players[i].state = DISCONNECTED;
      players[i].connected = false;
      players[i].progress = 0.0;
      sendCommandToPlayer(i + 1, "stop");
    }
    Serial.println("All players reset and LEDs turned off");
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
  
  // Player 1 progress (LEDs 1-8, left to right)
  if (players[0].state == PLAYING) {
    // Only animate if it's an automatic play (not manual led1:X command)
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
    // Only animate if it's an automatic play (not manual led2:X command)
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

