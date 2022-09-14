namespace ProjectZyheeda;

using System;
using Stride.Engine;

using TMissing = IUnion<Requirement, System.Type[]>;

[Flags]
public enum Requirement {
	Body = 0b0001,
	Mind = 0b0010,
	Soul = 0b0100
}

public interface IEquipment {
	IEither<TMissing, IBehaviorStateMachine> GetBehaviorFor(Entity agent);
}
