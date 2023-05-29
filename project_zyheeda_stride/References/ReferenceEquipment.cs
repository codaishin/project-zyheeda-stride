namespace ProjectZyheeda;

using Stride.Core.Serialization;

public class NoEquipment : ReferenceNone<IEquipment> { }

[DataSerializer(typeof(ReferenceSerializer<ReferenceMoveController, MoveController, IEquipment>))]
public class ReferenceMoveController : Reference<MoveController, IEquipment> { }
