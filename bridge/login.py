#!/usr/bin/env python3
"""
MobileGram Bridge — Интерактивная авторизация в Telegram (login.py v0.5)

РЕШЕНИЕ ПРОБЛЕМЫ Issue #4730 ("no login code anymore"):
Telegram перестал присылать коды подтверждения (SMS и сообщения в чат)
при входе через сторонние клиенты на серверных/LXC IP-адресах.
Официальный и 100% надёжный способ авторизации — вход по QR-коду (qr_login)!

Запуск:
    ./venv/bin/python3 login.py
"""

import os
import sys
import asyncio
import urllib.parse
import configparser

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

# Libraries
from telethon import TelegramClient, errors
from telethon.errors import (
    SessionPasswordNeededError,
    PhoneCodeInvalidError,
    PhoneCodeExpiredError,
    FloodWaitError,
    ApiIdInvalidError
)

try:
    import qrcode
except ImportError:
    qrcode = None

BRIDGE_DIR = os.path.dirname(os.path.abspath(__file__))
CONFIG_FILE = os.path.join(BRIDGE_DIR, "config_bridge.ini")
SESSION_NAME = os.environ.get("TG_SESSION_NAME", "mobilegram_session")

# Official Telegram Desktop client parameters
CLIENT_PARAMS = {
    "device_model": "Desktop",
    "system_version": "Windows 10",
    "app_version": "4.16.8 x64",
    "lang_code": "ru",
    "system_lang_code": "ru-RU"
}


def load_config():
    """Загрузка api_id и api_hash из файла или env."""
    cfg = configparser.ConfigParser()
    cfg.read(CONFIG_FILE, encoding="utf-8")

    api_id_str = os.environ.get("TG_API_ID", "")
    api_hash = os.environ.get("TG_API_HASH", "")

    if not api_id_str and cfg.has_option("bridge", "api_id"):
        api_id_str = cfg.get("bridge", "api_id")
    if not api_hash and cfg.has_option("bridge", "api_hash"):
        api_hash = cfg.get("bridge", "api_hash")

    try:
        api_id = int(api_id_str)
    except (ValueError, TypeError):
        api_id = 0

    return api_id, api_hash


def save_config(api_id: int, api_hash: str):
    """Сохранение настроек в config_bridge.ini."""
    cfg = configparser.ConfigParser()
    cfg.read(CONFIG_FILE, encoding="utf-8")
    if not cfg.has_section("bridge"):
        cfg.add_section("bridge")
    cfg.set("bridge", "api_id", str(api_id))
    cfg.set("bridge", "api_hash", api_hash)
    with open(CONFIG_FILE, "w", encoding="utf-8") as f:
        cfg.write(f)
    print(f"[OK] Данные сохранены в {CONFIG_FILE}")


def display_qr(url: str):
    """Отображение QR-кода в терминале и генерация HTML/ссылки."""
    print("\n" + "=" * 62)
    print("  ОТСКАНИРУЙТЕ QR-КОД В ПРИЛОЖЕНИИ TELEGRAM НА ТЕЛЕФОНЕ:")
    print("  1. Откройте Telegram на телефоне")
    print("  2. Перейдите: Настройки -> Устройства -> Подключить устройство")
    print("  3. Наведите камеру на QR-код ниже")
    print("=" * 62 + "\n")

    if qrcode is not None:
        try:
            qr = qrcode.QRCode()
            qr.add_data(url)
            qr.print_ascii(invert=True)
        except Exception:
            pass
    else:
        print("  [!] Модуль 'qrcode' не установлен (pip install qrcode для вывода прямо в консоль).")

    # Browser link
    encoded_url = urllib.parse.quote(url)
    web_qr = f"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={encoded_url}"
    print(f"\n  Если в консоли QR не виден или съехал, откройте ссылку:")
    print(f"  --> {web_qr}\n")

    # Save local HTML QR file
    html_file = os.path.join(BRIDGE_DIR, "qr_login.html")
    try:
        with open(html_file, "w", encoding="utf-8") as f:
            f.write(f"""<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8">
  <title>MobileGram QR Login</title>
  <style>
    body {{ font-family: sans-serif; text-align: center; background: #1c242d; color: #fff; padding-top: 30px; }}
    .card {{ background: #242f3d; display: inline-block; padding: 25px 35px; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.5); }}
    img {{ background: #fff; padding: 10px; border-radius: 8px; margin: 15px 0; }}
    ol {{ text-align: left; line-height: 1.8; }}
  </style>
</head>
<body>
  <div class="card">
    <h2>MobileGram Bridge — Вход по QR-коду</h2>
    <img src="{web_qr}" width="300" height="300" alt="QR Code" />
    <ol>
      <li>Откройте Telegram на смартфоне</li>
      <li>Зайдите в <b>Настройки &rarr; Устройства &rarr; Подключить устройство</b></li>
      <li>Наведите камеру на этот QR-код</li>
    </ol>
  </div>
</body>
</html>""")
        print(f"  (Страница с QR-кодом также сохранена: {html_file})")
    except Exception:
        pass


