#!/usr/bin/env python3
"""
Script de teste automatizado para validar comunicação ESP32 ↔ Player Web
"""

import asyncio
import websockets
import json
import time
import requests

class ESP32Tester:
    def __init__(self, esp32_ip="192.168.15.6"):
        self.esp32_ip = esp32_ip
        self.ws_url = f"ws://{esp32_ip}/ws"
        self.http_url = f"http://{esp32_ip}/"
        self.websocket = None
        self.test_results = []
        
    async def connect(self):
        """Conectar ao WebSocket do ESP32"""
        try:
            print(f"🔌 Conectando ao ESP32 em {self.ws_url}...")
            self.websocket = await websockets.connect(self.ws_url)
            print("✅ Conectado com sucesso!")
            return True
        except Exception as e:
            print(f"❌ Erro ao conectar: {e}")
            return False
    
    async def disconnect(self):
        """Desconectar do WebSocket"""
        if self.websocket:
            await self.websocket.close()
            print("🔌 Desconectado do ESP32")
    
    async def send_command(self, command):
        """Enviar comando para ESP32"""
        try:
            print(f"📤 Enviando: {command}")
            await self.websocket.send(command)
            
            # Aguardar resposta
            try:
                response = await asyncio.wait_for(self.websocket.recv(), timeout=2.0)
                print(f"📥 Recebido: {response}")
                return response
            except asyncio.TimeoutError:
                print("⏰ Timeout - sem resposta")
                return None
                
        except Exception as e:
            print(f"❌ Erro ao enviar comando: {e}")
            return None
    
    def test_http_server(self):
        """Testar servidor HTTP"""
        print(f"\n🌐 Testando servidor HTTP em {self.http_url}...")
        try:
            response = requests.get(self.http_url, timeout=5)
            if response.status_code == 200:
                print("✅ Servidor HTTP funcionando")
                print(f"📄 Resposta: {response.text[:100]}...")
                return True
            else:
                print(f"❌ HTTP Status: {response.status_code}")
                return False
        except Exception as e:
            print(f"❌ Erro HTTP: {e}")
            return False
    
    async def test_basic_commands(self):
        """Testar comandos básicos"""
        print("\n🧪 Testando comandos básicos...")
        
        commands = [
            "on1",
            "play1", 
            "led1:25",
            "led1:50",
            "led1:75",
            "led1:100",
            "on2",
            "play2",
            "led2:30",
            "led2:60",
            "led2:90"
        ]
        
        for cmd in commands:
            await self.send_command(cmd)
            await asyncio.sleep(0.5)  # Pequena pausa entre comandos
    
    async def test_player_sequence(self):
        """Testar sequência completa de um player"""
        print("\n🎮 Testando sequência completa Player 1...")
        
        sequence = [
            ("on1", "Player 1 ready"),
            ("play1", "Player 1 playing"),
            ("led1:10", "Progresso 10%"),
            ("led1:30", "Progresso 30%"),
            ("led1:60", "Progresso 60%"),
            ("led1:90", "Progresso 90%"),
            ("led1:100", "Progresso 100%"),
        ]
        
        for cmd, description in sequence:
            print(f"🎯 {description}")
            await self.send_command(cmd)
            await asyncio.sleep(1)
    
    async def test_dual_players(self):
        """Testar dois players simultaneamente"""
        print("\n👥 Testando dois players simultaneamente...")
        
        # Player 1
        await self.send_command("on1")
        await asyncio.sleep(0.5)
        await self.send_command("play1")
        await asyncio.sleep(0.5)
        await self.send_command("led1:40")
        
        # Player 2
        await self.send_command("on2")
        await asyncio.sleep(0.5)
        await self.send_command("play2")
        await asyncio.sleep(0.5)
        await self.send_command("led2:60")
        
        # Aguardar um pouco
        await asyncio.sleep(2)
        
        # Atualizar progressos
        await self.send_command("led1:80")
        await self.send_command("led2:90")
        
        await asyncio.sleep(2)
        
        # Parar tudo
        await self.send_command("led1:0")
        await self.send_command("led2:0")
    
    async def run_all_tests(self):
        """Executar todos os testes"""
        print("🚀 Iniciando testes automatizados do ESP32...")
        print(f"🎯 IP do ESP32: {self.esp32_ip}")
        
        # Teste HTTP
        if not self.test_http_server():
            print("❌ Servidor HTTP não está funcionando")
            return
        
        # Conectar WebSocket
        if not await self.connect():
            print("❌ Não foi possível conectar ao WebSocket")
            return
        
        try:
            # Executar testes
            await self.test_basic_commands()
            await asyncio.sleep(2)
            
            await self.test_player_sequence()
            await asyncio.sleep(2)
            
            await self.test_dual_players()
            await asyncio.sleep(2)
            
            print("\n✅ Todos os testes concluídos!")
            print("\n🌐 Agora você pode usar o player web:")
            print("   1. Abra: http://localhost:8000")
            print("   2. Insira o IP: 192.168.15.6")
            print("   3. Clique em 'Conectar'")
            print("   4. Teste os comandos manualmente!")
            
        finally:
            await self.disconnect()

def main():
    """Função principal"""
    print("🥽 ESP32 VR LED Controller - Teste Automatizado")
    print("=" * 50)
    
    # Criar e executar testes
    tester = ESP32Tester("192.168.15.6")
    
    try:
        asyncio.run(tester.run_all_tests())
    except KeyboardInterrupt:
        print("\n⏹️ Teste interrompido pelo usuário")
    except Exception as e:
        print(f"\n❌ Erro durante os testes: {e}")

if __name__ == "__main__":
    main()
