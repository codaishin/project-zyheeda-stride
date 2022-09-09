namespace ProjectZyheeda;

using System;

public static class Either {

	public class ValueGetter<TError> {
		public IEither<TError, T> WithValue<T>(T value) {
			return new Either.WithValue<TError, T>(value);
		}
	}

	public class ErrorGetter<T> {
		public IEither<TError, T> WithError<TError>(TError error) {
			return new Either.WithError<TError, T>(error);
		}
	}

	public static ValueGetter<TError> NoError<TError>() {
		return new ValueGetter<TError>();
	}

	public static ErrorGetter<T> NoValue<T>() {
		return new ErrorGetter<T>();
	}

	public static IEither<TError, TOut> Map<TError, T, TOut>(
		this IEither<TError, T> either,
		Func<T, TOut> map
	) {
		return either.Switch(
			error: e => Either.NoValue<TOut>().WithError(e),
			value: v => Either.NoError<TError>().WithValue(map(v))
		);
	}

	public static IEither<TError, TOut> FlatMap<TError, T, TOut>(
		this IEither<TError, T> either,
		Func<T, IEither<TError, TOut>> map
	) {
		return either.Switch(
			error: e => Either.NoValue<TOut>().WithError(e),
			value: v => map(v)
		);
	}

	public static T UnpackOr<TError, T>(this IEither<TError, T> maybe, T fallback) {
		return maybe.Switch(
			error: _ => fallback,
			value: v => v
		);
	}

	public static void Switch<TError, T>(
		this IEither<TError, T> either,
		Action<TError> error,
		Action<T> value
	) {
		var action = either.Switch<Action>(
			error: e => () => error(e),
			value: v => () => value(v)
		);
		action();
	}

	public static IEither<TError, T> FlatMap<TError, T>(
		this IEither<TError, IEither<TError, T>> either
	) {
		return either.FlatMap(v => v);
	}

	public static IEither<TError, TOut> Apply<TError, TIn, TOut>(
		this IEither<TError, Func<TIn, TOut>> apply,
		IEither<TError, TIn> either
	) {
		return apply.FlatMap(func => either.Map(v => func(v)));
	}

	private class WithValue<TError, T> : IEither<TError, T> {
		private readonly T value;

		public WithValue(T value) {
			this.value = value;
		}

		public TOut Switch<TOut>(
			System.Func<TError, TOut> error,
			System.Func<T, TOut> value
		) {
			return value(this.value);
		}
	}

	private class WithError<TError, T> : IEither<TError, T> {
		private readonly TError value;

		public WithError(TError value) {
			this.value = value;
		}

		public TOut Switch<TOut>(
			System.Func<TError, TOut> error,
			System.Func<T, TOut> value
		) {
			return error(this.value);
		}
	}
}
