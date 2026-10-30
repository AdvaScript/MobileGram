#!/usr/bin/env python3
"""
MobileGram Bridge v0.4f — шлюз для Windows Mobile 6 Professional (HTC P3300)
Транслирует Telegram MTProto 2.0 (Telethon) в лёгкий REST JSON по HTTP/1.1
без TLS — для работы на OMAP 850 @ 201 МГц.

НОВЫЕ ВОЗМОЖНОСТИ v0.4f:
  - Исправлен баг галочек: 1 галочка (sent) при отправке, 2 галочки (read) ТОЛЬКО когда прочитано собеседником.
  - Синхронизация статусов прочтения:
    /api/v1/messages/read отмечает сообщения прочитанными в Telegram,
    событие MessageRead(outbox=True) обновляет галочки в реальном времени.
  - Глобальный поиск по всему Telegram (/api/v1/search?q=...).
  - Просмотр медиа/фотографий (/api/v1/media).
  - Поддержка тем/форумов (Forum Topics), ответов (Replies) и реакций.
  - Встроенный веб-интерфейс QR-авторизации: http://<ip>:8080/qr
"""

import os
import sys
import io
import asyncio
import logging
import urllib.parse
import configparser

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

from aiohttp import web
from PIL import Image

# Telethon imports
from telethon import TelegramClient, events
from telethon.tl.types import (
    UserStatusOnline, UserStatusRecently, UserStatusOffline,
    UserStatusLastWeek, UserStatusLastMonth,
    MessageMediaPoll, MessageMediaPhoto, MessageMediaDocument,
    MessageActionChatAddUser, MessageActionChatDeleteUser,
    MessageActionChatJoinedByLink, MessageActionPinMessage
)
try:
    from telethon.tl.types import ForumTopic
except ImportError:
    ForumTopic = None

try:
    from telethon.tl.types import ReactionEmoji, ReactionCustomEmoji
except ImportError:
    ReactionEmoji, ReactionCustomEmoji = None, None

try:
    from telethon.tl.functions.messages import GetForumTopicsRequest
except ImportError:
    try:
        from telethon.tl.functions.channels import GetForumTopicsRequest
    except ImportError:
        GetForumTopicsRequest = None

from telethon.tl.functions.contacts import SearchRequest
from telethon.tl.functions.messages import SendVoteRequest, SendReactionRequest
from telethon.errors import (
    SessionPasswordNeededError, PhoneCodeInvalidError, PhoneCodeExpiredError,
    PasswordHashInvalidError, FloodWaitError, ApiIdInvalidError
)

# ======================================================================
# Logging configuration
# ======================================================================
logging.basicConfig(
    format="%(asctime)s [%(levelname)s] %(message)s",
    datefmt="%H:%M:%S",
    level=logging.INFO
)
logger = logging.getLogger("Bridge")

# ======================================================================
# Configuration & Environment
# ======================================================================
CONFIG_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "config_bridge.ini")
SESSION_NAME = os.environ.get("TG_SESSION_NAME", "mobilegram_session")
HOST = os.environ.get("BRIDGE_HOST", "0.0.0.0")
PORT = int(os.environ.get("BRIDGE_PORT", "8080"))

CLIENT_PARAMS = {
    "device_model": "Desktop",
    "system_version": "Windows 10",
    "app_version": "4.16.8 x64",
    "lang_code": "ru",
    "system_lang_code": "ru-RU"
}


def load_api_credentials():
    api_id_str = os.environ.get("TG_API_ID", "")
    api_hash = os.environ.get("TG_API_HASH", "")

    if not api_id_str or not api_hash:
        cfg = configparser.ConfigParser()
        cfg.read(CONFIG_FILE, encoding="utf-8")
        if cfg.has_option("bridge", "api_id"):
            api_id_str = api_id_str or cfg.get("bridge", "api_id")
        if cfg.has_option("bridge", "api_hash"):
            api_hash = api_hash or cfg.get("bridge", "api_hash")

    try:
        api_id = int(api_id_str)
    except (ValueError, TypeError):
        api_id = 0

    return api_id, api_hash


client = None
update_queue = None
auth_cache = {}

qr_state = {
    "active_qr": None,
    "url": "",
    "status": "idle",
    "message": ""
}


def parse_user_status(status):
    if isinstance(status, UserStatusOnline):
        return "online"
    elif isinstance(status, UserStatusRecently):
        return "recently"
    elif isinstance(status, UserStatusLastWeek):
        return "last_week"
    elif isinstance(status, UserStatusLastMonth):
        return "last_month"
    return "offline"


def ensure_connected(handler):
    async def wrapper(request):
        if client is None:
            return web.json_response(
                {"status": "error", "message": "Bridge не инициализирован"},
                status=503
            )
        if not client.is_connected():
            logger.warning("Клиент отключён, переподключаюсь...")
            try:
                await client.connect()
            except Exception as e:
                logger.error(f"Не удалось переподключиться: {e}")
                return web.json_response(
                    {"status": "error", "message": f"Нет соединения с Telegram: {e}"},
                    status=503
                )
        return await handler(request)
    wrapper.__name__ = handler.__name__
    return wrapper


