namespace ProjectZyheeda;

using Stride.Core.Serialization;

[DataSerializer(typeof(ReferenceSerializer<ReferenceBehaviorController, BehaviorController, IBehavior>))]
public class ReferenceBehaviorController : Reference<BehaviorController, IBehavior> { }
