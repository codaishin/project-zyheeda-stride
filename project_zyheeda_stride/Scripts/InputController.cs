namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class InputController : ProjectZyheedaAsyncScript {
	public IInputStreamEditor? input;
	public IBehaviorEditor? behavior;
	public ISchedulerEditor? scheduler;

	private Task LogErrors((IEnumerable<SystemError> system, IEnumerable<PlayerError> player) errors) {
		this.EssentialServices.systemMessage.Log(errors.system.ToArray());
		this.EssentialServices.playerMessage.Log(errors.player.ToArray());
		return Task.CompletedTask;
	}

	private Action<InputAction> RunBehavior(IBehavior behavior, IScheduler scheduler) {
		return action => {
			Func<Func<Coroutine>, Cancel, Result> runOrEnqueue =
				action is InputAction.Run
					? scheduler.Run
					: scheduler.Enqueue;
			behavior
				.GetExecution()
				.FlatMap(c => runOrEnqueue(c.coroutine, c.cancel))
				.Switch(errors => this.LogErrors(errors), () => { });
		};
	}

	private (IInputStreamEditor, Action<InputAction>)? GetInputAndRun() {
		var getInputAndRun =
			(IBehaviorEditor behavior) =>
			(ISchedulerEditor scheduler) =>
			(IInputStreamEditor input) =>
				(input, this.RunBehavior(behavior, scheduler));

		return getInputAndRun
			.Apply(this.behavior.OkOrSystemError(this.MissingField(nameof(this.behavior))))
			.Apply(this.scheduler.OkOrSystemError(this.MissingField(nameof(this.scheduler))))
			.Apply(this.input.OkOrSystemError(this.MissingField(nameof(this.input))))
			.Switch<(IInputStreamEditor, Action<InputAction>)?>(
				errors => {
					_ = this.LogErrors(errors);
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
