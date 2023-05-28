namespace Tests;

using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

public class TestKineticController : GameTestCollection {
	private class MockKineticController : BaseKineticController<IMove> {
		public MockKineticController(IMove move) : base(move) { }
	}

	private MockKineticController kineticController = new(move: Mock.Of<IMove>());
	private PhysicsComponent collider = new RigidbodyComponent();
	private FGetCoroutine getCoroutine = Mock.Of<FGetCoroutine>();
	private ISystemMessage systemMessage = Mock.Of<ISystemMessage>();

	[SetUp]
	public void Setup() {
		this.systemMessage = Mock.Of<ISystemMessage>();
		this.game.Services.RemoveService<ISystemMessage>();
		this.game.Services.AddService<ISystemMessage>(this.systemMessage);

		this.game.WaitFrames(1);

		this.collider = new RigidbodyComponent {
			IsKinematic = true,
			ColliderShape = new SphereColliderShape(is2D: false, radiusParam: 1f)
		};
		this.kineticController = new(move: Mock.Of<IMove>()) {
			collider = this.collider,
		};
		this.getCoroutine = Mock.Of<FGetCoroutine>();
		(Func<IEnumerable<IWait>>, Action) coroutine = (() => Array.Empty<IWait>(), Mock.Of<Action>());

		Mock
			.Get(this.getCoroutine)
			.SetReturnsDefault<(Func<IEnumerable<IWait>>, Action)>(coroutine);

		Mock
			.Get(this.kineticController.move)
			.SetReturnsDefault<FGetCoroutine>(this.getCoroutine);

		this.Scene.Entities.Add(new Entity{
			this.collider,
			this.kineticController,
		});
		this.game.WaitFrames(1);
	}

	[Test]
	public void PreparedCoroutineEntity() {
		this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(1, 1, 1), 42f);

