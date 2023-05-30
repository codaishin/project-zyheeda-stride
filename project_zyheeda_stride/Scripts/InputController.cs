namespace ProjectZyheeda;

using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Annotations;

public class InputController : ProjectZyheedaAsyncScript {

	public IInputStream? input;
	[NotNull] public IMaybe<IGetTarget> getTarget = new NoGetTarget();
	[NotNull] public IMaybe<IBehavior> behavior = new NoBehavior();
	[NotNull] public IMaybe<IScheduler> scheduler = new NoScheduler();

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
			.ApplyWeak(this.getTarget.OkOrSystemError(this.MissingField(nameof(this.getTarget))))
			.ApplyWeak(this.behavior.OkOrSystemError(this.MissingField(nameof(this.behavior))))
			.ApplyWeak(this.scheduler.OkOrSystemError(this.MissingField(nameof(this.scheduler))))
			.ApplyWeak(this.input.ToMaybe().OkOrSystemError(this.MissingField(nameof(this.input))))
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
