using System;
using System.Threading.Tasks;

namespace Open.TokenProvider;

public interface ITokenProvider<TToken>
{
	Task<IToken<TToken>> Token { get; }

	Task<IToken<TToken>> Refresh(DateTimeOffset timestamp);
}

public static class TokenProviderExtensions
{
	public static async Task<T> Attempt<TToken, T>(
		this ITokenProvider<TToken> provider,
		Func<TToken, Task<T>> handler)
	{
		if (provider is null) throw new ArgumentNullException(nameof(provider));
		if (handler is null) throw new ArgumentNullException(nameof(handler));

		var token = await provider.Token;

		try
		{
			return await handler(token.Value);
		}
		catch
		{
			token = await provider.Refresh(token.Timestamp);
			// Try 1 extra time because the curren token might have been stale.
			return await handler(token.Value);
		}
	}
}