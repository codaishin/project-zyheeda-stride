namespace ProjectZyheeda;

using Stride.Engine;

public interface IEquipment {
	BehaviorOrErrors GetBehaviorFor(Entity agent);
}
