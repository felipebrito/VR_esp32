#!/usr/bin/env python3
"""
Teste específico para verificar comportamento Headset OFF/ON
"""

import websocket
import time
import sys

def test_headset_off_on():
    esp32_ip = "192.168.15.6"
    ws_url = f"ws://{esp32_ip}/ws"
    
    print(f"Conectando ao ESP32: {ws_url}")
    
    try:
        ws = websocket.create_connection(ws_url, timeout=10)
        print("✅ Conectado ao ESP32!")
        
        # Sequência completa de teste
        test_sequence = [
            # Player 1: Ligar headset e iniciar filme
            ("on1", "Player 1: Headset ON (LED 4 verde piscando)"),
            ("play1", "Player 1: Filme iniciado (LEDs 1-8 azuis progressivos)"),
            ("led1:50", "Player 1: Filme no meio (LEDs 1-4 azuis)"),
            
            # Player 1: Remover headset (deve pausar e LED laranja)
            ("off1", "Player 1: Headset OFF (LED 4 laranja piscando)"),
            
            # Player 1: Colocar headset novamente (deve retomar)
            ("on1", "Player 1: Headset ON novamente (LED 4 verde piscando)"),
            ("led1:75", "Player 1: Retomar filme (LEDs 1-6 azuis)"),
            
            # Player 2: Mesmo teste
            ("on2", "Player 2: Headset ON (LED 12 verde piscando)"),
            ("play2", "Player 2: Filme iniciado (LEDs 9-16 vermelhos progressivos)"),
            ("led2:30", "Player 2: Filme no início (LEDs 9-11 vermelhos)"),
            
            # Player 2: Remover headset
            ("off2", "Player 2: Headset OFF (LED 12 laranja piscando)"),
            
            # Player 2: Colocar headset novamente
            ("on2", "Player 2: Headset ON novamente (LED 12 verde piscando)"),
            ("led2:80", "Player 2: Retomar filme (LEDs 9-15 vermelhos)"),
        ]
        
        for command, description in test_sequence:
            print(f"\n📤 {description}")
            print(f"   Comando: {command}")
            ws.send(command)
            time.sleep(3)  # Aguardar 3 segundos para observar LEDs
            
        print("\n✅ Teste completo!")
        ws.close()
        
    except Exception as e:
        print(f"❌ Erro: {e}")
        return False
    
    return True

if __name__ == "__main__":
    print("🧪 Teste Headset OFF/ON - Comportamento Correto")
    print("=" * 60)
    print("Comportamento esperado:")
    print("- Headset ON: LED verde piscando (pronto)")
    print("- Filme rodando: LEDs progressivos azuis/vermelhos")
    print("- Headset OFF: LED laranja piscando (pausado)")
    print("- Headset ON novamente: Retomar do ponto onde parou")
    print("=" * 60)
    
    success = test_headset_off_on()
    
    if success:
        print("\n🎉 Teste executado com sucesso!")
        print("\n📋 Verifique os LEDs físicos:")
        print("Player 1 (LEDs 1-8):")
        print("  - LED 4 verde piscando = Headset ON")
        print("  - LEDs 1-8 azuis = Filme rodando")
        print("  - LED 4 laranja piscando = Headset OFF (pausado)")
        print("\nPlayer 2 (LEDs 9-16):")
        print("  - LED 12 verde piscando = Headset ON")
        print("  - LEDs 9-16 vermelhos = Filme rodando")
        print("  - LED 12 laranja piscando = Headset OFF (pausado)")
    else:
        print("\n❌ Teste falhou!")
        sys.exit(1)