@web.middleware
async def request_logger_middleware(request, handler):
    client_ip = request.remote or "unknown"
    logger.info(f"--> {request.method} {request.path_qs} от {client_ip}")
    try:
        response = await handler(request)
        logger.info(f"<-- {request.method} {request.path} -> {response.status}")
        return response
    except Exception as ex:
        logger.error(f"<-- ОШИБКА {request.method} {request.path}: {ex}")
        raise


# ======================================================================
# 0. Diagnostic Endpoints
# ======================================================================
@ensure_connected
async def handle_debug(request):
    """GET /api/v1/debug"""
    api_id, api_hash = load_api_credentials()
    session_path = os.path.abspath(SESSION_NAME + ".session")
    try:
        is_auth = await client.is_user_authorized()
    except Exception:
        is_auth = False

    info = {
        "bridge_version": "0.4f",
        "api_id": api_id,
        "api_hash_set": bool(api_hash and api_hash != "your_api_hash_here"),
        "session_file_exists": os.path.exists(session_path),
        "session_path": session_path,
        "connected": client.is_connected() if client else False,
        "authorized": is_auth,
        "qr_login_url": f"http://{request.host}/qr",
        "queue_size": update_queue.qsize() if update_queue else -1
    }
    return web.json_response(info)


# ======================================================================
# 1. Web QR Authorization (/qr)
# ======================================================================
async def qr_background_listener(qr_obj):
    global qr_state
    try:
        qr_state["status"] = "waiting_scan"
        qr_state["message"] = "Ожидание сканирования с телефона..."
        user = await qr_obj.wait(timeout=120)
        qr_state["status"] = "authorized"
        qr_state["message"] = f"Успешно авторизован как {user.first_name}!"
        logger.info(f"[QR] Успешный вход через QR-код! Пользователь: {user.first_name}")
    except SessionPasswordNeededError:
        qr_state["status"] = "need_2fa"
        qr_state["message"] = "QR-код отсканирован! Введите 2FA облачный пароль."
        logger.info("[QR] QR отсканирован, требуется 2FA пароль.")
    except asyncio.TimeoutError:
        qr_state["status"] = "expired"
        qr_state["message"] = "Срок действия QR-кода истёк. Обновите страницу."
    except Exception as e:
        qr_state["status"] = "error"
        qr_state["message"] = str(e)
        logger.error(f"[QR] Ошибка: {e}")


async def handle_qr_page(request):
    global qr_state
    is_auth = False
    try:
        is_auth = await client.is_user_authorized()
    except Exception:
        pass

    if is_auth:
        me = await client.get_me()
        html = f"""<!DOCTYPE html>
<html>
<head><meta charset="utf-8"><title>MobileGram Bridge</title>
<style>
body {{ font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: #0e1621; color: #fff; text-align: center; padding-top: 50px; }}
.card {{ background: #17212b; max-width: 450px; margin: 0 auto; padding: 30px; border-radius: 14px; box-shadow: 0 4px 20px rgba(0,0,0,0.6); }}
.badge {{ background: #2f6ea5; padding: 6px 12px; border-radius: 6px; display: inline-block; margin-bottom: 15px; font-weight: bold; }}
</style></head>
<body>
  <div class="card">
    <div class="badge">MobileGram Bridge v0.4f</div>
    <h2>✓ Вы уже авторизованы!</h2>
    <p>Пользователь: <b>{me.first_name or ""} {me.last_name or ""}</b> (@{me.username or me.phone})</p>
    <p style="color: #6c7883; font-size: 14px;">Windows Mobile 6 client is ready to use!</p>
  </div>
</body></html>"""
        return web.Response(text=html, content_type="text/html")

    if qr_state["status"] not in ("waiting_scan", "need_2fa"):
        try:
            qr_obj = await client.qr_login()
            qr_state["active_qr"] = qr_obj
            qr_state["url"] = qr_obj.url
            asyncio.create_task(qr_background_listener(qr_obj))
        except Exception as e:
            logger.error(f"Не удалось создать QR-токен: {e}")
            qr_state["status"] = "error"
            qr_state["message"] = str(e)

    encoded_url = urllib.parse.quote(qr_state.get("url", ""))
    qr_img_src = f"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={encoded_url}"

    html = f"""<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8">
  <title>MobileGram — Вход по QR-коду</title>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: #0e1621; color: #fff; text-align: center; padding: 30px 10px; }}
    .card {{ background: #17212b; max-width: 480px; margin: 0 auto; padding: 30px; border-radius: 16px; box-shadow: 0 8px 30px rgba(0,0,0,0.6); }}
    .qr-box {{ background: #fff; display: inline-block; padding: 12px; border-radius: 12px; margin: 20px 0; }}
    .instructions {{ text-align: left; background: #242f3d; padding: 15px 20px; border-radius: 10px; font-size: 14px; line-height: 1.6; margin-top: 20px; }}
    .status-text {{ font-size: 16px; margin: 15px 0; color: #5288c1; font-weight: bold; }}
    input[type="password"] {{ width: 80%; padding: 10px; border-radius: 8px; border: 1px solid #5288c1; margin-top: 10px; font-size: 16px; background: #0e1621; color: #fff; }}
    button {{ padding: 10px 20px; background: #5288c1; color: #fff; border: none; border-radius: 8px; cursor: pointer; font-size: 16px; margin-top: 10px; }}
  </style>
</head>
<body>
  <div class="card">
    <h2>MobileGram Bridge v0.4f</h2>
    <p style="color: #6c7883;">Log in with QR Code</p>
    <div id="qr-container">
      <div class="qr-box">
        <img src="{qr_img_src}" width="280" height="280" alt="Telegram QR Login" />
      </div>
    </div>
    <div class="status-text" id="status-label">{qr_state.get('message', 'Ожидание...')}</div>
    <div id="pwd-box" style="display: {'block' if qr_state['status'] == 'need_2fa' else 'none'};">
      <input type="password" id="pwd-input" placeholder="Введите ваш 2FA пароль..." /><br/>
      <button onclick="submit2FA()">Подтвердить пароль</button>
    </div>
    <div class="instructions">
      <b>Инструкция:</b>
      <ol style="margin: 5px 0 0 15px; padding: 0;">
        <li>Откройте Telegram на телефоне</li>
        <li>Зайдите в: <b>Настройки → Устройства → Подключить устройство</b></li>
        <li>Наведите камеру на QR-код</li>
      </ol>
    </div>
  </div>
  <script>
    async function checkStatus() {{
      try {{
        let res = await fetch('/api/v1/auth/qr/status');
        let data = await res.json();
        let label = document.getElementById('status-label');
        let pwdBox = document.getElementById('pwd-box');
        if (data.status === 'need_2fa') {{
          label.innerText = 'QR принят! Введите 2FA облачный пароль:';
          pwdBox.style.display = 'block';
        }} else if (data.status === 'authorized') {{
          label.innerText = '✓ Вход выполнен! Обновляю страницу...';
          setTimeout(() => location.reload(), 1500);
        }} else if (data.status === 'expired') {{
          location.reload();
        }}
      }} catch (e) {{}}
    }}
    async function submit2FA() {{
      let pwd = document.getElementById('pwd-input').value;
      if (!pwd) return alert('Введите пароль');
      let res = await fetch('/api/v1/auth/2fa', {{
        method: 'POST',
        headers: {{ 'Content-Type': 'application/json' }},
        body: JSON.stringify({{ password: pwd }})
      }});
      let data = await res.json();
      if (data.status === 'ok') {{ location.reload(); }}
      else {{ alert('Ошибка: ' + (data.message || 'Неверный пароль')); }}
    }}
    setInterval(checkStatus, 2000);
  </script>
</body>
</html>"""
    return web.Response(text=html, content_type="text/html")


