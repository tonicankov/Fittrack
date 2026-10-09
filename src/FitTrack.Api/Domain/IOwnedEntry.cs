namespace FitTrack.Api.Domain;

/// <summary>
/// An entry that belongs to one member and carries its own visibility.
/// Implemented by everything a member can choose to share with friends.
/// </summary>
public interface IOwnedEntry
{
    Guid UserId { get; }

    Visibility Visibility { get; }
}
