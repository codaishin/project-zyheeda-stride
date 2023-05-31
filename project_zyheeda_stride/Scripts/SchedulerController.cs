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

	private Task LogErrors((SystemErrors system, PlayerErrors player) errors) {
		this.EssentialServices.systemMessage.Log(errors.system.ToArray());
		this.EssentialServices.playerMessage.Log(errors.player.ToArray());
		return Task.CompletedTask;
	}

	private Task WaitPause(IWait pause) {
		return pause.Wait(this.Script);
	}

	private async Task Dequeue() {
		while (this.queue.TryDequeue(out var execution)) {
			(var runExecution, this.cancelExecution) = execution;
			var coroutine = runExecution();
			foreach (var yield in coroutine) {
				await yield.Switch(this.LogErrors, this.WaitPause);
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

	public void Clear() {
		this.queue.Clear();
		this.cancelExecution?.Invoke().Switch(
			errors => this.LogErrors(errors),
			() => { }
		);
		this.cancelExecution = null;
		this.dequeueThread?.Cancel();
		this.dequeueThread = null;
	}

	public void Enqueue((Func<Coroutine>, Cancel) execution) {
		this.queue.Enqueue(execution);
		this.StartDequeue();
	}

	public void Run((Func<Coroutine>, Cancel) execution) {
		this.Clear();
		this.Enqueue(execution);
	}
}