async def handle_qr_status(request):
    global qr_state
    try:
        if await client.is_user_authorized():
            return web.json_response({"status": "authorized", "message": "Авторизован"})
    except Exception:
        pass
    return web.json_response({
        "status": qr_state.get("status", "idle"),
        "message": qr_state.get("message", "")
    })


# ======================================================================
# 2. Authorization REST API
# ======================================================================
@ensure_connected
async def handle_auth_status(request):
    try:
        is_auth = await client.is_user_authorized()
    except Exception:
        is_auth = False

    me_data = None
    if is_auth:
        try:
            me = await client.get_me()
            me_data = {
                "id": me.id,
                "first_name": me.first_name or "",
                "last_name": me.last_name or "",
                "username": me.username or "",
                "phone": me.phone or ""
            }
        except Exception as e:
            logger.warning(f"Не удалось выполнить get_me: {e}")

    logger.info(f"Статус авторизации: {'авторизован' if is_auth else 'НЕ авторизован'}")
    return web.json_response({"authorized": is_auth, "user": me_data})


@ensure_connected
async def handle_auth_login(request):
    try:
        data = await request.json()
        phone = data.get("phone", "").strip()
        if not phone:
            return web.json_response({"status": "error", "message": "Номер телефона не указан"}, status=400)

        logger.info(f"Запрос кода Telegram для номера: {phone}")
        res = await client.send_code_request(phone)
        auth_cache[phone] = res.phone_code_hash
        return web.json_response({
            "status": "code_sent",
            "phone": phone,
            "code_type": type(res.type).__name__,
            "timeout": 60
        })
    except FloodWaitError as e:
        return web.json_response({"status": "error", "message": f"Подождите {e.seconds} сек."}, status=429)
    except Exception as e:
        return web.json_response({"status": "error", "message": str(e)}, status=500)


@ensure_connected
async def handle_auth_code(request):
    try:
        data = await request.json()
        phone = data.get("phone", "").strip()
        code = data.get("code", "").strip()
        phone_code_hash = auth_cache.get(phone)

        if not phone or not code or not phone_code_hash:
            return web.json_response({"status": "error", "message": "Неверные параметры"}, status=400)

        try:
            user = await client.sign_in(phone=phone, code=code, phone_code_hash=phone_code_hash)
            auth_cache.pop(phone, None)
            return web.json_response({"status": "ok", "user": {"id": user.id, "first_name": user.first_name or ""}})
        except SessionPasswordNeededError:
            return web.json_response({"status": "need_2fa", "message": "Требуется 2FA пароль"})
        except Exception as e:
            return web.json_response({"status": "error", "message": str(e)}, status=400)
    except Exception as e:
        return web.json_response({"status": "error", "message": str(e)}, status=500)


