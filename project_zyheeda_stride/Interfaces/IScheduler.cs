namespace ProjectZyheeda;

using System;
using System.Threading.Tasks;

public interface IScheduler {
	void Run((Func<Task>, Cancel) execution);
	void Enqueue((Func<Task>, Cancel) execution);
	void Clear();
}
