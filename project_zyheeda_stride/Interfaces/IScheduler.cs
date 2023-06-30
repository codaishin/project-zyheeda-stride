namespace ProjectZyheeda;

public interface IScheduler {
	Result Run(Coroutine coroutine, Cancel? cancel = null);
	Result Enqueue(Coroutine coroutine, Cancel? cancel = null);
	Result Clear();
}

public interface ISchedulerEditor : IScheduler { }
