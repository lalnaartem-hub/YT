# MARX (Windows-first Messenger Platform)

MARX — расширяемая серверная платформа мессенджера с фокусом на **Windows-клиент** (WPF/Avalonia).

## Что открыть и компилировать в Windows

Открывай в Visual Studio файл решения:

- `MARX.Windows.sln`

Дальше просто `Build Solution`.

CLI-варианты для Windows:

```bat
build_windows.bat Release
```

или

```powershell
./build_windows.ps1 -Configuration Release
```

---

## Что улучшено в коде

- Усилена регистрация:
  - валидация username и password,
  - первый пользователь может стать admin, остальные — только user.
- Улучшена валидация входных данных по чатам/сообщениям/подаркам.
- Добавлены helper-guards `RequireAuth` / `RequireAdmin`.
- Добавлена очистка просроченных stories.
- Улучшена выдача метрик и сортировка данных.
- Расширен seed 3D-подарков (добавлены Dragon и Galaxy Cube).
- Модель подарков улучшена: категория, редкость, формат, эффекты, анимационный профиль, preview/source URL, recommended scale.
- В seed-каталоге добавлены публичные online-источники 3D-ассетов (modelviewer.dev и Khronos glTF sample models) для быстрого прототипирования.

---


## Как подключить ваш ZIP с GLB (flower.zip)

Я добавил авто-импорт локальных моделей подарков из папки:

- `SecureMessenger/bin/<Configuration>/net8.0/3d_models/flower/`

Что делать на Windows:

1. Скачайте ваш архив `flower.zip`.
2. Распакуйте его в папку `3d_models/flower` рядом с запускаемым `SecureMessenger.dll`.
3. Запустите сервер.
4. Все найденные `*.glb` автоматически добавятся в каталог подарков через `SeedLocalFlowerCollection`.

Для каждого файла автоматически:
- формируется красивое название (по имени файла),
- ставится русское описание,
- назначается анимация (например `Bloom+Sparkle`, `Sway+Petals`),
- выставляется адекватная цена по редкости.

## API (ядро)

### Auth
- `POST /api/auth/register`
- `POST /api/auth/login`

### Chats & Messages
- `POST /api/chats`
- `GET /api/chats`
- `POST /api/messages`
- `GET /api/messages/{chatId}`

### Stories / Voice / Streams
- `POST /api/stories`
- `GET /api/stories`
- `POST /api/voice-rooms`
- `POST /api/streams`

### Bots / AI
- `POST /api/bots/newbot`
- `POST /api/ai/assistants` (admin)

### Economy
- `GET /api/gifts`
- `POST /api/gifts/send`

### Admin
- `POST /api/admin/gifts`
- `POST /api/admin/coins`
- `GET /api/admin/metrics`

---

## Структура

```text
MARX.Windows.sln
build_windows.bat
build_windows.ps1
SecureMessenger/
  Program.cs
  Models/
  Services/
  Storage/
  Docs/
```

## Примечание

В текущем scope мы делаем основу под **Windows**. Web/mobile клиенты в этом релизе не входят в поставку.
