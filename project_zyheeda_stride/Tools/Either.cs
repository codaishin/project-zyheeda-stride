namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;

public struct Either<TError, T> {
	private readonly U<TError, T> errorOrValue;

	public Either(T value) {
		this.errorOrValue = value;
	}

	public Either(TError error) {
		this.errorOrValue = error;
	}

	public TOut Switch<TOut>(Func<TError, TOut> error, Func<T, TOut> value) {
		return this.errorOrValue.Switch(error, value);
	}

	public static implicit operator Either<TError, T>(T value) {
		return new Either<TError, T>(value);
	}

	public static implicit operator Either<TError, T>(TError error) {
		return new Either<TError, T>(error);
	}
}

public static class Either {
	public static Either<TError, TOut> Map<TError, T, TOut>(
		this Either<TError, T> either,
		Func<T, TOut> map
	) {
		return either.Switch<Either<TError, TOut>>(
			error => error,
			value => map(value)
		);
	}

	public static Either<TErrorOut, T> MapError<TError, T, TErrorOut>(
		this Either<TError, T> either,
		Func<TError, TErrorOut> map
	) {
		return either.Switch<Either<TErrorOut, T>>(
			error => map(error),
			value => value
		);
	}

	public static Either<TError, TOut> FlatMap<TError, T, TOut>(
		this Either<TError, T> either,
		Func<T, Either<TError, TOut>> map
	) {
		return either.Switch(
			error => (Either<TError, TOut>)error,
			value => map(value)
		);
	}

	public static Either<TError, T> Flatten<TError, T>(
		this Either<TError, Either<TError, T>> either
	) {
		return either.FlatMap(v => v);
	}

	public static T UnpackOr<TError, T>(this Either<TError, T> maybe, T fallback) {
		return maybe.Switch(
			_ => fallback,
			value => value
		);
	}

	public static TError UnpackErrorOr<TError, T>(
		this Either<TError, T> maybe,
		TError fallback
	) {
		return maybe.Switch(
			error => error,
			_ => fallback
		);
	}

	public static void Switch<TError, T>(
		this Either<TError, T> either,
		Action<TError> error,
		Action<T> value
	) {
		var action = either.Switch<Action>(
			e => () => error(e),
			v => () => value(v)
		);
		action();
	}

	public static Either<TError, TOut> Apply<TError, TIn, TOut>(
		this Either<TError, Func<TIn, TOut>> apply,
		Either<TError, TIn> either
	) {
		return apply.FlatMap(func => either.Map(v => func(v)));
	}

	public static Either<IEnumerable<TError>, TOut> ApplyWeak<TError, TIn, TOut>(
		this Either<IEnumerable<TError>, Func<TIn, TOut>> apply,
		Either<TError, TIn> either
	) {
		return apply.Switch(
			errors => either.Switch(
				error => new Either<IEnumerable<TError>, TOut>(errors.Append(error)),
				value => new Either<IEnumerable<TError>, TOut>(errors)
			),
			func => either.Switch<Either<IEnumerable<TError>, TOut>>(
				error => new Either<IEnumerable<TError>, TOut>(Either.FirstError(error)),
				value => func(value)
			)
		);
	}

	public static IMaybe<T> ToMaybe<TError, T>(this Either<TError, T> either) {
		return either.Switch(
			_ => Maybe.None<T>(),
			value => Maybe.Some(value)
		);
	}

	public static Either<TError, T> ToEither<TError, T>(this T? value, TError error)
		where T : class {
		return value != null
			? new Either<TError, T>(value)
			: new Either<TError, T>(error);
	}

	public static Either<TError, T> ToEither<TError, T>(this T? value, TError error)
		where T : struct {
		return value.HasValue
			? new Either<TError, T>(value.Value)
			: new Either<TError, T>(error);
	}

	private static IEnumerable<TError> FirstError<TError>(TError error) {
		yield return error;
	}
}
