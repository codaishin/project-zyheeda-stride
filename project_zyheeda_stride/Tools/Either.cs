namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;

public struct Either<TError, T> : IEither<TError, T> {
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

public static class EitherTools {
	public static IEither<TError, TOut> Map<TError, T, TOut>(
		this IEither<TError, T> either,
		Func<T, TOut> map
	) {
		return either.Switch<Either<TError, TOut>>(
			error => error,
			value => map(value)
		);
	}

	public static IEither<TErrorOut, T> MapError<TError, T, TErrorOut>(
		this IEither<TError, T> either,
		Func<TError, TErrorOut> map
	) {
		return either.Switch<Either<TErrorOut, T>>(
			error => map(error),
			value => value
		);
	}

	public static IEither<TError, TOut> FlatMap<TError, T, TOut>(
		this IEither<TError, T> either,
		Func<T, IEither<TError, TOut>> map
	) {
		return either.Switch(
			error => (Either<TError, TOut>)error,
			value => map(value)
		);
	}

	public static IEither<TError, T> Flatten<TError, T>(
		this IEither<TError, IEither<TError, T>> either
	) {
		return either.FlatMap(v => v);
	}

	public static T UnpackOr<TError, T>(this IEither<TError, T> maybe, T fallback) {
		return maybe.Switch(
			_ => fallback,
			value => value
		);
	}

	public static TError UnpackErrorOr<TError, T>(
		this IEither<TError, T> maybe,
		TError fallback
	) {
		return maybe.Switch(
			error => error,
			_ => fallback
		);
	}

	public static void Switch<TError, T>(
		this IEither<TError, T> either,
		Action<TError> error,
		Action<T> value
	) {
		var action = either.Switch<Action>(
			e => () => error(e),
			v => () => value(v)
		);
		action();
	}

	public static IEither<TError, TOut> Apply<TError, TIn, TOut>(
		this IEither<TError, Func<TIn, TOut>> apply,
		IEither<TError, TIn> either
	) {
		return apply.FlatMap(func => either.Map(v => func(v)));
	}

	public static IEither<IEnumerable<TError>, TOut> ApplyWeak<TError, TIn, TOut>(
		this IEither<IEnumerable<TError>, Func<TIn, TOut>> apply,
		IEither<TError, TIn> either
	) {
		return apply.Switch(
			errors => either.Switch<Either<IEnumerable<TError>, TOut>>(
				error => new Either<IEnumerable<TError>, TOut>(errors.Append(error)),
				value => new Either<IEnumerable<TError>, TOut>(errors)
			),
			func => either.Switch<Either<IEnumerable<TError>, TOut>>(
				error => new Either<IEnumerable<TError>, TOut>(EitherTools.FirstError(error)),
				value => func(value)
			)
		);
	}

	public static IMaybe<T> ToMaybe<TError, T>(this IEither<TError, T> either) {
		return either.Switch(
			_ => Maybe.None<T>(),
			value => Maybe.Some(value)
		);
	}

	private static IEnumerable<TError> FirstError<TError>(TError error) {
		yield return error;
	}
}
