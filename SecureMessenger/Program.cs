using System.Security.Cryptography;
using SecureMessenger.Models;
using SecureMessenger.Services;
using SecureMessenger.Storage;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<InMemoryStore>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<EncryptionService>();

var app = builder.Build();
SeedDefaults(app.Services.GetRequiredService<InMemoryStore>());

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "MARX Core", utc = DateTimeOffset.UtcNow }));
app.MapGet("/api/themes", () => Results.Ok(ThemeCatalog.All));
app.MapGet("/api/platforms", () => Results.Ok(new { windows = "supported", android = "planned", web = "planned" }));

app.MapPost("/api/auth/register", (RegisterRequest req, InMemoryStore store, PasswordService passwordService, AuthService authService) =>
{
    if (!IsValidUsername(req.Username))
        return Results.BadRequest("Username: only letters, numbers and underscore; length 3..24");

    if (string.IsNullOrWhiteSpace(req.DisplayName) || req.DisplayName.Length > 60)
        return Results.BadRequest("DisplayName is required and must be <= 60 chars");

    if (!IsStrongPassword(req.Password))
        return Results.BadRequest("Password must be >= 10 chars and contain uppercase, lowercase and digit");

    if (store.Users.Values.Any(x => x.Username.Equals(req.Username, StringComparison.OrdinalIgnoreCase)))
        return Results.BadRequest("Username already exists");

    var canBeAdmin = req.Admin && store.Users.IsEmpty;
    var user = new User(
        Guid.NewGuid(),
        req.Username.Trim(),
        req.DisplayName.Trim(),
        passwordService.Hash(req.Password),
        req.Bio?.Trim() ?? string.Empty,
        req.AvatarUrl?.Trim() ?? string.Empty,
        req.Status?.Trim() ?? "Online",
        req.Premium,
        0,
        canBeAdmin ? UserRole.Admin : UserRole.User,
        DateTimeOffset.UtcNow);

    store.Users[user.Id] = user;
    var token = authService.IssueToken(user.Id);
    return Results.Ok(new AuthResponse(token, user.Id, user.Username, user.Role.ToString()));
});

app.MapPost("/api/auth/login", (LoginRequest req, InMemoryStore store, PasswordService passwordService, AuthService authService) =>
{
    var user = store.Users.Values.FirstOrDefault(x => x.Username.Equals(req.Username, StringComparison.OrdinalIgnoreCase));
    if (user is null || !passwordService.Verify(req.Password, user.PasswordHash))
        return Results.Unauthorized();

    var token = authService.IssueToken(user.Id);
    return Results.Ok(new AuthResponse(token, user.Id, user.Username, user.Role.ToString()));
});

app.MapPost("/api/chats", (HttpRequest httpRequest, CreateChatRequest req, InMemoryStore store, AuthService authService) =>
{
    var actor = RequireAuth(httpRequest, authService);
    if (actor is null) return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(req.Title) || req.Title.Length > 120)
        return Results.BadRequest("Title must be 1..120 chars");

    var participants = req.ParticipantIds.Append(actor.Id).Distinct().ToArray();
    if (participants.Any(x => !store.Users.ContainsKey(x))) return Results.BadRequest("Unknown participant");
    if (req.Type == ChatType.Private && participants.Length != 2)
        return Results.BadRequest("Private chat should contain exactly two members");

    var chat = new Chat(Guid.NewGuid(), req.Title.Trim(), req.Type, participants, DateTimeOffset.UtcNow);
    store.Chats[chat.Id] = chat;
    return Results.Ok(chat);
});

app.MapGet("/api/chats", (HttpRequest httpRequest, InMemoryStore store, AuthService authService) =>
{
    var actor = RequireAuth(httpRequest, authService);
    if (actor is null) return Results.Unauthorized();
    return Results.Ok(store.Chats.Values.Where(c => c.ParticipantIds.Contains(actor.Id)).OrderByDescending(c => c.CreatedAt));
});

