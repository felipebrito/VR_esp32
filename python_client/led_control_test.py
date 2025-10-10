#!/usr/bin/env python3
"""
ESP32 VR LED Controller - Individual LED Control Test
Tests individual LED control commands: led1:0, led1:1, etc.
"""

import websocket
import json
import time
import sys

class LEDControlTest:
    def __init__(self, esp32_ip="192.168.15.6", port=80):
        self.ws_url = f"ws://{esp32_ip}:{port}/ws"
        self.ws = None
        self.is_connected = False
        
    def connect(self):
        """Connect to ESP32 WebSocket server"""
        try:
            self.ws = websocket.WebSocketApp(
                self.ws_url,
                on_open=self.on_open,
                on_message=self.on_message,
                on_error=self.on_error,
                on_close=self.on_close
            )
            
            print(f"🔗 Connecting to {self.ws_url}...")
            self.ws.run_forever()
        except Exception as e:
            print(f"❌ Connection failed: {e}")
    
    def on_open(self, ws):
        """Called when WebSocket connection is established"""
        self.is_connected = True
        print("✅ Connected to ESP32!")
        
        # Start LED control test sequence
        self.run_led_test_sequence()
    
    def on_message(self, ws, message):
        """Handle incoming messages from ESP32"""
        print(f"📨 Received: {message}")
    
    def on_error(self, ws, error):
        """Handle WebSocket errors"""
        print(f"❌ WebSocket error: {error}")
    
    def on_close(self, ws, close_status_code, close_msg):
        """Handle WebSocket close"""
        self.is_connected = False
        print("🔌 Disconnected from ESP32")
    
    def send_command(self, command):
        """Send command to ESP32"""
        if self.is_connected and self.ws:
            try:
                self.ws.send(command)
                print(f"📤 Sent: {command}")
            except Exception as e:
                print(f"❌ Failed to send command: {e}")
    
    def run_led_test_sequence(self):
        """Run LED control test sequence"""
        print("\n🧪 Starting individual LED control test...")
        
        # Test Player 1 LEDs (1-8) - Blue
        print("\n--- TESTING PLAYER 1 LEDs (1-8) - BLUE ---")
        for i in range(1, 9):
            self.send_command(f"led{i}:1")
            time.sleep(0.5)
        
        time.sleep(2)
        
        # Turn off Player 1 LEDs
        print("\n--- TURNING OFF PLAYER 1 LEDs ---")
        for i in range(1, 9):
            self.send_command(f"led{i}:0")
            time.sleep(0.2)
        
        time.sleep(1)
        
        # Test Player 2 LEDs (9-16) - Red
        print("\n--- TESTING PLAYER 2 LEDs (9-16) - RED ---")
        for i in range(9, 17):
            self.send_command(f"led{i}:1")
            time.sleep(0.5)
        
        time.sleep(2)
        
        # Turn off Player 2 LEDs
        print("\n--- TURNING OFF PLAYER 2 LEDs ---")
        for i in range(9, 17):
            self.send_command(f"led{i}:0")
            time.sleep(0.2)
        
        time.sleep(1)
        
        # Test alternating pattern
        print("\n--- TESTING ALTERNATING PATTERN ---")
        for i in range(1, 17):
            if i % 2 == 1:  # Odd numbers
                self.send_command(f"led{i}:1")
            time.sleep(0.3)
        
        time.sleep(2)
        
        # Clear all LEDs
        print("\n--- CLEARING ALL LEDs ---")
        for i in range(1, 17):
            self.send_command(f"led{i}:0")
        
        print("\n🎯 LED control test completed!")
        print("\n📋 Available LED Commands:")
        print("   led1:0 to led16:0 - Turn off individual LEDs")
        print("   led1:1 to led16:1 - Turn on individual LEDs")
        print("   LEDs 1-8: Blue (Player 1)")
        print("   LEDs 9-16: Red (Player 2)")
        
        # Keep connection open for manual testing
        print("\n⏳ Connection will stay open for manual testing...")
        print("   Press Ctrl+C to disconnect")

def main():
    print("ESP32 VR LED Controller - Individual LED Control Test")
    print("=" * 60)
    
    # Get ESP32 IP from command line or use discovered IP
    esp32_ip = "192.168.15.6"  # Discovered IP
    
    if len(sys.argv) > 1:
        esp32_ip = sys.argv[1]
    
    print(f"🎯 Target ESP32 IP: {esp32_ip}")
    print(f"🔗 WebSocket URL: ws://{esp32_ip}:80/ws")
    
    # Create test client
    client = LEDControlTest(esp32_ip)
    
    # Start connection
    try:
        client.connect()
    except KeyboardInterrupt:
        print("\n👋 Test interrupted by user")
    except Exception as e:
        print(f"❌ Test failed: {e}")

if __name__ == "__main__":
    main()
