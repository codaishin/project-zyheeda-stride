namespace ProjectZyheeda;

using Stride.Engine;

public abstract class ReferenceEquipment<TEquipment> : Reference<TEquipment>, IEquipmentEditor
	where TEquipment :
		IEquipment {

	public TEquipment? Target {
		get => this.GetRef();
		set => this.SetRef(value);
	}

	public Result<FGetCoroutine> PrepareCoroutineFor(Entity agent) {
		return this.target.FlatMap(e => e.PrepareCoroutineFor(agent));
	}
}

public class ReferenceMoveController : ReferenceEquipment<MoveController> { }
