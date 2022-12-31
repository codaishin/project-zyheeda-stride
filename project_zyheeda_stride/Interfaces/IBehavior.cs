namespace ProjectZyheeda;

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehavior {
	void Run(IAsyncEnumerable<U<Vector3, Entity>> targets);
	void Reset();
}