app.MapPost("/api/messages", (HttpRequest httpRequest, SendMessageRequest req, InMemoryStore store, AuthService authService, EncryptionService encryption) =>
{
    var actor = RequireAuth(httpRequest, authService);
    if (actor is null) return Results.Unauthorized();
    if (!store.Chats.TryGetValue(req.ChatId, out var chat)) return Results.NotFound("Chat not found");
    if (!chat.ParticipantIds.Contains(actor.Id)) return Results.Forbid();
    if (string.IsNullOrWhiteSpace(req.Payload) || req.Payload.Length > 16_000) return Results.BadRequest("Invalid payload length");

    var encrypted = encryption.EncryptForChat(req.Payload, chat.Id);
    var message = new EncryptedMessage(Guid.NewGuid(), req.ChatId, actor.Id, req.Type, encrypted.CipherText, encrypted.Nonce, encrypted.Tag, DateTimeOffset.UtcNow);

    lock (store.MessageLock)
    {
        if (!store.MessagesByChat.TryGetValue(req.ChatId, out var bucket))
        {
            bucket = [];
            store.MessagesByChat[req.ChatId] = bucket;
        }

        bucket.Add(message);
    }

    return Results.Ok(message);
});

app.MapGet("/api/messages/{chatId:guid}", (Guid chatId, HttpRequest httpRequest, InMemoryStore store, AuthService authService, EncryptionService encryption) =>
{
    var actor = RequireAuth(httpRequest, authService);
    if (actor is null) return Results.Unauthorized();
    if (!store.Chats.TryGetValue(chatId, out var chat)) return Results.NotFound("Chat not found");
    if (!chat.ParticipantIds.Contains(actor.Id)) return Results.Forbid();

    var result = store.MessagesByChat.TryGetValue(chatId, out var bucket)
        ? bucket.OrderBy(x => x.SentAt).Select(m => new MessageView(m.Id, m.ChatId, m.SenderId, m.Type, encryption.DecryptFromChat(m), m.SentAt))
        : Enumerable.Empty<MessageView>();

    return Results.Ok(result);
});

app.MapPost("/api/stories", (HttpRequest httpRequest, CreateStoryRequest req, InMemoryStore store, AuthService authService) =>
{
    var actor = RequireAuth(httpRequest, authService);
    if (actor is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(req.MediaUrl)) return Results.BadRequest("MediaUrl is required");

    CleanupExpiredStories(store);
    var story = new Story(Guid.NewGuid(), actor.Id, req.MediaUrl.Trim(), DateTimeOffset.UtcNow.AddHours(24), 0, 0);
    store.Stories[story.Id] = story;
    return Results.Ok(story);
});

app.MapGet("/api/stories", (InMemoryStore store) =>
{
    CleanupExpiredStories(store);
    var now = DateTimeOffset.UtcNow;
    return Results.Ok(store.Stories.Values.Where(s => s.ExpiresAt > now).OrderByDescending(s => s.ExpiresAt));
});

app.MapPost("/api/voice-rooms", (HttpRequest httpRequest, CreateVoiceRoomRequest req, InMemoryStore store, AuthService authService) =>
{
    var actor = RequireAuth(httpRequest, authService);
    if (actor is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(req.Title)) return Results.BadRequest("Title is required");

    var room = new VoiceRoom(Guid.NewGuid(), req.Title.Trim(), actor.Id, [actor.Id], DateTimeOffset.UtcNow, req.RecordingEnabled);
    store.VoiceRooms[room.Id] = room;
    return Results.Ok(room);
});

app.MapPost("/api/streams", (HttpRequest httpRequest, CreateStreamRequest req, InMemoryStore store, AuthService authService) =>
{
    var actor = RequireAuth(httpRequest, authService);
    if (actor is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(req.Title)) return Results.BadRequest("Title is required");

    var profile = string.IsNullOrWhiteSpace(req.TransportProfile) ? "WebRTC+HLS" : req.TransportProfile.Trim();
    var stream = new StreamSession(Guid.NewGuid(), actor.Id, req.Title.Trim(), true, DateTimeOffset.UtcNow, profile);
    store.Streams[stream.Id] = stream;
    return Results.Ok(stream);
});

