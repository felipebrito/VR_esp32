#!/usr/bin/env python3
"""
ESP32 VR LED Controller - Simple Test Client
Automatically tests the ESP32 WebSocket server
"""

import websocket
import json
import threading
import time
import sys

class SimpleTestClient:
    def __init__(self, esp32_ip="192.168.1.100", port=80):
        self.ws_url = f"ws://{esp32_ip}:{port}/ws"
        self.ws = None
        self.player_id = 1
        self.is_connected = False
        self.is_playing = False
        self.progress = 0.0
        
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
            
            print(f"Connecting to {self.ws_url}...")
            self.ws.run_forever()
        except Exception as e:
            print(f"Connection failed: {e}")
    
    def on_open(self, ws):
        """Called when WebSocket connection is established"""
        self.is_connected = True
        print(f"✓ Connected to ESP32 as Player {self.player_id}")
        
        # Send ready status
        self.send_ready()
        
        # Start automatic test sequence
        threading.Thread(target=self.test_sequence, daemon=True).start()
    
    def on_message(self, ws, message):
        """Handle incoming messages from ESP32"""
        try:
            data = json.loads(message)
            command = data.get("command")
            player = data.get("player")
            
            if player == self.player_id:
                print(f"📨 Received command: {command}")
                
        except json.JSONDecodeError:
            print(f"❌ Invalid JSON received: {message}")
    
    def on_error(self, ws, error):
        """Handle WebSocket errors"""
        print(f"❌ WebSocket error: {error}")
    
    def on_close(self, ws, close_status_code, close_msg):
        """Handle WebSocket close"""
        self.is_connected = False
        print("🔌 Disconnected from ESP32")
    
    def send_message(self, message):
        """Send message to ESP32"""
        if self.is_connected and self.ws:
            try:
                self.ws.send(json.dumps(message))
                print(f"📤 Sent: {message}")
            except Exception as e:
                print(f"❌ Failed to send message: {e}")
    
    def send_ready(self):
        """Send ready status to ESP32"""
        message = {
            "player": self.player_id,
            "status": "ready"
        }
        self.send_message(message)
    
    def start_playback(self):
        """Start video playback simulation"""
        if not self.is_playing:
            self.is_playing = True
            self.progress = 0.0
            print("▶️ Starting playback...")
            
            # Start progress simulation
            threading.Thread(target=self.simulate_progress, daemon=True).start()
    
    def simulate_progress(self):
        """Simulate video progress"""
        while self.is_playing and self.is_connected:
            time.sleep(0.2)  # Update every 200ms
            
            if self.is_playing:
                self.progress += 0.05  # Increase by 5% every 200ms
                
                if self.progress >= 1.0:
                    self.progress = 1.0
                    self.is_playing = False
                    print("✅ Playback completed!")
                    self.send_message({
                        "player": self.player_id,
                        "status": "stopped"
                    })
                    break
                
                # Send progress update
                self.send_message({
                    "player": self.player_id,
                    "status": "playing",
                    "progress": self.progress
                })
    
    def test_sequence(self):
        """Run automatic test sequence"""
        print("\n🧪 Starting automatic test sequence...")
        
        # Wait 2 seconds after ready
        time.sleep(2)
        
        # Start playback
        self.start_playback()
        
        # Wait for playback to complete
        while self.is_playing:
            time.sleep(1)
        
        # Wait 2 seconds then send ready again
        time.sleep(2)
        self.send_ready()
        
        print("🎯 Test sequence completed!")

def main():
    print("ESP32 VR LED Controller - Simple Test Client")
    print("=" * 50)
    
    # Get parameters from command line
    esp32_ip = "192.168.1.100"  # Default IP
    player_id = 1  # Default player ID
    
    if len(sys.argv) > 1:
        esp32_ip = sys.argv[1]
    if len(sys.argv) > 2:
        try:
            player_id = int(sys.argv[2])
            if player_id not in [1, 2]:
                player_id = 1
        except ValueError:
            player_id = 1
    
    print(f"🎯 Target ESP32 IP: {esp32_ip}")
    print(f"👤 Player ID: {player_id}")
    print(f"🔗 WebSocket URL: ws://{esp32_ip}:80/ws")
    
    # Create test client
    client = SimpleTestClient(esp32_ip)
    client.player_id = player_id
    
    # Start connection
    try:
        client.connect()
    except KeyboardInterrupt:
        print("\n👋 Test interrupted by user")
    except Exception as e:
        print(f"❌ Test failed: {e}")

if __name__ == "__main__":
    main()
