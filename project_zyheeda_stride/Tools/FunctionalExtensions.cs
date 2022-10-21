namespace ProjectZyheeda;

using System;

public static class FunctionalExtensions {
	public static TOut Apply<TIn, TOut>(this TIn value, Func<TIn, TOut> parse) {
		return parse(value);
	}
}
