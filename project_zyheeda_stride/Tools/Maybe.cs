namespace ProjectZyheeda;

using System;

public static class Maybe {
	public static IMaybe<T> Some<T>(T value) {
		return new Maybe.WithValue<T>(value);
	}

	public static IMaybe<T> None<T>() {
		return new Maybe.WithNoValue<T>();
	}

	public static IMaybe<TOut> Map<TIn, TOut>(
		this IMaybe<TIn> maybe,
		Func<TIn, TOut> mapper
	) {
		var result = Maybe.None<TOut>();
		maybe.Match(v => result = Some(mapper(v)));
		return result;
	}

	public static IMaybe<TOut> Bind<TIn, TOut>(
		this IMaybe<TIn> maybe,
		Func<TIn, IMaybe<TOut>> mapper
	) {
		var result = Maybe.None<TOut>();
		maybe.Match(v => result = mapper(v));
		return result;
	}

	public static T UnpackOr<T>(this IMaybe<T> maybe, T fallback) {
		maybe.Match(v => fallback = v);
		return fallback;
	}

	private class WithValue<T> : IMaybe<T> {
		private readonly T value;

		public WithValue(T value) {
			this.value = value;
		}

		public void Match(Action<T> some, Action? none = null) {
			some(this.value);
		}
	}

	private class WithNoValue<T> : IMaybe<T> {
		public void Match(Action<T> some, Action? none = null) {
			none?.Invoke();
		}
	}
}
