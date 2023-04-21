namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;

public class TestMoveController : GameTestCollection, System.IDisposable {
	private readonly VectorTolerance tolerance = new(0.001f);
	private IGetAnimation getAnimation = Mock.Of<IGetAnimation>();
	private AnimationComponent agentAnimation = new();
	private MoveController moveComponent = new();
	private Entity agent = new();
	private Entity move = new();
	private SchedulerController scheduler = new();


	private static string ErrorsToString(IEnumerable<U<SystemStr, PlayerStr>> errors) {
		var errorsUnpacked = errors.Select(error => error.Switch(
			v => $"{v.value} ({v.GetType().Name})",
			v => $"{v.value} ({v.GetType().Name})"
		));
		return string.Join(", ", errorsUnpacked);
	}

	private static FGetCoroutine FailWithErrors(IEnumerable<U<SystemStr, PlayerStr>> errors) {
		Assert.Fail($"Errors: {TestMoveController.ErrorsToString(errors)}");
		return Mock.Of<FGetCoroutine>();
	}

	[SetUp]
	public void SetUp() {
		this.scheduler = new();

		this.getAnimation = Mock.Of<IGetAnimation>();
		this.game.Services.RemoveService<IGetAnimation>();
		this.game.Services.AddService<IGetAnimation>(this.getAnimation);

		this.agentAnimation = new AnimationComponent();
		this.agent = new Entity();
		this.agent.AddChild(new Entity { this.agentAnimation });
		this.moveComponent = new MoveController { speed = 1, playAnimation = "walk" };
		this.move = new Entity { this.moveComponent };

		Mock
			.Get(this.getAnimation)
			.SetReturnsDefault(false);
		Mock
			.Get(this.getAnimation)
			.SetReturnsDefault(Maybe.None<IPlayingAnimation>());

		this.scene.Entities.Add(new Entity { this.scheduler });
		this.scene.Entities.Add(this.move);
		this.scene.Entities.Add(this.agent);

		this.game.WaitFrames(1);
	}

