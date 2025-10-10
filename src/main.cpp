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

// WiFi Configuration
const char* ssid = "VIVOFIBRA-WIFI6-2D81";
const char* password = "xgsxJmdzjFNro5q";

// LED Array
CRGB leds[NUM_LEDS];

// WebSocket Server
AsyncWebServer server(80);
AsyncWebSocket ws("/ws");

// Player States
enum PlayerState {
  DISCONNECTED,
  CONNECTED,
  READY,
  PLAYING,
  PAUSED
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

// Button States
struct ButtonState {
  bool pressed;
  bool lastPressed;
  unsigned long pressTime;
  unsigned long lastDebounceTime;
  bool longPressDetected;
};

ButtonState buttonPlayPause = {false, false, 0, 0, false};
ButtonState buttonEffectStop = {false, false, 0, 0, false};

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
const unsigned long DEBOUNCE_DELAY = 50;
const unsigned long LONG_PRESS_TIME = 1000;
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
  StaticJsonDocument<200> doc;
  DeserializationError error = deserializeJson(doc, message);
  
  if (error) {
    Serial.print("JSON parsing failed: ");
    Serial.println(error.c_str());
    return;
  }
  
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
  int playerIndex = playerId - 1;
  if (players[playerIndex].connected && players[playerIndex].client) {
    StaticJsonDocument<100> doc;
    doc["command"] = command;
    doc["player"] = playerId;
    
    String message;
    serializeJson(doc, message);
    ws.text(players[playerIndex].client->id(), message);
    Serial.printf("Sent command '%s' to player %d\n", command.c_str(), playerId);
  }
}

void handleButtons() {
  handleButton(BUTTON_PLAY_PAUSE, &buttonPlayPause, []() {
    // Toggle play/pause for both players
    for (int i = 0; i < 2; i++) {
      if (players[i].state == PLAYING) {
        sendCommandToPlayer(i + 1, "pause");
      } else if (players[i].state == PAUSED || players[i].state == READY) {
        sendCommandToPlayer(i + 1, "play");
      }
    }
  }, nullptr);
  
  handleButton(BUTTON_EFFECT_STOP, &buttonEffectStop, []() {
    // Short press: change effect
    if (!buttonEffectStop.longPressDetected) {
      cycleEffect();
    }
  }, []() {
    // Long press: stop all players
    for (int i = 0; i < 2; i++) {
      if (players[i].state == PLAYING || players[i].state == PAUSED) {
        sendCommandToPlayer(i + 1, "stop");
      }
    }
    Serial.println("Long press detected - stopping all players");
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
  // Player 1 progress (LEDs 1-8, left to right)
  if (players[0].state == PLAYING) {
    int player1LEDs = (int)(players[0].progress * PLAYER1_LEDS);
    for (int i = 0; i < player1LEDs; i++) {
      leds[i] = CRGB::Blue;
    }
  }
  
  // Player 2 progress (LEDs 9-16, right to left)
  if (players[1].state == PLAYING) {
    int player2LEDs = (int)(players[1].progress * PLAYER2_LEDS);
    for (int i = 0; i < player2LEDs; i++) {
      leds[NUM_LEDS - 1 - i] = CRGB::Red;
    }
  }
  
  // Ready state blinking
  unsigned long now = millis();
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
  
  if (blinkState) {
    for (int i = 0; i < NUM_LEDS; i++) {
      leds[i] = CRGB::Green;
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
