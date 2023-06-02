namespace ProjectZyheeda;

using System;

public interface IScheduler {
	Result Run((Func<Coroutine>, Cancel) execution);
	Result Enqueue((Func<Coroutine>, Cancel) execution);
	Result Clear();
}
