namespace SecureMessenger.Models;

public record RegisterCodeRequest(string Username, string Email, string Phone);

public record RegisterRequest(
    string Username,
    string Email,
    string Phone,
    string VerificationCode,
    string DisplayName,
    string Password,
    string Bio,
    string AvatarUrl,
    string Status,
    bool Premium,
    bool Admin,
    bool AcceptTerms);

public record LoginRequest(string Login, string Password);
public record AuthResponse(string Token, Guid UserId, string Username, string Role);

public record CreateChatRequest(string Title, ChatType Type, IReadOnlyCollection<Guid> ParticipantIds);
public record SendMessageRequest(Guid ChatId, MessageType Type, string Payload);
public record MessageView(Guid Id, Guid ChatId, Guid SenderId, MessageType Type, string PlainText, DateTimeOffset SentAt);

public record CreateStoryRequest(string MediaUrl);
public record CreateVoiceRoomRequest(string Title, bool RecordingEnabled);
public record CreateStreamRequest(string Title, string TransportProfile);

public record CreateGiftRequest(
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
    decimal RecommendedScale);

public record SendGiftRequest(Guid GiftId, Guid ToUserId);
public record CreditCoinsRequest(Guid UserId, decimal Amount);

public record NewBotRequest(string Name, string Description);
public record CreateAiAssistantRequest(string Name, string Capability, bool ModerationEnabled);

public record ThemePreset(string Name, string Background, string Primary, string Secondary, string BubbleIn, string BubbleOut);
