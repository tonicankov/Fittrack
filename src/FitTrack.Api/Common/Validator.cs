namespace FitTrack.Api.Common;

/// <summary>
/// Collects field errors and throws one <see cref="RequestValidationException"/> with all of them.
/// </summary>
public sealed class Validator
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.OrdinalIgnoreCase);

    public bool HasErrors => _errors.Count > 0;

    public Validator Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
        {
            _errors[field] = messages = [];
        }

        messages.Add(message);
        return this;
    }

    /// <summary>Requires a non-blank string of at most <paramref name="maxLength"/> characters.</summary>
    public Validator Text(string field, string? value, int maxLength, bool required = true)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                Add(field, "Required.");
            }
        }
        else if (value.Trim().Length > maxLength)
        {
            Add(field, $"Must be at most {maxLength} characters.");
        }

        return this;
    }

    public Validator Range(string field, decimal? value, decimal min, decimal max, bool required = true)
    {
        if (value is null)
        {
            if (required)
            {
                Add(field, "Required.");
            }
        }
        else if (value < min || value > max)
        {
            Add(field, $"Must be between {min} and {max}.");
        }

        return this;
    }

    public void ThrowIfInvalid()
    {
        if (HasErrors)
        {
            throw new RequestValidationException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }
    }

    /// <summary>Trims and turns blank strings into null.</summary>
    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