app.MapPost("/api/bots/newbot", (HttpRequest httpRequest, NewBotRequest req, InMemoryStore store, AuthService authService) =>
{
    var actor = RequireAuth(httpRequest, authService);
    if (actor is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest("Name is required");

    var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var bot = new BotAccount(Guid.NewGuid(), req.Name.Trim(), token, req.Description?.Trim() ?? string.Empty, DateTimeOffset.UtcNow);
    store.Bots[bot.Id] = bot;
    return Results.Ok(bot);
});

app.MapPost("/api/ai/assistants", (HttpRequest httpRequest, CreateAiAssistantRequest req, InMemoryStore store, AuthService authService) =>
{
    var actor = RequireAdmin(httpRequest, authService);
    if (actor is null) return Results.Forbid();

    if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Capability))
        return Results.BadRequest("Name and Capability are required");

    var assistant = new AiAssistant(Guid.NewGuid(), req.Name.Trim(), req.Capability.Trim(), req.ModerationEnabled);
    store.Assistants[assistant.Id] = assistant;
    return Results.Ok(assistant);
});

app.MapGet("/api/gifts", (InMemoryStore store, GiftRarity? rarity, GiftCategory? category) =>
{
    var query = store.Gifts.Values.AsEnumerable();
    if (rarity is not null) query = query.Where(x => x.Rarity == rarity);
    if (category is not null) query = query.Where(x => x.Category == category);
    return Results.Ok(query.OrderBy(x => x.Price).ThenBy(x => x.Name));
});

app.MapPost("/api/gifts/send", (HttpRequest httpRequest, SendGiftRequest req, InMemoryStore store, AuthService authService) =>
{
    var actor = RequireAuth(httpRequest, authService);
    if (actor is null) return Results.Unauthorized();
    if (!store.Gifts.ContainsKey(req.GiftId)) return Results.NotFound("Gift not found");
    if (!store.Users.TryGetValue(req.ToUserId, out var receiver)) return Results.NotFound("User not found");

    var tx = new GiftTransaction(Guid.NewGuid(), req.GiftId, actor.Id, req.ToUserId, DateTimeOffset.UtcNow);
    store.GiftTransactions.Add(tx);
    return Results.Ok(new { transaction = tx, receiver = receiver.Username });
});

app.MapPost("/api/admin/gifts", (HttpRequest httpRequest, CreateGiftRequest req, InMemoryStore store, AuthService authService) =>
{
    var actor = RequireAdmin(httpRequest, authService);
    if (actor is null) return Results.Forbid();

    if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Model3dUrl) || req.Price <= 0)
        return Results.BadRequest("Gift name/model3dUrl required, price must be > 0");

    if (string.IsNullOrWhiteSpace(req.PreviewImageUrl) || string.IsNullOrWhiteSpace(req.SourceUrl))
        return Results.BadRequest("PreviewImageUrl and SourceUrl are required");

    var gift = new DonationGift(
        Guid.NewGuid(),
        req.Name.Trim(),
        req.Description?.Trim() ?? string.Empty,
        req.Price,
        req.Category,
        req.Rarity,
        req.Model3dUrl.Trim(),
        req.PreviewImageUrl.Trim(),
        string.IsNullOrWhiteSpace(req.ModelFormat) ? "GLB" : req.ModelFormat.Trim().ToUpperInvariant(),
        string.IsNullOrWhiteSpace(req.AnimationProfile) ? "Rotate+Glow" : req.AnimationProfile.Trim(),
        string.IsNullOrWhiteSpace(req.ParticleFx) ? "None" : req.ParticleFx.Trim(),
        req.SourceUrl.Trim(),
        req.ThemeAccent?.Trim() ?? "#2AABEE",
        req.RecommendedScale <= 0 ? 1.0m : req.RecommendedScale);
    store.Gifts[gift.Id] = gift;
    return Results.Ok(gift);
});

app.MapPost("/api/admin/coins", (HttpRequest httpRequest, CreditCoinsRequest req, InMemoryStore store, AuthService authService) =>
{
    var actor = RequireAdmin(httpRequest, authService);
    if (actor is null) return Results.Forbid();
    if (req.Amount == 0) return Results.BadRequest("Amount must not be zero");
    if (!store.Users.TryGetValue(req.UserId, out var user)) return Results.NotFound("User not found");

    var next = user.Coins + req.Amount;
    if (next < 0) return Results.BadRequest("Coins cannot be negative");

    store.Users[user.Id] = user with { Coins = next };
    return Results.Ok(store.Users[user.Id]);
});

