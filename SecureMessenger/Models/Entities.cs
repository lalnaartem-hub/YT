namespace SecureMessenger.Models;

public enum UserRole
{
    User,
    Admin
}

public enum ChatType
{
    Private,
    Group,
    Channel,
    Secret
}

public enum MessageType
{
    Text,
    Image,
    Video,
    Voice,
    File,
    Reaction,
    Forward
}

public enum GiftCategory
{
    Luxury,
    SciFi,
    Nature,
    Fantasy,
    Event
}

public enum GiftRarity
{
    Common,
    Rare,
    Epic,
    Legendary,
    Mythic
}

public record User(
    Guid Id,
    string Username,
    string Email,
    string Phone,
    string DisplayName,
    string PasswordHash,
    string Bio,
    string AvatarUrl,
    string Status,
    bool Premium,
    bool IsVerified,
    decimal Coins,
    UserRole Role,
    DateTimeOffset CreatedAt
);

public record PendingRegistrationCode(
    string Key,
    string Code,
    DateTimeOffset ExpiresAt
);

public record Chat(
    Guid Id,
    string Title,
    ChatType Type,
    IReadOnlyCollection<Guid> ParticipantIds,
    DateTimeOffset CreatedAt
);

public record EncryptedMessage(
    Guid Id,
    Guid ChatId,
    Guid SenderId,
    MessageType Type,
    string CipherText,
    string Nonce,
    string Tag,
    DateTimeOffset SentAt
);

public record Story(
    Guid Id,
    Guid UserId,
    string MediaUrl,
    DateTimeOffset ExpiresAt,
    int Views,
    int Reactions
);

public record VoiceRoom(
    Guid Id,
    string Title,
    Guid OwnerId,
    IReadOnlyCollection<Guid> ModeratorIds,
    DateTimeOffset CreatedAt,
    bool RecordingEnabled
);

public record StreamSession(
    Guid Id,
    Guid HostUserId,
    string Title,
    bool IsLive,
    DateTimeOffset StartedAt,
    string TransportProfile
);

public record DonationGift(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    GiftCategory Category,
    GiftRarity Rarity,
    string Model3dUrl,
    string PreviewImageUrl,
    string ModelFormat,
    string AnimationProfile,
    string ParticleFx,
    string SourceUrl,
    string ThemeAccent,
    decimal RecommendedScale
);

public record GiftTransaction(
    Guid Id,
    Guid GiftId,
    Guid FromUserId,
    Guid ToUserId,
    DateTimeOffset CreatedAt
);

public record BotAccount(
    Guid Id,
    string Name,
    string ApiToken,
    string Description,
    DateTimeOffset CreatedAt
);

public record AiAssistant(
    Guid Id,
    string Name,
    string Capability,
    bool ModerationEnabled
);

public record AuthSession(
    string Token,
    Guid UserId,
    DateTimeOffset ExpiresAt
);
