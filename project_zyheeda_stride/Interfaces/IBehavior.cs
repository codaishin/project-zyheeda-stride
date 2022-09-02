namespace ProjectZyheeda;

using Stride.Engine;

public interface IBehavior {
	void Init(Entity agent);
	void Run();
	void Reset();
}
