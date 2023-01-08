namespace ProjectZyheeda;

using System.Collections.Generic;
using Stride.Engine;

public interface IEquipment {
	Either<IEnumerable<U<SystemString, PlayerString>>, IBehaviorStateMachine> GetBehaviorFor(Entity agent);
}
