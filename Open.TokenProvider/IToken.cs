using System;
using System.Collections.Generic;
using System.Text;

namespace Open.TokenProvider;

public interface IToken<TToken>
{
	TToken Value { get; }
	DateTimeOffset Timestamp { get; }
}
