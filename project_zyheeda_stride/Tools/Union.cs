namespace ProjectZyheeda;

using System;

public static class Union {
	public static IUnion<T1, T2, T3> New<T1, T2, T3>(T1 value) {
		return new Union.FirstOfThree<T1, T2, T3> { value = value };
	}

	public static IUnion<T1, T2, T3> New<T1, T2, T3>(T2 value) {
		return new Union.SecondOfThree<T1, T2, T3> { value = value };
	}

	public static IUnion<T1, T2, T3> New<T1, T2, T3>(T3 value) {
		return new Union.ThirdOfThree<T1, T2, T3> { value = value };
	}

	private struct FirstOfThree<T1, T2, T3> : IUnion<T1, T2, T3> {
		public T1 value;

		public TOut Switch<TOut>(Func<T1, TOut> fst, Func<T2, TOut> snd, Func<T3, TOut> trd) {
			return fst(this.value);
		}
	}

	private struct SecondOfThree<T1, T2, T3> : IUnion<T1, T2, T3> {
		public T2 value;

		public TOut Switch<TOut>(Func<T1, TOut> fst, Func<T2, TOut> snd, Func<T3, TOut> trd) {
			return snd(this.value);
		}
	}

	private struct ThirdOfThree<T1, T2, T3> : IUnion<T1, T2, T3> {
		public T3 value;

		public TOut Switch<TOut>(Func<T1, TOut> fst, Func<T2, TOut> snd, Func<T3, TOut> trd) {
			return trd(this.value);
		}
	}
}
