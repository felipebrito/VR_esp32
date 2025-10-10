#!/usr/bin/env python3
"""
ESP32 VR LED Controller - Dual Player Test
Tests both Player 1 and Player 2 sequentially
"""

import websocket
import json
import threading
import time
import sys

class DualPlayerTest:
    def __init__(self, esp32_ip="192.168.15.6", port=80):
        self.ws_url = f"ws://{esp32_ip}:{port}/ws"
        self.ws = None
        self.current_player = 1
        self.is_connected = False
        self.is_playing = False
        self.progress = 0.0
        self.test_completed = False
        
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
        print(f"✅ Connected to ESP32!")
        
        # Start test sequence
        threading.Thread(target=self.test_sequence, daemon=True).start()
    
    def on_message(self, ws, message):
        """Handle incoming messages from ESP32"""
        try:
            data = json.loads(message)
            command = data.get("command")
            player = data.get("player")
            
            if player == self.current_player:
                print(f"📨 Player {player} received command: {command}")
                
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
                print(f"📤 Player {self.current_player} sent: {message}")
            except Exception as e:
                print(f"❌ Failed to send message: {e}")
    
    def send_ready(self, player_id):
        """Send ready status to ESP32"""
        message = {
            "player": player_id,
            "status": "ready"
        }
        self.current_player = player_id
        self.send_message(message)
    
    def start_playback(self, player_id):
        """Start video playback simulation"""
        self.current_player = player_id
        self.is_playing = True
        self.progress = 0.0
        print(f"▶️ Player {player_id} starting playback...")
        
        # Start progress simulation
        threading.Thread(target=self.simulate_progress, daemon=True).start()
    
    def simulate_progress(self):
        """Simulate video progress"""
        while self.is_playing and self.is_connected:
            time.sleep(0.3)  # Update every 300ms
            
            if self.is_playing:
                self.progress += 0.1  # Increase by 10% every 300ms
                
                if self.progress >= 1.0:
                    self.progress = 1.0
                    self.is_playing = False
                    print(f"✅ Player {self.current_player} playback completed!")
                    self.send_message({
                        "player": self.current_player,
                        "status": "stopped"
                    })
                    break
                
                # Send progress update
                self.send_message({
                    "player": self.current_player,
                    "status": "playing",
                    "progress": self.progress
                })
    
    def test_sequence(self):
        """Run test sequence for both players"""
        print("\n🧪 Starting dual player test sequence...")
        
        # Test Player 1
        print("\n--- TESTING PLAYER 1 ---")
        self.send_ready(1)
        time.sleep(2)
        self.start_playback(1)
        
        # Wait for Player 1 to complete
        while self.is_playing:
            time.sleep(1)
        
        time.sleep(2)
        
        # Test Player 2
        print("\n--- TESTING PLAYER 2 ---")
        self.send_ready(2)
        time.sleep(2)
        self.start_playback(2)
        
        # Wait for Player 2 to complete
        while self.is_playing:
            time.sleep(1)
        
        time.sleep(2)
        
        # Final ready for both
        print("\n--- FINAL TEST ---")
        self.send_ready(1)
        time.sleep(1)
        self.send_ready(2)
        
        print("\n🎯 Dual player test completed!")
        self.test_completed = True
        
        # Close connection
        if self.ws:
            self.ws.close()

def main():
    print("ESP32 VR LED Controller - Dual Player Test")
    print("=" * 50)
    
    # Get ESP32 IP from command line or use discovered IP
    esp32_ip = "192.168.15.6"  # Discovered IP
    
    if len(sys.argv) > 1:
        esp32_ip = sys.argv[1]
    
    print(f"🎯 Target ESP32 IP: {esp32_ip}")
    print(f"🔗 WebSocket URL: ws://{esp32_ip}:80/ws")
    
    # Create test client
    client = DualPlayerTest(esp32_ip)
    
    # Start connection
    try:
        client.connect()
    except KeyboardInterrupt:
        print("\n👋 Test interrupted by user")
    except Exception as e:
        print(f"❌ Test failed: {e}")

if __name__ == "__main__":
    main()
