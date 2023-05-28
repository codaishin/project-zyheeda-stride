namespace ProjectZyheeda;

using System;
using System.Linq;
using System.Threading.Tasks;

public class InputController : ProjectZyheedaAsyncScript {

	public IInputStream? input;
	public IMaybe<IGetTarget>? getTarget;
	public IMaybe<IBehavior>? behavior;
	public IMaybe<IScheduler>? scheduler;

	private Action<InputAction> RunBehavior(IGetTarget getTarget, IBehavior behavior, IScheduler scheduler) {
		return action => {
			Action<(Func<Coroutine>, Cancel)> runOrEnqueue =
				action is InputAction.Run
					? scheduler.Run
					: scheduler.Enqueue;
			getTarget
				.GetTarget()
				.Switch(
					errors => {
						this.EssentialServices.systemMessage.Log(errors.system.ToArray());
						this.EssentialServices.playerMessage.Log(errors.player.ToArray());
					},
					getTarget => runOrEnqueue(behavior.GetCoroutine(getTarget))
				);
		};
	}

	private (IInputStream, Action<InputAction>)? GetInputAndRun() {
		var getInputAndRun =
			(IGetTarget getTarget) =>
			(IBehavior behavior) =>
			(IScheduler scheduler) =>
			(IInputStream input) => (input, this.RunBehavior(getTarget, behavior, scheduler));

		return getInputAndRun
			.ApplyWeak(this.getTarget.ToMaybe().Flatten().ToOkOrSystemError(this.MissingField(nameof(this.getTarget))))
			.ApplyWeak(this.behavior.ToMaybe().Flatten().ToOkOrSystemError(this.MissingField(nameof(this.behavior))))
			.ApplyWeak(this.scheduler.ToMaybe().Flatten().ToOkOrSystemError(this.MissingField(nameof(this.scheduler))))
			.ApplyWeak(this.input.ToMaybe().ToOkOrSystemError(this.MissingField(nameof(this.input))))
			.Switch<(IInputStream, Action<InputAction>)?>(
				errors => {
					this.EssentialServices.systemMessage.Log(errors.system.ToArray());
					return null;
				},
				run => run
			);
	}

	public override async Task Execute() {
		var inputAndRun = this.GetInputAndRun();
		if (inputAndRun is null) {
			return;
		}

		var (input, run) = inputAndRun.Value;

		this.EssentialServices.inputDispatcher.Add(input);

		_ = this.CancellationToken.Register(() => {
			this.EssentialServices.inputDispatcher.Remove(input);
		});

		while (this.Game.IsRunning) {
			var action = await input.NewAction();
			run(action);
		}
	}
}
