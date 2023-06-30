namespace ProjectZyheeda;

using System;
using System.Linq;


public readonly struct Result : IResult<(SystemErrors system, PlayerErrors player)> {
	public readonly struct ResultErrors : IResult<(SystemErrors system, PlayerErrors player)> {
		public readonly (SystemErrors system, PlayerErrors player) errors;

		public ResultErrors((SystemErrors system, PlayerErrors player) errors) {
			this.errors = errors;
		}

		public TOut Switch<TOut>(Func<(SystemErrors system, PlayerErrors player), TOut> error, Func<TOut> ok) {
			return error(this.errors);
		}
	}

	private readonly struct ResultOk : IResult<(SystemErrors system, PlayerErrors player)> {
		public TOut Switch<TOut>(Func<(SystemErrors system, PlayerErrors player), TOut> error, Func<TOut> ok) {
			return ok();
		}
	}

	private readonly IResult<(SystemErrors system, PlayerErrors player)> errorsOrOk;

	public Result() {
		this.errorsOrOk = new ResultOk();
	}

	private Result((SystemErrors system, PlayerErrors player) errors) {
		this.errorsOrOk = new Result.ResultErrors(errors);
	}

	public TOut Switch<TOut>(Func<(SystemErrors system, PlayerErrors player), TOut> error, Func<TOut> ok) {
		return this.errorsOrOk.Switch(
			errors => error(errors),
			() => ok()
		);
	}

	public override string ToString() {
		var inner = this.Switch(
			errors => {
				var systemErrors = errors.system.Select(e => (string)e);
				var playerErrors = errors.player.Select(e => (string)e);
				return $"SystemErrors({string.Join(", ", systemErrors)}), PlayerErrors({string.Join(", ", playerErrors)})";
			},
			() => "Ok"
		);
		return $"{nameof(Result)}({inner})";
	}

	public static Result Ok() {
		return new Result();
	}

	public static Result<T> Ok<T>(T value) {
		return new Result<T>(value);
	}

	public static Result.ResultErrors PlayerError(string error) {
		return Result.Error((PlayerError)error);
	}

	public static Result.ResultErrors SystemError(string error) {
		return Result.Error((SystemError)error);
	}

	public static Result.ResultErrors Error(PlayerError error) {
		var errors = (Enumerable.Empty<SystemError>(), new PlayerError[] { error });
		return new Result.ResultErrors(errors);
	}

	public static Result.ResultErrors Error(SystemError error) {
		var errors = (new SystemError[] { error }, Enumerable.Empty<PlayerError>());
		return new Result.ResultErrors(errors);
	}

	public static Result.ResultErrors Errors((SystemErrors system, PlayerErrors player) errors) {
		return new Result.ResultErrors(errors);
	}

	public static implicit operator Result(Result.ResultErrors result) {
		return result.Switch(
			error => new Result(error),
			() => new Result()
		);
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

	public static implicit operator Result<T>(Result.ResultErrors result) {
		return new Result<T>(result.errors);
	}

	public static implicit operator Result<T>(T value) {
		return new Result<T>(value);
	}

	public static implicit operator Result<T>(SystemError error) {
		return new Result<T>((new SystemError[] { error }, Enumerable.Empty<PlayerError>()));
	}

	public static implicit operator Result<T>(PlayerError error) {
		return new Result<T>((Enumerable.Empty<SystemError>(), new PlayerError[] { error }));
	}

	public static implicit operator Result(Result<T> result) {
		return result.Switch(
			errors => Result.Errors(errors),
			_ => Result.Ok()
		);
	}
}
