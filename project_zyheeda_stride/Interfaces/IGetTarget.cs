namespace ProjectZyheeda;

using Stride.Core.Mathematics;
using Stride.Engine;

public interface IGetTargets {
	U<Vector3, Entity>[] GetTargets();
}
