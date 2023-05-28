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

public readonly struct Result<T> : IResult<(SystemErrors system, PlayerErrors player), T> {
	private struct Value : IResult<(SystemErrors system, PlayerErrors player), T> {
		public T value;
		public TOut Switch<TOut>(Func<(SystemErrors system, PlayerErrors player), TOut> fst, Func<T, TOut> snd) {
			return snd(this.value);
		}
	}

	private struct Errors : IResult<(SystemErrors system, PlayerErrors player), T> {
		public (SystemErrors system, PlayerErrors player) errors;
		public TOut Switch<TOut>(Func<(SystemErrors system, PlayerErrors player), TOut> fst, Func<T, TOut> snd) {
			return fst(this.errors);
		}
	}

	private readonly IResult<(SystemErrors system, PlayerErrors player), T> errorsOrValue;

	public Result(T value) {
		this.errorsOrValue = new Result<T>.Value { value = value };
	}

	public Result((SystemErrors system, PlayerErrors player) errors) {
		this.errorsOrValue = new Result<T>.Errors { errors = errors };
	}

	public TOut Switch<TOut>(Func<(SystemErrors system, PlayerErrors player), TOut> error, Func<T, TOut> value) {
		return this.errorsOrValue.Switch(error, value);
	}

	public static implicit operator Result<T>(Result result) {
		return new Result<T>(result.errors);
	}
}
