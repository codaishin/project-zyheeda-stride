namespace ProjectZyheeda;

using System;
using System.Threading.Tasks;

public interface IScheduler {
	void Run(Func<Task> func);
	void Enqueue(Func<Task> func);
	void Clear();
}
