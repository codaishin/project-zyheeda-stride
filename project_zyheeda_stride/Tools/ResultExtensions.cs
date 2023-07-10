namespace ProjectZyheeda;

using System;
using System.Linq;
using System.Threading.Tasks;

public static class ResultExtensions {
	public static void Switch<TError>(this IResult<TError> result, Action<TError> error, Action ok) {
		_ = result.Switch<byte>(
			v => { error(v); return default; },
			() => { ok(); return default; }
		);
	}
}

public static class GenericResultExtensions {
	public static void Switch<TError, T>(this IResult<TError, T> result, Action<TError> error, Action<T> ok) {
		_ = result.Switch<byte>(
			v => { error(v); return default; },
			v => { ok(v); return default; }
		);
	}

	public static string UnpackToString<T>(this Result<T> result) {
		var inner = result.Switch(
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

	public static Result<TOut> Map<TOut>(this Result result, Func<TOut> map) {
		return result.Switch(
			errors => Result.Errors(errors),
			() => Result.Ok(map())
		);
	}

	public static Result Map(this Result result, Action map) {
		return result.Switch(
			errors => Result.Errors(errors),
			() => {
				map();
				return Result.Ok();
			}
		);
	}

	public static Result<TOut> FlatMap<T, TOut>(this Result<T> result, Func<T, Result<TOut>> map) {
		return result.Switch(
			errors => Result.Errors(errors),
			value => map(value)
		);
	}

	public static Result FlatMap<T>(this Result<T> result, Func<T, Result> map) {
		return result.Switch(
			errors => Result.Errors(errors),
			value => map(value)
		);
	}

	public static Result FlatMap(this Result result, Func<Result> map) {
		return result.Switch(
			errors => map().Switch(
				mapErrors => Result.Errors((
					errors.system.Concat(mapErrors.system),
					errors.player.Concat(mapErrors.player)
				)),
				() => Result.Errors(errors)
			),
			() => map()
		);
	}

	public static Result<T> FlatMap<T>(this Result result, Func<Result<T>> map) {
		return result.Switch(
			errors => map().Switch(
				mapErrors => Result.Errors((
					errors.system.Concat(mapErrors.system),
					errors.player.Concat(mapErrors.player)
				)),
				_ => Result.Errors(errors)
			),
			() => map()
		);
	}

	public static Result<T> Flatten<T>(this Result<Result<T>> result) {
		return result.FlatMap(v => v);
	}

	public static Result Flatten(this Result<Result> result) {
		return result.FlatMap(v => v);
	}

	public static Task<Result> Flatten(this Result<Task<Result>> result) {
		return result.Switch(
			errors => Task.FromResult<Result>(Result.Errors(errors)),
			async result => await result
		);
	}

	public static T UnpackOr<T>(this Result<T> result, T fallback) {
		return result.Switch(
			_ => fallback,
			value => value
		);
	}

	public static T? UnpackOrDefault<T>(this Result<T> target) {
		return target.Switch<T?>(
			_ => default,
			value => value
		);
	}

	public static Result<TOut> Apply<TIn, TOut>(this Result<Func<TIn, TOut>> apply, Result<TIn> result) {
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

	public static Result<TOut> Apply<TIn, TOut>(this Func<TIn, TOut> apply, Result<TIn> result) {
		return result.Switch(
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
