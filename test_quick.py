#!/usr/bin/env python3
"""
Teste rápido de WebSocket
"""

import asyncio
import websockets
import json

async def quick_test():
    try:
        print("🔌 Testando conexão WebSocket...")
        async with websockets.connect("ws://192.168.0.1:80/ws") as websocket:
            print("✅ Conectado!")
            
            # Enviar mensagem
            message = {"player": 1, "status": "ready"}
            await websocket.send(json.dumps(message))
            print(f"📤 Enviado: {message}")
            
            # Tentar receber uma mensagem
            try:
                response = await asyncio.wait_for(websocket.recv(), timeout=3)
                print(f"📥 Recebido: {response}")
            except asyncio.TimeoutError:
                print("⏰ Nenhuma resposta (normal)")
                
            print("✅ Teste concluído com sucesso!")
            
    except Exception as e:
        print(f"❌ Erro: {e}")

if __name__ == "__main__":
    asyncio.run(quick_test())
