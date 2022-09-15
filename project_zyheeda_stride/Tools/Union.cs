namespace ProjectZyheeda;

using System;

public static class Union {
	public static IUnion<T1, T2> New<T1, T2>(T1 value) {
		return new Union.First<T1, T2, object> { value = value };
	}

	public static IUnion<T1, T2> New<T1, T2>(T2 value) {
		return new Union.Second<T1, T2, object> { value = value };
	}

	public static IUnion<T1, T2, T3> Expand<T1, T2, T3>(this IUnion<T1, T2> union) {
		return union.Switch(Union.New<T1, T2, T3>, Union.New<T1, T2, T3>);
	}

	public static IUnion<T1, T2, T3> New<T1, T2, T3>(T1 value) {
		return new Union.First<T1, T2, T3> { value = value };
	}

	public static IUnion<T1, T2, T3> New<T1, T2, T3>(T2 value) {
		return new Union.Second<T1, T2, T3> { value = value };
	}

	public static IUnion<T1, T2, T3> New<T1, T2, T3>(T3 value) {
		return new Union.Third<T1, T2, T3> { value = value };
	}

	private struct First<T1, T2, T3> : IUnion<T1, T2>, IUnion<T1, T2, T3> {
		public T1 value;

		public TOut Switch<TOut>(Func<T1, TOut> fst, Func<T2, TOut> snd) {
			return fst(this.value);
		}

		public TOut Switch<TOut>(Func<T1, TOut> fst, Func<T2, TOut> snd, Func<T3, TOut> trd) {
			return fst(this.value);
		}
	}

	private struct Second<T1, T2, T3> : IUnion<T1, T2>, IUnion<T1, T2, T3> {
		public T2 value;

		public TOut Switch<TOut>(Func<T1, TOut> fst, Func<T2, TOut> snd) {
			return snd(this.value);
		}

		public TOut Switch<TOut>(Func<T1, TOut> fst, Func<T2, TOut> snd, Func<T3, TOut> trd) {
			return snd(this.value);
		}
	}

	private struct Third<T1, T2, T3> : IUnion<T1, T2, T3> {
		public T3 value;

		public TOut Switch<TOut>(Func<T1, TOut> fst, Func<T2, TOut> snd, Func<T3, TOut> trd) {
			return trd(this.value);
		}
	}
}
