namespace ProjectZyheeda;

using System;

public static class Maybe {
	public static IMaybe<T> Some<T>(T value) {
		return new Maybe.WithValue<T>(value);
	}

	public static IMaybe<T> None<T>() {
		return new Maybe.WithNoValue<T>();
	}

	public static IMaybe<TOut> Map<TIn, TOut>(this IMaybe<TIn> maybe, Func<TIn, TOut> mapper) {
		return maybe.Switch(v => Some(mapper(v)), () => Maybe.None<TOut>());
	}

	public static IMaybe<TOut> FlatMap<TIn, TOut>(this IMaybe<TIn> maybe, Func<TIn, IMaybe<TOut>> mapper) {
		return maybe.Switch(v => mapper(v), () => Maybe.None<TOut>());
	}

	public static IMaybe<TIn> Flatten<TIn>(this IMaybe<IMaybe<TIn>> maybe) {
		return maybe.FlatMap(v => v);
	}

	public static T UnpackOr<T>(this IMaybe<T> maybe, T fallback) {
		return maybe.Switch(v => v, () => fallback);
	}

	public static void Switch<T>(this IMaybe<T> maybe, Action<T> some, Action none) {
		_ = maybe.Switch<byte>(
			some: v => { some(v); return 0; },
			none: () => { none(); return 0; }
		);
	}

	public static IMaybe<TOut> Apply<TIn, TOut>(this IMaybe<Func<TIn, TOut>> apply, IMaybe<TIn> maybe) {
		return apply.FlatMap(func => maybe.Map(func));
	}

	public static Either<TError, T> MaybeToEither<TError, T>(this IMaybe<T> maybe, TError error) {
		return maybe.Switch<Either<TError, T>>(
			some: v => v,
			none: () => error
		);
	}

	public static IMaybe<T> ToMaybe<T>(this T? value) where T : class {
		return value is not null ? Maybe.Some(value) : Maybe.None<T>();
	}

	public static IMaybe<T> ToMaybe<T>(this T? value) where T : struct {
		return value.HasValue ? Maybe.Some(value.Value) : Maybe.None<T>();
	}

	private readonly struct WithValue<T> : IMaybe<T> {
		private readonly T value;

		public WithValue(T value) {
			this.value = value;
		}

		public TReturn Switch<TReturn>(Func<T, TReturn> some, Func<TReturn> none) {
			return some(this.value);
		}
	}

	private readonly struct WithNoValue<T> : IMaybe<T> {
		public TReturn Switch<TReturn>(Func<T, TReturn> some, Func<TReturn> none) {
			return none();
		}
	}
}
