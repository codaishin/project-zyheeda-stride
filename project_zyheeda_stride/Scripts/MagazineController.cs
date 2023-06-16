namespace ProjectZyheeda;

using System.Collections.Generic;
using System.Linq;
using Stride.Engine;

public class MagazineController : ProjectZyheedaStartupScript, IMagazine {
	public Prefab? prefab;

	private Result<List<Entity>> InstantiatePrefab() {
		return this.prefab is null
			? Result.SystemError(this.MissingField(nameof(this.prefab)))
			: this.EssentialServices.prefabLoader.Instantiate(this.prefab);
	}

	private Result<Entity> GetFirstInstance(List<Entity> instance) {
		return instance.Count switch {
			0 => Result.SystemError(this.MissingEntitiesOnPrefab(nameof(this.prefab))),
			> 1 => Result.SystemError(this.EntitiesCountMismatchOnPrefab(nameof(this.prefab), 1, instance.Count)),
			_ => instance[0]
		};
	}

	private void ManageInstanceLifetime(Entity instance, IProjectile projectile) {
		this.Entity.Scene.Entities.Add(instance);
		projectile.OnHit += _ => this.Entity.Scene.Entities.Remove(instance);
		projectile.OnRangeLimit += () => this.Entity.Scene.Entities.Remove(instance);
	}

	private Result<IProjectile> GetInstanceProjectileComponent(Entity instance) {
		var projectileOrNull = instance.GetAll<EntityComponent>().FirstOrDefault(c => c is IProjectile);

		if (projectileOrNull is not IProjectile projectile) {
			return Result.SystemError(this.MissingComponentOnPrefab(nameof(this.prefab), nameof(IProjectile)));
		}

		this.ManageInstanceLifetime(instance, projectile);

		return Result.Ok(projectile);
	}

	public Result<IProjectile> GetProjectile() {
		return this
			.InstantiatePrefab()
			.FlatMap(this.GetFirstInstance)
			.FlatMap(this.GetInstanceProjectileComponent);
	}
}
