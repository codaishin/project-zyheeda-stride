namespace ProjectZyheeda;

using System;
using System.Linq;


public readonly struct Result {
	public readonly (SystemErrors system, PlayerErrors player) errors;

	private Result((SystemErrors system, PlayerErrors player) errors) {
		this.errors = errors;
	}

	public static Result<T> Ok<T>(T value) {
		return new Result<T>(value);
	}

	public static Result PlayerError(string error) {
		return Result.Error((PlayerError)error);
	}

	public static Result SystemError(string error) {
		return Result.Error((SystemError)error);
	}

	public static Result Error(PlayerError error) {
		var errors = (Enumerable.Empty<SystemError>(), new PlayerError[] { error });
		return new Result(errors);
	}

	public static Result Error(SystemError error) {
		var errors = (new SystemError[] { error }, Enumerable.Empty<PlayerError>());
		return new Result(errors);
	}

	public static Result Errors((SystemErrors system, PlayerErrors player) errors) {
		return new Result(errors);
	}
}

public readonly struct Result<T> : IUnion<(SystemErrors system, PlayerErrors player), T> {
	private readonly U<(SystemErrors system, PlayerErrors player), T> errorsOrValue;

	public Result(T value) {
		this.errorsOrValue = value;
	}

	public Result((SystemErrors system, PlayerErrors player) errors) {
		this.errorsOrValue = errors;
	}

	public TOut Switch<TOut>(Func<(SystemErrors system, PlayerErrors player), TOut> errors, Func<T, TOut> value) {
		return this.errorsOrValue.Switch(errors, value);
	}

	public static implicit operator Result<T>(Result result) {
		return new Result<T>(result.errors);
	}
}

public static class ResultExtensions {
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
				newErrors => Result.Errors((errors.Item1.Concat(newErrors.Item1), errors.Item2.Concat(newErrors.Item2))),
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
