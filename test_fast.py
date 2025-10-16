#!/usr/bin/env python3
import asyncio
import websockets
import json

async def test():
    try:
        print("🔌 Conectando...")
        async with websockets.connect("ws://192.168.0.1:80/ws") as ws:
            print("✅ Conectado!")
            
            # Enviar ready
            await ws.send(json.dumps({"player": 1, "status": "ready"}))
            print("📤 Enviado ready")
            
            # Aguardar 5 segundos por mensagens
            print("🎮 Aguardando 5 segundos... (pressione botões)")
            
            for i in range(5):
                try:
                    msg = await asyncio.wait_for(ws.recv(), timeout=1.0)
                    print(f"📨 Recebido: {msg}")
                except asyncio.TimeoutError:
                    print(f"⏳ {i+1}/5 - Nenhuma mensagem")
                    
            print("✅ Teste finalizado")
            
    except Exception as e:
        print(f"❌ Erro: {e}")

asyncio.run(test())
