using System;
using System.Runtime.CompilerServices;

namespace Open.TokenProvider;

/// <inheritdoc cref="IToken{T}"/>
public record class Token<T> : IToken<T>
{
    /// <summary>
    /// Constructs a <see cref="Token{T}"/> with the <paramref name="value"/> and <paramref name="timestamp"/>.
    /// </summary>
	public Token(T value, DateTimeOffset timestamp)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        if (timestamp == DateTimeOffset.MaxValue) // Prevent accidents.
            throw new ArgumentOutOfRangeException(nameof(timestamp), "Using DateTimeOffset.MaxValue will result in a permanent token.");
        Timestamp = timestamp;
    }

    /// <inheritdoc/>
	public T Value { get; }

    /// <inheritdoc/>
	public DateTimeOffset Timestamp { get; }
}

/// <summary>
/// Extensions for <see cref="Token{T}"/>.
/// </summary>
public static class Token
{
    /// <summary>
    /// Creates a <see cref="Token{T}"/> with the current UTC <see cref="DateTimeOffset"/> as the timestamp.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Token<T> Create<T>(T token)
        => new(token, DateTimeOffset.UtcNow);

    /// <summary>
    /// Returns the amount of time that has passed (UTC) relative to the token's timestamp.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan GetAge<T>(this IToken<T> token)
    {
        if (token is null)
            throw new ArgumentNullException(nameof(token));

        return DateTimeOffset.UtcNow - token.Timestamp;
    }
}
