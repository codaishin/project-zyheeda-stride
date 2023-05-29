namespace ProjectZyheeda;

using Stride.Core.Serialization;

[DataSerializer(typeof(ReferenceSerializer<ReferenceMoveController, MoveController, IEquipment>))]
public class ReferenceMoveController : Reference<MoveController, IEquipment> { }
