namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;

public class TestMove : GameTestCollection, System.IDisposable {
	private readonly VectorTolerance tolerance = new(0.001f);
	private IGetAnimation getAnimation = Mock.Of<IGetAnimation>();
	private AnimationComponent agentAnimation = new();
	private Move moveComponent = new();
	private Entity agent = new();
	private Entity move = new();

	private static string ErrorsToString(IEnumerable<U<SystemString, PlayerString>> errors) {
		var errorsUnpacked = errors.Select(error => error.Switch(
			v => $"{v.value} ({v.GetType().Name})",
			v => $"{v.value} ({v.GetType().Name})"
		));
		return string.Join(", ", errorsUnpacked);
	}

	private static IBehaviorStateMachine GetBehaviorFail(IEnumerable<U<SystemString, PlayerString>> errors) {
		Assert.Fail($"Errors: {TestMove.ErrorsToString(errors)}");
		return Mock.Of<IBehaviorStateMachine>();
	}

	[SetUp]
	public void SetUp() {
		this.getAnimation = Mock.Of<IGetAnimation>();
		this.game.Services.RemoveService<IGetAnimation>();
		this.game.Services.AddService<IGetAnimation>(this.getAnimation);

		this.agentAnimation = new AnimationComponent();
		this.agent = new Entity();
		this.agent.AddChild(new Entity { this.agentAnimation });
		this.moveComponent = new Move { speed = 1, playAnimation = "walk" };
		this.move = new Entity { this.moveComponent };

		Mock
			.Get(this.getAnimation)
			.SetReturnsDefault(false);
		Mock
			.Get(this.getAnimation)
			.SetReturnsDefault(Maybe.None<IPlayingAnimation>());

		this.scene.Entities.Add(this.move);
		this.scene.Entities.Add(this.agent);

		this.game.WaitFrames(1);
	}

	[Test]
	public void MoveTowardsTarget100() {
		var target = new Vector3(1, 0, 0);

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(1);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetEntity() {
		var target = new Entity();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		target.Transform.Position = new Vector3(1, 0, 0);
		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(1);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetEntityAfterChangingTargetPosition() {
		var target = new Entity();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		target.Transform.Position = new Vector3(1, 0, 0);

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
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
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(5);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetFaster() {
		var target = new Vector3(100, 0, 0);

		this.moveComponent.speed = 42;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
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
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
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
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(5);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(0, -distance, 0)).Using(this.tolerance));
	}


	[Test]
	public void MoveTowardsTargetFromOffsetPosition() {
		var target = new Vector3(1, 1, 0);

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		this.agent.Transform.Position = new Vector3(1, 0, 0);

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(5);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(1, distance, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetWithNotNormalizedInitialDistance() {
		var target = new Vector3(1, 1, 0);

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
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
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

		this.game.WaitFrames(2);

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(1, 0, 0)));
	}

	[Test]
	public void SuspendBehaviorWhenResetAndIdleCalled() {
		var target = new Vector3(1, 0, 0);

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(1);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		behavior.ResetAndIdle();

		this.game.WaitFrames(1);

		Assert.That(
			this.agent.Transform.Position,
			Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance)
		);
	}

	[Test]
	public void ExecuteNextAfterResetAndIdle() {
		var target = new Vector3(1, 0, 0);

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ResetAndIdle();

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(1);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		Assert.That(
			this.agent.Transform.Position,
			Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance)
		);
	}

	[Test]
	public void ExecuteNextOverridesLastExecution() {
		var targetA = new Vector3(1, 0, 0);
		var targetB = new Vector3(-1, 0, 0);

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		_ = behavior.Execute(targetA);
		_ = behavior.Execute(targetB);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(1);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		Assert.That(
			this.agent.Transform.Position,
			Is.EqualTo(new Vector3(-distance, 0, 0)).Using(this.tolerance)
		);
	}

	[Test]
	public void LookAtTarget() {
		var target = new Vector3(1, 0, 0);

		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

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

		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		_ = behavior.Execute(target);
		this.game.WaitFrames(1);

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

		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		var expectedRotation = this.agent.Transform.Rotation;

		_ = behavior.Execute(target);
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
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		this.game.WaitFrames(1);

		_ = behavior.Execute(target);

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
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		this.game.WaitFrames(1);

		_ = behavior.Execute(target);

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
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		this.game.WaitFrames(1);

		_ = behavior.Execute(target);

		this.game.WaitFrames(10);

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, Move.fallbackAnimationKey), Times.Once);
	}

	[Test]
	public void DoNotPlayRunningAnimations() {
		var target = new Vector3(1, 0, 0);

		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.IsPlaying(this.agentAnimation, It.IsAny<string>()))
			.Returns(true);

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		this.game.WaitFrames(1);

		_ = behavior.Execute(target);

		this.game.WaitFrames(2);

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, It.IsAny<string>()), Times.Never);
	}

	[Test]
	public void MissingAnimationComponent() {
		this.agent.Name = "Agent";
		this.agent.RemoveChild(this.agentAnimation.Entity);

		var error = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.ErrorsToString, _ => "no error, got actual behavior");

		Assert.That(error, Is.EqualTo("Missing AnimationComponent on Agent (SystemString)"));
	}

	[Test]
	public void MissingGetAnimationService() {
		this.game.Services.RemoveService<IGetAnimation>();

		var moveComponent = new Move();
		this.scene.Entities.Add(new Entity { moveComponent });

		var error = moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.ErrorsToString, _ => "no error, got actual behavior");

		Assert.That(error, Is.EqualTo("Missing IGetAnimation Service (SystemString)"));
	}

	[Test]
	public void MissingGetAnimationServiceAndAnimationComponent() {
		this.game.Services.RemoveService<IGetAnimation>();
		this.agent.Name = "Agent";
		this.agent.RemoveChild(this.agentAnimation.Entity);

		var moveComponent = new Move();
		this.scene.Entities.Add(new Entity { moveComponent });

		var error = moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.ErrorsToString, _ => "no error, got actual behavior");

		Assert.Multiple(() => {
			Assert.That(error, Contains.Substring("Missing IGetAnimation Service (SystemString)"));
			Assert.That(error, Contains.Substring("Missing AnimationComponent on Agent (SystemString)"));
		});
	}

	[Test]
	public async Task AwaitMovementCompletion() {
		var target = new Entity();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(GetBehaviorFail, b => b);

		target.Transform.Position = new Vector3(1, 0, 0);
		var completed = await behavior.Execute(target);

		var position = this.agent.Transform.Position;

		Assert.Multiple(() => {
			Assert.That(position, Is.EqualTo(new Vector3(1, 0, 0)));
			Assert.That(completed, Is.True);
		});
	}

	[Test, Timeout(1000)]
	public async Task DoNotWaitForCanceledMovementCompletion() {
		var firstTarget = new Entity();
		var secondTarget = new Entity();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(GetBehaviorFail, b => b);

		firstTarget.Transform.Position = new Vector3(2, 0, 0);
		secondTarget.Transform.Position = new Vector3(0, 1, 0);

		var firstTask = behavior.Execute(firstTarget);
		_ = behavior.Execute(secondTarget);

		var firstCompleted = await firstTask;
		Assert.That(firstCompleted, Is.False);
	}

	[Test]
	public async Task NewTaskAfterOldFinished() {
		var target = new Entity();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(GetBehaviorFail, b => b);

		target.Transform.Position = new Vector3(1, 0, 0);
		_ = await behavior.Execute(target);

		Assert.DoesNotThrow(() => behavior.Execute(target));
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
