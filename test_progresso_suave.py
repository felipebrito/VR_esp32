#!/usr/bin/env python3
"""
Teste de Progresso Suave - ESP32 LED Strip
Testa a nova funcionalidade de progresso gradual com luminosidade variável
"""

import asyncio
import websockets
import time

async def test_smooth_progress():
    """Testa progresso suave com diferentes percentuais"""
    
    # Conectar ao ESP32
    uri = "ws://192.168.15.6/ws"
    
    try:
        async with websockets.connect(uri) as websocket:
            print("🔗 Conectado ao ESP32")
            
            # Preparar Player 1
            print("\n📤 Player 1: Headset ON (LED 4 roxo piscando)")
            await websocket.send("on1")
            await asyncio.sleep(2)
            
            print("\n📤 Player 1: Iniciando filme")
            await websocket.send("play1")
            await asyncio.sleep(2)
            
            # Testar progresso suave com diferentes percentuais
            test_percentages = [5, 12, 25, 37, 50, 62, 75, 87, 100]
            
            for percent in test_percentages:
                print(f"\n📤 Player 1: Progresso {percent}% (deve mostrar progresso suave)")
                await websocket.send(f"led1:{percent}")
                await asyncio.sleep(1)
            
            print("\n📤 Player 1: Headset OFF (LED 4 roxo piscando)")
            await websocket.send("off1")
            await asyncio.sleep(2)
            
            print("\n📤 Player 1: Headset ON novamente (deve manter progresso)")
            await websocket.send("on1")
            await asyncio.sleep(2)
            
            # Testar Player 2
            print("\n📤 Player 2: Headset ON (LED 12 roxo piscando)")
            await websocket.send("on2")
            await asyncio.sleep(2)
            
            print("\n📤 Player 2: Iniciando filme")
            await websocket.send("play2")
            await asyncio.sleep(2)
            
            # Testar progresso suave Player 2
            for percent in test_percentages:
                print(f"\n📤 Player 2: Progresso {percent}% (deve mostrar progresso suave)")
                await websocket.send(f"led2:{percent}")
                await asyncio.sleep(1)
            
            print("\n📤 Player 2: Headset OFF (LED 12 roxo piscando)")
            await websocket.send("off2")
            await asyncio.sleep(2)
            
            print("\n📤 Limpando todos os LEDs")
            await websocket.send("led1:0")
            await websocket.send("led2:0")
            await asyncio.sleep(1)
            
            print("\n✅ Teste de progresso suave concluído!")
            
    except Exception as e:
        print(f"❌ Erro: {e}")

if __name__ == "__main__":
    print("🧪 Teste de Progresso Suave - ESP32 LED Strip")
    print("=" * 50)
    print("Testando:")
    print("- Progresso gradual com luminosidade variável")
    print("- LEDs roxo para headset OFF (em vez de laranja)")
    print("- Manutenção de progresso ao remover/colocar headset")
    print("=" * 50)
    
    asyncio.run(test_smooth_progress())