@ensure_connected
async def handle_auth_2fa(request):
    try:
        data = await request.json()
        password = data.get("password", "")
        if not password:
            return web.json_response({"status": "error", "message": "Пароль не указан"}, status=400)

        user = await client.sign_in(password=password)
        return web.json_response({"status": "ok", "user": {"id": user.id, "first_name": user.first_name or ""}})
    except Exception as e:
        return web.json_response({"status": "error", "message": str(e)}, status=400)


# ======================================================================
# 3. Chat list, topics, global search & message history
# ======================================================================
@ensure_connected
async def handle_get_chats(request):
    """GET /api/v1/chats?filter=all|chats|groups"""
    chat_filter = request.query.get("filter", "all")
    chats_data = []

    try:
        async for dialog in client.iter_dialogs(limit=35):
            entity = dialog.entity
            chat_type = "user" if dialog.is_user else ("group" if dialog.is_group else "channel")

            if chat_filter == "chats" and chat_type != "user":
                continue
            if chat_filter == "groups" and chat_type not in ("group", "channel"):
                continue

            user_status = parse_user_status(getattr(entity, "status", None)) if dialog.is_user else ""
            is_forum = bool(getattr(entity, "forum", False))

            last_text = ""
            if dialog.message:
                if dialog.message.action:
                    last_text = "[системное уведомление]"
                elif dialog.message.media:
                    if isinstance(dialog.message.media, MessageMediaPoll):
                        last_text = "[Опрос]"
                    elif isinstance(dialog.message.media, MessageMediaPhoto):
                        last_text = "[Фото]"
                    else:
                        last_text = "[Файл/GIF]"
                else:
                    last_text = dialog.message.message or ""

            chats_data.append({
                "id": dialog.id,
                "type": chat_type,
                "title": dialog.name or "Без названия",
                "username": getattr(entity, "username", "") or "",
                "unread": dialog.unread_count,
                "time": dialog.date.strftime("%H:%M") if dialog.date else "",
                "last_msg": last_text[:60],
                "status": user_status,
                "typing": False,
                "muted": bool(getattr(dialog.dialog, "notify_settings", None)
                              and getattr(dialog.dialog.notify_settings, "mute_until", 0)),
                "members_cnt": getattr(entity, "participants_count", 0) or (2 if dialog.is_user else 0),
                "is_forum": is_forum
            })

        return web.json_response(chats_data)
    except Exception as e:
        logger.error(f"Ошибка получения списка чатов: {e}")
        return web.json_response([], status=500)


@ensure_connected
async def handle_search(request):
    """GET /api/v1/search?q=query — глобальный поиск по контактам, чатам и каналам."""
    query = request.query.get("q", "").strip()
    if not query:
        return web.json_response([])

    results = []
    seen_ids = set()

    # 1. Search in local dialogs
    try:
        async for dialog in client.iter_dialogs(limit=50):
            if query.lower() in (dialog.name or "").lower():
                entity = dialog.entity
                chat_type = "user" if dialog.is_user else ("group" if dialog.is_group else "channel")
                results.append({
                    "id": dialog.id,
                    "title": dialog.name or "Без названия",
                    "username": getattr(entity, "username", "") or "",
                    "type": chat_type,
                    "is_forum": bool(getattr(entity, "forum", False)),
                    "is_global": False
                })
                seen_ids.add(dialog.id)
    except Exception as e:
        logger.warning(f"Ошибка локального поиска: {e}")

    # 2. Global search across Telegram
    try:
        global_res = await client(SearchRequest(q=query, limit=20))
        for u in getattr(global_res, "users", []):
            if u.id not in seen_ids and not getattr(u, "is_self", False):
                uname = getattr(u, "username", "") or ""
                fname = u.first_name or ""
                lname = u.last_name or ""
                full_name = f"{fname} {lname}".strip() or uname or f"ID {u.id}"
                results.append({
                    "id": u.id,
                    "title": full_name,
                    "username": uname,
                    "type": "user",
                    "is_forum": False,
                    "is_global": True
                })
                seen_ids.add(u.id)

        for c in getattr(global_res, "chats", []):
            if c.id not in seen_ids:
                c_title = getattr(c, "title", "Группа")
                c_uname = getattr(c, "username", "") or ""
                c_type = "channel" if getattr(c, "broadcast", False) else "group"
                results.append({
                    "id": c.id,
                    "title": c_title,
                    "username": c_uname,
                    "type": c_type,
                    "is_forum": bool(getattr(c, "forum", False)),
                    "is_global": True
                })
                seen_ids.add(c.id)
    except Exception as e:
        logger.warning(f"Ошибка глобального поиска: {e}")

    logger.info(f"Поиск по '{query}': найдено {len(results)} результатов.")
    return web.json_response(results)


