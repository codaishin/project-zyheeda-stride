namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class SchedulerController : ProjectZyheedaAsyncScript, IScheduler, IDisposable {
	private class DequeueWrapper : IEnumerator<Task<Result>> {
		private readonly IEnumerator<Task<Result>> dequeueSteps;
		private bool canceled;

		public Task<Result> Current => this.dequeueSteps.Current;

		object System.Collections.IEnumerator.Current => this.dequeueSteps.Current;

		public DequeueWrapper(IEnumerator<Task<Result>> dequeueSteps) {
			this.dequeueSteps = dequeueSteps;
		}

		public void Dispose() {
			this.dequeueSteps.Dispose();
		}

		public bool MoveNext() {
			return !this.canceled && this.dequeueSteps.MoveNext();
		}

		public void Reset() {
			this.dequeueSteps.Reset();
		}

		public void Cancel() {
			this.canceled = true;
		}
	}

	private readonly Queue<(Coroutine, Cancel?)> queue = new();
	private DequeueWrapper? dequeue;
	private readonly object dequeueLock = new();
	private Cancel? currentCancel;

	private void LogErrors((SystemErrors system, PlayerErrors player) errors) {
		this.EssentialServices.systemMessage.Log(errors.system.ToArray());
		this.EssentialServices.playerMessage.Log(errors.player.ToArray());
	}

	private IEnumerator<Task<Result>> Dequeue() {
		while (this.queue.TryDequeue(out var coroutineAndCancel)) {
			(var coroutine, this.currentCancel) = coroutineAndCancel;
			foreach (var step in coroutine) {
				yield return step
					.Map(wait => wait.Wait(this.Script).Task)
					.Flatten();
			}
			this.currentCancel = null;
		}
	}

	private bool NextStep(out Task<Result> task) {
		lock (this.dequeueLock) {
			if (this.dequeue?.MoveNext() == true) {
				task = this.dequeue.Current;
				return true;
			}
		}

		task = new TaskCompletionSource<Result>().Task;
		return false;
	}

	public Result Clear() {
		lock (this.dequeueLock) {
			this.queue.Clear();
			if (this.currentCancel is null) {
				return Result.Ok();
			}
			var result = this.currentCancel();
			this.currentCancel = null;
			this.dequeue?.Cancel();
			this.dequeue = null;
			return result;
		}
	}

	public Result Run(Coroutine coroutine, Cancel? cancel = null) {
		return this
			.Clear()
			.FlatMap(() => this.Enqueue(coroutine, cancel));
	}

	public Result Enqueue(Coroutine coroutine, Cancel? cancel = null) {
		lock (this.dequeueLock) {
			this.queue.Enqueue((coroutine, cancel));
			if (this.dequeue is not null) {
				return Result.Ok();
			}
			this.dequeue = new(this.Dequeue());
			return Result.Ok();
		}
	}

	public override async Task Execute() {
		while (this.Game.IsRunning) {
			while (this.NextStep(out var current)) {
				var result = await current;
				result.Switch(this.LogErrors, () => { });
			}
			lock (this.dequeueLock) {
				this.dequeue = null;
			}
			_ = await this.Script.NextFrame();
		}
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
