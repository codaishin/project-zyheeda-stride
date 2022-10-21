namespace ProjectZyheeda;

using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehavior {
	void Run(IMaybe<IUnion<Vector3, Entity>> target);
	void Reset();
}