@ensure_connected
async def handle_get_topics(request):
    """GET /api/v1/topics?chat_id=123 — темы форума."""
    try:
        chat_id = int(request.query.get("chat_id", 0))
        if not chat_id:
            return web.json_response([], status=400)

        if GetForumTopicsRequest is None:
            logger.warning("GetForumTopicsRequest не поддерживается установленной версией Telethon")
            return web.json_response([])

        # Telethon version compatibility for peer/channel argument
        try:
            result = await client(GetForumTopicsRequest(
                peer=chat_id,
                offset_date=0,
                offset_id=0,
                offset_topic=0,
                limit=60
            ))
        except (TypeError, Exception):
            result = await client(GetForumTopicsRequest(
                channel=chat_id,
                offset_date=None,
                offset_id=0,
                offset_topic=0,
                limit=60
            ))

        topics_data = []
        for t in getattr(result, "topics", []):
            t_id = getattr(t, "id", 0)
            t_title = getattr(t, "title", None) or f"Тема {t_id}"
            t_color = getattr(t, "icon_color", 0) or 0
            t_unread = getattr(t, "unread_count", 0) or 0
            t_closed = bool(getattr(t, "closed", False))
            topics_data.append({
                "id": t_id,
                "title": t_title,
                "icon_color": t_color,
                "unread_count": t_unread,
                "closed": t_closed
            })

        return web.json_response(topics_data)
    except Exception as e:
        logger.error(f"Ошибка получения тем форума: {e}")
        return web.json_response([], status=500)


@ensure_connected
async def handle_get_messages(request):
    """GET /api/v1/messages?chat_id=123&topic_id=456&limit=30"""
    try:
        chat_id = int(request.query.get("chat_id", 0))
        topic_id = int(request.query.get("topic_id", 0))
        limit = int(request.query.get("limit", 30))
        if not chat_id:
            return web.json_response([], status=400)

        # Determine max read outbox ID for genuine read receipts
        read_outbox_max_id = 0
        try:
            async for d in client.iter_dialogs(limit=50):
                if d.id == chat_id:
                    read_outbox_max_id = getattr(d.dialog, "read_outbox_max_id", 0) or 0
                    break
        except Exception:
            pass

        raw_messages = []
        needed_reply_ids = set()
        messages_by_id = {}

        iter_kwargs = {"limit": limit}
        if topic_id > 0:
            iter_kwargs["reply_to"] = topic_id

        async for m in client.iter_messages(chat_id, **iter_kwargs):
            raw_messages.append(m)
            messages_by_id[m.id] = m
            if m.reply_to and getattr(m.reply_to, "reply_to_msg_id", 0):
                r_id = m.reply_to.reply_to_msg_id
                if r_id != topic_id:
                    needed_reply_ids.add(r_id)

        missing_reply_ids = [rid for rid in needed_reply_ids if rid not in messages_by_id]
        if missing_reply_ids:
            try:
                parent_msgs = await client.get_messages(chat_id, ids=missing_reply_ids[:15])
                for pm in parent_msgs:
                    if pm:
                        messages_by_id[pm.id] = pm
            except Exception:
                pass

        msg_list = []
        for m in raw_messages:
            sender = await m.get_sender()
            sender_id = m.sender_id or 0
            sender_name = "Вы" if m.out else (getattr(sender, "first_name", "Неизвестно") or "Неизвестно")

            is_service = False
            service_text = ""
            is_poll = False
            poll_question = ""
            poll_options = []
            poll_votes = []
            my_vote_idx = -1
            has_media = False
            media_type = ""
            media_size_kb = 0
            body_text = m.text or ""

            reply_to_msg_id = 0
            reply_to_sender = ""
            reply_to_text = ""
            if m.reply_to and getattr(m.reply_to, "reply_to_msg_id", 0):
                r_id = m.reply_to.reply_to_msg_id
                if r_id != topic_id:
                    reply_to_msg_id = r_id
                    if r_id in messages_by_id:
                        parent_m = messages_by_id[r_id]
                        p_sender = await parent_m.get_sender()
                        reply_to_sender = "Вы" if parent_m.out else (getattr(p_sender, "first_name", "") or "")
                        reply_to_text = (parent_m.text or "[Медиа/Файл]")[:50]

            reactions_list = []
            if m.reactions and hasattr(m.reactions, "results") and m.reactions.results:
                for rc in m.reactions.results:
                    emoticon = ""
                    if isinstance(rc.reaction, ReactionEmoji):
                        emoticon = rc.reaction.emoticon
                    elif hasattr(rc.reaction, "emoticon"):
                        emoticon = rc.reaction.emoticon
                    else:
                        emoticon = "👍"
                    chosen = getattr(rc, "chosen_order", None) is not None
                    reactions_list.append({
                        "emoji": emoticon,
                        "count": rc.count,
                        "chosen": chosen
                    })

            if m.action:
                is_service = True
                if isinstance(m.action, MessageActionChatAddUser):
                    service_text = f"{sender_name} добавил(а) участника"
                elif isinstance(m.action, MessageActionChatDeleteUser):
                    service_text = f"{sender_name} покинул(а) группу"
                elif isinstance(m.action, MessageActionChatJoinedByLink):
                    service_text = f"{sender_name} присоединился по ссылке"
                elif isinstance(m.action, MessageActionPinMessage):
                    service_text = f"{sender_name} закрепил(а) сообщение"
                else:
                    service_text = f"Событие ({sender_name})"

            elif m.media and isinstance(m.media, MessageMediaPoll):
                is_poll = True
                poll_obj = m.media.poll
                try:
                    poll_question = poll_obj.question or "Опрос"
                except Exception:
                    poll_question = "Опрос"

                if hasattr(poll_obj, "answers"):
                    for idx, ans in enumerate(poll_obj.answers):
                        try:
                            poll_options.append(ans.text)
                        except Exception:
                            poll_options.append(f"Вариант {idx + 1}")
                        vote_cnt = 0
                        if hasattr(m.media, "results") and m.media.results and m.media.results.results:
                            for r in m.media.results.results:
                                if getattr(r, "option", None) == getattr(ans, "option", None):
                                    vote_cnt = getattr(r, "voters", 0)
                                    if getattr(r, "chosen", False):
                                        my_vote_idx = idx
                        poll_votes.append(vote_cnt)

            elif m.media and not is_service:
                if isinstance(m.media, MessageMediaPhoto):
                    has_media = True
                    media_type = "photo"
                    media_size_kb = 15
                    body_text = (body_text + "\n" if body_text else "") + "[Фото — нажмите для просмотра]"
                elif isinstance(m.media, MessageMediaDocument):
                    has_media = True
                    media_type = "doc"
                    media_size_kb = 30
                    body_text = (body_text + "\n" if body_text else "") + "[Файл/GIF — нажмите для просмотра]"

            # Accurate read delivery status:
            # If outgoing, "read" (double checkmark) only when
            # read_outbox_max_id >= m.id, otherwise "sent" (single checkmark)
            if m.out:
                msg_status = "read" if (read_outbox_max_id > 0 and m.id <= read_outbox_max_id) else "sent"
            else:
                msg_status = "read"

            msg_list.insert(0, {
                "msg_id": m.id,
                "sender_id": sender_id,
                "sender_name": sender_name,
                "is_self": bool(m.out),
                "time": m.date.strftime("%H:%M") if m.date else "",
                "text": service_text if is_service else body_text,
                "status": msg_status,
                "is_service": is_service,
                "is_poll": is_poll,
                "poll_question": poll_question,
                "poll_options": poll_options,
                "poll_votes": poll_votes,
                "my_vote_idx": my_vote_idx,
                "has_media": has_media,
                "media_type": media_type,
                "media_size_kb": media_size_kb,
                "reply_to_msg_id": reply_to_msg_id,
                "reply_to_sender": reply_to_sender,
                "reply_to_text": reply_to_text,
                "reactions": reactions_list
            })

        return web.json_response(msg_list)
    except Exception as e:
        logger.error(f"Ошибка получения сообщений: {e}")
        return web.json_response([], status=500)


