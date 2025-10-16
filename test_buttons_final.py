#!/usr/bin/env python3
import asyncio
import websockets
import json
import time

async def test_buttons():
    try:
        print("🔌 Conectando ao ESP32...")
        async with websockets.connect("ws://192.168.0.1:80/ws") as ws:
            print("✅ Conectado!")
            
            # Enviar ready
            await ws.send(json.dumps({"player": 1, "status": "ready"}))
            print("📤 Player 1 pronto")
            
            print("\n🎮 TESTE DOS BOTÕES")
            print("=" * 30)
            print("Pressione os botões físicos na ESP32:")
            print("  • Botão 1: Play/Pause")
            print("  • Botão 2: Stop")
            print("\nAguardando comandos... (Ctrl+C para sair)")
            
            message_count = 0
            start_time = time.time()
            
            while True:
                try:
                    message = await asyncio.wait_for(ws.recv(), timeout=1.0)
                    message_count += 1
                    elapsed = int(time.time() - start_time)
                    
                    print(f"\n📨 [{elapsed}s] Mensagem #{message_count}:")
                    print(f"   Raw: {message}")
                    
                    # Parsear JSON
                    try:
                        data = json.loads(message)
                        if "command" in data:
                            cmd = data["command"]
                            player = data.get("player", "N/A")
                            print(f"   🎮 COMANDO: {cmd} (Player: {player})")
                            
                            if cmd == "play":
                                print("   ▶️  AÇÃO: Iniciar vídeo")
                            elif cmd == "pause":
                                print("   ⏸️  AÇÃO: Pausar vídeo")
                            elif cmd == "stop":
                                print("   ⏹️  AÇÃO: Parar vídeo")
                                
                    except json.JSONDecodeError:
                        print(f"   📝 Texto simples: {message}")
                        
                except asyncio.TimeoutError:
                    # Mostrar status a cada 10 segundos
                    elapsed = int(time.time() - start_time)
                    if elapsed % 10 == 0 and elapsed > 0:
                        print(f"⏳ {elapsed}s - Aguardando botões... (pressione na ESP32)")
                    continue
                    
    except KeyboardInterrupt:
        print(f"\n👋 Teste finalizado. Total de mensagens: {message_count}")
    except Exception as e:
        print(f"❌ Erro: {e}")

asyncio.run(test_buttons())

