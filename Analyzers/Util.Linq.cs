﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers;
public partial class Util
{
    public static IEnumerable<(T1 first, T2 second)> Zip<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second) =>
        first.Zip(second, (a, b) => (a, b));

    [Obsolete("Use Choose and Optional.MaybeValue")]
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

    [Obsolete("Use TrySingle and ValueOrDefault")]
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

    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null)
    {
        comparer ??= EqualityComparer<TKey>.Default;

        foreach (var group in source.GroupBy(keySelector, comparer))
        {
            yield return group.First();
        }
    }

    public static IEnumerable<U> Upcast<T, U>(this IEnumerable<T> source) where T : U =>
        source.Select<T, U>(element => element);

    public static IEnumerable<U> SelectWhere<T, U>(
        this IEnumerable<T> source, Func<T, U> selector, Func<U, bool> predicate) =>
        source.Select(selector).Where(predicate);

    public static IEnumerable<TValue> SelectWhere<T, TKey, TValue>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        Func<TKey, bool> predicate,
        Func<T, TValue> valueSelector) =>
        source
            .Where(element => predicate(keySelector(element)))
            .Select(valueSelector);

    public static IEnumerable<T> Concat<T>(this IEnumerable<IEnumerable<T>> source) => source.SelectMany(element => element);

    public static IEnumerable<(T, T)> Pairwise<T>(this IEnumerable<T> source)
    {
        Optional<T> previous = default;
        foreach (var element in source)
        {
            if (!previous.HasValue)
            {
                previous = element;
                continue;
            }

            yield return (previous.Value, element);
        }
    }
}
