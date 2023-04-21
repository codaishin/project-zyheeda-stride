namespace ProjectZyheeda;

using System;

public interface IScheduler {
	void Run((Func<Coroutine>, Cancel) execution);
	void Enqueue((Func<Coroutine>, Cancel) execution);
	void Clear();
}
