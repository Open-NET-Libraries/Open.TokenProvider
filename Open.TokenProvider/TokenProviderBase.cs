using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace Open.TokenProvider;

/// <summary>
/// A base class for implementing a <see cref="ITokenProvider{T}"/>.
/// </summary>
public abstract class TokenProviderBase<T> : ITokenProvider<T>
{
    /// <summary>
    /// Primary constructor for <see cref="TokenProviderBase{T}"/>
    /// </summary>
    /// <param name="timeout"><inheritdoc cref="Timeout" path="/summary"/></param>
    protected TokenProviderBase(TimeSpan timeout)
    {
        Timeout = timeout;
    }

    /// <summary>
    /// The amount of time to allow before a new token will be used.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Acquires a new token.
    /// </summary>
    protected abstract Task<T> RequestTokenAsync();

    /// <summary>
    /// Calls <see cref="RequestTokenAsync"/> and generates a timestamp.
    /// </summary>
    protected async Task<IToken<T>> GetFreshTokenAsync()
        => Token.Create(await RequestTokenAsync().ConfigureAwait(false));

    private Lazy<Task<IToken<T>>>? _currentToken;

    /// <inheritdoc/>
    [SuppressMessage("Reliability", "CA2008:Do not create tasks without passing a TaskScheduler")]
    [SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Checks for completion first.")]
    public Task<IToken<T>> CurrentToken
    {
        get
        {
            if (Timeout <= TimeSpan.Zero) return GetFreshTokenAsync();
            // If Timeout == TimeSpan.Zero then allow for multiple threads to get the same token
            // but subsequent threads will get a new one.

            bool timeoutChecked = false;

        retry:
            // We need to atomically get a single running task.
            Lazy<Task<IToken<T>>>? lazy = null;
            lazy = LazyInitializer.EnsureInitialized(ref _currentToken,
                () => new Lazy<Task<IToken<T>>>(
                () => GetFreshTokenAsync().ContinueWith(t =>
                {
                    // If the task failed for this lazy, we need to clear the result.
                    if (t.IsFaulted || t.IsCanceled)
                        Interlocked.CompareExchange(ref _currentToken, lazy, null);
                    return t;
                }).Unwrap()))!;

            try
            {
                var task = lazy.Value;
                // In flight tasks are assumed new. Allows for resetting if timeout has expired.
                if (timeoutChecked || !task.IsCompleted || task.Result.GetAge() < Timeout) return task;
            }
            catch
            {
                // If getting the task failed (probably due to implmentation) don't retain the result.
                Interlocked.CompareExchange(ref _currentToken, lazy, null);
                throw;
            }

            timeoutChecked = true;
            Interlocked.CompareExchange(ref _currentToken, lazy, null);
            goto retry; // avoid recursion.
        }
    }

    /// <inheritdoc/>
    [SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Checks for completion first.")]
    public Task<IToken<T>> GetTokenAsync(DateTimeOffset timestamp)
    {
        // The following is necessary to ensure threads that are requesting a token at the same time don't keep resetting the process.
        var lazy = _currentToken;
        if (lazy is null) return CurrentToken;
        var task = lazy.Value;
        if (!task.IsCompleted || task.Result.Timestamp > timestamp) return task;
        Interlocked.CompareExchange(ref _currentToken, lazy, null);
        return CurrentToken;
    }
}
