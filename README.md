# ESP32 VR LED Controller

A PlatformIO-based ESP32 project that controls 16 WS2812B LEDs and acts as a WebSocket server for VR headset communication, with two buttons for playback control.

## ✨ Features

- **16 WS2812B LED Control**: Player 1 (LEDs 1-8) and Player 2 (LEDs 9-16)
- **WebSocket Server**: Real-time communication with VR headsets/APARATO
- **Button Controls**: Play/Pause/Stop/Reset for each player independently
- **Progress Visualization**: Bidirectional LED progress bars (0-100%)
- **Direct LED Control**: Commands `led1:X` and `led2:X` for precise control
- **Multiple LED Effects**: Rainbow, pulse, chase, and more
- **Web-based Testing**: Complete web interface for testing
- **Fluid Animations**: Smooth LED transitions without flickering
- **360° Video Player**: Web-based VR video player with Three.js
- **360° Navigation**: Drag to rotate inside concave sphere, real-time degree display
- **Meta Quest Simulation**: Complete VR headset event simulation

## Hardware Setup

### Wiring Diagram

```
ESP32 Dev Board
├── GPIO 2 (D2) → WS2812B LED Strip Data Pin
├── GPIO 4 (D4)   → Button 1 (Play/Pause) → GND
├── GPIO 5 (D5)   → Button 2 (Effect/Stop) → GND
├── 5V            → LED Strip Power (+)
├── GND           → LED Strip Power (-) + Button Common Ground
└── WiFi          → VIVOFIBRA-WIFI6-2D81
```

### Component Connections

| Component | ESP32 Pin | Notes |
|-----------|-----------|-------|
| WS2812B Data | GPIO 2 (D2) | Data input for LED strip |
| Button 1 | GPIO 4 (D4) | Play/Pause control |
| Button 2 | GPIO 5 (D5) | Effect change/Stop |
| LED Power | 5V | Ensure adequate power supply |
| Common Ground | GND | Connect all grounds together |

### LED Layout

```
Player 1 (0-50%)     Player 2 (50-100%)
LED 1 → LED 8        LED 16 ← LED 9
[████████]           [████████]
```

## Software Setup

### Prerequisites

