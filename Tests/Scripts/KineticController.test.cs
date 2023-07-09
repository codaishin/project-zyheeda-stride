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

public class TestKineticController : GameTestCollection {
	private readonly KineticController kineticController;
	private readonly IMoveEditor move;
	private readonly PhysicsComponent collider;
	private readonly FGetCoroutine getCoroutine;
	private readonly ISystemMessage systemMessage;
	private readonly IPlayerMessage playerMEssage;

	public TestKineticController(GameFixture fixture) : base(fixture) {
		this.systemMessage = Mock.Of<ISystemMessage>();
		this.game.Services.RemoveService<ISystemMessage>();
		this.game.Services.AddService<ISystemMessage>(this.systemMessage);

		this.playerMEssage = Mock.Of<IPlayerMessage>();
		this.game.Services.RemoveService<IPlayerMessage>();
		this.game.Services.AddService<IPlayerMessage>(this.playerMEssage);

		this.game.Frames(1).Wait();

		this.collider = new RigidbodyComponent {
			IsKinematic = true,
			ColliderShape = new SphereColliderShape(is2D: false, radiusParam: 1f)
		};
		this.kineticController = new() {
			move = this.move = Mock.Of<IMoveEditor>(),
			collider = this.collider,
		};
		this.getCoroutine = Mock.Of<FGetCoroutine>();
		(Func<IEnumerable<Result<IWait>>>, Cancel) coroutine = (() => Array.Empty<Result<IWait>>(), Mock.Of<Cancel>());

		Mock
			.Get(this.getCoroutine)
			.SetReturnsDefault<(Func<IEnumerable<Result<IWait>>>, Cancel)>(coroutine);

		Mock
			.Get(this.kineticController.move)
			.SetReturnsDefault<Result<FGetCoroutine>>(this.getCoroutine);

		this.scene.Entities.Add(new Entity{
			this.collider,
			this.kineticController,
		});
		this.game.Frames(3).Wait();
	}

	[Fact]
	public void PreparedCoroutineEntity() {
		_ = this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(1, 1, 1), 42f);

