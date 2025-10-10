#!/usr/bin/env python3
"""
Teste simples de comandos ESP32 via WebSocket
"""

import asyncio
import websockets
import sys

async def test_command(ip, command):
    """Testar um comando específico"""
    uri = f"ws://{ip}/ws"
    
    try:
        print(f"🔌 Conectando ao ESP32 em {uri}...")
        async with websockets.connect(uri) as websocket:
            print(f"✅ Conectado! Enviando: {command}")
            await websocket.send(command)
            print(f"📤 Comando '{command}' enviado!")
            
            # Aguardar um pouco para ver o resultado
            await asyncio.sleep(2)
            print("✅ Teste concluído!")
            
    except Exception as e:
        print(f"❌ Erro: {e}")

def main():
    if len(sys.argv) != 3:
        print("Uso: python3 test_simple.py <IP_ESP32> <COMANDO>")
        print("Exemplos:")
        print("  python3 test_simple.py 192.168.15.6 on1")
        print("  python3 test_simple.py 192.168.15.6 led1:50")
        print("  python3 test_simple.py 192.168.15.6 play1")
        sys.exit(1)
    
    ip = sys.argv[1]
    command = sys.argv[2]
    
    print(f"🧪 Testando comando '{command}' no ESP32 {ip}")
    asyncio.run(test_command(ip, command))

if __name__ == "__main__":
    main()
