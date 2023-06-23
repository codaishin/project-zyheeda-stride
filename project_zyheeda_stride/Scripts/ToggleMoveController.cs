namespace ProjectZyheeda;

using System;

public class ToggleMoveController : ProjectZyheedaStartupScript, IBehavior {
	public float toggleSpeed;
	public string toggleAnimationKey = "";
	public MoveController? target;

	private Result ToggleTargetMove(IAnimatedMove move) {
		var storeOldValues =
			(OldSpeed oldSpeed) =>
			(OldAnimationKey oldAnimationKey) => {
				this.toggleSpeed = oldSpeed;
				this.toggleAnimationKey = oldAnimationKey;
				return Result.Ok();
			};

		return storeOldValues
			.Apply(move.SetSpeed(this.toggleSpeed))
			.Apply(move.SetAnimation(this.toggleAnimationKey));
	}

	private Result ToggleTarget(MoveController target) {
		return target.move
			.OkOrSystemError(target.MissingField(nameof(target.move)))
			.FlatMap(this.ToggleTargetMove);
	}

	private Coroutine Toggle() {
		yield return this.target
			.OkOrSystemError(this.MissingField(nameof(this.target)))
			.FlatMap(this.ToggleTarget)
			.Map(() => (IWait)new NoWait());
	}

	private Result DoNothing() {
		return Result.Ok();
	}

	public Result<(Func<Coroutine> coroutine, Cancel cancel)> GetExecution() {
		return (this.Toggle, this.DoNothing);
	}
}