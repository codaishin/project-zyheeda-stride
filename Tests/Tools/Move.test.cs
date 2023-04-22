namespace Tests;

using System;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;

public class TestMove {
	private readonly VectorTolerance tolerance = new(0.001f);
	private Entity agent = new();
	private Move move = new();

	[SetUp]
	public void SetUp() {
		this.agent = new();
		this.move = new();
	}

	[Test]
	public void MoveTowardsTarget() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, _ => 0.1f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		_ = runner.MoveNext();
		_ = runner.MoveNext();
		_ = runner.MoveNext();

		Assert.That(this.agent.Transform.Position, Is.EqualTo(new Vector3(0.3f, 0, 0)));
	}

	[Test]
	public void MoveTowardsTargetEntity() {
		var target = new Entity();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, _ => 0.2f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		target.Transform.Position = new Vector3(1, 0, 0);
		_ = runner.MoveNext();
		_ = runner.MoveNext();

		Assert.That(this.agent.Transform.Position, Is.EqualTo(new Vector3(0.4f, 0, 0)));
	}

	[Test, Timeout(1000)]
	public void YieldsWaitFrames() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, _ => 0.1f);
		var (run, _) = getCoroutine(target);

		Assert.That(run(), Is.All.InstanceOf<WaitFrame>());
	}

	[Test]
	public void UseSpeedToGetDelta() {
		var target = new Vector3(1, 0, 0);
		var delta = Mock.Of<IMove.FDelta>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, delta);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		_ = Mock
			.Get(delta)
			.Setup(func => func(It.IsAny<float>()))
			.Returns(0.01f);

		this.move.speed = 42;

		_ = runner.MoveNext();
		_ = runner.MoveNext();
		_ = runner.MoveNext();
		_ = runner.MoveNext();
		_ = runner.MoveNext();

		Mock
			.Get(delta)
			.Verify(func => func(42), Times.Exactly(5));
	}

	[Test]
	public void UseCurrentSpeedToGetDelta() {
		var target = new Vector3(1, 0, 0);
		var delta = Mock.Of<IMove.FDelta>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, delta);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		_ = Mock
			.Get(delta)
			.Setup(func => func(It.IsAny<float>()))
			.Returns(0.01f);

		this.move.speed = 5;

		_ = runner.MoveNext();
		_ = runner.MoveNext();

		this.move.speed = 3;

		_ = runner.MoveNext();
		_ = runner.MoveNext();
		_ = runner.MoveNext();

		Assert.Multiple(() => {
			Mock
				.Get(delta)
				.Verify(func => func(5), Times.Exactly(2));
			Mock
				.Get(delta)
				.Verify(func => func(3), Times.Exactly(3));
		});
	}

	[Test]
	public void MoveTowardsTargetEntityAfterChangingTargetPosition() {
		var target = new Entity();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, _ => 0.1f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		target.Transform.Position = new Vector3(1, 0, 0);

		_ = runner.MoveNext();
		_ = runner.MoveNext();

		target.Transform.Position = new Vector3(this.agent.Transform.Position.X, 1, 0);

		_ = runner.MoveNext();
		_ = runner.MoveNext();
		_ = runner.MoveNext();

		Assert.That(this.agent.Transform.Position, Is.EqualTo(new Vector3(0.2f, 0.3f, 0)));
	}

	[Test]
	public void MoveTowardsTargetWithChangingDeltas() {
		var target = new Vector3(1, 0, 0);
		var delta = Mock.Of<IMove.FDelta>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, delta);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		_ = Mock
			.Get(delta)
			.Setup(func => func(It.IsAny<float>()))
			.Returns(0.1f);

		_ = runner.MoveNext();
		_ = runner.MoveNext();

		_ = Mock
			.Get(delta)
			.Setup(func => func(It.IsAny<float>()))
			.Returns(0.2f);

		_ = runner.MoveNext();
		_ = runner.MoveNext();
		_ = runner.MoveNext();

		Assert.That(this.agent.Transform.Position, Is.EqualTo(new Vector3(0.8f, 0f, 0)));
	}

	[Test]
	public void MoveTowardsTarget0Neg10() {
		var target = new Vector3(0, -1, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, _ => 0.2f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		_ = runner.MoveNext();
		_ = runner.MoveNext();

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(0, -0.4f, 0)).Using(this.tolerance));
	}


	[Test]
	public void MoveTowardsTargetFromOffsetPosition() {
		var target = new Vector3(1, 1, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, _ => 0.3f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		this.agent.Transform.Position = new Vector3(1, 0, 0);
		_ = runner.MoveNext();
		_ = runner.MoveNext();

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(1, 0.6f, 0)).Using(this.tolerance));
	}

	[Test]
	public void MoveTowardsTargetWithNotNormalizedInitialDistance() {
		var target = new Vector3(1, 1, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, _ => 0.3f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		_ = runner.MoveNext();
		_ = runner.MoveNext();

		var direction = Vector3.Normalize(target);

		Assert.That(
			this.agent.Transform.Position,
			Is.EqualTo(direction * 0.6f).Using(this.tolerance)
		);
	}

	[Test]
	public void DoNotOvershoot() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, _ => 0.8f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		_ = runner.MoveNext();
		_ = runner.MoveNext();

		Assert.That(this.agent.Transform.Position, Is.EqualTo(new Vector3(1, 0, 0)));
	}


	[Test]
	public void LookAtTarget() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, _ => 0f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		_ = runner.MoveNext();

		Assert.That(
			this.agent.Transform.Rotation,
			Is.EqualTo(Quaternion.LookRotation(Vector3.UnitX, Vector3.UnitY))
		);
	}

	[Test]
	public void LookAtTargetFromOffset() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, _ => 0f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		this.agent.Transform.Position = new Vector3(3, 0, 0);
		_ = runner.MoveNext();

		Assert.That(
			this.agent.Transform.Rotation,
			Is.EqualTo(Quaternion.LookRotation(-Vector3.UnitX, Vector3.UnitY))
		);
	}

	[Test]
	public void NoRotationChangeWhenTargetIsCurrentPosition() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => { }, _ => 0f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();
		var expectedRotation = this.agent.Transform.Rotation;

		this.agent.Transform.Position = new Vector3(1, 0, 0);
		_ = runner.MoveNext();

		Assert.That(this.agent.Transform.Rotation, Is.EqualTo(expectedRotation));
	}

	[Test]
	public void PlayAnimationWalk() {
		var target = new Vector3(1, 0, 0);
		var play = Mock.Of<Action<string>>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, play, _ => 0f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		this.move.animationKey = "walk";
		_ = runner.MoveNext();

		Mock
			.Get(play)
			.Verify(func => func("walk"), Times.Once);
	}

	[Test]
	public void PlayAnimationRun() {
		var target = new Vector3(1, 0, 0);
		var play = Mock.Of<Action<string>>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, play, _ => 0f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		this.move.animationKey = "run";
		_ = runner.MoveNext();

		Mock
			.Get(play)
			.Verify(func => func("run"), Times.Once);
	}

	[Test, Timeout(1000)]
	public void PlayAnimationIdleOnDone() {
		var target = new Vector3(0.3f, 0, 0);
		var play = Mock.Of<Action<string>>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, play, _ => 0.1f);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		while (runner.MoveNext()) { }

		Mock
			.Get(play)
			.Verify(func => func(Move.fallbackAnimationKey), Times.Once);
	}

	[Test]
	public void PlayIdleOnCancel() {
		var target = new Vector3(1, 0, 0);
		var play = Mock.Of<Action<string>>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, play, _ => 0);
		var (run, cancel) = getCoroutine(target);
		var runner = run().GetEnumerator();

		_ = runner.MoveNext();
		cancel();

		Mock
			.Get(play)
			.Verify(func => func(Move.fallbackAnimationKey), Times.Once);
	}
}
