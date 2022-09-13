namespace ProjectZyheeda;

using System;

public interface IUnion<T1, T2, T3> {
	TOut Switch<TOut>(
		Func<T1, TOut> fst,
		Func<T2, TOut> snd,
		Func<T3, TOut> trd
	);
}
