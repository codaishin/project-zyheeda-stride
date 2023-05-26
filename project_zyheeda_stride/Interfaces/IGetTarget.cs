namespace ProjectZyheeda;

using Stride.Core.Mathematics;
using Stride.Engine;

public interface IGetTarget {
	Result<U<Vector3, Entity>> GetTarget();
}
