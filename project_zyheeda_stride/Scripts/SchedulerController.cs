namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.MicroThreading;
using Stride.Engine;

public class SchedulerController : StartupScript, IScheduler {
	private readonly Queue<(Func<Task>, Cancel)> queue = new();
	private MicroThread? dequeueThread;
	private Cancel? cancelExecution;

	private async Task Dequeue() {
		while (this.queue.TryDequeue(out var execution)) {
			(var runExecution, this.cancelExecution) = execution;
			await runExecution();
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
		this.cancelExecution?.Invoke();
		this.cancelExecution = null;
		this.dequeueThread?.Cancel();
		this.dequeueThread = null;
	}

	public void Enqueue((Func<Task>, Cancel) execution) {
		this.queue.Enqueue(execution);
		this.StartDequeue();
	}

	public void Run((Func<Task>, Cancel) execution) {
		this.Clear();
		this.Enqueue(execution);
	}
}
