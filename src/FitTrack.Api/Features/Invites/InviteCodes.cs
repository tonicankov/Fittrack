using System.Security.Cryptography;

namespace FitTrack.Api.Features.Invites;

public static class InviteCodes
{
    // No 0/O or 1/I/L, so codes can be read out or typed without mix-ups
    public const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    public const int Length = 10;

    public static string Generate() => string.Create(Length, 0, static (span, _) =>
    {
        for (var i = 0; i < span.Length; i++)
        {
            span[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
    });

    /// <summary>Accepts codes typed in lower case or with spaces and dashes.</summary>
    public static string Normalize(string? code) =>
        new((code ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
