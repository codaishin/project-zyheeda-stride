namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public abstract class BaseInputController<TInputStream> :
	ProjectZyheedaAsyncScript
	where TInputStream :
		IInputStream {

	public readonly TInputStream input;
	public Reference<IGetTarget> getTarget = new();
	public Reference<IBehavior> behavior = new();
	public Reference<IScheduler> scheduler = new();

	public BaseInputController(TInputStream input) {
		this.input = input;
	}

	private void LogErrors(IEnumerable<string> errors) {
		foreach (var error in errors) {
			this.EssentialServices.systemMessage.Log(new SystemStr(error));
		}
	}

	private Action<InputAction>? GetRunBehavior() {
		var runBehavior =
			(IGetTarget getTarget) =>
			(IBehavior behavior) =>
			(IScheduler scheduler) =>
			(InputAction action) => {
				Action<(Func<Coroutine>, Cancel)> deploy =
					action is InputAction.Run
						? scheduler.Run
						: scheduler.Enqueue;
				var m = getTarget.GetTarget();
				m.Switch(
						target => deploy(behavior.GetCoroutine(target)),
						() => { }
					);
			};

		return runBehavior
			.ApplyWeak(this.getTarget.MaybeToEither(this.MissingField(nameof(this.getTarget))))
			.ApplyWeak(this.behavior.MaybeToEither(this.MissingField(nameof(this.behavior))))
			.ApplyWeak(this.scheduler.MaybeToEither(this.MissingField(nameof(this.scheduler))))
			.Switch<Action<InputAction>?>(
				errors => {
					this.LogErrors(errors);
					return null;
				},
				run => run
			);
	}

	public override async Task Execute() {
		var runBehavior = this.GetRunBehavior();
		if (runBehavior is null) {
			return;
		}

		this.EssentialServices.inputDispatcher.Add(this.input);

		_ = this.CancellationToken.Register(() => {
			this.EssentialServices.inputDispatcher.Remove(this.input);
		});

		while (this.Game.IsRunning) {
			var action = await this.input.NewAction();
			runBehavior(action);
		}
	}
}

public class InputController : BaseInputController<InputStream> {
	public InputController() : base(new InputStream()) { }
}
