#!/usr/bin/env python3
"""
Teste simples para verificar comandos off1 e off2 no ESP32
"""

import websocket
import time
import sys

def test_off_commands():
    esp32_ip = "192.168.15.6"
    ws_url = f"ws://{esp32_ip}/ws"
    
    print(f"Conectando ao ESP32: {ws_url}")
    
    try:
        ws = websocket.create_connection(ws_url, timeout=10)
        print("✅ Conectado ao ESP32!")
        
        # Teste sequencial dos comandos
        commands = [
            ("on1", "Player 1 pronto"),
            ("off1", "Player 1 offline"),
            ("on2", "Player 2 pronto"), 
            ("off2", "Player 2 offline"),
            ("on1", "Player 1 pronto novamente"),
            ("on2", "Player 2 pronto novamente"),
            ("off1", "Player 1 offline novamente"),
            ("off2", "Player 2 offline novamente")
        ]
        
        for command, description in commands:
            print(f"\n📤 Enviando: {command} - {description}")
            ws.send(command)
            time.sleep(2)  # Aguardar 2 segundos entre comandos
            
        print("\n✅ Teste concluído!")
        ws.close()
        
    except Exception as e:
        print(f"❌ Erro: {e}")
        return False
    
    return True

if __name__ == "__main__":
    print("🧪 Teste de Comandos off1/off2 - ESP32")
    print("=" * 50)
    
    success = test_off_commands()
    
    if success:
        print("\n🎉 Teste executado com sucesso!")
        print("Verifique os LEDs físicos do ESP32:")
        print("- LED 4 (Player 1): deve piscar verde com 'on1' e laranja com 'off1'")
        print("- LED 12 (Player 2): deve piscar verde com 'on2' e laranja com 'off2'")
    else:
        print("\n❌ Teste falhou!")
        sys.exit(1)
