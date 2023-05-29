namespace ProjectZyheeda;

using Stride.Core.Serialization;

public class NoEquipment : ReferenceNone<IEquipment> { }

[DataSerializer(typeof(ReferenceMoveController.Serializer<ReferenceMoveController>))]
public class ReferenceMoveController : Reference<MoveController, IEquipment> { }
