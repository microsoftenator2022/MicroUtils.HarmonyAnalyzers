using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace MicroUtils.HarmonyAnalyzers;
public partial class Util
{
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source) where T : notnull =>
        source.SelectMany<T?, T>(element => element is not null ? [element] : []);

    public static bool ContainsAny<T>(this IEnumerable<T> source, IEnumerable<T> values, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;

        foreach (var value in values)
        {
            if (source.Contains(value, comparer))
                return true;
        }

        return false;
    }

    public static T? TryExactlyOne<T>(this IEnumerable<T> source)
    {
        var firstTwo = source.Take(2).ToImmutableArray();
        if (firstTwo.Length != 1)
            return default;

        return firstTwo[0];
    }

    public static IEnumerable<T> ReturnSeq<T>(T? source) where T : notnull
    {
        if (source is not null)
            yield return source;
    }

    public static IEnumerable<(int index, T element)> Indexed<T>(this IEnumerable<T> source)
    {
        var i = 0;
        foreach (var element in source)
        {
            yield return (i, element);
            i++;
        }
    }

    /// <summary>
    /// Returns a singleton collection containg the source element or an empty collection if the source element is null
    /// </summary>
    public static IEnumerable<T> EmptyIfNull<T>(this T? source) where T : notnull =>
        source is not null ? [source] : [];
}
