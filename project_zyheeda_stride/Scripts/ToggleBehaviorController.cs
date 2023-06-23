namespace ProjectZyheeda;

using System;

public class ToggleBehaviorController : ProjectZyheedaStartupScript, IToggle, IBehavior {
	public IBehaviorEditor? behaviorA;
	public IBehaviorEditor? behaviorB;
	private bool toggled;

	private Coroutine Toggle() {
		this.toggled = !this.toggled;
		yield break;
	}

	private static Result DoNothing() {
		return Result.Ok();
	}

	public Result<(Func<Coroutine> coroutine, Cancel cancel)> GetToggle() {
		return (this.Toggle, ToggleBehaviorController.DoNothing);
	}

	public Result<(Func<Coroutine> coroutine, Cancel cancel)> GetExecution() {
		return this.toggled
			? this.behaviorB
				.OkOrSystemError(this.MissingField(nameof(this.behaviorB)))
				.FlatMap(b => b.GetExecution())
			: this.behaviorA
				.OkOrSystemError(this.MissingField(nameof(this.behaviorA)))
				.FlatMap(b => b.GetExecution());
	}
}
