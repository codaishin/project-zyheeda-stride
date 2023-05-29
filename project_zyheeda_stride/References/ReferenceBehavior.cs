namespace ProjectZyheeda;

using Stride.Core.Serialization;

public class NoBehavior : ReferenceNone<IBehavior> { }

[DataSerializer(typeof(ReferenceSerializer<ReferenceBehaviorController, BehaviorController, IBehavior>))]
public class ReferenceBehaviorController : Reference<BehaviorController, IBehavior> { }
