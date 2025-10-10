#!/usr/bin/env python3
"""
ESP32 VR LED Controller - APARATO Command Test
Sends simple commands: on1, play1, on2, play2
"""

import websocket
import json
import time
import sys

class APARATOCommandTest:
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
        
        # Start command sequence
        self.run_command_sequence()
    
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
                message = {"command": command}
                self.ws.send(json.dumps(message))
                print(f"📤 Sent command: {command}")
            except Exception as e:
                print(f"❌ Failed to send command: {e}")
    
    def run_command_sequence(self):
        """Run test sequence with APARATO commands"""
        print("\n🧪 Starting APARATO command test sequence...")
        
        # Test Player 1
        print("\n--- TESTING PLAYER 1 ---")
        print("1. Sending 'on1' - Player 1 should blink green")
        self.send_command("on1")
        time.sleep(3)
        
        print("2. Sending 'play1' - Player 1 should show 5s blue progress")
        self.send_command("play1")
        time.sleep(6)  # Wait for 5s animation + 1s buffer
        
        # Test Player 2
        print("\n--- TESTING PLAYER 2 ---")
        print("3. Sending 'on2' - Player 2 should blink green")
        self.send_command("on2")
        time.sleep(3)
        
        print("4. Sending 'play2' - Player 2 should show 5s red progress")
        self.send_command("play2")
        time.sleep(6)  # Wait for 5s animation + 1s buffer
        
        # Test both ready
        print("\n--- TESTING BOTH READY ---")
        print("5. Sending 'on1' and 'on2' - Both should blink green")
        self.send_command("on1")
        time.sleep(1)
        self.send_command("on2")
        time.sleep(3)
        
        print("\n🎯 APARATO command test completed!")
        print("\n📋 Manual Test Instructions:")
        print("   - Press Button 1 (D4) to start/pause players")
        print("   - Hold Button 1 (D4) to stop all and turn off LEDs")
        print("   - Press Button 2 (D5) to change LED effects")
        print("   - Hold Button 2 (D5) to stop all players")
        
        # Keep connection open for manual testing
        print("\n⏳ Connection will stay open for manual testing...")
        print("   Press Ctrl+C to disconnect")

def main():
    print("ESP32 VR LED Controller - APARATO Command Test")
    print("=" * 50)
    
    # Get ESP32 IP from command line or use discovered IP
    esp32_ip = "192.168.15.6"  # Discovered IP
    
    if len(sys.argv) > 1:
        esp32_ip = sys.argv[1]
    
    print(f"🎯 Target ESP32 IP: {esp32_ip}")
    print(f"🔗 WebSocket URL: ws://{esp32_ip}:80/ws")
    
    # Create test client
    client = APARATOCommandTest(esp32_ip)
    
    # Start connection
    try:
        client.connect()
    except KeyboardInterrupt:
        print("\n👋 Test interrupted by user")
    except Exception as e:
        print(f"❌ Test failed: {e}")

if __name__ == "__main__":
    main()
