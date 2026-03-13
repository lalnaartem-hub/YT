# MARX (Windows-first Messenger Platform)

MARX — расширяемая серверная платформа мессенджера с фокусом на **Windows-клиент** (WPF/Avalonia).

## Что открыть и компилировать в Windows

Открывай в Visual Studio файл решения:

- `MARX.Windows.sln` (обновлён под SDK-style .NET GUID для VS 2022/2024)

Сборка через CLI:

```bat
build_windows.bat Release
```

или

```powershell
./build_windows.ps1 -Configuration Release
```

---

## Что улучшено в этом обновлении

- Усилен Windows build pipeline:
  - добавлены scripts для авто-подготовки ассетов,
  - `SecureMessenger.csproj` копирует `3d_models/**` в output,
  - build scripts вызывают импорт ассетов перед сборкой.
- Регистрация и логин:
  - request-code + verification code + AcceptTerms,
  - логин по username/email/phone.
- Gifts/анимации:
  - больше animation presets (`Meteor+Trail`, `Crystal+Prism`, `PetalStorm` и др.),
  - расширенный online seed-каталог GLB,
  - авто-импорт локальных flower GLB с профилями анимации, FX и цены.

---

## Если в Visual Studio "пустой проект"

Проверьте по шагам:

1. Открывайте именно `MARX.Windows.sln`, а не пустую папку.
2. В `Solution Explorer` должен быть проект `SecureMessenger` и папка `Solution Items`.
3. Нажмите правой кнопкой по решению → `Restore NuGet Packages`.
4. Запустите проект (`F5`) и откройте `http://localhost:5000/` — теперь там есть стартовый JSON, чтобы видеть, что сервер реально запущен.

Если проект не загрузился (Unloaded/Unavailable), обычно причина — не установлен .NET 8 SDK или workload ASP.NET в Visual Studio.

---

## Flower ZIP (ваша ссылка Dropbox)

Добавлены скрипты:

- `scripts/setup_flower_assets.ps1`
- `scripts/setup_flower_assets.bat`

Они:
1. скачивают `flower.zip` с Dropbox,
2. распаковывают в `SecureMessenger/3d_models/flower/`,
3. при запуске сервера модели автоматически добавляются в подарки.

Ручной путь также поддерживается: распаковать GLB в
`SecureMessenger/3d_models/flower/`.

---

## API (ядро)

### Auth
- `POST /api/auth/request-code`
- `POST /api/auth/register`
- `POST /api/auth/login`

### Gifts
- `GET /api/gifts`
- `GET /api/gifts/animation-presets`
- `POST /api/gifts/send`
- `POST /api/admin/gifts` (admin)

### Остальное
- `POST /api/chats`, `GET /api/chats`
- `POST /api/messages`, `GET /api/messages/{chatId}`
- `POST /api/stories`, `GET /api/stories`
- `POST /api/voice-rooms`
- `POST /api/streams`
- `POST /api/bots/newbot`
- `POST /api/ai/assistants` (admin)
- `POST /api/admin/coins` (admin)
- `GET /api/admin/metrics` (admin)

---

## Структура

```text
MARX.Windows.sln
build_windows.bat
build_windows.ps1
scripts/
  setup_flower_assets.ps1
  setup_flower_assets.bat
SecureMessenger/
  SecureMessenger.csproj
  3d_models/
  Program.cs
  Models/
  Services/
  Storage/
  Docs/
```
