namespace ProjectZyheeda;

using System;
using System.Linq;

public abstract class BaseInputController<IInput> :
	ProjectZyheedaSyncScript
	where IInput :
		ProjectZyheeda.IInput,
		new() {

	public readonly IInput input = new();
	public Reference<IGetTarget> getTarget = new();
	public Reference<IBehavior> behavior = new();
	public Reference<IScheduler> scheduler = new();

	private void RunBehavior(InputAction action) {
		var runBehavior =
			(IGetTarget getTarget) =>
			(IBehavior behavior) =>
			(IScheduler scheduler) =>
			() => {
				Action<(Func<Coroutine>, Cancel)> deploy =
					action is InputAction.Run
						? scheduler.Run
						: scheduler.Enqueue;
				getTarget
					.GetTarget()
					.Switch(
						target => deploy(behavior.GetCoroutine(target)),
						() => { }
					);
			};

		runBehavior
			.ApplyWeak(this.getTarget.MaybeToEither(nameof(this.getTarget)))
			.ApplyWeak(this.behavior.MaybeToEither(nameof(this.behavior)))
			.ApplyWeak(this.scheduler.MaybeToEither(nameof(this.scheduler)))
			.Switch(
				missingFields => throw new MissingField(this, missingFields.ToArray()),
				run => run()
			);
	}

	public override void Update() {
		var action = this.input.GetAction(this.EssentialServices.inputManager);
		if (action is InputAction.None) {
			return;
		}
		this.RunBehavior(action);
	}
}

public class InputController : BaseInputController<Input> { }
