namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core;

public class ExecutionController : ProjectZyheedaAsyncScript {
	[DataMember(0)] public IExecutionStreamEditor? input;
	[DataMember(1)] public IBehaviorEditor? behavior;

	private Task LogErrors((IEnumerable<SystemError> system, IEnumerable<PlayerError> player) errors) {
		this.EssentialServices.systemMessage.Log(errors.system.ToArray());
		this.EssentialServices.playerMessage.Log(errors.player.ToArray());
		return Task.CompletedTask;
	}

	private void RunBehavior(IBehavior behavior, FExecute execute) {
		behavior
			.GetExecution()
			.FlatMap(e => execute(e.coroutine, e.cancel))
			.Switch(errors => this.LogErrors(errors), () => { });
	}

	private Result<(IExecutionStreamEditor, IBehaviorEditor)> GetInputAndBehavior() {
		var getInputAndRun =
			(IBehaviorEditor behavior) =>
			(IExecutionStreamEditor input) =>
				(input, behavior);

		return getInputAndRun
			.Apply(this.behavior.OkOrSystemError(this.MissingField(nameof(this.behavior))))
			.Apply(this.input.OkOrSystemError(this.MissingField(nameof(this.input))));
	}
	private Action RegisterCleanup(IExecutionStream input) {
		return () => this.CancellationToken.Register(
			() => this.EssentialServices.inputDispatcher
				.Remove(input)
				.Switch(errors => this.LogErrors(errors), () => { })
		);
	}

	private Func<Task> RunGameLoop(IExecutionStream input, IBehavior behavior) {
		return async () => {
			while (this.Game.IsRunning) {
				var run = await input.NewExecute();
				run.Switch(
					errors => this.LogErrors(errors),
					run => this.RunBehavior(behavior, run)
				);
			}
		};
	}

	public override async Task Execute() {
		var runLoop = async ((IExecutionStreamEditor, IBehaviorEditor) inputAndBehavior) => {
			var (input, behavior) = inputAndBehavior;

			await this.EssentialServices.inputDispatcher
				.Add(input)
				.Map(this.RegisterCleanup(input))
				.Switch(this.LogErrors, this.RunGameLoop(input, behavior));
		};

		await this.GetInputAndBehavior().Switch(
			this.LogErrors,
			runLoop
		);
	}
}
