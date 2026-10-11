namespace FitTrack.Api.Common;

/// <summary>
/// One page of a list that is ordered newest first. Pass <see cref="NextBefore"/> as
/// <c>?before=</c> to get the next page; it is null on the last page.
/// </summary>
public sealed record Page<T>(List<T> Items, DateTime? NextBefore)
{
    public static Page<T> From<TSource>(List<TSource> rows, int pageSize, Func<TSource, DateTime> cursor, Func<TSource, T> map)
    {
        var hasMore = rows.Count > pageSize;
        var items = rows.Take(pageSize).ToList();
        return new Page<T>(items.Select(map).ToList(), hasMore ? cursor(items[^1]) : null);
    }
}