# ======================================================================
# 4. Send messages, read sync, reactions, polls & media
# ======================================================================
@ensure_connected
async def handle_send_message(request):
    """POST /api/v1/send"""
    try:
        data = await request.json()
        chat_id = int(data.get("chat_id", 0))
        text = str(data.get("text", "")).strip()
        topic_id = int(data.get("topic_id", 0))
        reply_to_msg_id = int(data.get("reply_to_msg_id", 0))

        if not chat_id or not text:
            return web.json_response({"status": "error", "message": "Нужен chat_id и текст"}, status=400)

        reply_target = reply_to_msg_id if reply_to_msg_id > 0 else (topic_id if topic_id > 0 else None)
        sent = await client.send_message(chat_id, text, reply_to=reply_target)
        return web.json_response({
            "status": "ok",
            "msg_id": sent.id,
            "time": sent.date.strftime("%H:%M") if sent.date else ""
        })
    except Exception as e:
        logger.error(f"Ошибка отправки сообщения: {e}")
        return web.json_response({"status": "error", "message": str(e)}, status=500)


@ensure_connected
async def handle_mark_as_read(request):
    """POST /api/v1/messages/read {"chat_id": 123, "max_id": 456}"""
    try:
        data = await request.json()
        chat_id = int(data.get("chat_id", 0))
        max_id = int(data.get("max_id", 0))

        if not chat_id:
            return web.json_response({"status": "error", "message": "chat_id обязателен"}, status=400)

        await client.send_read_acknowledge(chat_id, max_id=max_id if max_id > 0 else None)
        logger.info(f"Отмечено прочитанным в {chat_id} (до max_id={max_id})")
        return web.json_response({"status": "ok"})
    except Exception as e:
        logger.error(f"Ошибка отметки прочтения: {e}")
        return web.json_response({"status": "error", "message": str(e)}, status=500)


