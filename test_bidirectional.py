#!/usr/bin/env python3
import asyncio
import websockets
import json
import time

async def test_bidirectional():
    try:
        print("🔌 Conectando ao ESP32...")
        async with websockets.connect("ws://192.168.0.1:80/ws") as ws:
            print("✅ Conectado!")
            
            # Enviar ready
            await ws.send(json.dumps({"player": 1, "status": "ready"}))
            print("📤 Player 1 pronto")
            
            # Enviar comando para controlar LED
            print("🎨 Enviando comando para controlar LED...")
            await ws.send(json.dumps({"player": 1, "message": "led1:50"}))
            print("📤 Comando LED enviado (50%)")
            
            print("\n🎮 Pressione os botões na ESP32!")
            print("Aguardando 15 segundos...")
            
            message_count = 0
            for i in range(15):
                try:
                    message = await asyncio.wait_for(ws.recv(), timeout=1.0)
                    message_count += 1
                    print(f"📨 Mensagem #{message_count}: {message}")
                    
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
                    print(f"⏳ {i+1}/15 - Aguardando...")
                    
            print(f"✅ Teste finalizado. Total de mensagens: {message_count}")
            
    except Exception as e:
        print(f"❌ Erro: {e}")

asyncio.run(test_bidirectional())
