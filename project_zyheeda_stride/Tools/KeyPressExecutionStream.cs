namespace ProjectZyheeda;

using System.Threading.Tasks;
using Stride.Core;

public enum InputActivation {
	OnPress,
	OnRelease,
}

public enum InputKeys {
	None = default,
	ShiftLeft,
	CapsLock,
	MouseLeft,
	MouseRight,
}

[DataContract]
public class KeyPressExecutionStream : IExecutionStreamEditor {
	[DataMember(0)] public InputKeys activationKey = InputKeys.None;
	[DataMember(1)] public InputActivation activation = InputActivation.OnPress;
	[DataMember(2)] public InputKeys enqueueKey = InputKeys.None;
	[DataMember(3)] public ISchedulerEditor? scheduler;
	[DataMember(4)] public bool canBeCanceled = true;

	private TaskCompletionSource<Result<FExecute>> actionToken = new();
	private bool primedForEnqueueing;

	public Task<Result<FExecute>> NewExecute() {
		return this.actionToken.Task;
	}

	public Result ProcessEvent(InputKeys key, bool isDown) {
		return key == this.enqueueKey
			? this.TryPrimeForChaining(isDown)
			: key == this.activationKey
			? this.TryRunOrChain(isDown)
			: Result.Ok();
	}

	private Result TryPrimeForChaining(bool isDown) {
		this.primedForEnqueueing = KeyPressExecutionStream.Matches(InputActivation.OnPress, isDown);
		return Result.Ok();
	}

	private FExecute GetExecution(IScheduler scheduler) {
		Result Run(Coroutine coroutine, Cancel cancel) {
			return scheduler.Run(coroutine, this.canBeCanceled ? cancel : null);
		}

		Result Enqueue(Coroutine coroutine, Cancel cancel) {
			return scheduler.Enqueue(coroutine, this.canBeCanceled ? cancel : null);
		}

		return this.primedForEnqueueing ? Enqueue : Run;
	}

	private Result TryRunOrChain(bool isDown) {
		var ok = Result.Ok();
		if (!KeyPressExecutionStream.Matches(this.activation, isDown)) {
			return ok;
		}

		var execution = this.scheduler
			.OkOrSystemError(this.MissingField(nameof(this.scheduler)))
			.Map(this.GetExecution);

		this.actionToken.SetResult(execution);
		this.actionToken = new();
		return ok;
	}

	private static bool Matches(InputActivation activation, bool isDown) {
		return isDown
			? activation is InputActivation.OnPress
			: activation is InputActivation.OnRelease;
	}
}
