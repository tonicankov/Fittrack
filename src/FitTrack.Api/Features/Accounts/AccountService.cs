using FitTrack.Api.Auth;
using FitTrack.Api.Common;
using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Invites;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Accounts;

public sealed class AccountService(
    AppDbContext db,
    InviteService invites,
    TokenService tokens,
    IPasswordHasher<User> hasher,
    TimeProvider time)
{
    public const int MinPasswordLength = 8;
    private const string InvalidInvite = "This invite code is not valid.";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var timeZone = Validator.Clean(request.TimeZone) ?? TimeZones.Default;

        var validator = new Validator()
            .Text("displayName", request.DisplayName, 50)
            .Text("email", email, 256);
        if (email is not null && !LooksLikeEmail(email))
        {
            validator.Add("email", "Not a valid e-mail address.");
        }
        ValidatePassword(validator, request.Password);
        if (!TimeZones.IsValid(timeZone))
        {
            validator.Add("timeZone", "Unknown time zone.");
        }
        validator.ThrowIfInvalid();

        var invite = await invites.FindRedeemableAsync(request.InviteCode, ct)
            ?? throw RequestValidationException.For("inviteCode", InvalidInvite);

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            throw RequestValidationException.For("email", "This e-mail address is already registered.");
        }

        var now = time.GetUtcNow().UtcDateTime;
        var user = new User
        {
            Email = email!,
            DisplayName = request.DisplayName!.Trim(),
            TimeZone = timeZone,
            DefaultVisibility = Visibility.Friends,
            CreatedAt = now,
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password!);
        db.Users.Add(user);

        invite.UsedById = user.Id;
        invite.UsedAt = now;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else redeemed the same code a moment earlier
            throw RequestValidationException.For("inviteCode", InvalidInvite);
        }

        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = email is null ? null : await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || string.IsNullOrEmpty(request.Password))
        {
            throw new AuthenticationFailedException();
        }

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new AuthenticationFailedException();
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(ct);
        }

        return CreateResponse(user);
    }

    public async Task<MeDto> GetMeAsync(Guid userId, CancellationToken ct) =>
        MeDto.From(await FindUserAsync(userId, ct));

    public async Task<MeDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct)
    {
        var timeZone = Validator.Clean(request.TimeZone);
        var validator = new Validator().Text("displayName", request.DisplayName, 50);
        if (timeZone is null || !TimeZones.IsValid(timeZone))
        {
            validator.Add("timeZone", "Unknown time zone.");
        }
        if (request.DefaultVisibility is null)
        {
            validator.Add("defaultVisibility", "Required.");
        }
        validator.ThrowIfInvalid();

        var user = await FindUserAsync(userId, ct);
        user.DisplayName = request.DisplayName!.Trim();
        user.TimeZone = timeZone!;
        user.DefaultVisibility = request.DefaultVisibility!.Value;
        await db.SaveChangesAsync(ct);
        return MeDto.From(user);
    }

    private async Task<User> FindUserAsync(Guid userId, CancellationToken ct) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct) ?? throw new NotFoundException();

    private AuthResponse CreateResponse(User user)
    {
        var token = tokens.CreateToken(user);
        return new AuthResponse(token.Token, token.ExpiresAt, MeDto.From(user));
    }

    private static void ValidatePassword(Validator validator, string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
        {
            validator.Add("password", $"Must be at least {MinPasswordLength} characters.");
        }
        else if (password.Length > 128)
        {
            validator.Add("password", "Must be at most 128 characters.");
        }
    }

    private static string? NormalizeEmail(string? email) => Validator.Clean(email)?.ToLowerInvariant();

    private static bool LooksLikeEmail(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 && at == email.LastIndexOf('@') && at < email.Length - 1 && !email.Any(char.IsWhiteSpace);
    }
}
