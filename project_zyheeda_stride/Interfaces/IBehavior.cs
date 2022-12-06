namespace ProjectZyheeda;

using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehavior {
	void Run(IMaybe<U<Vector3, Entity>> target);
	void Reset();
}
