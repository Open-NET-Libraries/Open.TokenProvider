using System;

namespace Open.TokenProvider;

/// <summary>
/// Represents a token value with a timestamp of when the token was acquired.
/// </summary>
public interface IToken<T>
{
    /// <summary>
    /// The value of the token.
    /// </summary>
    T Value { get; }

    /// <summary>
    /// When the token was acquired.
    /// </summary>
	DateTimeOffset Timestamp { get; }
}
