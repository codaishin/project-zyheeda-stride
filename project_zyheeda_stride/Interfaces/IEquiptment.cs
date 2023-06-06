namespace ProjectZyheeda;

using Stride.Engine;


public interface IEquipment {
	Result<FGetCoroutine> PrepareCoroutineFor(Entity agent);
}

public interface IEquipmentEditor : IEquipment { }
