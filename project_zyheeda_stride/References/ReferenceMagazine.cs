namespace ProjectZyheeda;

public abstract class ReferenceMagazine<TMagazine> : Reference<TMagazine>, IMagazineEditor
	where TMagazine :
		IMagazine {

	public TMagazine? Target {
		get => this.GetRef();
		set => this.SetRef(value);
	}

	public Result<IProjectile> GetProjectile() {
		return this.target.FlatMap(m => m.GetProjectile());
	}
}

public class ReferenceMagazineController : ReferenceMagazine<MagazineController> { }
