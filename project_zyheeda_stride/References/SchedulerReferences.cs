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

	public Result Enqueue((Func<Coroutine>, Cancel) execution) {
		return this.target.FlatMap(s => s.Enqueue(execution));
	}

	public Result Run((Func<Coroutine>, Cancel) execution) {
		return this.target.FlatMap(s => s.Run(execution));
	}
}

public class ReferenceSchedularController : ReferenceScheduler<SchedulerController> { }
