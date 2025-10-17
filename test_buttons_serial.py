#!/usr/bin/env python3
"""
Teste simples para verificar se os botões estão funcionando
"""

import serial
import time

def test_buttons_serial():
    try:
        print("🔌 Conectando à ESP32 via serial...")
        ser = serial.Serial('/dev/cu.usbserial-110', 115200, timeout=1)
        print("✅ Conectado via serial!")
        
        print("\n🎮 TESTE DOS BOTÕES VIA SERIAL")
        print("=" * 40)
        print("Pressione os botões físicos na ESP32")
        print("Você deve ver logs como:")
        print("  • '=== BUTTON 1 PRESSED ==='")
        print("  • 'Button 1 pressed - toggling play/pause'")
        print("  • 'SENDCMD: Tentando enviar comando...'")
        print("\nAguardando logs... (Ctrl+C para sair)")
        
        message_count = 0
        start_time = time.time()
        
        while True:
            try:
                line = ser.readline().decode('utf-8').strip()
                if line:
                    message_count += 1
                    elapsed = int(time.time() - start_time)
                    print(f"[{elapsed}s] {line}")
                    
                    # Detectar logs de botão
                    if "BUTTON" in line or "SENDCMD" in line:
                        print(f"🎮 BOTÃO DETECTADO: {line}")
                        
            except KeyboardInterrupt:
                print(f"\n👋 Teste finalizado. Total de mensagens: {message_count}")
                break
            except Exception as e:
                print(f"❌ Erro: {e}")
                break
                
        ser.close()
        
    except Exception as e:
        print(f"❌ Erro ao conectar serial: {e}")
        print("Verifique se a ESP32 está conectada e a porta está correta")

if __name__ == "__main__":
    test_buttons_serial()