app.MapGet("/api/admin/metrics", (HttpRequest httpRequest, InMemoryStore store, AuthService authService) =>
{
    var actor = RequireAdmin(httpRequest, authService);
    if (actor is null) return Results.Forbid();

    CleanupExpiredStories(store);

    return Results.Ok(new
    {
        users = store.Users.Count,
        chats = store.Chats.Count,
        messages = store.MessagesByChat.Values.Sum(x => x.Count),
        stories = store.Stories.Count,
        voiceRooms = store.VoiceRooms.Count,
        streams = store.Streams.Count,
        bots = store.Bots.Count,
        assistants = store.Assistants.Count,
        gifts = store.Gifts.Count,
        giftTransactions = store.GiftTransactions.Count,
        generatedAt = DateTimeOffset.UtcNow
    });
});

app.Run();

static User? RequireAuth(HttpRequest request, AuthService authService)
    => authService.Resolve(request.Headers.Authorization);

static User? RequireAdmin(HttpRequest request, AuthService authService)
{
    var user = RequireAuth(request, authService);
    return user?.Role == UserRole.Admin ? user : null;
}

static bool IsValidUsername(string username)
{
    if (string.IsNullOrWhiteSpace(username) || username.Length is < 3 or > 24)
        return false;

    return username.All(ch => char.IsLetterOrDigit(ch) || ch == '_');
}

static bool IsStrongPassword(string password)
{
    if (string.IsNullOrWhiteSpace(password) || password.Length < 10)
        return false;

    return password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit);
}

static void CleanupExpiredStories(InMemoryStore store)
{
    var now = DateTimeOffset.UtcNow;
    var expired = store.Stories.Where(pair => pair.Value.ExpiresAt <= now).Select(pair => pair.Key).ToArray();
    foreach (var storyId in expired)
    {
        store.Stories.TryRemove(storyId, out _);
    }
}

static void SeedDefaults(InMemoryStore store)
{
    var gifts = new[]
    {
        new DonationGift(Guid.NewGuid(), "Rocket", "Фирменная ракета MARX", 2.99m, GiftCategory.SciFi, GiftRarity.Rare,
            "https://modelviewer.dev/shared-assets/models/RocketToy.glb",
            "https://images.unsplash.com/photo-1446776653964-20c1d3a81b06?auto=format&fit=crop&w=800&q=60",
            "GLB", "Launch+Glow", "Trail", "https://modelviewer.dev/", "#2AABEE", 1.15m),

        new DonationGift(Guid.NewGuid(), "Diamond", "Бриллиант с неоновым свечением", 9.99m, GiftCategory.Luxury, GiftRarity.Epic,
            "https://modelviewer.dev/shared-assets/models/Astronaut.glb",
            "https://images.unsplash.com/photo-1521106581851-da5b6457f674?auto=format&fit=crop&w=800&q=60",
            "GLB", "Rotate+Bloom", "Sparkles", "https://modelviewer.dev/", "#22D3EE", 0.95m),

        new DonationGift(Guid.NewGuid(), "Crown", "Корона для премиум-чата", 19.99m, GiftCategory.Luxury, GiftRarity.Legendary,
            "https://modelviewer.dev/shared-assets/models/NeilArmstrong.glb",
            "https://images.unsplash.com/photo-1463100099107-aa0980c362e6?auto=format&fit=crop&w=800&q=60",
            "GLB", "Float+Shine", "GoldenDust", "https://modelviewer.dev/", "#F59E0B", 1.0m),

        new DonationGift(Guid.NewGuid(), "Dragon", "Редкий дракон с анимацией пламени", 49.99m, GiftCategory.Fantasy, GiftRarity.Mythic,
            "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/DragonAttenuation/glTF-Binary/DragonAttenuation.glb",
            "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=800&q=60",
            "GLB", "Hover+Fire", "FlameAura", "https://github.com/KhronosGroup/glTF-Sample-Models", "#EF4444", 1.25m),

        new DonationGift(Guid.NewGuid(), "Galaxy Cube", "Космический куб с внутренним свечением", 29.99m, GiftCategory.SciFi, GiftRarity.Legendary,
            "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Cube/glTF-Binary/Cube.glb",
            "https://images.unsplash.com/photo-1534447677768-be436bb09401?auto=format&fit=crop&w=800&q=60",
            "GLB", "Spin+Pulse", "Nebula", "https://github.com/KhronosGroup/glTF-Sample-Models", "#8B5CF6", 1.2m)
    };

    foreach (var gift in gifts)
        store.Gifts[gift.Id] = gift;

    SeedLocalFlowerCollection(store);
}