		Mock
			.Get(this.move)
			.Verify(m => m.PrepareCoroutineFor(this.kineticController.Entity, It.IsAny<FSpeedToDelta>()), Times.Once);
	}

	[Fact]
	public void PreparedCoroutineMissingMove() {
		this.kineticController.move = null;
		var result = this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(1, 1, 1), 42f);

		var errors = result.Switch(
			errors => (string)errors.system.FirstOrDefault(),
			() => "no error"
		);
		Assert.Equal(this.kineticController.MissingField(nameof(this.kineticController.move)), errors);
	}

	[Fact]
	public void PreparedCoroutineDeltaFunc() {
		var speedToDelta = null as FSpeedToDelta;

		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(It.IsAny<Entity>(), It.IsAny<FSpeedToDelta>()))
			.Returns<Entity, FSpeedToDelta>((_, _speedToDelta) => {
				speedToDelta = _speedToDelta;
				return (FGetCoroutine)(_ => (Enumerable.Empty<Result<IWait>>(), Mock.Of<Cancel>()));
			});

		_ = this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(1, 1, 1), 42f);

		var delta = (float)this.game.UpdateTime.Elapsed.TotalSeconds;
		Assert.Equal(delta * 3, speedToDelta!(new UnitsPerSecond(3)));
	}

	[Fact]
	public async void PreparedCoroutineDeltaFuncWithCurrentDelta() {
		var speedToDelta = null as FSpeedToDelta;

		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(It.IsAny<Entity>(), It.IsAny<FSpeedToDelta>()))
			.Returns<Entity, FSpeedToDelta>((_, _speedToDelta) => {
				speedToDelta = _speedToDelta;
				return (FGetCoroutine)(_ => (Enumerable.Empty<Result<IWait>>(), Mock.Of<Cancel>()));
			});

		_ = this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(1, 1, 1), 42f);

		await this.game.Frames(3);

		var delta = (float)this.game.UpdateTime.Elapsed.TotalSeconds;
		Assert.Equal(delta * 10, speedToDelta!(new UnitsPerSecond(10)));
	}

	[Fact]
	public void FollowResultOk() {
		var result = this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(3, 2, 1), 1f);
		var ok = result.Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);
	}

	[Fact]
	public async void SetStartingPosition() {
		_ = this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(3, 2, 1), 1f);

		await this.game.Frames(1);

		var entityPosition = this.kineticController.Entity.Transform.Position;
		Assert.Equal(new Vector3(1, 2, 3), entityPosition);
	}

	[Fact]
	public async void UseGetCoroutineWithAdjustedTarget() {
		var target = new Vector3(2, 3, 4);
		var start = new Vector3(1, 2, 3);
		var adjustedTarget = Vector3.Zero;

		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Result<Vector3>>>()))
			.Returns((Func<Result<Vector3>> getTarget) => {
				adjustedTarget = getTarget().UnpackOr(Vector3.Zero);
				return (Enumerable.Empty<Result<IWait>>(), Mock.Of<Cancel>());
			});

		this.kineticController.baseRange = 10;
		_ = this.kineticController.Follow(start, () => target, 1);

		await this.game.Frames(1);

		var normalizedDirection = target - start;
		normalizedDirection.Normalize();
		Assert.Equal(start + (normalizedDirection * 10), adjustedTarget);
	}

	[Fact]
	public async void UseGetCoroutineWithAdjustedTargetScaledByRangeMultiplier() {
		var target = new Vector3(2, 3, 4);
		var start = new Vector3(1, 2, 3);
		var adjustedTarget = Vector3.Zero;

		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Result<Vector3>>>()))
			.Returns((Func<Result<Vector3>> getTarget) => {
				adjustedTarget = getTarget().UnpackOr(Vector3.Zero);
				return (Enumerable.Empty<Result<IWait>>(), Mock.Of<Cancel>());
			});

		this.kineticController.baseRange = 10;
		_ = this.kineticController.Follow(start, () => target, 5);

		await this.game.Frames(1);

		var normalizedDirection = target - start;
		normalizedDirection.Normalize();
		Assert.Equal(start + (normalizedDirection * 10 * 5), adjustedTarget);
	}

	[Fact]
	public async void UseCoroutine() {
		IEnumerable<Result<IWait>> Coroutine() {
			yield return new WaitFrame();
			this.kineticController.Entity.Transform.Position = new Vector3(5, 3, 70);
		}

		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Result<Vector3>>>()))
			.Returns((Coroutine(), Mock.Of<Cancel>()));

		_ = this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(3, 2, 1), 42f);

		await this.game.Frames(1);

		Assert.Equal(new Vector3(1, 2, 3), this.kineticController.Entity.Transform.Position);

		await this.game.Frames(2);

		Assert.Equal(new Vector3(5, 3, 70), this.kineticController.Entity.Transform.Position);
	}

	[Fact]
	public async void UseLatestFollow() {
		IEnumerable<Result<IWait>> Coroutine() {
			while (this.game.IsRunning) {
				yield return new WaitFrame();
				this.kineticController.Entity.Transform.Position.X += 1;
			}
		}

		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Result<Vector3>>>()))
			.Returns((Coroutine(), Mock.Of<Cancel>()));

		_ = this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(3, 2, 1), 42f);
		_ = this.kineticController.Follow(new Vector3(100, 200, 300), () => new Vector3(3, 2, 1), 42f);

		await this.game.Frames(1);

		Assert.Equal(new Vector3(100, 200, 300), this.kineticController.Entity.Transform.Position);

		await this.game.Frames(4);

		Assert.Equal(new Vector3(104, 200, 300), this.kineticController.Entity.Transform.Position);
	}

	[Fact]
	public async void StopCoroutineOnCollision() {
		IEnumerable<Result<IWait>> Coroutine() {
			for (var i = 0; i < 20; ++i) {
				this.kineticController.Entity.Transform.Position.X += 0.5f;
				yield return new WaitFrame();
			}
		}
		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Result<Vector3>>>()))
			.Returns((Coroutine(), Mock.Of<Cancel>()));

		var obstacle = new Entity {
			new StaticColliderComponent {
				ColliderShape = new BoxColliderShape(is2D: false, size: new Vector3(1, 1, 1)),
			},
		};
		obstacle.Transform.Position = new Vector3(3, 0, 0);

		this.scene.Entities.Add(obstacle);
		await this.game.Frames(1);

		_ = this.kineticController.Follow(new Vector3(0, 0, 0), () => obstacle.Transform.Position, 42f);
		await this.game.Frames(11);

		Assert.InRange(this.kineticController.Entity.Transform.Position.X, 2, 2.5);

		await this.game.Frames(1);

		_ = this.kineticController.Follow(new Vector3(0, 0, 0), () => obstacle.Transform.Position, 42f);
		await this.game.Frames(11);

		Assert.InRange(this.kineticController.Entity.Transform.Position.X, 2, 2.5);
	}

	[Fact]
	public async void CallOnHitAndCancelOnCollision() {
		IEnumerable<Result<IWait>> Coroutine() {
			for (var i = 0; i < 10; ++i) {
				this.kineticController.Entity.Transform.Position.X += 1f;
				yield return new WaitFrame();
			}
		}
		var cancel = Mock.Of<Cancel>();
		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Result<Vector3>>>()))
			.Returns((Coroutine(), cancel));

		var onHit = Mock.Of<Action<PhysicsComponent>>();
		this.kineticController.OnHit += onHit;

		var obstacle = new Entity {
			new StaticColliderComponent {
				ColliderShape = new BoxColliderShape(is2D: false, size: new Vector3(1.1f, 1.1f, 1.1f)),
			},
		};
		obstacle.Transform.Position = new Vector3(3, 0, 0);

		this.scene.Entities.Add(obstacle);
		await this.game.Frames(1);

		_ = this.kineticController.Follow(new Vector3(0, 0, 0), () => obstacle.Transform.Position, 42f);
		await this.game.Frames(11);

		Mock
			.Get(onHit)
			.Verify(onHit => onHit(obstacle.Get<StaticColliderComponent>()), Times.Once);
		Mock
			.Get(cancel)
			.Verify(cancel => cancel(), Times.Once);
	}

	[Fact]
	public async void CallOnRangeLimitWhenReachingMaxRange() {
		static IEnumerable<Result<IWait>> Coroutine() {
			yield return new WaitFrame();
		}
		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Result<Vector3>>>()))
			.Returns((Coroutine(), Mock.Of<Cancel>()));

		var onRangeLimit = Mock.Of<Action>();
		this.kineticController.OnRangeLimit += onRangeLimit;
		this.kineticController.baseRange = 1;

		_ = this.kineticController.Follow(Vector3.Zero, () => Vector3.UnitX, 1);

		await this.game.Frames(1);

		Mock
			.Get(onRangeLimit)
			.Verify(onRangeLimit => onRangeLimit(), Times.Never);

		await this.game.Frames(1);

		Mock
			.Get(onRangeLimit)
			.Verify(onRangeLimit => onRangeLimit(), Times.Once);
	}

	[Fact]
	public async void NoCollider() {
		var controller = this.kineticController;
		controller.collider = null;

		_ = this.scene.Entities.Remove(controller.Entity);
		this.scene.Entities.Add(controller.Entity);
		await this.game.Frames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log((SystemError)controller.MissingField(nameof(controller.collider))), Times.Once);
	}

	[Fact]
	public async void LogMoveSystemError() {
		static IEnumerable<Result<IWait>> SystemError() {
			yield return Result.SystemError("AAAA");
		}

		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Result<Vector3>>>()))
			.Returns((SystemError(), () => Result.Ok()));

		_ = this.kineticController.Follow(Vector3.Zero, () => Vector3.Zero, 1f);
		await this.game.Frames(3);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log((SystemError)"AAAA"), Times.Once);
	}

	[Fact]
	public async void LogMovePlayerError() {
		static IEnumerable<Result<IWait>> PlayerError() {
			yield return Result.PlayerError("AAAA");
		}

		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Result<Vector3>>>()))
			.Returns((PlayerError(), () => Result.Ok()));

		_ = this.kineticController.Follow(Vector3.Zero, () => Vector3.Zero, 1f);
		await this.game.Frames(2);

		Mock
			.Get(this.playerMEssage)
			.Verify(m => m.Log((PlayerError)"AAAA"), Times.Once);
	}

	[Fact]
	public async void LogCancelErrors() {
		IEnumerable<Result<IWait>> Coroutine() {
			for (var i = 0; i < 20; ++i) {
				this.kineticController.Entity.Transform.Position.X += 0.5f;
				yield return new WaitFrame();
			}
		}
		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Result<Vector3>>>()))
			.Returns((Coroutine(), () => Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }))));

		var obstacle = new Entity {
			new StaticColliderComponent {
				ColliderShape = new BoxColliderShape(is2D: false, size: new Vector3(1, 1, 1)),
			},
		};
		obstacle.Transform.Position = new Vector3(3, 0, 0);

		this.scene.Entities.Add(obstacle);
		await this.game.Frames(1);

		_ = this.kineticController.Follow(new Vector3(0, 0, 0), () => obstacle.Transform.Position, 42f);
		await this.game.Frames(11);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log((SystemError)"AAA"));

		Mock
			.Get(this.playerMEssage)
			.Verify(m => m.Log((PlayerError)"BBB"));
	}

	[Fact]
	public void ReturnMovePrepareCoroutineErrors() {
		_ = Mock
			.Get(this.move)
			.Setup(m => m.PrepareCoroutineFor(It.IsAny<Entity>(), It.IsAny<FSpeedToDelta>()))
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" })));

		var result = this.kineticController.Follow(new Vector3(0, 0, 0), () => Vector3.Zero, 42f);
		var errors = result.Switch(
			errors => $"{(string)errors.system.First()}, {(string)errors.player.First()}",
			() => "no errors"
		);

		Assert.Equal("AAA, BBB", errors);
	}

	[Fact]
	public void GetTargetErrors() {
		var result = this.kineticController.Follow(
			new Vector3(0, 0, 0),
			() => Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" })),
			42f
		);
		var errors = result.Switch(
			errors => $"{(string)errors.system.First()}, {(string)errors.player.First()}",
			() => "no errors"
		);
		Assert.Equal("AAA, BBB", errors);
	}
}
