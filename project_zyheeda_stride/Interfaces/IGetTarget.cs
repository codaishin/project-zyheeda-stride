namespace ProjectZyheeda;

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IGetTargets {
	IAsyncEnumerable<U<Vector3, Entity>> GetTargets();
}
