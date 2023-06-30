namespace ProjectZyheeda;

using System;
using System.Threading.Tasks;
using Stride.Core;

[DataContract]
public class KeyHoldExecutionStream : IExecutionStreamEditor, IDisposable {
	[DataMember(0)] public InputKeys key;
	[DataMember(1)] public int minimumHoldMilliseconds = 100;
	[DataMember(2)] public ISchedulerEditor? scheduler;

	private TaskCompletionSource<Result<FExecute>> beginToken = new();
	private TaskCompletionSource keyReleasedToken = new();

	private static Coroutine CancelOnKeyRelease(Coroutine coroutine, Cancel cancel, Task keyReleased) {
		foreach (var step in coroutine) {
			if (keyReleased.IsCompleted) {
				yield return cancel().Map(() => (IWait)new NoWait());
				yield break;
			}
			yield return step;
		}
	}

	private void SetNewExecution(Task keyReleased) {
		FExecute begin = (Coroutine coroutine, Cancel cancel) => {
			coroutine = KeyHoldExecutionStream.CancelOnKeyRelease(coroutine, cancel, keyReleased);
			return this.scheduler
				.OkOrSystemError(this.MissingField(nameof(this.scheduler)))
				.FlatMap(scheduler => scheduler.Run(coroutine, cancel));
		};

		this.beginToken.SetResult(begin);
		this.beginToken = new();
	}

	private async void NewExecutionAfterMinimumHold(Task keyReleased) {
		var firstDone = await Task.WhenAny(Task.Delay(this.minimumHoldMilliseconds), keyReleased);
		if (keyReleased == firstDone) {
			return;
		}
		this.SetNewExecution(keyReleased);
	}

	private void CancelNewExecution() {
		this.keyReleasedToken.SetResult();
		this.keyReleasedToken = new();
	}

	private Action GetProcessFunc(bool isDown) {
		return isDown switch {
			true => () => this.NewExecutionAfterMinimumHold(this.keyReleasedToken.Task),
			false => () => this.CancelNewExecution()
		};
	}

	public Task<Result<FExecute>> NewExecute() {
		return this.beginToken.Task;
	}

	public Result ProcessEvent(InputKeys key, bool isDown) {
		if (key != this.key) {
			return Result.Ok();
		}
		var processFunc = this.GetProcessFunc(isDown);
		processFunc();
		return Result.Ok();
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
