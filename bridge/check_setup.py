#!/usr/bin/env python3
"""
MobileGram Bridge — Проверка окружения перед запуском (v0.5)
Запуск: ./venv/bin/python3 check_setup.py
"""

import os
import sys
import configparser

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

BRIDGE_DIR = os.path.dirname(os.path.abspath(__file__))
CONFIG_FILE = os.path.join(BRIDGE_DIR, "config_bridge.ini")
SESSION_NAME = os.environ.get("TG_SESSION_NAME", "mobilegram_session")
SESSION_FILE = os.path.join(BRIDGE_DIR, SESSION_NAME + ".session")

OK = "  [OK]"
WARN = "  [!!]"
ERR = "  [XX]"

print("=" * 60)
print("  MobileGram Bridge — Проверка окружения v0.5")
print("=" * 60)

all_ok = True

# 1. Python
print(f"\n[1] Python: {sys.version.split()[0]}")
if sys.version_info < (3, 7):
    print(f"{ERR} Требуется Python 3.7+")
    all_ok = False
else:
    print(f"{OK} Python версия подходит")

# 2. Telethon
try:
    import telethon
    print(f"{OK} Telethon {telethon.__version__} установлен")
except ImportError:
    print(f"{ERR} Telethon НЕ установлен! Выполните: pip install telethon")
    all_ok = False

# 3. aiohttp
try:
    import aiohttp
    print(f"{OK} aiohttp {aiohttp.__version__} установлен")
except ImportError:
    print(f"{ERR} aiohttp НЕ установлен! Выполните: pip install aiohttp")
    all_ok = False

# 4. Pillow
try:
    from PIL import Image
    import PIL
    print(f"{OK} Pillow {PIL.__version__} установлен")
except ImportError:
    print(f"{ERR} Pillow НЕ установлен! Выполните: pip install Pillow")
    all_ok = False

# 5. qrcode (for terminal QR code display)
try:
    import qrcode
    print(f"{OK} qrcode {qrcode.__version__} установлен (консольный QR готов)")
except ImportError:
    print(f"{WARN} qrcode не установлен (не критично, но рекомендуется: pip install qrcode)")

# 6. config_bridge.ini
print(f"\n[6] Конфигурация API:")
api_id = 0
api_hash = ""
if os.path.exists(CONFIG_FILE):
    cfg = configparser.ConfigParser()
    cfg.read(CONFIG_FILE, encoding="utf-8")
    try:
        api_id = int(cfg.get("bridge", "api_id", fallback="0"))
        api_hash = cfg.get("bridge", "api_hash", fallback="")
        print(f"{OK} config_bridge.ini найден")
    except Exception as e:
        print(f"{WARN} Ошибка чтения config_bridge.ini: {e}")
else:
    print(f"{WARN} config_bridge.ini ещё не создан (будет создан в login.py)")

api_id_env = os.environ.get("TG_API_ID", "")
api_hash_env = os.environ.get("TG_API_HASH", "")
if api_id_env:
    try:
        api_id = int(api_id_env)
        print(f"{OK} TG_API_ID задан через переменную окружения")
    except ValueError:
        pass
if api_hash_env:
    api_hash = api_hash_env
    print(f"{OK} TG_API_HASH задан через переменную окружения")

if api_id and api_id not in (0, 12345678):
    print(f"{OK} api_id = {api_id} (задан)")
else:
    print(f"{ERR} api_id не настроен! Запустите login.py")
    all_ok = False

if api_hash and api_hash not in ("", "your_api_hash_here"):
    masked = api_hash[:4] + "*" * (len(api_hash) - 8) + api_hash[-4:]
    print(f"{OK} api_hash = {masked} (задан)")
else:
    print(f"{ERR} api_hash не настроен! Запустите login.py")
    all_ok = False

# 7. Telegram session file
print(f"\n[7] Файл сессии: {SESSION_FILE}")
if os.path.exists(SESSION_FILE):
    size = os.path.getsize(SESSION_FILE)
    print(f"{OK} Файл сессии найден ({size} байт)")
else:
    print(f"{WARN} Файл сессии отсутствует (требуется вход через login.py или /qr)")
    all_ok = False

# 8. Summary
print("\n" + "=" * 60)
if all_ok:
    print("  [OK] Всё готово к запуску: ./venv/bin/python3 bridge.py")
else:
    print("  [!] Для завершения настройки запустите:")
    print("      ./venv/bin/python3 login.py")
    print("      (выберите [1] для мгновенного входа по QR-коду)")
print("=" * 60)
