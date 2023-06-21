namespace ProjectZyheeda;

using System;

public interface IScheduler {
	Result Run(Func<Coroutine> coroutine, Cancel? cancel = null);
	Result Enqueue(Func<Coroutine> coroutine, Cancel? cancel = null);
	Result Clear();
}

public interface ISchedulerEditor : IScheduler { }
