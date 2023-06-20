namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.MicroThreading;

public class SchedulerController : ProjectZyheedaStartupScript, IScheduler {
	private readonly Queue<(Func<Coroutine>, Cancel?)> queue = new();
	private MicroThread? dequeueThread;
	private Cancel? cancelExecution;
	private TaskCompletionSource<Result>? currentStepToken;

	private void LogErrors((SystemErrors system, PlayerErrors player) errors) {
		this.EssentialServices.systemMessage.Log(errors.system.ToArray());
		this.EssentialServices.playerMessage.Log(errors.player.ToArray());
	}

	private Task<Result> WaitAndSetCurrentStepToken(IWait wait) {
		this.currentStepToken = wait.Wait(this.Script);
		return this.currentStepToken.Task;
	}

	private async Task Dequeue() {
		while (this.queue.TryDequeue(out var execution)) {
			(var runExecution, this.cancelExecution) = execution;
			var coroutine = runExecution();
			foreach (var step in coroutine) {
				var result = await step
					.Map(this.WaitAndSetCurrentStepToken)
					.Flatten();
				result.Switch(this.LogErrors, () => { });
			}
			this.cancelExecution = null;
			this.currentStepToken = null;
		}
	}

	private void StartDequeue() {
		if (this.dequeueThread?.State is MicroThreadState.Starting or MicroThreadState.Running) {
			return;
		}
		this.dequeueThread = this.Script.AddTask(this.Dequeue);
	}

	public Result Clear() {
		this.queue.Clear();

		if (this.cancelExecution is null) {
			return Result.Ok();
		}

		var result = this.cancelExecution();
		this.cancelExecution = null;
		this.dequeueThread?.Cancel();
		this.dequeueThread = null;
		_ = (this.currentStepToken?.TrySetCanceled());
		this.currentStepToken = null;
		return result;
	}

	public Result Run(Func<Coroutine> coroutine, Cancel? cancel = null) {
		return this
			.Clear()
			.FlatMap(() => this.Enqueue(coroutine, cancel));
	}

	public Result Enqueue(Func<Coroutine> coroutine, Cancel? cancel = null) {
		this.queue.Enqueue((coroutine, cancel));
		this.StartDequeue();
		return Result.Ok();
	}
}
