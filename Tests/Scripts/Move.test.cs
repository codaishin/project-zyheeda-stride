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

	[Test]
	public void MoveTowardsTarget100() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(targets);
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
		var targets = new U<Vector3, Entity>[] { target }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		target.Transform.Position = new Vector3(1, 0, 0);
		behavior.ExecuteNext(targets);
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
		var targets = new U<Vector3, Entity>[] { target }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		target.Transform.Position = new Vector3(1, 0, 0);

		behavior.ExecuteNext(targets);
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
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(targets);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(5);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetFaster() {
		var targets = new U<Vector3, Entity>[] { new Vector3(100, 0, 0) }.ToAsyncEnumerable();

		this.moveComponent.speed = 42;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(targets);
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
		var targets = new U<Vector3, Entity>[] { new Vector3(100, 0, 0) }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(targets);
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
		var targets = new U<Vector3, Entity>[] { new Vector3(0, -1, 0) }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(targets);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(5);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(0, -distance, 0)).Using(this.tolerance));
	}

	[Test]
	public void UseMultipleTargetsAsMoveWaypoints() {
		var waypoints = new List<Vector3>();
		var targets = new U<Vector3, Entity>[]{
			new Vector3(1, 0, 0),
			new Vector3(1, 1, 0),
		}.ToAsyncEnumerable();

		this.moveComponent.speed = 100;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(targets);

		this.game.WaitFrames(2);
		waypoints.Add(this.agent.Transform.Position);

		this.game.WaitFrames(1);
		waypoints.Add(this.agent.Transform.Position);

		Assert.That(waypoints, Is.EqualTo(new[] { new Vector3(1, 0, 0), new Vector3(1, 1, 0) }));
	}

	[Test]
	public void MoveTowardsTargetFromOffsetPosition() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 1, 0) }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		this.agent.Transform.Position = new Vector3(1, 0, 0);

		behavior.ExecuteNext(targets);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(5);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(1, distance, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetWithNotNormalizedInitialDistance() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 1, 0) }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(targets);
		this.game.WaitFrames(1);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(5);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var target = new Vector3(1, 1, 0);
		target.Normalize();

		Assert.That(
			this.agent.Transform.Position,
			Is.EqualTo(target * distance).Using(this.tolerance)
		);
	}

	[Test]
	public void DoNotOvershoot() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

		this.moveComponent.speed = 100_000;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(targets);
		this.game.WaitFrames(1);

		this.game.WaitFrames(2);

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(1, 0, 0)));
	}

	[Test]
	public void SuspendBehaviorWhenResetAndIdleCalled() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(targets);
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
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ResetAndIdle();

		behavior.ExecuteNext(targets);
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
		var targetsA = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();
		var targetsB = new U<Vector3, Entity>[] { new Vector3(-1, 0, 0) }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(targetsA);
		behavior.ExecuteNext(targetsB);
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
	public async Task DoNotBlockNewWaypoints() {
		var startFrame = this.game.UpdateTime.FrameCount;

		var token = new TaskCompletionSource<List<int>>();
		async IAsyncEnumerable<U<Vector3, Entity>> getTargets() {
			var frames = new List<int>();
			_ = await this.game.Script.NextFrame();
			yield return new Vector3(1, 0, 0);
			frames.Add(this.game.UpdateTime.FrameCount);
			_ = await this.game.Script.NextFrame();
			yield return new Vector3(1, 1, 0);
			frames.Add(this.game.UpdateTime.FrameCount);
			token.SetResult(frames);
		}

		this.moveComponent.speed = 100;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		_ = behavior.ExecuteNext(getTargets());

		var frames = await token.Task;
		var offset = 2;

		Assert.That(
			frames,
			Is.EqualTo(new[] { startFrame + offset, startFrame + 1 + offset })
		);
	}

	[Test]
	public void LookAtTarget() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(targets);
		this.game.WaitFrames(1);

		this.game.WaitFrames(2);

		Assert.That(
			this.agent.Transform.Rotation,
			Is.EqualTo(Quaternion.LookRotation(Vector3.UnitX, Vector3.UnitY))
		);
	}

	[Test]
	public void LookAtTargetFromOffset() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

		this.agent.Transform.Position = new Vector3(3, 0, 0);

		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(targets);
		this.game.WaitFrames(1);

		this.game.WaitFrames(2);

		Assert.That(
			this.agent.Transform.Rotation,
			Is.EqualTo(Quaternion.LookRotation(-Vector3.UnitX, Vector3.UnitY))
		);
	}

	[Test]
	public void NoRotationChangeWhenTargetIsCurrentPosition() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

		this.agent.Transform.Position = new Vector3(1, 0, 0);

		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		var expectedRotation = this.agent.Transform.Rotation;

		behavior.ExecuteNext(targets);
		this.game.WaitFrames(1);

		this.game.WaitFrames(2);

		Assert.That(
			this.agent.Transform.Rotation,
			Is.EqualTo(expectedRotation)
		);
	}

	[Test]
	public void PlayAnimationWalk() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.Play(this.agentAnimation, It.IsAny<string>()))
			.Returns(Maybe.None<IPlayingAnimation>());

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		this.game.WaitFrames(1);

		behavior.ExecuteNext(targets);

		this.game.WaitFrames(10);

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, "walk"), Times.Once);
	}

	[Test]
	public void PlayAnimationRun() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();
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

		behavior.ExecuteNext(targets);

		this.game.WaitFrames(10);

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, "run"), Times.Once);
	}

	[Test]
	public void PlayIdle() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.Play(this.agentAnimation, It.IsAny<string>()))
			.Returns(Maybe.None<IPlayingAnimation>());

		this.moveComponent.speed = 100_000;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		this.game.WaitFrames(1);

		behavior.ExecuteNext(targets);

		this.game.WaitFrames(10);

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, Move.fallbackAnimationKey), Times.Once);
	}

	[Test]
	public void PlayWalkWithMultipleWaypoints() {
		var targets = new U<Vector3, Entity>[] {
			new Vector3(1, 0, 0),
			new Vector3(1, 1, 0)
		}.ToAsyncEnumerable();

		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.Play(this.agentAnimation, It.IsAny<string>()))
			.Returns(Maybe.None<IPlayingAnimation>());
		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.IsPlaying(this.agentAnimation, It.IsAny<string>()))
			.Returns(false);

		this.moveComponent.speed = 100_000;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		this.game.WaitFrames(1);

		behavior.ExecuteNext(targets);

		this.game.WaitFrames(10);

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, "walk"), Times.Exactly(2));
		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, Move.fallbackAnimationKey), Times.Once);
	}

	[Test]
	public void DoNotPlayIfPlaying() {
		var targets = new U<Vector3, Entity>[] {
			new Vector3(1, 0, 0),
			new Vector3(1, 1, 0),
		}.ToAsyncEnumerable();

		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.Play(this.agentAnimation, It.IsAny<string>()))
			.Returns(Maybe.None<IPlayingAnimation>());
		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.IsPlaying(this.agentAnimation, It.IsAny<string>()))
			.Returns(true);

		this.moveComponent.speed = 100_000;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		this.game.WaitFrames(1);

		behavior.ExecuteNext(targets);

		this.game.WaitFrames(10);

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, "walk"), Times.Never);
		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, Move.fallbackAnimationKey), Times.Never);
	}

	[Test]
	public void DoNotPlayRunningAnimations() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

		_ = Mock
			.Get(this.getAnimation)
			.Setup(g => g.IsPlaying(this.agentAnimation, It.IsAny<string>()))
			.Returns(true);

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		this.game.WaitFrames(1);

		behavior.ExecuteNext(targets);

		this.game.WaitFrames(2);

		Mock
			.Get(this.getAnimation)
			.Verify(g => g.Play(this.agentAnimation, It.IsAny<string>()), Times.Never);
	}

	[Test]
	public void MissingAnimationComponent() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

		this.agent.Name = "Agent";
		this.agent.RemoveChild(this.agentAnimation.Entity);

		var error = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(TestMove.ErrorsToString, _ => "no error, got actual behavior");

		Assert.That(error, Is.EqualTo("Missing AnimationComponent on Agent (SystemString)"));
	}

	[Test]
	public void MissingGetAnimationService() {
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

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
		var targets = new U<Vector3, Entity>[] { new Vector3(1, 0, 0) }.ToAsyncEnumerable();

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
		var targets = new U<Vector3, Entity>[] { target }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(GetBehaviorFail, b => b);

		target.Transform.Position = new Vector3(1, 0, 0);
		var completed = await behavior.ExecuteNext(targets);

		var position = this.agent.Transform.Position;

		Assert.Multiple(() => {
			Assert.That(position, Is.EqualTo(new Vector3(1, 0, 0)));
			Assert.That(completed, Is.True);
		});
	}

	[Test, Timeout(1000)]
	public async Task DoNotWaitForCanceledMovementCompletion() {
		var firstTarget = new Entity();
		var firstTargets = new U<Vector3, Entity>[] { firstTarget }.ToAsyncEnumerable();
		var secondTarget = new Entity();
		var secondTargets = new U<Vector3, Entity>[] { firstTarget }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(GetBehaviorFail, b => b);

		firstTarget.Transform.Position = new Vector3(2, 0, 0);
		secondTarget.Transform.Position = new Vector3(0, 1, 0);

		var firstTask = behavior.ExecuteNext(firstTargets);
		_ = behavior.ExecuteNext(new U<Vector3, Entity>[] { firstTarget }.ToAsyncEnumerable());

		var firstCompleted = await firstTask;
		Assert.That(firstCompleted, Is.False);
	}

	[Test]
	public async Task NewTaskAfterOldFinished() {
		var target = new Entity();
		var targets = new U<Vector3, Entity>[] { target }.ToAsyncEnumerable();

		this.moveComponent.speed = 1;
		var behavior = this.moveComponent
			.GetBehaviorFor(this.agent)
			.Switch(GetBehaviorFail, b => b);

		target.Transform.Position = new Vector3(1, 0, 0);
		_ = await behavior.Execute(targets);

		Assert.DoesNotThrow(() => behavior.Execute(targets));
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
