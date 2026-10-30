# MobileGram Bridge (Debian LXC / Linux Server)

Легковесный HTTP-шлюз между серверами Telegram (MTProto 2.0) и КПК Windows Mobile 6 Professional (HTC P3300 Artemis).

---

## 1. Быстрая установка в Debian LXC

```bash
# 1. Установка пакетов
apt update && apt install -y python3 python3-pip python3-venv git

# 2. Создание каталога и venv
mkdir -p /opt/mobilegram-bridge
cd /opt/mobilegram-bridge
python3 -m venv venv

# 3. Скопируйте файлы:
# bridge.py, login.py, check_setup.py, requirements.txt

# 4. Установка зависимостей
./venv/bin/pip install --upgrade pip
./venv/bin/pip install -r requirements.txt
```

---

## 2. Авторизация (Решение проблемы не приходящих кодов)

> [!IMPORTANT]
> **Issue #4730:** Telegram блокирует отправку SMS и кодов на серверные/LXC IP-адреса для сторонних клиентов.
> **Решение:** Авторизация по QR-коду! Не требует SMS и кодов.

### Вариант А: Через консоль (login.py)
```bash
./venv/bin/python3 login.py
```
1. Введите `api_id` и `api_hash` (с https://my.telegram.org).
2. Выберите способ **[1] По QR-коду**.
3. Откройте Telegram на телефоне: **Настройки → Устройства → Подключить устройство**.
4. Наведите камеру на QR-код в консоли (или откройте ссылку на экран).
5. Если включён 2FA — введите облачный пароль. Сессия создана!

### Вариант Б: Через браузер (/qr)
1. Запустите шлюз: `./venv/bin/python3 bridge.py`
2. Откройте в браузере: `http://<IP_ВАШЕГО_LXC>:8080/qr`
3. Отсканируйте QR-код прямо с монитора телефоном.

---

## 3. Запуск шлюза

```bash
./venv/bin/python3 bridge.py
```

Проверка статуса:
```bash
curl http://localhost:8080/api/v1/debug
```

---

## 4. Автозапуск через Systemd

1. Скопируйте файл сервиса:
```bash
cp mobilegram-bridge.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable mobilegram-bridge
systemctl start mobilegram-bridge
```

2. Логи в реальном времени:
```bash
journalctl -u mobilegram-bridge -f
```
