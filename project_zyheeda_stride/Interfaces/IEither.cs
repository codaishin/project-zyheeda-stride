namespace ProjectZyheeda;

using System;

public interface IEither<TError, T> {
	TOut Switch<TOut>(Func<TError, TOut> error, Func<T, TOut> value);
}
