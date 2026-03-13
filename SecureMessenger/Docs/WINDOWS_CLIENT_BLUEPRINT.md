# MARX Windows Client Blueprint (WPF / Avalonia)

## Цель
Сделать полноценный нативный клиент для Windows, без web/mobile в текущем релизе.

## Слои клиента

- Presentation (Views + Styles + Themes)
- ViewModel (MVVM)
- Application services (chat, stories, voice, stream, gifts)
- Transport (HTTP + WebSocket)
- Security (device keychain, local encryption)
- Persistence (SQLite encrypted cache)

## Главные модули UI

1. Auth Window
2. Main Shell (левая панель, список чатов, рабочая область)
3. Chat View (типизированные сообщения)
4. Channel/Group management
5. Stories strip
6. Voice rooms overlay
7. Stream panel
8. Gifts store (3D preview)
9. Admin center (если роль Admin)

## Темы

- Dark
- Light
- Neon
- Cosmic
- Custom palette editor (следующий этап)

## Реалтайм

- HTTP для CRUD
- WebSocket/SignalR для live сообщений и presence
- Reconnect strategy: exponential backoff

## Безопасность Windows

- DPAPI для device secret
- Шифрование локального кэша
- Сертификат pinning (опционально)

## Очередность разработки

1. Auth + chats + messages
2. Attachments + stories
3. Voice rooms + streams
4. Gifts + economy
5. Bots + AI panes
6. Admin tools
