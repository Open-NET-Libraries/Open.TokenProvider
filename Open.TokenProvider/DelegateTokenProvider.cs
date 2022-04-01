using System;
using System.Threading.Tasks;

namespace Open.TokenProvider;

/// <summary>
/// A <see cref="ITokenProvider{T}"/> implementation.
/// </summary>
public sealed class DelegateTokenProvider<T> : TokenProviderBase<T>
{
    private readonly Func<Task<T>> _requestor;

    /// <summary>
    /// Constructs a <see cref="DelegateTokenProvider{T}"/> that acquires tokens from the <paramref name="requestor"/>.
    /// </summary>
    /// <param name="requestor">The delegate to invoke in order to acquire a new token.</param>
    /// <param name="timeout"><inheritdoc cref="TokenProviderBase{T}.TokenProviderBase(TimeSpan)" path="/param[@name='timeout']"/></param>
    public DelegateTokenProvider(Func<Task<T>> requestor, TimeSpan timeout) : base(timeout)
        => _requestor = requestor ?? throw new ArgumentNullException(nameof(requestor));

    /// <inheritdoc />
    protected override Task<T> RequestTokenAsync()
        => _requestor();
}
