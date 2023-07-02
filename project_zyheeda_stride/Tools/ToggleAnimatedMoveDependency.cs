namespace ProjectZyheeda;

using Stride.Core;

[DataContract]
public class ToggleAnimatedMoveDependency : IBehaviorEditor {
	public float toggleSpeed;
	public string toggleAnimationKey = "";
	public CharacterDependencies? target;

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

	private Result ToggleTarget(CharacterDependencies target) {
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

	public Result<(Coroutine coroutine, Cancel cancel)> GetExecution() {
		return (this.Toggle(), this.DoNothing);
	}
}