	[Test]
	public void MoveTowardsTarget1() {
		var target = new Vector3(1, 0, 0);

		this.moveComponent.speed = 1;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;

		this.scheduler.Run(getCoroutine(target));
		this.game.WaitFrames(1);

		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetEntity() {
		var target = new Entity();

		this.moveComponent.speed = 1;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		target.Transform.Position = new Vector3(1, 0, 0);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;

		this.scheduler.Run(getCoroutine(target));
		this.game.WaitFrames(1);

		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetEntityAfterChangingTargetPosition() {
		var target = new Entity();

		this.moveComponent.speed = 1;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		target.Transform.Position = new Vector3(1, 0, 0);


		var start = (float)this.game.UpdateTime.Total.TotalSeconds;

		this.scheduler.Run(getCoroutine(target));
		this.game.WaitFrames(1);

		var distanceX = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		target.Transform.Position = new Vector3(0, 1, 0);

		start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(1);
		var distanceY = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(distanceX, distanceY, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetFor5Frames() {
		var target = new Vector3(1, 0, 0);

		this.moveComponent.speed = 1;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);


		var start = (float)this.game.UpdateTime.Total.TotalSeconds;

		this.scheduler.Run(getCoroutine(target));
		this.game.WaitFrames(5);

		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetFaster() {
		var target = new Vector3(100, 0, 0);

		this.moveComponent.speed = 42;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);


		var start = (float)this.game.UpdateTime.Total.TotalSeconds;

		this.scheduler.Run(getCoroutine(target));
		this.game.WaitFrames(5);

		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;
		distance *= 42f;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetWithChangingSpeed() {
		var target = new Vector3(100, 0, 0);

		this.moveComponent.speed = 1;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);


		var start = (float)this.game.UpdateTime.Total.TotalSeconds;

		this.scheduler.Run(getCoroutine(target));
		this.game.WaitFrames(1);

		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		start = (float)this.game.UpdateTime.Total.TotalSeconds;

		this.moveComponent.speed = 0.5f;
		this.game.WaitFrames(1);

		distance += ((float)this.game.UpdateTime.Total.TotalSeconds - start) * 0.5f;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTarget0Neg10() {
		var target = new Vector3(0, -1, 0);

		this.moveComponent.speed = 1;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);


		var start = (float)this.game.UpdateTime.Total.TotalSeconds;

		this.scheduler.Run(getCoroutine(target));
		this.game.WaitFrames(5);

		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(0, -distance, 0)).Using(this.tolerance));
	}


	[Test]
	public void MoveTowardsTargetFromOffsetPosition() {
		var target = new Vector3(1, 1, 0);

		this.moveComponent.speed = 1;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		this.agent.Transform.Position = new Vector3(1, 0, 0);


		var start = (float)this.game.UpdateTime.Total.TotalSeconds;

		this.scheduler.Run(getCoroutine(target));
		this.game.WaitFrames(5);

		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(1, distance, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetWithNotNormalizedInitialDistance() {
		var target = new Vector3(1, 1, 0);

		this.moveComponent.speed = 1;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;

		this.scheduler.Run(getCoroutine(target));
		this.game.WaitFrames(5);

		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var direction = new Vector3(1, 1, 0);
		direction.Normalize();

		Assert.That(
			this.agent.Transform.Position,
			Is.EqualTo(direction * distance).Using(this.tolerance)
		);
	}

	[Test]
	public void DoNotOvershoot() {
		var target = new Vector3(1, 0, 0);

		this.moveComponent.speed = 100_000;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		this.scheduler.Run(getCoroutine(target));

		this.game.WaitFrames(2);

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(1, 0, 0)));
	}


	[Test]
	public void LookAtTarget() {
		var target = new Vector3(1, 0, 0);

		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		this.scheduler.Run(getCoroutine(target));

		this.game.WaitFrames(2);

		Assert.That(
			this.agent.Transform.Rotation,
			Is.EqualTo(Quaternion.LookRotation(Vector3.UnitX, Vector3.UnitY))
		);
	}

	[Test]
	public void LookAtTargetFromOffset() {
		var target = new Vector3(1, 0, 0);

		this.agent.Transform.Position = new Vector3(3, 0, 0);

		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		this.scheduler.Run(getCoroutine(target));

		this.game.WaitFrames(2);

		Assert.That(
			this.agent.Transform.Rotation,
			Is.EqualTo(Quaternion.LookRotation(-Vector3.UnitX, Vector3.UnitY))
		);
	}

	[Test]
	public void NoRotationChangeWhenTargetIsCurrentPosition() {
		var target = new Vector3(1, 0, 0);

		this.agent.Transform.Position = new Vector3(1, 0, 0);

		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		var expectedRotation = this.agent.Transform.Rotation;

		this.scheduler.Run(getCoroutine(target));
		this.game.WaitFrames(1);

		this.game.WaitFrames(2);

		Assert.That(
			this.agent.Transform.Rotation,
			Is.EqualTo(expectedRotation)
		);
	}

	[Test]
	public void PlayAnimationWalk() {
		var target = new Vector3(1, 0, 0);

		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.Play(this.agentAnimation, It.IsAny<string>()))
			.Returns(Maybe.None<IPlayingAnimation>());

		this.moveComponent.speed = 1;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		this.game.WaitFrames(1);

		this.scheduler.Run(getCoroutine(target));
		this.game.WaitFrames(10);

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, "walk"), Times.Once);
	}

	[Test]
	public void PlayAnimationRun() {
		var target = new Vector3(1, 0, 0);
		this.moveComponent.playAnimation = "run";

		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.Play(this.agentAnimation, It.IsAny<string>()))
			.Returns(Maybe.None<IPlayingAnimation>());

		this.moveComponent.speed = 1;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		this.game.WaitFrames(1);

		this.scheduler.Run(getCoroutine(target));
		this.game.WaitFrames(10);

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, "run"), Times.Once);
	}

	[Test]
	public void PlayIdle() {
		var target = new Vector3(1, 0, 0);

		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.Play(this.agentAnimation, It.IsAny<string>()))
			.Returns(Maybe.None<IPlayingAnimation>());

		this.moveComponent.speed = 100_000;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		this.game.WaitFrames(1);

		this.scheduler.Run(getCoroutine(target));

		this.game.WaitFrames(10);

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, MoveController.fallbackAnimationKey), Times.Once);
	}

	[Test]
	public void DoNotPlayRunningAnimations() {
		var target = new Vector3(1, 0, 0);

		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.IsPlaying(this.agentAnimation, It.IsAny<string>()))
			.Returns(true);

		this.moveComponent.speed = 1;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		this.game.WaitFrames(1);

		this.scheduler.Run(getCoroutine(target));

		this.game.WaitFrames(2);

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, It.IsAny<string>()), Times.Never);
	}

	[Test]
	public void PlayIdleOnCancel() {
		var target = new Vector3(1, 0, 0);

		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.Play(this.agentAnimation, It.IsAny<string>()))
			.Returns(Maybe.None<IPlayingAnimation>());

		this.moveComponent.speed = 100_000;
		var getCoroutine = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.FailWithErrors, getCoroutine => getCoroutine);

		this.game.WaitFrames(1);
		var (run, cancel) = getCoroutine(target);

		this.scheduler.Run((run, cancel));

		this.game.WaitFrames(1);

		cancel();

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, MoveController.fallbackAnimationKey), Times.Once);
	}

	[Test]
	public void MissingAnimationComponent() {
		this.agent.Name = "Agent";
		this.agent.RemoveChild(this.agentAnimation.Entity);

		var error = this.moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.ErrorsToString, _ => "no error, got actual behavior");

		Assert.That(error, Is.EqualTo($"Missing AnimationComponent on Agent ({nameof(SystemStr)})"));
	}

	[Test]
	public void NoGetAnimationServiceBeforeStart() {
		this.game.Services.RemoveService<IGetAnimation>();

		var moveComponent = new MoveController();
		this.scene.Entities.Add(new Entity { moveComponent });

		var error = moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.ErrorsToString, _ => "no error, got actual behavior");

		Assert.That(error, Is.EqualTo($"No IGetAnimation assigned ({nameof(SystemStr)})"));
	}

	[Test]
	public void MissingGetAnimationService() {
		this.game.Services.RemoveService<IGetAnimation>();

		var moveComponent = new MoveController();
		this.scene.Entities.Add(new Entity { moveComponent });

		this.game.WaitFrames(1);

		var error = moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.ErrorsToString, _ => "no error, got actual behavior");

		Assert.That(error, Is.EqualTo($"Missing IGetAnimation Service ({nameof(SystemStr)})"));
	}

	[Test]
	public void MissingGetAnimationServiceAndAnimationComponent() {
		this.game.Services.RemoveService<IGetAnimation>();
		this.agent.Name = "Agent";
		this.agent.RemoveChild(this.agentAnimation.Entity);

		var moveComponent = new MoveController();
		this.scene.Entities.Add(new Entity { moveComponent });

		this.game.WaitFrames(1);

		var error = moveComponent
			.PrepareCoroutineFor(this.agent)
			.Switch(TestMoveController.ErrorsToString, _ => "no error, got actual behavior");

		Assert.Multiple(() => {
			Assert.That(error, Contains.Substring($"Missing AnimationComponent on Agent ({nameof(SystemStr)})"));
			Assert.That(error, Contains.Substring($"Missing AnimationComponent on Agent ({nameof(SystemStr)})"));
		});
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
