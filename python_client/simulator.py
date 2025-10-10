#!/usr/bin/env python3
"""
ESP32 VR LED Controller - Python WebSocket Client Simulator
Simulates VR headset behavior for testing the ESP32 controller
"""

import websocket
import json
import threading
import time
import sys
from datetime import datetime

class VRHeadsetSimulator:
    def __init__(self, esp32_ip="192.168.1.100", port=80):
        self.ws_url = f"ws://{esp32_ip}:{port}"
        self.ws = None
        self.player_id = 1
        self.is_connected = False
        self.is_playing = False
        self.progress = 0.0
        self.running = True
        
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
        print(f"Connected to ESP32 as Player {self.player_id}")
        self.send_ready()
    
    def on_message(self, ws, message):
        """Handle incoming messages from ESP32"""
        try:
            data = json.loads(message)
            command = data.get("command")
            player = data.get("player")
            
            if player == self.player_id:
                print(f"Received command: {command}")
                
                if command == "play":
                    self.start_playback()
                elif command == "pause":
                    self.pause_playback()
                elif command == "stop":
                    self.stop_playback()
                    
        except json.JSONDecodeError:
            print(f"Invalid JSON received: {message}")
    
    def on_error(self, ws, error):
        """Handle WebSocket errors"""
        print(f"WebSocket error: {error}")
    
    def on_close(self, ws, close_status_code, close_msg):
        """Handle WebSocket close"""
        self.is_connected = False
        print("Disconnected from ESP32")
    
    def send_message(self, message):
        """Send message to ESP32"""
        if self.is_connected and self.ws:
            try:
                self.ws.send(json.dumps(message))
                print(f"Sent: {message}")
            except Exception as e:
                print(f"Failed to send message: {e}")
    
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
            print("Starting playback...")
            
            # Start progress simulation in a separate thread
            threading.Thread(target=self.simulate_progress, daemon=True).start()
    
    def pause_playback(self):
        """Pause video playback"""
        self.is_playing = False
        print("Playback paused")
    
    def stop_playback(self):
        """Stop video playback"""
        self.is_playing = False
        self.progress = 0.0
        print("Playback stopped")
        self.send_ready()
    
    def simulate_progress(self):
        """Simulate video progress"""
        while self.is_playing and self.is_connected:
            time.sleep(0.1)  # Update every 100ms
            
            if self.is_playing:
                self.progress += 0.01  # Increase by 1% every 100ms
                
                if self.progress >= 1.0:
                    self.progress = 1.0
                    self.is_playing = False
                    print("Playback completed!")
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
    
    def manual_control(self):
        """Manual control interface"""
        print("\n=== Manual Control ===")
        print("Commands:")
        print("  r - Send ready status")
        print("  p - Start playback")
        print("  s - Stop playback")
        print("  q - Quit")
        print("  h - Show this help")
        
        while self.running:
            try:
                cmd = input("\nEnter command: ").strip().lower()
                
                if cmd == 'q':
                    self.running = False
                    if self.ws:
                        self.ws.close()
                    break
                elif cmd == 'r':
                    self.send_ready()
                elif cmd == 'p':
                    self.start_playback()
                elif cmd == 's':
                    self.stop_playback()
                elif cmd == 'h':
                    print("Commands: r=ready, p=play, s=stop, q=quit, h=help")
                else:
                    print("Unknown command. Type 'h' for help.")
                    
            except KeyboardInterrupt:
                self.running = False
                if self.ws:
                    self.ws.close()
                break

def main():
    print("ESP32 VR LED Controller - Headset Simulator")
    print("=" * 50)
    
    # Get ESP32 IP address from command line or use default
    import sys
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
    
    print(f"Using ESP32 IP: {esp32_ip}")
    print(f"Using Player ID: {player_id}")
    
    # Create simulator
    simulator = VRHeadsetSimulator(esp32_ip)
    simulator.player_id = player_id
    
    # Start connection in a separate thread
    connection_thread = threading.Thread(target=simulator.connect, daemon=True)
    connection_thread.start()
    
    # Wait a moment for connection
    time.sleep(2)
    
    # Start manual control interface
    simulator.manual_control()

if __name__ == "__main__":
    main()
