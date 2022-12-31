namespace ProjectZyheeda;

using Stride.Core.Mathematics;
using Stride.Engine;

public interface IGetTarget {
	IMaybe<U<Vector3, Entity>> GetTarget();
}
