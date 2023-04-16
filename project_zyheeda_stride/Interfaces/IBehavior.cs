namespace ProjectZyheeda;

using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

public interface IBehavior {
	Task<bool> Run(U<Vector3, Entity> target);
	void Reset();
}
