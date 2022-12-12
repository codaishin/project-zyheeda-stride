namespace ProjectZyheeda;

using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehavior {
	void Run(U<Vector3, Entity>[] targets);
	void Reset();
}
