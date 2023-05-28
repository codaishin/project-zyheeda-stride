namespace ProjectZyheeda;

using System;
using System.Linq;

public static class ResultExtensions {
	public static void Switch<TError, T>(this IResult<TError, T> union, Action<TError> fst, Action<T> snd) {
		_ = union.Switch<byte>(
			v => { fst(v); return default; },
			v => { snd(v); return default; }
		);
	}

	public static string UnpackToString<T>(this Result<T> union) {
		var inner = union.Switch(
			errors => {
				var systemErrors = errors.system.Select(e => (string)e);
				var playerErrors = errors.player.Select(e => (string)e);
				return $"SystemErrors({string.Join(", ", systemErrors)}), PlayerErrors({string.Join(", ", playerErrors)})";
			},
			value => $"{value}"
		);
		return $"{nameof(Result<T>)}<{typeof(T).Name}>({inner})";
	}

	public static Result<TOut> Map<T, TOut>(this Result<T> result, Func<T, TOut> map) {
		return result.Switch(
			errors => Result.Errors(errors),
			value => Result.Ok(map(value))
		);
	}

	public static Result<TOut> FlatMap<T, TOut>(this Result<T> result, Func<T, Result<TOut>> map) {
		return result.Switch(
			errors => Result.Errors(errors),
			value => map(value)
		);
	}

	public static Result<T> Flatten<T>(this Result<Result<T>> result) {
		return result.FlatMap(v => v);
	}

	public static T UnpackOr<T>(this Result<T> result, T fallback) {
		return result.Switch(
			_ => fallback,
			value => value
		);
	}

	public static Result<TOut> Apply<TIn, TOut>(this Result<Func<TIn, TOut>> apply, Result<TIn> result) {
		return apply.FlatMap(func => result.Map(v => func(v)));
	}

	public static Result<TOut> Apply<TIn, TOut>(this Func<TIn, TOut> apply, Result<TIn> result) {
		return result.Switch(
			error => Result.Errors(error),
			value => Result.Ok(apply(value))
		);
	}

	public static Result<TOut> ApplyWeak<TIn, TOut>(this Result<Func<TIn, TOut>> apply, Result<TIn> result) {
		return apply.Switch(
			errors => result.Switch(
				newErrors => Result.Errors((errors.system.Concat(newErrors.system), errors.player.Concat(newErrors.player))),
				value => Result.Errors(errors)
			),
			func => result.Switch<Result<TOut>>(
				newErrors => Result.Errors(newErrors),
				value => Result.Ok(func(value))
			)
		);
	}

	public static Result<TOut> ApplyWeak<TIn, TOut>(this Func<TIn, TOut> apply, Result<TIn> result) {
		return result.Switch<Result<TOut>>(
			errors => Result.Errors(errors),
			value => Result.Ok(apply(value))
		);
	}

	public static IMaybe<T> ToMaybe<T>(this Result<T> result) {
		return result.Switch(
			_ => Maybe.None<T>(),
			value => Maybe.Some(value)
		);
	}

	public static Result<T> OkOrPlayerError<T>(this T? value, string error) where T : class {
		return value is null
			? Result.PlayerError(error)
			: Result.Ok(value);
	}

	public static Result<T> OkOrPlayerError<T>(this T? value, string error) where T : struct {
		return value is null
			? Result.PlayerError(error)
			: Result.Ok(value.Value);
	}

	public static Result<T> OkOrSystemError<T>(this T? value, string error) where T : class {
		return value is null
			? Result.SystemError(error)
			: Result.Ok(value);
	}

	public static Result<T> OkOrSystemError<T>(this T? value, string error) where T : struct {
		return value is null
			? Result.SystemError(error)
			: Result.Ok(value.Value);
	}
}