namespace ProjectZyheeda;

using System.Collections.Generic;
using Stride.Engine;

public interface IPrefabLoader {
	Result<List<Entity>> Instantiate(Prefab prefab);
}
