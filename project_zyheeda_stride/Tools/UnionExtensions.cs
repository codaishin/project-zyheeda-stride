namespace ProjectZyheeda;

using System;

public static class Union2Extensions {
	public static void Switch<T1, T2>(this IUnion<T1, T2> union, Action<T1> fst, Action<T2> snd) {
		_ = union.Switch<byte>(
			v => { fst(v); return default; },
			v => { snd(v); return default; }
		);
	}

	public static string UnionToString<T1, T2>(this IUnion<T1, T2> union) {
		var inner = union.Switch(
			v => $"fst: {v}",
			v => $"snd: {v}"
		);
		return $"{nameof(IUnion<T1, T2>)}<{typeof(T1).Name}, {typeof(T2).Name}>({inner})";
	}
}

public static class Union3Extensions {
	public static void Switch<T1, T2, T3>(this IUnion<T1, T2, T3> union, Action<T1> fst, Action<T2> snd, Action<T3> trd) {
		_ = union.Switch<byte>(
			v => { fst(v); return default; },
			v => { snd(v); return default; },
			v => { trd(v); return default; }
		);
	}

	public static string UnionToString<T1, T2, T3>(this IUnion<T1, T2, T3> union) {
		var inner = union.Switch(
			v => $"fst: {v}",
			v => $"snd: {v}",
			v => $"trd: {v}"
		);
		return $"{nameof(IUnion<T1, T2>)}<{typeof(T1).Name}, {typeof(T2).Name}, {typeof(T3).Name}>({inner})";
	}
}
