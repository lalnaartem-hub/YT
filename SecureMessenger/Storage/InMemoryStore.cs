using System.Collections.Concurrent;
using SecureMessenger.Models;

namespace SecureMessenger.Storage;

public class InMemoryStore
{
    public ConcurrentDictionary<Guid, User> Users { get; } = new();
    public ConcurrentDictionary<string, PendingRegistrationCode> RegistrationCodes { get; } = new();

    public ConcurrentDictionary<Guid, Chat> Chats { get; } = new();
    public ConcurrentDictionary<Guid, List<EncryptedMessage>> MessagesByChat { get; } = new();
    public ConcurrentDictionary<Guid, Story> Stories { get; } = new();
    public ConcurrentDictionary<Guid, VoiceRoom> VoiceRooms { get; } = new();
    public ConcurrentDictionary<Guid, StreamSession> Streams { get; } = new();
    public ConcurrentDictionary<Guid, DonationGift> Gifts { get; } = new();
    public ConcurrentBag<GiftTransaction> GiftTransactions { get; } = new();
    public ConcurrentDictionary<Guid, BotAccount> Bots { get; } = new();
    public ConcurrentDictionary<Guid, AiAssistant> Assistants { get; } = new();
    public ConcurrentDictionary<string, AuthSession> Tokens { get; } = new();

    public object MessageLock { get; } = new();
}
