using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly:CLSCompliant(true)]
namespace Open.TokenProvider;

/// <summary>
/// Represents a provider for acquiring tokens.
/// </summary>
public interface ITokenProvider<T>
{
    /// <summary>
    /// The most recently available token.
    /// </summary>
    Task<IToken<T>> CurrentToken { get; }

    /// <summary>
    /// Updates the current token if the timestamp is greater than the current one.
    /// </summary>
    /// <param name="timestamp">The <see cref="DateTimeOffset"/> to compare against the current token.</param>
    /// <returns>A valid token that is at least as fresh as the timestamp.</returns>
    Task<IToken<T>> GetTokenAsync(DateTimeOffset timestamp);
}

/// <summary>
/// Extensions for <see cref="ITokenProvider{T}"/>.
/// </summary>
public static class TokenProviderExtensions
{
    /// <inheritdoc cref="ITokenProvider{T}.GetTokenAsync(DateTimeOffset)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<IToken<T>> GetTokenAsync<T>(
        this ITokenProvider<T> provider,
        IToken<T> previousToken)
    {
        if (provider is null)
            throw new ArgumentNullException(nameof(provider));
        if (previousToken is null)
            throw new ArgumentNullException(nameof(previousToken));

        return provider.GetTokenAsync(previousToken.Timestamp);
    }

    /// <summary>
    /// Provides the current token to the <paramref name="handler"/>.
    /// If the <paramref name="handler"/> throws an exception,
    /// a new token is acquired and the handler is invoked a second time with the new token.
    /// </summary>
    /// <exception cref="ArgumentNullException">If either the provider or the handler are null.</exception>
    public static async Task Attempt<T>(
        this ITokenProvider<T> provider,
        Func<T, Task> handler)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var token = await provider.CurrentToken.ConfigureAwait(false);

        try
        {
            await handler(token.Value).ConfigureAwait(false);
            return;
        }
        catch
        {
            var newToken = await provider.GetTokenAsync(token.Timestamp).ConfigureAwait(false);
            if (token == newToken) throw; // If by chance there's no difference, then no need to retry.
            token = newToken;
        }

        // Try 1 extra time because the current token might have been stale.
        await handler(token.Value).ConfigureAwait(false);
    }

    /// <inheritdoc cref="Attempt{T}(ITokenProvider{T}, Func{T, Task})"/>
    public static async Task<TResult> Attempt<T, TResult>(
        this ITokenProvider<T> provider,
        Func<T, Task<TResult>> handler)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var token = await provider.CurrentToken.ConfigureAwait(false);

        try
        {
            return await handler(token.Value).ConfigureAwait(false);
        }
        catch
        {
            var newToken = await provider.GetTokenAsync(token.Timestamp).ConfigureAwait(false);
            if (token == newToken) throw; // If by chance there's no difference, then no need to retry.
            token = newToken;
        }

        return await handler(token.Value).ConfigureAwait(false);
    }
}