		Mock
			.Get(this.kineticController.move)
			.Verify(m => m.PrepareCoroutineFor(this.kineticController.Entity, It.IsAny<FSpeedToDelta>()), Times.Once);
	}

	[Test]
	public void PreparedCoroutineDeltaFunc() {
		var speedToDelta = null as FSpeedToDelta;

		_ = Mock
			.Get(this.kineticController.move)
			.Setup(m => m.PrepareCoroutineFor(It.IsAny<Entity>(), It.IsAny<FSpeedToDelta>()))
			.Returns<Entity, FSpeedToDelta>((_, _speedToDelta) => {
				speedToDelta = _speedToDelta;
				return _ => (() => Array.Empty<IWait>(), Mock.Of<Action>());
			});

		this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(1, 1, 1), 42f);

		var delta = (float)this.game.UpdateTime.Elapsed.TotalSeconds;
		Assert.That(speedToDelta!(3), Is.EqualTo(delta * 3));
	}

	[Test]
	public void PreparedCoroutineDeltaFuncWithCurrentDelta() {
		var speedToDelta = null as FSpeedToDelta;

		_ = Mock
			.Get(this.kineticController.move)
			.Setup(m => m.PrepareCoroutineFor(It.IsAny<Entity>(), It.IsAny<FSpeedToDelta>()))
			.Returns<Entity, FSpeedToDelta>((_, _speedToDelta) => {
				speedToDelta = _speedToDelta;
				return _ => (() => Array.Empty<IWait>(), Mock.Of<Action>());
			});

		this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(1, 1, 1), 42f);

		this.game.WaitFrames(3);

		var delta = (float)this.game.UpdateTime.Elapsed.TotalSeconds;
		Assert.That(speedToDelta!(10), Is.EqualTo(delta * 10));
	}

	[Test]
	public void SetStartingPosition() {
		this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(3, 2, 1), 1f);

		this.game.WaitFrames(1);

		var entityPosition = this.kineticController.Entity.Transform.Position;
		Assert.That(entityPosition, Is.EqualTo(new Vector3(1, 2, 3)));
	}

	[Test]
	public void UseGetCoroutineWithAdjustedTarget() {
		this.kineticController.baseRange = 10;
		this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(2, 3, 4), 1);

		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns((Func<Vector3> getTarget) => {
				Assert.That(getTarget(), Is.EqualTo(new Vector3(1, 2, 3) + new Vector3(10, 10, 10)));
				return (() => Array.Empty<IWait>(), Mock.Of<Action>());
			});

		this.game.WaitFrames(1);

		Mock
			.Get(this.getCoroutine)
			.Verify(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()), Times.Once);
	}

	[Test]
	public void UseGetCoroutineWithAdjustedTargetScaledByRangeMultiplier() {
		this.kineticController.baseRange = 10;
		this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(2, 3, 4), 5);

		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns((Func<Vector3> getTarget) => {
				Assert.That(getTarget(), Is.EqualTo(new Vector3(1, 2, 3) + new Vector3(50, 50, 50)));
				return (() => Array.Empty<IWait>(), Mock.Of<Action>());
			});

		this.game.WaitFrames(1);

		Mock
			.Get(this.getCoroutine)
			.Verify(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()), Times.Once);
	}

	[Test]
	public void UseCoroutine() {
		IEnumerable<IWait> run() {
			yield return new WaitFrame();
			this.kineticController.Entity.Transform.Position = new Vector3(5, 3, 70);
		}

		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns((run, Mock.Of<Action>()));

		this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(3, 2, 1), 42f);

		this.game.WaitFrames(1);

		Assert.That(this.kineticController.Entity.Transform.Position, Is.EqualTo(new Vector3(1, 2, 3)));

		this.game.WaitFrames(2);

		Assert.That(this.kineticController.Entity.Transform.Position, Is.EqualTo(new Vector3(5, 3, 70)));
	}

	[Test]
	public void UseLatestFollow() {
		IEnumerable<IWait> run() {
			while (this.game.IsRunning) {
				yield return new WaitFrame();
				this.kineticController.Entity.Transform.Position.X += 1;
			}
		}

		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns((run, Mock.Of<Action>()));

		this.kineticController.Follow(new Vector3(1, 2, 3), () => new Vector3(3, 2, 1), 42f);
		this.kineticController.Follow(new Vector3(100, 200, 300), () => new Vector3(3, 2, 1), 42f);

		this.game.WaitFrames(1);

		Assert.That(this.kineticController.Entity.Transform.Position, Is.EqualTo(new Vector3(100, 200, 300)));

		this.game.WaitFrames(4);

		Assert.That(this.kineticController.Entity.Transform.Position, Is.EqualTo(new Vector3(104, 200, 300)));
	}

	[Test]
	public void StopCoroutineOnCollision() {
		IEnumerable<IWait> run() {
			for (var i = 0; i < 20; ++i) {
				this.kineticController.Entity.Transform.Position.X += 0.5f;
				yield return new WaitFrame();
			}
		}
		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns((run, Mock.Of<Action>()));

		var obstacle = new Entity {
			new StaticColliderComponent {
				ColliderShape = new BoxColliderShape(is2D: false, size: new Vector3(1, 1, 1)),
			},
		};
		obstacle.Transform.Position = new Vector3(3, 0, 0);

		this.Scene.Entities.Add(obstacle);
		this.game.WaitFrames(1);

		this.kineticController.Follow(new Vector3(0, 0, 0), () => obstacle.Transform.Position, 42f);
		this.game.WaitFrames(11);

		Assert.That(this.kineticController.Entity.Transform.Position.X, Is.InRange(2, 2.5));

		this.game.WaitFrames(1);

		this.kineticController.Follow(new Vector3(0, 0, 0), () => obstacle.Transform.Position, 42f);
		this.game.WaitFrames(11);

		Assert.That(this.kineticController.Entity.Transform.Position.X, Is.InRange(2, 2.5));
	}

	[Test]
	public void CallOnHitAndCancelOnCollision() {
		IEnumerable<IWait> run() {
			for (var i = 0; i < 10; ++i) {
				this.kineticController.Entity.Transform.Position.X += 1f;
				yield return new WaitFrame();
			}
		}
		var cancel = Mock.Of<Action>();
		_ = Mock
			.Get(this.getCoroutine)
			.Setup(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()))
			.Returns((run, cancel));

		var onHit = Mock.Of<Action<PhysicsComponent>>();
		this.kineticController.OnHit += onHit;

		var obstacle = new Entity {
			new StaticColliderComponent {
				ColliderShape = new BoxColliderShape(is2D: false, size: new Vector3(1.1f, 1.1f, 1.1f)),
			},
		};
		obstacle.Transform.Position = new Vector3(3, 0, 0);

		this.Scene.Entities.Add(obstacle);
		this.game.WaitFrames(1);

		this.kineticController.Follow(new Vector3(0, 0, 0), () => obstacle.Transform.Position, 42f);
		this.game.WaitFrames(11);

		Mock
			.Get(onHit)
			.Verify(onHit => onHit(obstacle.Get<StaticColliderComponent>()), Times.Once);
		Mock
			.Get(cancel)
			.Verify(cancel => cancel(), Times.Once);
	}

	[Test]
	public void NoCollider() {
		var controller = this.kineticController;
		controller.collider = null;

		_ = this.Scene.Entities.Remove(controller.Entity);
		this.Scene.Entities.Add(controller.Entity);
		this.game.WaitFrames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log(new SystemError(controller.MissingField(nameof(controller.collider)))), Times.Once);
	}
}
