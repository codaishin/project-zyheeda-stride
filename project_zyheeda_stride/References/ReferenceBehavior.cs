namespace ProjectZyheeda;

using Stride.Core.Serialization;

public class NoBehavior : ReferenceNone<IBehavior> { }

[DataSerializer(typeof(ReferenceBehaviorController.Serializer<ReferenceBehaviorController>))]
public class ReferenceBehaviorController : Reference<BehaviorController, IBehavior> { }
