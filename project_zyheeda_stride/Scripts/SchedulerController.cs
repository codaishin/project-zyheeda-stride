namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.MicroThreading;
using Stride.Engine;

public class SchedulerController : StartupScript, IScheduler {
	private readonly Queue<Func<Task>> queue = new();
	private MicroThread? dequeueThread;

	private async Task Dequeue() {
		while (this.queue.TryDequeue(out var nextFunc)) {
			await nextFunc();
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
	}

	public void Enqueue(Func<Task> func) {
		this.queue.Enqueue(func);
		this.StartDequeue();
	}

	public void Run(Func<Task> func) {
		this.Clear();
		this.Enqueue(func);
	}
}
