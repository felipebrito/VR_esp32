# ESP32 VR LED Controller

A PlatformIO-based ESP32 project that controls 16 WS2812B LEDs and acts as a WebSocket server for VR headset communication, with two buttons for playback control.

## Features

- **16 WS2812B LED Control**: Player 1 (LEDs 1-8) and Player 2 (LEDs 9-16)
- **WebSocket Server**: Communication with VR headsets
- **Button Controls**: Play/Pause and Effect Change/Stop
- **Independent Players**: Each player can operate independently
- **Progress Visualization**: Bidirectional LED progress bars
- **Multiple LED Effects**: Rainbow, pulse, chase, and more
- **Python Simulator**: Test client for development

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
- Python 3.x (for simulator)
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

### Python Simulator Setup

1. **Install Dependencies**
   ```bash
   cd python_client
   pip install -r requirements.txt
   ```

2. **Run Simulator**
   ```bash
   python simulator.py
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

- **Button 1 (D4)**: 
  - Short press: Toggle play/pause for both players
  - Works independently for each player

- **Button 2 (D5)**:
  - Short press: Cycle through LED effects
  - Long press (>1s): Stop all players

### WebSocket Protocol

#### Messages from VR Headset to ESP32:

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

#### Messages from ESP32 to VR Headset:

```json
// Play command
{"command": "play", "player": 1}

// Pause command
{"command": "pause", "player": 1}

// Stop command
{"command": "stop", "player": 1}
```

### Python Simulator Commands

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
   - Verify GPIO 13 connection
   - Check ground connections

2. **Buttons not responding**:
   - Verify GPIO 4 and GPIO 5 connections
   - Check button wiring (pull-up configuration)

3. **WebSocket connection failed**:
   - Verify WiFi credentials in `main.cpp`
   - Check ESP32 IP address
   - Ensure firewall allows port 80

4. **Python simulator errors**:
   - Install required packages: `pip install websocket-client`
   - Check ESP32 IP address
   - Verify WebSocket server is running

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

### WiFi Settings

Edit `main.cpp` to change WiFi credentials:

```cpp
const char* ssid = "YOUR_WIFI_SSID";
const char* password = "YOUR_WIFI_PASSWORD";
```

### LED Settings

Modify LED configuration in `main.cpp`:

```cpp
#define LED_PIN 2           // Change LED data pin
#define NUM_LEDS 16         // Change number of LEDs
#define PLAYER1_LEDS 8      // Change player 1 LED count
#define PLAYER2_LEDS 8      // Change player 2 LED count
```

### Button Settings

Change button pins in `main.cpp`:

```cpp
#define BUTTON_PLAY_PAUSE 4  // Change play/pause button pin
#define BUTTON_EFFECT_STOP 5 // Change effect/stop button pin
```

## Development

### Project Structure

```
BIJARI_VR/
├── platformio.ini          # PlatformIO configuration
├── src/
│   └── main.cpp             # ESP32 firmware
├── python_client/
│   ├── simulator.py         # Python WebSocket client
│   └── requirements.txt     # Python dependencies
└── README.md                # This file
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
