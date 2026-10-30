# MobileGram

> A native Telegram client for **Windows Mobile 6 Professional / Classic** (.NET Compact Framework 3.5) paired with a high-performance **Python 3 MTProto Bridge**.

[![Platform](https://img.shields.io/badge/Platform-Windows%20Mobile%206.x-blue.svg)](https://en.wikipedia.org/wiki/Windows_Mobile)
[![Framework](https://img.shields.io/badge/.NET%20Compact%20Framework-3.5-brightgreen.svg)](https://en.wikipedia.org/.NET_Compact_Framework)
[![Bridge](https://img.shields.io/badge/Bridge-Python%203%20%2F%20Telethon-blueviolet.svg)](https://github.com/LonamiWebs/Telethon)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## Overview

**MobileGram** brings modern Telegram back to vintage Windows Mobile devices (such as the HTC P3300 Artemis, HTC Touch Cruise, HP iPAQ, and similar Pocket PCs). Designed specifically for QVGA (240×320) touchscreens and low-memory ARM processors (such as the TI OMAP 850 @ 201 MHz), MobileGram couples a native C# client styled after the classic Pocket MSN Messenger with a lightweight, asynchronous Python MTProto gateway.

```
+------------------------------------+          +------------------------------------+          +-----------------------+
|        Windows Mobile 6.x          |          |         MobileGram Bridge          |          |    Telegram Cloud     |
|   (HTC P3300 Artemis / OMAP 850)   |  HTTP    |    (Python 3 + Telethon + aiohttp) | MTProto  |   (Official Servers)  |
|                                    |<-------->|                                    |<-------->|                       |
|   - Pocket MSN Messenger UI        |  REST &  |   - Local / VPS Server             |  TCP/TLS |   - Telegram API      |
|   - Double-buffered GDI rendering  |  Polling |   - Web QR Login (/qr)             |          |   - Channels & Topics |
|   - Zero third-party runtime DLLs  |          |   - Media & Poll Transcoder        |          |   - Reactions & Polls |
+------------------------------------+          +------------------------------------+          +-----------------------+
```

---

## Key Features

- **Classic Pocket MSN Aesthetics & Double-Buffered GDI:** Smooth scrolling on 200 MHz CPUs with zero flicker, styled with familiar Windows Mobile MSN Messenger chat bubbles and delivery checkmarks.
- **Genuine Read Receipts (Honest Checkmarks):** Accurate single-tick (sent) and double-tick (read by peer) statuses synchronized in real time with Telegram's outbox read markers.
- **Forum Topics Support:** Full navigation and messaging inside Telegram Supergroups with topics enabled.
- **Quoted Replies with Jump Navigation:** Quoted reply headers (`↳ Sender: preview...`) rendered above messages. Tapping a quote jumps directly to the original message and highlights it with a blue glow.
- **Message Reactions:** View incoming reactions and send reactions (`👍 [+1]`, `❤️ [<3]`, `🔥 [TOP]`, `😂 [XD]`, `👎 [-1]`) through a stylus Tap & Hold context menu. Badges are rendered cleanly underneath message bubbles without overlapping.
- **Media Viewer:** View JPEG/PNG photos, avatars, and GIF previews adapted for QVGA screens.
- **Interactive Polls:** View questions, options, and vote percentages with native radio controls and progress bars.
- **Global Search:** Search local dialogs as well as the entire public Telegram directory (users, channels, and groups).
- **Smart Windows Mobile Notifications:** Automatic suppression of notification balloons when the active chat is in foreground, full sound/vibrate alerts when running in background, and per-chat Mute toggling.
- **Multilingual Support:** English and Russian localization with auto-detection of system locale and runtime switching in Settings.
- **Modern Web QR Authorization:** Built-in web dashboard at `http://<bridge-ip>:8080/qr` enabling instant login via official Telegram mobile app (Settings → Devices → Link Desktop Device), bypassing SMS/login code blocks (Issue #4730).

---

## Architecture & Design Decisions

### 1. Embedded ARM Optimization
Vintage Windows Mobile devices run on limited hardware (typically 64 MB RAM and 200 MHz single-core ARM CPUs). To avoid memory leaks and garbage collection pauses:
- All GDI `Pen`, `Brush`, and `Font` objects are strictly cached in static memory.
- Paint events operate entirely off-screen via an internal bitmap and transfer pixels using `BitBlt`.
- JSON serialization uses `MiniJson`, a zero-allocation recursive descent parser without external dependencies.

### 2. The Python MTProto Bridge
Direct MTProto implementation in .NET Compact Framework 3.5 is hindered by outdated TLS 1.0/1.1 crypto stacks and CPU exhaustion from heavy DH-key exchanges. The bridge handles:
- Full MTProto 2.0 communication over modern TLS 1.3 via `Telethon`.
- Transformation of complex Telegram TL objects into minimal JSON payloads.
- Background long-polling event dispatcher.

---

## Requirements

### Client Device
- **OS:** Windows Mobile 5.0, 6.0, 6.1, or 6.5 (Professional or Classic).
- **Framework:** Microsoft .NET Compact Framework 3.5 installed in ROM or device storage.
- **Screen:** QVGA (240×320) or higher (VGA/WVGA compatible). Touch screen / stylus recommended.
- **Connectivity:** Wi-Fi, ActiveSync pass-through, or GPRS/EDGE/3G.

### Bridge Server
- Python 3.9, 3.10, 3.11, 3.12, 3.13, or 3.14.
- Runs on any local PC, Raspberry Pi, home server, or Linux VPS (Debian, Ubuntu, Alpine, Windows, macOS).

---

## Quick Start Guide

### Step 1: Set Up the Bridge

1. Obtain your Telegram API credentials (`api_id` and `api_hash`) from [my.telegram.org](https://my.telegram.org).
2. Clone or extract the repository:
   ```bash
   cd bridge
   ```
3. Create a Python virtual environment and install dependencies:
   ```bash
   python3 -m venv venv
   source venv/bin/activate    # On Windows: venv\Scripts\activate
   pip install -r requirements.txt
   ```
4. Set your Telegram credentials via environment variables or a `.env` file:
   ```bash
   export TG_API_ID="12345678"
   export TG_API_HASH="0123456789abcdef0123456789abcdef"
   export BRIDGE_HOST="0.0.0.0"
   export BRIDGE_PORT="8080"
   ```
5. Start the bridge:
   ```bash
   python3 bridge.py
   ```
6. Open `http://<server-ip>:8080/qr` in any web browser on your PC or phone.
7. Open the official Telegram app on your smartphone, go to **Settings → Devices → Link Desktop Device**, and scan the QR code displayed on the webpage.

### Step 2: Install MobileGram on Windows Mobile

1. Copy `MobileGram.CAB` to your Pocket PC (via ActiveSync, Windows Mobile Device Center, or microSD card).
2. Open **File Explorer** on the device and tap `MobileGram.CAB` to install.
3. Launch **MobileGram** from the Start Menu (`Start → Programs → MobileGram`).
4. Enter your bridge IP address and port (e.g., `http://192.168.1.100:8080`).
5. Tap **Log In** / **Войти**. The client will verify the session and load your chats immediately!

---

## Building from Source

### Windows Mobile Client (C#)

1. Open `MobileGram.sln` in **Microsoft Visual Studio 2008** (Standard, Professional, or Team Edition).
2. Ensure the **Windows Mobile 6 Professional SDK** and **.NET Compact Framework 3.5** are installed.
3. Select configuration **Release** and target device **Windows Mobile 6 Professional Device** (or emulator).
4. Build the solution (`Ctrl+Shift+B`).
5. To generate `MobileGram.CAB`, run the Microsoft CabWiz tool:
   ```batch
   cd MobileGram
   "C:\Program Files (x86)\Microsoft Visual Studio 9.0\SmartDevices\SDK\SDKTools\cabwiz.exe" MobileGram.inf /dest bin\Release
   ```

---

## Project Structure

```
MobileGram/
├── README.md                      # Project documentation
├── MobileGram.sln                 # Visual Studio 2008 solution
├── bridge/                        # Python MTProto HTTP gateway
│   ├── bridge.py                  # Main asynchronous gateway (aiohttp + Telethon)
│   ├── requirements.txt           # Python dependencies (telethon, aiohttp, qrcode, pillow)
│   ├── check_setup.py             # Environment verification script
│   └── login.py                   # Standalone terminal/HTML QR login utility
└── MobileGram/                    # Windows Mobile C# application
    ├── MobileGram.csproj          # Project file (.NET Compact Framework 3.5)
    ├── MobileGram.inf             # CAB installer definition for CabWiz
    ├── App.ico                    # High-DPI application icon
    ├── Program.cs                 # Application entry point
    ├── Models.cs                  # Data models (ChatItem, MessageItem, ReactionBadge, Poll)
    ├── MiniJson.cs                # High-speed compact JSON parser
    ├── ConfigManager.cs           # Local XML configuration persistence
    ├── GatewayClient.cs           # HTTP communication client for the bridge
    ├── L10n.cs                    # Dual English / Russian localization engine
    ├── ChatListControl.cs         # Custom double-buffered chat list control
    ├── PocketMsnChatControl.cs    # Custom double-buffered message thread control
    ├── frmSettings.cs             # Login & bridge connection settings dialog
    ├── frmChatList.cs             # Main dialogs list and global search window
    ├── frmDirectChat.cs           # Conversation window (reactions, topics, replies, polls)
    ├── frmContactProfile.cs       # User & group profile viewer
    ├── frmTopicList.cs            # Forum topics selector dialog
    └── frmMediaViewer.cs          # Fullscreen QVGA photo & media viewer
```

---

## License

This project is licensed under the [MIT License](LICENSE).
Feel free to modify, distribute, and run on retro hardware!
