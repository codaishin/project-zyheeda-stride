namespace ProjectZyheeda;

using System;

public abstract class ReferenceScheduler<TScheduler> : Reference<TScheduler>, ISchedulerEditor
	where TScheduler :
		IScheduler {

	public TScheduler? Target {
		get => this.GetRef();
		set => this.SetRef(value);
	}

	public Result Clear() {
		return this.target.FlatMap(s => s.Clear());
	}

	public Result Run(Func<Coroutine> coroutine, Cancel? cancel = null) {
		return this.target.FlatMap(s => s.Run(coroutine, cancel));
	}

	public Result Enqueue(Func<Coroutine> coroutine, Cancel? cancel = null) {
		return this.target.FlatMap(s => s.Enqueue(coroutine, cancel));
	}
}

public class ReferenceSchedularController : ReferenceScheduler<SchedulerController> { }