- [PlatformIO](https://platformio.org/) installed
- Modern web browser (for testing)
- ESP32 development board
- WS2812B LED strip (16 LEDs)

### ESP32 Firmware Installation

1. **Clone/Download this project**
   ```bash
   git clone <repository-url>
   cd BIJARI_VR
   ```

2. **Open in PlatformIO**
   ```bash
   pio project init --board esp32dev
   ```

3. **Compile and Upload**
   ```bash
   pio run --target upload
   ```

4. **Monitor Serial Output**
   ```bash
   pio device monitor
   ```

### Web Player Setup

1. **Start Local Server**
   ```bash
   cd web_player
   python -m http.server 8000
   ```

2. **Open in Browser**
   ```
   http://localhost:8000
   ```

## Usage

### ESP32 Operation

1. **Power On**: ESP32 connects to WiFi and starts WebSocket server
2. **LED Status**: 
   - **Off**: No players connected
   - **Green Blinking**: Player ready and waiting
   - **Blue Progress**: Player 1 playing (LEDs 1→8)
   - **Red Progress**: Player 2 playing (LEDs 16→9)

### Button Controls

- **Button 1 (D4) - Player 1 Control**: 
  - Short press: Play/Pause/Resume Player 1
  - Long press (>1s): Stop all players and turn off LEDs

- **Button 2 (D5) - Player 2 Control**:
  - Short press: Play/Pause/Resume Player 2
  - Long press (>1s): Reset all players and turn off LEDs

### WebSocket Protocol

#### Commands from VR Headset/APARATO to ESP32:

**Simple String Commands:**
- `on1` - Player 1 ready (green blinking)
- `play1` - Player 1 starts 5-second animation
- `on2` - Player 2 ready (green blinking)  
- `play2` - Player 2 starts 5-second animation
- `led1:X` - Set Player 1 progress to X% (0-100)
- `led2:X` - Set Player 2 progress to X% (0-100)

**JSON Commands (Legacy Support):**
```json
// Player ready
{"player": 1, "status": "ready"}

// Player playing with progress
{"player": 1, "status": "playing", "progress": 0.45}

// Player paused
{"player": 1, "status": "paused"}

// Player stopped
{"player": 1, "status": "stopped"}
```

#### Player States:
- **DISCONNECTED**: Player not connected
- **CONNECTED**: Player connected but not ready
- **READY**: Player ready (green blinking LEDs)
- **PLAYING**: Player actively playing (progress LEDs)
- **PAUSED**: Player paused (dimmed progress LEDs)

### Web Interface Testing

**Complete VR Testing Environment:**

1. **Connect to ESP32**: Enter ESP32 IP address (default: 192.168.15.3)
2. **Load Video**: Upload or select 360° video file
3. **Test Controls**: Use physical buttons on ESP32 or web interface
4. **Monitor LEDs**: Real-time LED visualization
5. **360° Navigation**: Drag to rotate, view degrees, center/reset

### 🌐 Web Player Simulator

**Complete VR Event Simulator:**
```bash
cd web_player
# Open index.html in modern browser
# Or use local server:
python -m http.server 8000
# Access: http://localhost:8000
```

### 🎥 Arquivo de Vídeo Principal

**Pierre_Final [HighRes]-001.mov**
- **Localização**: Raiz do projeto (não versionado no Git)
- **Tamanho**: ~2.9GB
- **Uso**: Vídeo principal para teste do sistema VR
- **Formato**: MOV (QuickTime)

**Como adicionar:**
1. Copie `Pierre_Final [HighRes]-001.mov` para a raiz do projeto
2. Abra o player web e carregue o vídeo
3. Teste a sincronização com LEDs

**Nota**: O arquivo está no `.gitignore` devido ao tamanho. Mantenha-o localmente para desenvolvimento.

**Features:**
- **Meta Quest Event Simulation**: Headset ON/OFF, App Focus, Player States
- **360° Video Player**: Three.js-based 360° video playback
- **Real-time LED Visualization**: Visual representation of ESP32 LEDs
- **WebSocket Integration**: Full bidirectional communication with ESP32
- **Automatic Mode**: Complete event sequence for testing
- **Keyboard Shortcuts**: Full keyboard control for all functions

**Interactive Commands:**
- `r` - Send ready status
- `p` - Start playback simulation
- `s` - Stop playback
- `q` - Quit simulator
- `h` - Show help

## LED Effects

1. **Progress Mode**: Shows actual playback progress
2. **Ready Blink**: All LEDs blink green
3. **Rainbow**: Colorful rainbow animation
4. **Pulse**: Breathing effect
5. **Chase**: Moving dot effect
6. **Solid**: All LEDs white

## Troubleshooting

### Common Issues

1. **LEDs not working**:
   - Check power supply (5V, adequate current)
   - Verify GPIO 2 (D2) connection
   - Check ground connections

2. **Buttons not responding**:
   - Verify GPIO 4 and GPIO 5 connections
   - Check button wiring (pull-up configuration)

3. **WebSocket connection failed**:
   - Verify WiFi credentials in `main.cpp`
   - Check ESP32 IP address
   - Ensure firewall allows port 80

4. **Web interface not connecting**:
   - Check ESP32 IP address in browser
   - Verify WebSocket server is running
   - Check browser console for errors

### Serial Monitor Output

```
Connecting to WiFi...
Connected! IP address: 192.168.1.100
ESP32 VR LED Controller Ready!
WebSocket server started on port 80
Client 0 connected from 192.168.1.50
Player 1 is ready
```

## Configuration

### LED Settings

Modify LED configuration in `main.cpp`:

```cpp
#define LED_PIN 2           // LED data pin (GPIO 2)
#define NUM_LEDS 16         // Total number of LEDs
#define PLAYER1_LEDS 8      // Player 1 LED count (LEDs 1-8)
#define PLAYER2_LEDS 8      // Player 2 LED count (LEDs 9-16)
```

### Button Settings

Change button pins in `main.cpp`:

```cpp
#define BUTTON_PLAY_PAUSE 4  // Player 1 control button (GPIO 4)
#define BUTTON_EFFECT_STOP 5 // Player 2 control button (GPIO 5)
```

### WiFi Settings

Edit WiFi credentials in `main.cpp`:

```cpp
const char* ssid = "VIVOFIBRA-WIFI6-2D81";
const char* password = "xgsxJmdzjFNro5q";
```

## Development

### Project Structure

```
BIJARI_VR/
├── platformio.ini              # PlatformIO configuration
├── src/
│   └── main.cpp                 # ESP32 firmware
├── web_player/                  # Web-based VR interface
│   ├── index.html               # Main interface
│   ├── styles.css               # Styling
│   ├── websocket-client.js      # WebSocket communication
│   ├── quest-simulator.js       # Meta Quest event simulation
│   ├── video-player.js          # 360° video player
│   ├── led-visualizer.js        # LED status visualization
│   ├── main.js                  # Module integration
│   ├── README.md                # Web player documentation
│   └── VIDEO_SAMPLES.md         # Video samples for testing
├── .gitignore                   # Git ignore file
├── VIDEO_PLACEHOLDER.md         # Video file instructions
└── README.md                    # This file
```

### Adding New Features

1. **New LED Effects**: Add to `LEDEffect` enum and implement in `updateLEDs()`
2. **New WebSocket Commands**: Extend `handleWebSocketMessage()`
3. **New Button Functions**: Modify `handleButtons()`

## License

This project is open source. Feel free to modify and distribute.

## Support

For issues and questions:
1. Check the troubleshooting section
2. Review serial monitor output
3. Verify hardware connections
4. Test with Python simulator
