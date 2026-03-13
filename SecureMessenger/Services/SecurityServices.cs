using System.Security.Cryptography;
using System.Text;
using SecureMessenger.Models;
using SecureMessenger.Storage;

namespace SecureMessenger.Services;

public class PasswordService
{
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split(':');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}

public class AuthService(InMemoryStore store)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);

    public string IssueToken(Guid userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        store.Tokens[token] = new AuthSession(token, userId, DateTimeOffset.UtcNow.Add(TokenLifetime));
        return token;
    }

    public User? Resolve(string? bearer)
    {
        if (string.IsNullOrWhiteSpace(bearer)) return null;
        var token = bearer.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();

        if (!store.Tokens.TryGetValue(token, out var session))
            return null;

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            store.Tokens.TryRemove(token, out _);
            return null;
        }

        return store.Users.TryGetValue(session.UserId, out var user) ? user : null;
    }
}

public class EncryptionService
{
    public (string PublicKey, string PrivateKey) GenerateRsaKeyPair()
    {
        using var rsa = RSA.Create(3072);
        return (
            Convert.ToBase64String(rsa.ExportRSAPublicKey()),
            Convert.ToBase64String(rsa.ExportRSAPrivateKey()));
    }

    public (string CipherText, string Nonce, string Tag) EncryptForChat(string text, Guid chatId)
    {
        var key = DeriveChatKey(chatId);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(text);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plaintext, cipher, tag);

        return (Convert.ToBase64String(cipher), Convert.ToBase64String(nonce), Convert.ToBase64String(tag));
    }

    public string DecryptFromChat(EncryptedMessage message)
    {
        var key = DeriveChatKey(message.ChatId);
        var nonce = Convert.FromBase64String(message.Nonce);
        var cipher = Convert.FromBase64String(message.CipherText);
        var tag = Convert.FromBase64String(message.Tag);
        var plaintext = new byte[cipher.Length];

        using var aes = new AesGcm(key, tag.Length);
        aes.Decrypt(nonce, cipher, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] DeriveChatKey(Guid chatId)
    {
        var secret = Encoding.UTF8.GetBytes("ChangeMe_Production_MasterKey_Use_Vault");
        using var hmac = new HMACSHA256(secret);
        return hmac.ComputeHash(chatId.ToByteArray());
    }
}

public static class ThemeCatalog
{
    public static IReadOnlyCollection<ThemePreset> All { get; } =
    [
        new("Dark", "#121822", "#2AABEE", "#82C8FF", "#1B2533", "#2AABEE"),
        new("Light", "#F3F6FA", "#2684FF", "#5AA3FF", "#FFFFFF", "#DFEBFF"),
        new("Neon", "#0C0B16", "#8B5CF6", "#22D3EE", "#191629", "#8B5CF6"),
        new("Cosmic", "#090915", "#F97316", "#EC4899", "#1A132A", "#F97316")
    ];
}
