namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class InputController : ProjectZyheedaAsyncScript {
	public IInputStream? input;
	public IGetTargetEditor? getTarget;
	public IMaybe<IBehavior>? behavior;
	public IMaybe<IScheduler>? scheduler;

	private Task LogErrors((IEnumerable<SystemError> system, IEnumerable<PlayerError> player) errors) {
		this.EssentialServices.systemMessage.Log(errors.system.ToArray());
		this.EssentialServices.playerMessage.Log(errors.player.ToArray());
		return Task.CompletedTask;
	}

	private Action<InputAction> RunBehavior(IGetTargetEditor getTarget, IBehavior behavior, IScheduler scheduler) {
		return action => {
			Func<(Func<Coroutine>, Cancel), Result> runOrEnqueue =
				action is InputAction.Run
					? scheduler.Run
					: scheduler.Enqueue;
			getTarget
				.GetTarget()
				.FlatMap(behavior.GetCoroutine)
				.FlatMap(runOrEnqueue)
				.Switch(errors => this.LogErrors(errors), () => { });
		};
	}

	private (IInputStream, Action<InputAction>)? GetInputAndRun() {
		var getInputAndRun =
			(IGetTargetEditor getTarget) =>
			(IBehavior behavior) =>
			(IScheduler scheduler) =>
			(IInputStream input) => (input, this.RunBehavior(getTarget, behavior, scheduler));

		return getInputAndRun
			.Apply(this.getTarget.OkOrSystemError(this.MissingField(nameof(this.getTarget))))
			.Apply(this.behavior.ToMaybe().Flatten().ToOkOrSystemError(this.MissingField(nameof(this.behavior))))
			.Apply(this.scheduler.ToMaybe().Flatten().ToOkOrSystemError(this.MissingField(nameof(this.scheduler))))
			.Apply(this.input.ToMaybe().ToOkOrSystemError(this.MissingField(nameof(this.input))))
			.Switch<(IInputStream, Action<InputAction>)?>(
				errors => {
					this.EssentialServices.systemMessage.Log(errors.system.ToArray());
					return null;
				},
				run => run
			);
	}
	private Action RegisterCleanup(IInputStream input) {
		return () => this.CancellationToken.Register(
			() => this.EssentialServices.inputDispatcher
				.Remove(input)
				.Switch(errors => this.LogErrors(errors), () => { })
		);
	}

	private Func<Task> RunGameLoop(IInputStream input, Action<InputAction> run) {
		return async () => {
			while (this.Game.IsRunning) {
				var action = await input.NewAction();
				action.Switch(
					errors => this.LogErrors(errors),
					action => run(action)
				);
			}
		};
	}

	public override async Task Execute() {
		var inputAndRun = this.GetInputAndRun();
		if (inputAndRun is null) {
			return;
		}

		var (input, run) = inputAndRun.Value;

		await this.EssentialServices.inputDispatcher
			.Add(input)
			.Map(this.RegisterCleanup(input))
			.Switch(this.LogErrors, this.RunGameLoop(input, run));
	}
}
