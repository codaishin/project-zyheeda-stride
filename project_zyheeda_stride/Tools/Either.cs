namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;

public static class Either {
	public static IEitherPartial<TErrorOrT> New<TErrorOrT>(TErrorOrT errorOrValue) {
		return new Either.Partial<TErrorOrT> { errorOrValue = errorOrValue };
	}

	public static IEither<TError, TOut> Map<TError, T, TOut>(
		this IEither<TError, T> either,
		Func<T, TOut> map
	) {
		return either.Switch(
			error => Either.New(error).WithNoValue<TOut>(),
			value => Either.New(map(value)).WithNoError<TError>()
		);
	}

	public static IEither<TErrorOut, T> MapError<TError, T, TErrorOut>(
		this IEither<TError, T> either,
		Func<TError, TErrorOut> map
	) {
		return either.Switch(
			error => Either.New(map(error)).WithNoValue<T>(),
			value => Either.New<T>(value).WithNoError<TErrorOut>()
		);
	}

	public static IEither<TError, TOut> FlatMap<TError, T, TOut>(
		this IEither<TError, T> either,
		Func<T, IEither<TError, TOut>> map
	) {
		return either.Switch(
			error => Either.New(error).WithNoValue<TOut>(),
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

	public static IEither<IEnumerable<TError>, TOut> Apply<TError, TIn, TOut>(
		this IEither<IEnumerable<TError>, Func<TIn, TOut>> apply,
		IEither<TError, TIn> either
	) {
		return apply.Switch(
			errors => either.Switch(
				error => Either.New(errors.Append(error)).WithNoValue<TOut>(),
				value => Either.New(errors).WithNoValue<TOut>()
			),
			func => either.Switch(
				error => Either.New(Either.FirstError(error)).WithNoValue<TOut>(),
				value => Either.New(func(value)).WithNoError<IEnumerable<TError>>()
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

	private struct Partial<TErrorOrT> : IEitherPartial<TErrorOrT> {
		public TErrorOrT errorOrValue;

		public IEither<TError, TErrorOrT> WithNoError<TError>() {
			return new Either.WithValue<TError, TErrorOrT> { value = this.errorOrValue };
		}

		public IEither<TErrorOrT, T> WithNoValue<T>() {
			return new Either.WithError<TErrorOrT, T> { error = this.errorOrValue };
		}
	}

	private struct WithValue<TError, T> : IEither<TError, T> {
		public T value;

		public TOut Switch<TOut>(
			System.Func<TError, TOut> error,
			System.Func<T, TOut> value
		) {
			return value(this.value);
		}
	}

	private struct WithError<TError, T> : IEither<TError, T> {
		public TError error;

		public TOut Switch<TOut>(
			System.Func<TError, TOut> error,
			System.Func<T, TOut> value
		) {
			return error(this.error);
		}
	}
}
