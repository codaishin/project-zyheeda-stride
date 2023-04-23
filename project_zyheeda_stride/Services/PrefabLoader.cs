namespace ProjectZyheeda;

using System.Collections.Generic;
using Stride.Engine;

public class PrefabLoader : IPrefabLoader {
	public List<Entity> Instantiate(Prefab prefab) {
		return prefab.Instantiate();
	}
}
