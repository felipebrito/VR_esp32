#!/usr/bin/env python3
"""
Teste completo dos comandos ESP32 atualizados
"""

import asyncio
import websockets
import time

async def test_all_commands():
    """Testar todos os comandos atualizados"""
    uri = "ws://192.168.15.6/ws"
    
    print("🧪 Teste Completo dos Comandos ESP32 Atualizados")
    print("=" * 50)
    
    try:
        print("🔌 Conectando ao ESP32...")
        async with websockets.connect(uri) as websocket:
            print("✅ Conectado!")
            
            # Teste 1: Player 1 Ready (verde)
            print("\n📋 Teste 1: Player 1 Ready (LED 4 verde)")
            await websocket.send("on1")
            await asyncio.sleep(2)
            
            # Teste 2: Player 1 Playing (azul)
            print("📋 Teste 2: Player 1 Playing (LEDs 1-8 azul)")
            await websocket.send("play1")
            await asyncio.sleep(3)
            
            # Teste 3: Player 1 Pause (azul dimmed)
            print("📋 Teste 3: Player 1 Pause (LEDs dimmed)")
            await websocket.send("pause1")
            await asyncio.sleep(2)
            
            # Teste 4: Player 1 Offline (laranja)
            print("📋 Teste 4: Player 1 Offline (LED 4 laranja)")
            await websocket.send("off1")
            await asyncio.sleep(2)
            
            # Teste 5: Player 2 Ready (verde)
            print("📋 Teste 5: Player 2 Ready (LED 12 verde)")
            await websocket.send("on2")
            await asyncio.sleep(2)
            
            # Teste 6: Player 2 Playing (vermelho)
            print("📋 Teste 6: Player 2 Playing (LEDs 9-16 vermelho)")
            await websocket.send("play2")
            await asyncio.sleep(3)
            
            # Teste 7: Player 2 Pause (vermelho dimmed)
            print("📋 Teste 7: Player 2 Pause (LEDs dimmed)")
            await websocket.send("pause2")
            await asyncio.sleep(2)
            
            # Teste 8: Player 2 Offline (laranja)
            print("📋 Teste 8: Player 2 Offline (LED 12 laranja)")
            await websocket.send("off2")
            await asyncio.sleep(2)
            
            # Teste 9: Progresso direto Player 1
            print("📋 Teste 9: Progresso direto Player 1 (50%)")
            await websocket.send("led1:50")
            await asyncio.sleep(2)
            
            # Teste 10: Progresso direto Player 2
            print("📋 Teste 10: Progresso direto Player 2 (75%)")
            await websocket.send("led2:75")
            await asyncio.sleep(2)
            
            # Teste 11: Limpar tudo
            print("📋 Teste 11: Limpar tudo")
            await websocket.send("led1:0")
            await websocket.send("led2:0")
            await asyncio.sleep(1)
            
            print("\n✅ Todos os testes concluídos!")
            print("\n🎯 Comportamentos esperados:")
            print("  - LED 4 verde: Player 1 pronto")
            print("  - LED 4 laranja: Player 1 offline")
            print("  - LEDs 1-8 azul: Player 1 progresso")
            print("  - LED 12 verde: Player 2 pronto")
            print("  - LED 12 laranja: Player 2 offline")
            print("  - LEDs 9-16 vermelho: Player 2 progresso")
            
    except Exception as e:
        print(f"❌ Erro: {e}")

if __name__ == "__main__":
    asyncio.run(test_all_commands())