static void SeedLocalFlowerCollection(InMemoryStore store)
{
    var basePath = Path.Combine(AppContext.BaseDirectory, "3d_models", "flower");
    if (!Directory.Exists(basePath))
        return;

    var files = Directory.GetFiles(basePath, "*.glb", SearchOption.AllDirectories)
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    foreach (var file in files)
    {
        var fileName = Path.GetFileNameWithoutExtension(file);
        var profile = BuildFlowerGiftProfile(fileName);

        var alreadyExists = store.Gifts.Values.Any(g =>
            g.Model3dUrl.Equals(file, StringComparison.OrdinalIgnoreCase) ||
            g.Name.Equals(profile.DisplayName, StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
            continue;

        var gift = new DonationGift(
            Guid.NewGuid(),
            profile.DisplayName,
            profile.Description,
            profile.Price,
            profile.Category,
            profile.Rarity,
            file,
            "",
            "GLB",
            profile.AnimationProfile,
            profile.ParticleFx,
            "user-local-zip:flower",
            profile.ThemeAccent,
            profile.Scale);

        store.Gifts[gift.Id] = gift;
    }
}

static (string DisplayName, string Description, decimal Price, GiftCategory Category, GiftRarity Rarity, string AnimationProfile, string ParticleFx, string ThemeAccent, decimal Scale) BuildFlowerGiftProfile(string sourceName)
{
    var normalized = sourceName.Replace('_', ' ').Replace('-', ' ').Trim();
    var title = string.IsNullOrWhiteSpace(normalized)
        ? "Flower Gift"
        : string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => char.ToUpperInvariant(x[0]) + x[1..].ToLowerInvariant()));

    var lower = normalized.ToLowerInvariant();

    var rarity = lower.Contains("gold") || lower.Contains("royal") ? GiftRarity.Legendary
        : lower.Contains("crystal") || lower.Contains("diamond") ? GiftRarity.Epic
        : lower.Contains("rose") || lower.Contains("tulip") ? GiftRarity.Rare
        : GiftRarity.Common;

    var category = lower.Contains("rose") || lower.Contains("tulip") || lower.Contains("orchid")
        ? GiftCategory.Nature
        : GiftCategory.Event;

    var animation = lower.Contains("rose") ? "Bloom+Sparkle"
        : lower.Contains("tulip") ? "Sway+Petals"
        : lower.Contains("orchid") ? "Float+Glow"
        : "Rotate+Bloom";

    var fx = lower.Contains("gold") ? "GoldenDust"
        : lower.Contains("crystal") ? "Shards"
        : "Petals";

    var accent = lower.Contains("rose") ? "#FF4D8D"
        : lower.Contains("tulip") ? "#F97316"
        : lower.Contains("orchid") ? "#A855F7"
        : "#22D3EE";

    var price = rarity switch
    {
        GiftRarity.Common => 1.99m,
        GiftRarity.Rare => 3.99m,
        GiftRarity.Epic => 8.99m,
        GiftRarity.Legendary => 17.99m,
        GiftRarity.Mythic => 34.99m,
        _ => 2.99m
    };

    return (
        $"{title} Flower",
        $"3D-подарок из вашего ZIP: {title}. Подготовлен для витрины MARX.",
        price,
        category,
        rarity,
        animation,
        fx,
        accent,
        1.0m);
}
