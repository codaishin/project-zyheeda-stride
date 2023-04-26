namespace ProjectZyheeda;

using System.Collections.Generic;
using Stride.Engine;

public interface IPrefabLoader {
	List<Entity> Instantiate(Prefab prefab);
}
