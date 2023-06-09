namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using Xunit;

public class MagazineControllerTests : GameTestCollection {
	private readonly MagazineController controller;
	private readonly IPrefabLoader prefabLoader;
	private readonly IProjectile projectile;
	private readonly Entity projectileInstance;

	public MagazineControllerTests(GameFixture fixture) : base(fixture) {
		var mProjectile = new Mock<EntityComponent>().As<IProjectile>();
		_ = mProjectile
			.Setup(p => p.Follow(It.IsAny<Vector3>(), It.IsAny<Func<Vector3>>(), It.IsAny<float>()))
			.Returns(Result.Ok());

		this.projectileInstance = new Entity { (EntityComponent)mProjectile.Object };
		this.projectile = mProjectile.Object;
		this.controller = new MagazineController { prefab = new Prefab() };
		this.scene.Entities.Add(new Entity { this.controller });

		this.game.Services.RemoveService<IPrefabLoader>();
		this.game.Services.AddService<IPrefabLoader>(this.prefabLoader = Mock.Of<IPrefabLoader>());
		_ = Mock
			.Get(this.prefabLoader)
			.Setup(l => l.Instantiate(It.IsAny<Prefab>()))
			.Returns(Result.Ok(new List<Entity> { this.projectileInstance }));

		this.game.WaitFrames(2);
	}

	[Fact]
	public void GetProjectile() {
		var result = this.controller.GetProjectile();
		var projectile = result.UnpackOr(Mock.Of<IProjectile>());

		Assert.Same(this.projectile, projectile);
		Mock
			.Get(this.prefabLoader)
			.Verify(l => l.Instantiate(this.controller.prefab!), Times.Once);
	}

	[Fact]
	public void ProjectileAttachedToSceneEntities() {
		_ = this.controller.GetProjectile();

		Assert.Contains(this.projectileInstance, this.scene.Entities);
	}

	private class MockProjectileComponent : StartupScript, IProjectile {
		public event Action<PhysicsComponent>? OnHit;

		public Result Follow(Vector3 start, Func<Vector3> getTarget, float rangeMultiplier) {
			this.OnHit?.Invoke(new RigidbodyComponent());
			return Result.Ok();
		}
	}

	[Fact]
	public void OnHitRemovesEntity() {
		var projectileInstance = new Entity { new MockProjectileComponent() };

		_ = Mock
			.Get(this.prefabLoader)
			.Setup(l => l.Instantiate(It.IsAny<Prefab>()))
			.Returns(new List<Entity> { projectileInstance });

		_ = this.controller
			.GetProjectile()
			.UnpackOr(Mock.Of<IProjectile>())
			.Follow(Vector3.Zero, () => Vector3.Zero, 1f);

		Assert.DoesNotContain(projectileInstance, this.scene.Entities);
	}

	[Fact]
	public void MissingPrefab() {
		this.controller.prefab = null;

		var result = this.controller.GetProjectile();
		var error = result.Switch(
			errors => (string)errors.system.FirstOrDefault(),
			_ => "no errors"
		);

		Assert.Equal(this.controller.MissingField(nameof(this.controller.prefab)), error);
	}

	[Fact]
	public void MissingProjectileComponent() {
		_ = Mock
			.Get(this.prefabLoader)
			.Setup(l => l.Instantiate(It.IsAny<Prefab>()))
			.Returns(Result.Ok(new List<Entity> { new Entity() }));

		var result = this.controller.GetProjectile();
		var error = result.Switch(
			errors => (string)errors.system.FirstOrDefault(),
			_ => "no errors"
		);
		Assert.Equal(
			this.controller.MissingComponentOnPrefab(nameof(this.controller.prefab), nameof(IProjectile)),
			error
		);
	}

	[Fact]
	public void MissingEntities() {
		_ = Mock
			.Get(this.prefabLoader)
			.Setup(l => l.Instantiate(It.IsAny<Prefab>()))
			.Returns(Result.Ok(new List<Entity> { }));

		var result = this.controller.GetProjectile();
		var error = result.Switch(
			errors => (string)errors.system.FirstOrDefault(),
			_ => "no errors"
		);
		Assert.Equal(
			this.controller.MissingEntitiesOnPrefab(nameof(this.controller.prefab)),
			error
		);
	}

	[Fact]
	public void TooManyEntitiesOnPrefab() {
		_ = Mock
			.Get(this.prefabLoader)
			.Setup(l => l.Instantiate(It.IsAny<Prefab>()))
			.Returns(Result.Ok(new List<Entity> { new Entity(), new Entity() }));

		var result = this.controller.GetProjectile();
		var error = result.Switch(
			errors => (string)errors.system.FirstOrDefault(),
			_ => "no errors"
		);
		Assert.Equal(
			this.controller.EntitiesCountMismatchOnPrefab(nameof(this.controller.prefab), 1, 2),
			error
		);
	}
}
