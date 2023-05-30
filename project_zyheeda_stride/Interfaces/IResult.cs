namespace ProjectZyheeda;

using System;

public interface IResult<TError> {
	TOut Switch<TOut>(Func<TError, TOut> error, Func<TOut> ok);
}

public interface IResult<TError, T> {
	TOut Switch<TOut>(Func<TError, TOut> error, Func<T, TOut> value);
}
