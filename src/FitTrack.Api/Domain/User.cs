namespace FitTrack.Api.Domain;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stored in lower case so lookups are case-insensitive.</summary>
    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public string PasswordHash { get; set; } = "";

    /// <summary>IANA time zone, e.g. Europe/Amsterdam. Used to decide what "a day" means for this member.</summary>
    public string TimeZone { get; set; } = "Europe/Amsterdam";

    public Visibility DefaultVisibility { get; set; } = Visibility.Friends;

    public DateTime CreatedAt { get; set; }
}