async def login_via_qr(client: TelegramClient):
    """QR code authorization (Telegram login without SMS code)."""
    print("\nГенерация токена авторизации Telegram QR...")
    qr_login = await client.qr_login()

    while True:
        display_qr(qr_login.url)
        print("\nОжидание сканирования QR-кода с телефона (таймаут 30 сек)...")

        try:
            # Wait for QR scan
            user = await qr_login.wait(timeout=30)
            return user

        except asyncio.TimeoutError:
            print("\n[!] Время действия QR-кода истекло. Обновляю код...")
            await qr_login.recreate()

        except SessionPasswordNeededError:
            print("\n" + "=" * 60)
            print("  [!] QR-код принят! На аккаунте включён 2FA облачный пароль.")
            print("=" * 60)
            for attempt in range(3):
                password = input(f"  Введите ваш 2FA пароль (попытка {attempt + 1}/3): ").strip()
                if not password:
                    continue
                try:
                    user = await client.sign_in(password=password)
                    return user
                except Exception as e:
                    print(f"  [!] Ошибка пароля: {e}")
            raise Exception("Все попытки ввода 2FA исчерпаны.")


async def login_via_phone(client: TelegramClient):
    """Классическая авторизация по номеру телефона (с эмуляцией Desktop-клиента)."""
    while True:
        phone = input("\nВведите номер телефона (+79991234567): ").strip()
        if phone.startswith("+") and len(phone) >= 10:
            break
        print("  [!] Некорректный номер, попробуйте снова.")

    print(f"Отправка запроса кода для {phone}...")
    try:
        sent = await client.send_code_request(phone)
    except FloodWaitError as e:
        print(f"[!] Ограничение Telegram: подождите {e.seconds} секунд.")
        return None
    except Exception as e:
        print(f"[ОШИБКА] Не удалось запросить код: {e}")
        return None

    print(f"[OK] Запрос принят сервером. Тип доставки: {type(sent.type).__name__}")
    print("[!] Внимание: если код не пришёл в течение 1 минуты, Telegram")
    print("    заблокировал отправку кодов для вашего IP (Issue #4730).")
    print("    В таком случае используйте вход по QR-коду!\n")

    for attempt in range(3):
        code = input(f"Введите код из Telegram (попытка {attempt + 1}/3): ").strip()
        if not code:
            continue
        try:
            user = await client.sign_in(phone=phone, code=code, phone_code_hash=sent.phone_code_hash)
            return user
        except PhoneCodeExpiredError:
            print("[!] Срок действия кода истёк.")
            return None
        except PhoneCodeInvalidError:
            print("  [!] Неверный код.")
        except SessionPasswordNeededError:
            print("\n[!] Требуется 2FA пароль:")
            pwd = input("  Введите пароль: ").strip()
            user = await client.sign_in(password=pwd)
            return user
        except Exception as e:
            print(f"[ОШИБКА]: {e}")
            return None
    return None


async def main():
    print("=" * 64)
    print("   MobileGram Bridge — Авторизация (с поддержкой QR и Issue #4730)")
    print("=" * 64)

    api_id, api_hash = load_config()

    if api_id in (0, 12345678) or api_hash in ("", "your_api_hash_here"):
        print("\n[!] Введите данные API с сайта https://my.telegram.org")
        while True:
            try:
                val_id = input("  Введите api_id (число): ").strip()
                api_id = int(val_id)
                if api_id > 0:
                    break
            except (ValueError, EOFError):
                pass
        while True:
            val_hash = input("  Введите api_hash (32 символа): ").strip()
            if len(val_hash) >= 10:
                api_hash = val_hash
                break
        save_config(api_id, api_hash)

    print(f"\n  API ID: {api_id}")
    print(f"  Файл сессии: {os.path.abspath(SESSION_NAME + '.session')}")

    # Initialize client with Telegram Desktop device parameters
    client = TelegramClient(
        SESSION_NAME,
        api_id,
        api_hash,
        **CLIENT_PARAMS
    )

    try:
        print("Подключение к MTProto серверам Telegram...")
        await client.connect()
        print("[OK] Соединение установлено.")
    except ApiIdInvalidError:
        print("[ОШИБКА] Неверный api_id или api_hash! Проверьте данные на my.telegram.org")
        sys.exit(1)
    except Exception as e:
        print(f"[ОШИБКА] Подключение не удалось: {e}")
        sys.exit(1)

    if await client.is_user_authorized():
        me = await client.get_me()
        print(f"\n[✓] Вы УЖЕ авторизованы как: {me.first_name} (@{me.username or me.phone})")
        print(f"    Файл сессии: {os.path.abspath(SESSION_NAME + '.session')}")
        print("\nШлюз готов к запуску: ./venv/bin/python3 bridge.py")
        await client.disconnect()
        return

    print("\nВыберите способ входа:")
    print("  [1] По QR-коду (РЕКОМЕНДУЕТСЯ — мгновенно, без SMS и блокировок кодов)")
    print("  [2] По номеру телефона и коду (часто не доходят коды из-за ограничений TG)")

    choice = input("\nВаш выбор [1/2, по умолчанию 1]: ").strip()
    if not choice:
        choice = "1"

    user = None
    if choice == "2":
        user = await login_via_phone(client)
    else:
        user = await login_via_qr(client)

    if user:
        name = getattr(user, "first_name", "") or ""
        uname = f"@{user.username}" if getattr(user, "username", None) else f"ID: {user.id}"
        print("\n" + "=" * 64)
        print("  [✓] АВТОРИЗАЦИЯ УСПЕШНО ЗАВЕРШЕНА!")
        print(f"  Пользователь: {name} ({uname})")
        print(f"  Файл сессии:  {os.path.abspath(SESSION_NAME + '.session')}")
        print("\n  Теперь запускайте шлюз:")
        print("    ./venv/bin/python3 bridge.py")
        print("=" * 64)
    else:
        print("\n[!] Авторизация не завершена. Попробуйте снова: ./venv/bin/python3 login.py")

    await client.disconnect()


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n\nПрервано.")
        sys.exit(0)
