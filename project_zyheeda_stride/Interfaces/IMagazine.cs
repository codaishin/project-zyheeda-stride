namespace ProjectZyheeda;

public interface IMagazine {
	Result<IProjectile> GetProjectile();
}

public interface IMagazineEditor : IMagazine { }
