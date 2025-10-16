#!/usr/bin/env python3
import asyncio
import websockets
import json

async def test_buttons_simple():
    try:
        print("🔌 Conectando...")
        async with websockets.connect("ws://192.168.0.1:80/ws") as ws:
            print("✅ Conectado!")
            
            # Enviar ready
            await ws.send(json.dumps({"player": 1, "status": "ready"}))
            print("📤 Player 1 pronto")
            
            print("\n🎮 Pressione os botões na ESP32 agora!")
            print("Aguardando 10 segundos...")
            
            # Aguardar mensagens por 10 segundos
            for i in range(10):
                try:
                    message = await asyncio.wait_for(ws.recv(), timeout=1.0)
                    print(f"📨 Recebido: {message}")
                    
                    # Parsear JSON
                    try:
                        data = json.loads(message)
                        if "command" in data:
                            cmd = data["command"]
                            player = data.get("player", "N/A")
                            print(f"🎮 COMANDO: {cmd} (Player: {player})")
                    except:
                        print(f"📝 Texto: {message}")
                        
                except asyncio.TimeoutError:
                    print(f"⏳ {i+1}/10 - Aguardando...")
                    
            print("✅ Teste finalizado")
            
    except Exception as e:
        print(f"❌ Erro: {e}")

asyncio.run(test_buttons_simple())