@ensure_connected
async def handle_send_reaction(request):
    """POST /api/v1/reactions/send"""
    try:
        data = await request.json()
        chat_id = int(data.get("chat_id", 0))
        msg_id = int(data.get("msg_id", 0))
        reaction_str = str(data.get("reaction", "")).strip()

        if not chat_id or not msg_id:
            return web.json_response({"status": "error", "message": "chat_id и msg_id обязательны"}, status=400)

        input_peer = await client.get_input_entity(chat_id)

        emoji_map = {
            "like": "👍", "+1": "👍", "👍": "👍",
            "heart": "❤️", "<3": "❤️", "❤️": "❤️",
            "fire": "🔥", "top": "🔥", "🔥": "🔥",
            "rofl": "😂", "xd": "😂", "laugh": "😂", "😂": "😂",
            "dislike": "👎", "-1": "👎", "👎": "👎"
        }
        clean_emoji = emoji_map.get(reaction_str.lower(), reaction_str)

        if ReactionEmoji and clean_emoji:
            reaction_obj = [ReactionEmoji(emoticon=clean_emoji)]
        else:
            reaction_obj = []

        await client(SendReactionRequest(peer=input_peer, msg_id=msg_id, reaction=reaction_obj))
        logger.info(f"Реакция '{clean_emoji}' успешно поставлена на msg {msg_id} в чате {chat_id}")
        return web.json_response({"status": "ok"})
    except Exception as e:
        logger.error(f"Ошибка отправки реакции: {e}")
        return web.json_response({"status": "error", "message": str(e)}, status=500)


@ensure_connected
async def handle_chat_mute(request):
    """POST /api/v1/chats/mute {"chat_id": 123, "muted": true}"""
    try:
        data = await request.json()
        chat_id = int(data.get("chat_id", 0))
        muted = bool(data.get("muted", True))

        if not chat_id:
            return web.json_response({"status": "error", "message": "chat_id обязателен"}, status=400)

        input_peer = await client.get_input_entity(chat_id)
        from telethon.tl.types import InputPeerNotifySettings
        from telethon.tl.functions.account import UpdateNotifySettingsRequest
        mute_until = 2147483647 if muted else 0
        settings = InputPeerNotifySettings(mute_until=mute_until, silent=muted)
        await client(UpdateNotifySettingsRequest(peer=input_peer, settings=settings))
        logger.info(f"Статус уведомлений для {chat_id} изменен: muted={muted}")
        return web.json_response({"status": "ok", "muted": muted})
    except Exception as e:
        logger.error(f"Ошибка изменения mute: {e}")
        return web.json_response({"status": "error", "message": str(e)}, status=500)


@ensure_connected
async def handle_poll_vote(request):
    """POST /api/v1/poll/vote"""
    try:
        data = await request.json()
        chat_id = int(data.get("chat_id", 0))
        msg_id = int(data.get("msg_id", 0))
        option_idx = int(data.get("option", 0))

        msg = await client.get_messages(chat_id, ids=msg_id)
        if msg and msg.media and isinstance(msg.media, MessageMediaPoll):
            poll = msg.media.poll
            opt_data = poll.answers[option_idx].option
            await client(SendVoteRequest(peer=chat_id, msg_id=msg_id, options=[opt_data]))
            return web.json_response({"status": "ok"})

        return web.json_response({"status": "error", "message": "Опрос не найден"}, status=404)
    except Exception as e:
        logger.error(f"Ошибка голосования в опросе: {e}")
        return web.json_response({"status": "error", "message": str(e)}, status=500)


@ensure_connected
async def handle_get_media(request):
    """GET /api/v1/media?chat_id=123&msg_id=456&max_w=200"""
    try:
        chat_id = int(request.query.get("chat_id", 0))
        msg_id = int(request.query.get("msg_id", 0))
        max_w = int(request.query.get("max_w", 200))

        msg = await client.get_messages(chat_id, ids=msg_id)
        if not msg or not msg.media:
            return web.Response(status=404, text="Медиа не найдено")

        logger.info(f"Скачивание и сжатие медиа для msg_id={msg_id}...")
        buf = io.BytesIO()
        await client.download_media(msg, file=buf)
        buf.seek(0)

        im = Image.open(buf).convert("RGB")
        w, h = im.size
        if w > max_w:
            new_h = int(h * (max_w / float(w)))
            im = im.resize((max_w, new_h), Image.Resampling.LANCZOS)

        out_buf = io.BytesIO()
        im.save(out_buf, format="JPEG", quality=70, optimize=True)
        out_data = out_buf.getvalue()
        return web.Response(body=out_data, content_type="image/jpeg")
    except Exception as e:
        logger.error(f"Ошибка скачивания медиа: {e}")
        return web.Response(status=500, text=str(e))


# ======================================================================
# 5. Long-polling events events
# ======================================================================
async def handle_get_updates(request):
    """GET /api/v1/updates?timeout=25"""
    if update_queue is None:
        return web.json_response({"events": []})
    timeout = int(request.query.get("timeout", 25))
    try:
        event = await asyncio.wait_for(update_queue.get(), timeout=timeout)
        return web.json_response({"events": [event]})
    except asyncio.TimeoutError:
        return web.json_response({"events": []})


