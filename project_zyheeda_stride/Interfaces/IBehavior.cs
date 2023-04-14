namespace ProjectZyheeda;

using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehavior {
	Task<bool> Run(IAsyncEnumerable<U<Vector3, Entity>> targets);
	void Reset();
}
