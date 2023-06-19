namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.MicroThreading;

public class SchedulerController : ProjectZyheedaStartupScript, IScheduler {
	private readonly Queue<(Func<Coroutine>, Cancel)> queue = new();
	private MicroThread? dequeueThread;
	private Cancel? cancelExecution;

	private void LogErrors((SystemErrors system, PlayerErrors player) errors) {
		this.EssentialServices.systemMessage.Log(errors.system.ToArray());
		this.EssentialServices.playerMessage.Log(errors.player.ToArray());
	}

	private async Task Dequeue() {
		while (this.queue.TryDequeue(out var execution)) {
			(var runExecution, this.cancelExecution) = execution;
			var coroutine = runExecution();
			foreach (var yield in coroutine) {
				var result = await yield
					.Map(y => y.Wait(this.Script).Task)
					.Flatten();
				result.Switch(this.LogErrors, () => { });
			}
			this.cancelExecution = null;
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
		var result = this.cancelExecution?.Invoke();
		this.cancelExecution = null;
		this.dequeueThread?.Cancel();
		this.dequeueThread = null;
		return result ?? Result.Ok();
	}

	public Result Enqueue((Func<Coroutine>, Cancel) execution) {
		this.queue.Enqueue(execution);
		this.StartDequeue();
		return Result.Ok();
	}

	public Result Run((Func<Coroutine>, Cancel) execution) {
		return this
			.Clear()
			.FlatMap(() => this.Enqueue(execution));
	}
}
