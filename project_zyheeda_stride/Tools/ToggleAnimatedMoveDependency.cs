namespace ProjectZyheeda;

using Stride.Core;

[DataContract]
public class ToggleAnimatedMoveDependency : IBehaviorEditor {
	public ISpeedEditor? toggleSpeed;
	public string toggleAnimationKey = "";
	public CharacterDependencies? target;

	private Result ToggleTargetMove(IAnimatedMove move, ISpeedEditor newSpeed) {
		var storeOldValues =
			(OldSpeed oldSpeed) =>
			(OldAnimationKey oldAnimationKey) => {
				this.toggleSpeed = oldSpeed;
				this.toggleAnimationKey = oldAnimationKey;
				return Result.Ok();
			};

		return storeOldValues
			.Apply(move.SetSpeed(newSpeed))
			.Apply(move.SetAnimation(this.toggleAnimationKey));
	}

	private Result ToggleTarget(CharacterDependencies target) {
		var toggle =
			(IAnimatedMove move) =>
			(ISpeedEditor newSpeed) =>
				this.ToggleTargetMove(move, newSpeed);

		var move = target.move.OkOrSystemError(target.MissingField(nameof(target.move)));
		var newSpeed = this.toggleSpeed.OkOrSystemError(this.MissingField(nameof(this.toggleSpeed)));
		return toggle
			.Apply(move)
			.Apply(newSpeed)
			.Flatten();
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

	public Result<(Coroutine coroutine, Cancel cancel)> GetExecution() {
		return (this.Toggle(), this.DoNothing);
	}
}
