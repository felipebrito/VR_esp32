#!/usr/bin/env python3
"""
Teste direto de comunicação WebSocket com ESP32
"""
import websocket
import json
import time

def on_message(ws, message):
    print(f'📨 ESP32 → Unity: {message}')

def on_error(ws, error):
    print(f'❌ Erro: {error}')

def on_close(ws, close_status_code, close_msg):
    print('🔌 Conexão fechada')

def on_open(ws):
    print('✅ Conectado ao ESP32 WebSocket!')
    
    # Simular Player 1 conectando
    ready_message = {
        "player": 1,
        "status": "ready"
    }
    ws.send(json.dumps(ready_message))
    print(f'📤 Unity → ESP32: {json.dumps(ready_message)}')
    
    # Simular progresso LED
    time.sleep(2)
    led_message = {
        "player": 1,
        "message": "led1:25"
    }
    ws.send(json.dumps(led_message))
    print(f'📤 Unity → ESP32: {json.dumps(led_message)}')
    
    time.sleep(2)
    led_message2 = {
        "player": 1,
        "message": "led1:50"
    }
    ws.send(json.dumps(led_message2))
    print(f'📤 Unity → ESP32: {json.dumps(led_message2)}')

if __name__ == "__main__":
    print("🧪 Teste de Comunicação WebSocket ESP32")
    print("=====================================")
    
    ws = websocket.WebSocketApp('ws://192.168.0.1:80/ws',
                                on_open=on_open,
                                on_message=on_message,
                                on_error=on_error,
                                on_close=on_close)
    
    print("Conectando ao ESP32...")
    ws.run_forever()
