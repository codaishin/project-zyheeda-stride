namespace ProjectZyheeda;

using System;

public interface IResult<TError, T> {
	TOut Switch<TOut>(Func<TError, TOut> error, Func<T, TOut> value);
}
