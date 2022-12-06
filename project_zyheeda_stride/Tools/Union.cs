namespace ProjectZyheeda;

using System;

public struct U<T1, T2> {
	private interface IU {
		TOut Switch<TOut>(Func<T1, TOut> fst, Func<T2, TOut> snd);
	}

	private struct Fst : IU {
		public T1 value;

		public TOut Switch<TOut>(Func<T1, TOut> fst, Func<T2, TOut> _) {
			return fst(this.value);
		}
	}

	private struct Snd : IU {
		public T2 value;

		public TOut Switch<TOut>(Func<T1, TOut> _, Func<T2, TOut> snd) {
			return snd(this.value);
		}
	}

	private readonly IU fstOrSnd;

	public U(T1 value) {
		this.fstOrSnd = new Fst { value = value };
	}

	public U(T2 value) {
		this.fstOrSnd = new Snd { value = value };
	}

	public TOut Switch<TOut>(Func<T1, TOut> fst, Func<T2, TOut> snd) {
		return this.fstOrSnd.Switch(fst, snd);
	}

	public static implicit operator U<T1, T2>(T1 value) {
		return new(value);
	}

	public static implicit operator U<T1, T2>(T2 value) {
		return new(value);
	}

	public static implicit operator U<T2, T1>(U<T1, T2> value) {
		return value.Switch<U<T2, T1>>(fst => new(fst), snd => new(snd));
	}
}

public struct U<T1, T2, T3> {
	private readonly U<T1, U<T2, T3>> fstSndOrTrd;

	public U(T1 value) {
		this.fstSndOrTrd = value;
	}

	public U(T2 value) {
		this.fstSndOrTrd = new U<T2, T3>(value);
	}

	public U(T3 value) {
		this.fstSndOrTrd = new U<T2, T3>(value);
	}

	public TOut Switch<TOut>(Func<T1, TOut> fst, Func<T2, TOut> snd, Func<T3, TOut> trd) {
		return this.fstSndOrTrd.Switch(fst, sndOrTrd => sndOrTrd.Switch(snd, trd));
	}

	public static implicit operator U<T1, T2, T3>(T1 value) {
		return new(value);
	}

	public static implicit operator U<T1, T2, T3>(T2 value) {
		return new(value);
	}

	public static implicit operator U<T1, T2, T3>(T3 value) {
		return new(value);
	}

	public static implicit operator U<T1, T2, T3>(U<T1, T2> value) {
		return value.Switch<U<T1, T2, T3>>(v => new(v), v => new(v));
	}

	public static implicit operator U<T1, T2, T3>(U<T1, T3> value) {
		return value.Switch<U<T1, T2, T3>>(v => new(v), v => new(v));
	}

	public static implicit operator U<T1, T2, T3>(U<T2, T1> value) {
		return value.Switch<U<T1, T2, T3>>(v => new(v), v => new(v));
	}

	public static implicit operator U<T1, T2, T3>(U<T2, T3> value) {
		return value.Switch<U<T1, T2, T3>>(v => new(v), v => new(v));
	}

	public static implicit operator U<T1, T2, T3>(U<T3, T2> value) {
		return value.Switch<U<T1, T2, T3>>(v => new(v), v => new(v));
	}

	public static implicit operator U<T1, T2, T3>(U<T3, T1> value) {
		return value.Switch<U<T1, T2, T3>>(v => new(v), v => new(v));
	}

	public static implicit operator U<T1, T3, T2>(U<T1, T2, T3> value) {
		return value.Switch<U<T1, T3, T2>>(v => new(v), v => new(v), v => new(v));
	}

	public static implicit operator U<T2, T1, T3>(U<T1, T2, T3> value) {
		return value.Switch<U<T2, T1, T3>>(v => new(v), v => new(v), v => new(v));
	}

	public static implicit operator U<T2, T3, T1>(U<T1, T2, T3> value) {
		return value.Switch<U<T2, T3, T1>>(v => new(v), v => new(v), v => new(v));
	}

	public static implicit operator U<T3, T1, T2>(U<T1, T2, T3> value) {
		return value.Switch<U<T3, T1, T2>>(v => new(v), v => new(v), v => new(v));
	}

	public static implicit operator U<T3, T2, T1>(U<T1, T2, T3> value) {
		return value.Switch<U<T3, T2, T1>>(v => new(v), v => new(v), v => new(v));
	}
}

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