# ======================================================================
# 6. Web application factory
# ======================================================================
def create_app():
    app = web.Application(middlewares=[request_logger_middleware])
    app.router.add_get("/api/v1/debug",           handle_debug)
    app.router.add_get("/qr",                     handle_qr_page)
    app.router.add_get("/api/v1/auth/qr/status",  handle_qr_status)

    app.router.add_get("/api/v1/auth/status",     handle_auth_status)
    app.router.add_post("/api/v1/auth/login",     handle_auth_login)
    app.router.add_post("/api/v1/auth/code",      handle_auth_code)
    app.router.add_post("/api/v1/auth/2fa",       handle_auth_2fa)

    app.router.add_get("/api/v1/chats",           handle_get_chats)
    app.router.add_get("/api/v1/search",          handle_search)
    app.router.add_get("/api/v1/topics",          handle_get_topics)
    app.router.add_get("/api/v1/messages",        handle_get_messages)
    app.router.add_post("/api/v1/send",           handle_send_message)
    app.router.add_post("/api/v1/messages/read",  handle_mark_as_read)
    app.router.add_post("/api/v1/chats/mute",     handle_chat_mute)
    app.router.add_post("/api/v1/reactions/send", handle_send_reaction)
    app.router.add_post("/api/v1/poll/vote",      handle_poll_vote)
    app.router.add_get("/api/v1/media",           handle_get_media)
    app.router.add_get("/api/v1/updates",         handle_get_updates)
    return app


# ======================================================================
# 7. Entry point main()
# ======================================================================
async def main():
    global client, update_queue

    print("=" * 65)
    print("  MobileGram Bridge v0.4f — шлюз для Windows Mobile 6")
    print("  Поддержка: Честные галочки, Синхронизация прочтения, Поиск")
    print("  Поддержка QR-авторизации: http://<ip>:8080/qr")
    print("=" * 65)

    api_id, api_hash = load_api_credentials()
    if api_id in (0, 12345678) or api_hash in ("", "your_api_hash_here"):
        logger.error("КРИТИЧНО: API ID или API Hash не настроены! Запустите login.py")
        sys.exit(1)

    session_path = os.path.abspath(SESSION_NAME + ".session")
    update_queue = asyncio.Queue()

    client = TelegramClient(
        SESSION_NAME,
        api_id,
        api_hash,
        **CLIENT_PARAMS
    )

    @client.on(events.NewMessage)
    async def on_new_message(event):
        try:
            sender = await event.get_sender()
            sender_name = "Вы" if event.out else (getattr(sender, "first_name", "Неизвестно") or "Неизвестно")
            topic_id = 0
            if event.reply_to:
                topic_id = event.reply_to.reply_to_top_id or event.reply_to.reply_to_msg_id or 0

            has_media = bool(event.photo or event.document)
            media_type = "photo" if event.photo else ("doc" if event.document else "")

            await update_queue.put({
                "type": "new_message",
                "chat_id": event.chat_id,
                "message": {
                    "msg_id": event.id,
                    "sender_id": event.sender_id or 0,
                    "sender_name": sender_name,
                    "is_self": bool(event.out),
                    "time": event.date.strftime("%H:%M") if event.date else "",
                    "text": event.raw_text or ("[Фото]" if event.photo else "[Медиа/Файл]"),
                    "status": "sent" if event.out else "read",
                    "is_service": bool(event.action),
                    "reply_to_msg_id": event.reply_to_msg_id or 0,
                    "topic_id": topic_id,
                    "has_media": has_media,
                    "media_type": media_type,
                    "reactions": []
                }
            })
        except Exception as e:
            logger.error(f"Ошибка в NewMessage: {e}")

    # Event: Outgoing messages marked as read by peer (double ticks)
    @client.on(events.MessageRead)
    async def on_messages_read(event):
        try:
            if getattr(event, "outbox", False) or not getattr(event, "inbox", False):
                logger.info(f"Сообщения в чате {event.chat_id} прочитаны собеседником до {event.max_id}")
                await update_queue.put({
                    "type": "messages_read",
                    "chat_id": event.chat_id,
                    "max_id": event.max_id
                })
        except Exception as e:
            logger.error(f"Ошибка в MessageRead: {e}")

    @client.on(events.UserUpdate)
    async def on_user_update(event):
        try:
            if event.typing:
                await update_queue.put({
                    "type": "user_typing",
                    "chat_id": event.chat_id,
                    "user_id": event.user_id
                })
            elif event.status:
                await update_queue.put({
                    "type": "user_status",
                    "user_id": event.user_id,
                    "status": parse_user_status(event.status)
                })
        except Exception as e:
            logger.error(f"Ошибка в UserUpdate: {e}")

    logger.info("Подключение к серверам Telegram MTProto...")
    try:
        await client.connect()
    except Exception as e:
        logger.error(f"Не удалось подключиться: {e}")
        sys.exit(1)

    is_auth = await client.is_user_authorized()
    if is_auth:
        me = await client.get_me()
        logger.info(f"[✓] Авторизован как: {me.first_name} (@{me.username or me.phone})")
    else:
        logger.warning(f"Сессия не авторизована! Откройте http://localhost:{PORT}/qr")

    app = create_app()
    runner = web.AppRunner(app)
    await runner.setup()
    site = web.TCPSite(runner, HOST, PORT)
    await site.start()

    logger.info(f"HTTP шлюз запущен: http://{HOST}:{PORT}")
    logger.info(f"Веб-страница QR-входа: http://{HOST}:{PORT}/qr")
    print("=" * 65)

    await asyncio.Event().wait()


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except (KeyboardInterrupt, SystemExit):
        logger.info("Шлюз остановлен.")
