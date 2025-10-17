#!/usr/bin/env python3
"""
Teste Completo do Firmware ESP32 - CoralVivoVR
Todas as funcionalidades disponíveis no firmware
"""

import tkinter as tk
from tkinter import ttk
import websocket
import threading
import time
import json

class ESP32TestComplete:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("CoralVivoVR - Teste Completo ESP32")
        self.root.geometry("1000x700")
        self.root.configure(bg='#2c3e50')
        
        # Estados dos jogadores
        self.player1_state = "idle"
        self.player2_state = "idle"
        self.player1_running = False
        self.player2_running = False
        
        # WebSocket
        self.ws = None
        self.connected = False
        
        # LEDs visuais
        self.player1_leds = []
        self.player2_leds = []
        
        self.setup_ui()
        
    def setup_ui(self):
        # Título
        title_label = tk.Label(
            self.root, 
            text="🎮 Teste Completo ESP32 - Todas as Funcionalidades", 
            font=("Arial", 16, "bold"),
            bg='#2c3e50',
            fg='#ecf0f1'
        )
        title_label.pack(pady=10)
        
        # Explicação completa
        info_text = """💡 COMANDOS DISPONÍVEIS NO FIRMWARE:
🔤 String Simples: on1/on2, play1/play2, off1/off2, pause1/pause2, led1:X/led2:X
🎨 Perda de Sinal: signal_lost1/2, signal_lost1:chase, signal_lost2:rainbow
📄 JSON: {"command":"ready"}, {"player":1,"status":"playing","progress":0.5}
🎮 Físicos: Botões longos (DISCONNECTED), Botão Effect (muda efeito)"""
        
        info_label = tk.Label(
            self.root, 
            text=info_text,
            font=("Arial", 9),
            bg='#2c3e50',
            fg='#f39c12',
            justify='left'
        )
        info_label.pack(pady=5)
        
        # Frame de conexão
        connection_frame = tk.Frame(self.root, bg='#34495e', relief='raised', bd=2)
        connection_frame.pack(fill='x', padx=20, pady=10)
        
        tk.Label(connection_frame, text="IP ESP32:", bg='#34495e', fg='white').pack(side='left', padx=5)
        
        self.ip_entry = tk.Entry(connection_frame, width=15)
        self.ip_entry.insert(0, "192.168.0.1")
        self.ip_entry.pack(side='left', padx=5)
        
        self.port_entry = tk.Entry(connection_frame, width=8)
        self.port_entry.insert(0, "80")
        self.port_entry.pack(side='left', padx=5)
        
        self.connect_btn = tk.Button(
            connection_frame, 
            text="Conectar", 
            command=self.connect_to_esp32,
            bg='#27ae60',
            fg='white',
            font=('Arial', 10, 'bold')
        )
        self.connect_btn.pack(side='left', padx=5)
        
        self.disconnect_btn = tk.Button(
            connection_frame, 
            text="Desconectar", 
            command=self.disconnect_from_esp32,
            bg='#e74c3c',
            fg='white',
            font=('Arial', 10, 'bold')
        )
        self.disconnect_btn.pack(side='left', padx=5)
        
        self.status_label = tk.Label(
            connection_frame, 
            text="Desconectado", 
            bg='#34495e', 
            fg='#e74c3c',
            font=('Arial', 10, 'bold')
        )
        self.status_label.pack(side='right', padx=10)
        
        # Frame principal
        main_frame = tk.Frame(self.root, bg='#2c3e50')
        main_frame.pack(fill='both', expand=True, padx=20, pady=10)
        
        # Jogador 1
        self.create_player_section(main_frame, 1, 0)
        
        # Jogador 2
        self.create_player_section(main_frame, 2, 1)
        
        # Frame de comandos especiais
        special_frame = tk.Frame(self.root, bg='#34495e', relief='raised', bd=2)
        special_frame.pack(fill='x', padx=20, pady=10)
        
        tk.Label(special_frame, text="🎨 Comandos Especiais:", bg='#34495e', fg='white', font=('Arial', 12, 'bold')).pack(side='left', padx=5)
        
        tk.Button(special_frame, text="🌈 Rainbow All", command=lambda: self.send_command("signal_lost1"), bg='#ff6b6b', fg='white').pack(side='left', padx=2)
        tk.Button(special_frame, text="🏃 Chase All", command=lambda: self.send_command("signal_lost2"), bg='#4ecdc4', fg='white').pack(side='left', padx=2)
        tk.Button(special_frame, text="🔄 Reset All", command=self.reset_all, bg='#95a5a6', fg='white').pack(side='left', padx=2)
        tk.Button(special_frame, text="📊 Status", command=self.show_status, bg='#3498db', fg='white').pack(side='left', padx=2)
        
    def create_player_section(self, parent, player_num, column):
        # Frame do jogador
        player_frame = tk.Frame(parent, bg='#34495e', relief='raised', bd=2)
        player_frame.grid(row=0, column=column, padx=10, pady=5, sticky='nsew')
        
        # Título do jogador
        title_label = tk.Label(
            player_frame, 
            text=f"🎮 Jogador {player_num}", 
            font=("Arial", 14, "bold"),
            bg='#34495e',
            fg='#ecf0f1'
        )
        title_label.pack(pady=10)
        
        # LEDs visuais
        leds_frame = tk.Frame(player_frame, bg='#34495e')
        leds_frame.pack(pady=5)
        
        leds = []
        for i in range(8):
            led = tk.Label(
                leds_frame, 
                text=str(i+1), 
                width=3, 
                height=1,
                bg='#2c3e50',
                fg='white',
                font=('Arial', 8),
                relief='raised',
                bd=1
            )
            led.grid(row=0, column=i, padx=1)
            leds.append(led)
        
        if player_num == 1:
            self.player1_leds = leds
        else:
            self.player2_leds = leds
        
        # Botões de controle
        controls_frame = tk.Frame(player_frame, bg='#34495e')
        controls_frame.pack(pady=10)
        
        # Comandos básicos
        tk.Button(controls_frame, text="🟢 Ready", command=lambda: self.send_command(f"on{player_num}"), bg='#27ae60', fg='white', width=15).pack(pady=1)
        tk.Button(controls_frame, text="🔵 Play", command=lambda: self.send_command(f"play{player_num}"), bg='#3498db', fg='white', width=15).pack(pady=1)
        tk.Button(controls_frame, text="⏸️ Pause", command=lambda: self.send_command(f"pause{player_num}"), bg='#f39c12', fg='white', width=15).pack(pady=1)
        tk.Button(controls_frame, text="🔴 Headset OFF", command=lambda: self.send_command(f"off{player_num}"), bg='#e74c3c', fg='white', width=15).pack(pady=1)
        
        # Comandos de perda de sinal - ambos os jogadores têm ambos os efeitos
        tk.Button(controls_frame, text="🌈 Rainbow", command=lambda: self.send_command(f"signal_lost{player_num}:rainbow" if player_num == 2 else f"signal_lost{player_num}"), bg='#ff6b6b', fg='white', width=15).pack(pady=1)
        tk.Button(controls_frame, text="🏃 Chase", command=lambda: self.send_command(f"signal_lost{player_num}:chase" if player_num == 1 else f"signal_lost{player_num}"), bg='#4ecdc4', fg='white', width=15).pack(pady=1)
        
        # Controle de progresso
        progress_frame = tk.Frame(controls_frame, bg='#34495e')
        progress_frame.pack(pady=5)
        
        tk.Label(progress_frame, text="Progresso:", bg='#34495e', fg='white').pack(side='left')
        
        self.progress_var = tk.IntVar()
        progress_scale = tk.Scale(
            progress_frame, 
            from_=0, 
            to=100, 
            orient='horizontal',
            variable=self.progress_var,
            command=lambda x: self.send_command(f"led{player_num}:{int(x)}"),
            bg='#34495e',
            fg='white',
            length=150
        )
        progress_scale.pack(side='left', padx=5)
        
        # Comandos JSON
        json_frame = tk.Frame(controls_frame, bg='#34495e')
        json_frame.pack(pady=5)
        
        tk.Button(json_frame, text="📄 JSON Ready", command=lambda: self.send_json_command(player_num, "ready"), bg='#9b59b6', fg='white', width=12).pack(side='left', padx=1)
        tk.Button(json_frame, text="📄 JSON Play", command=lambda: self.send_json_command(player_num, "playing"), bg='#8e44ad', fg='white', width=12).pack(side='left', padx=1)
        
        # Configurar grid
        parent.grid_columnconfigure(0, weight=1)
        parent.grid_columnconfigure(1, weight=1)
        
    def connect_to_esp32(self):
        try:
            ip = self.ip_entry.get()
            port = self.port_entry.get()
            url = f"ws://{ip}:{port}/ws"
            
            print(f"🔌 Conectando ao ESP32: {url}")
            
            self.ws = websocket.WebSocketApp(
                url,
                on_open=self.on_open,
                on_message=self.on_message,
                on_error=self.on_error,
                on_close=self.on_close
            )
            
            # Executar em thread separada
            thread = threading.Thread(target=self.ws.run_forever)
            thread.daemon = True
            thread.start()
            
        except Exception as e:
            print(f"❌ Erro ao conectar: {e}")
            
    def disconnect_from_esp32(self):
        if self.ws:
            self.ws.close()
            self.connected = False
            self.status_label.config(text="Desconectado", fg='#e74c3c')
            print("🔌 Desconectado do ESP32")
            
    def on_open(self, ws):
        self.connected = True
        self.status_label.config(text="Conectado", fg='#27ae60')
        print("✅ Conectado ao ESP32")
        
    def on_message(self, ws, message):
        print(f"📨 ESP32: {message}")
        
    def on_error(self, ws, error):
        print(f"❌ Erro WebSocket: {error}")
        
    def on_close(self, ws, close_status_code, close_msg):
        self.connected = False
        self.status_label.config(text="Desconectado", fg='#e74c3c')
        print("🔌 Conexão fechada")
        
    def send_command(self, command):
        if self.connected and self.ws:
            try:
                self.ws.send(command)
                print(f"✅ Comando enviado: {command}")
            except Exception as e:
                print(f"❌ Erro ao enviar comando: {e}")
        else:
            print("❌ Não conectado ao ESP32")
            
    def send_json_command(self, player_num, status):
        if self.connected and self.ws:
            try:
                if status == "playing":
                    # Comando JSON com progresso
                    command = {
                        "player": player_num,
                        "status": "playing",
                        "progress": 0.5
                    }
                else:
                    # Comando JSON simples
                    command = {
                        "player": player_num,
                        "status": status
                    }
                
                json_str = json.dumps(command)
                self.ws.send(json_str)
                print(f"✅ Comando JSON enviado: {json_str}")
            except Exception as e:
                print(f"❌ Erro ao enviar JSON: {e}")
        else:
            print("❌ Não conectado ao ESP32")
            
    def reset_all(self):
        """Reset todos os jogadores"""
        self.send_command("on1")
        time.sleep(0.5)
        self.send_command("on2")
        print("🔄 Reset completo executado")
        
    def show_status(self):
        """Mostra status atual"""
        print("📊 Status atual:")
        print(f"   Jogador 1: {self.player1_state}")
        print(f"   Jogador 2: {self.player2_state}")
        print(f"   Conectado: {self.connected}")
        
    def run(self):
        self.root.mainloop()

if __name__ == "__main__":
    app = ESP32TestComplete()
    app.run()
