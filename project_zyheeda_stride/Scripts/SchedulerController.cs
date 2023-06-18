namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.MicroThreading;

public class SchedulerController : ProjectZyheedaStartupScript, IScheduler {
	private readonly Queue<(Func<Coroutine>, Cancel)> executionQueue = new();
	private readonly Queue<IWait> clearQueue = new();
	private MicroThread? dequeueThread;
	private Cancel cancelExecution = SchedulerController.DefaultCancel;

	private static Result<IWait> DefaultCancel() {
		return Result.Ok<IWait>(new NoWait());
	}

	private void LogErrors((SystemErrors system, PlayerErrors player) errors) {
		this.EssentialServices.systemMessage.Log(errors.system.ToArray());
		this.EssentialServices.playerMessage.Log(errors.player.ToArray());
	}

	private async Task Dequeue() {
		while (this.clearQueue.TryDequeue(out var clear)) {
			_ = await clear.Wait(this.Script);
		}
		while (this.executionQueue.TryDequeue(out var execution)) {
			(var runExecution, this.cancelExecution) = execution;
			var coroutine = runExecution();
			foreach (var yield in coroutine) {
				var result = await yield
					.Map(y => y.Wait(this.Script))
					.Flatten();
				result.Switch(this.LogErrors, () => { });
			}
			this.cancelExecution = SchedulerController.DefaultCancel;
		}
	}

	private void StartDequeue() {
		if (this.dequeueThread?.State is MicroThreadState.Starting or MicroThreadState.Running) {
			return;
		}
		this.dequeueThread = this.Script.AddTask(this.Dequeue);
	}

	private Result Clear(IWait clearYield) {
		this.executionQueue.Clear();
		this.cancelExecution = SchedulerController.DefaultCancel;
		this.dequeueThread?.Cancel();
		this.dequeueThread = null;
		this.clearQueue.Enqueue(clearYield);
		this.StartDequeue();
		return Result.Ok();
	}

	public Result Clear() {
		return this
			.cancelExecution()
			.FlatMap(this.Clear);
	}

	public Result Enqueue((Func<Coroutine>, Cancel) execution) {
		this.executionQueue.Enqueue(execution);
		this.StartDequeue();
		return Result.Ok();
	}

	public Result Run((Func<Coroutine>, Cancel) execution) {
		return this
			.Clear()
			.FlatMap(() => this.Enqueue(execution));
	}
}
