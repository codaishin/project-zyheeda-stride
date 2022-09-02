namespace ProjectZyheeda;

using Stride.Engine;

public interface IEquipment {
	IMaybe<IBehaviorStateMachine> GetBehaviorFor(Entity agent);
}
