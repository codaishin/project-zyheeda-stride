namespace ProjectZyheeda;

using Stride.Core;

[DataContract(Inherited = true)]
[Display(Expand = ExpandRule.Always)]
public abstract class ReferenceMagazine<TMagazine> : IReference<TMagazine>, IMagazineEditor
	where TMagazine :
		class,
		IMagazine {

	private Result<TMagazine> target;

	protected ReferenceMagazine() {
		this.target = Result.SystemError(this.MissingTarget());
	}

	public TMagazine? Target {
		get => this.target.UnpackOrDefault();
		set => this.target = value.OkOrSystemError(this.MissingTarget());
	}

	public Result<IProjectile> GetProjectile() {
		return this.target.FlatMap(m => m.GetProjectile());
	}
}

public class ReferenceMagazineController : ReferenceMagazine<MagazineController> { }
