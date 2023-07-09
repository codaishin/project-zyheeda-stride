namespace ProjectZyheeda;

using Stride.Core;
using Stride.Engine;

[DataContract(Inherited = true)]
[Display(Expand = ExpandRule.Always)]
public abstract class ReferenceEquipment<TEquipment> : IReference<TEquipment>, IEquipmentEditor
	where TEquipment :
		class,
		IEquipment {

	private Result<TEquipment> target;

	protected ReferenceEquipment() {
		this.target = Result.SystemError(this.MissingTarget());
	}

	public TEquipment? Target {
		get => this.target.UnpackOrDefault();
		set => this.target = value.OkOrSystemError(this.MissingTarget());
	}


	public Result<FGetCoroutine> PrepareCoroutineFor(Entity agent) {
		return this.target.FlatMap(e => e.PrepareCoroutineFor(agent));
	}
}

public class ReferenceMoveController : ReferenceEquipment<MoveController> { }
public class ReferenceLaunchController : ReferenceEquipment<LauncherController> { }
