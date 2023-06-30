namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;
using Xunit.Sdk;

public class TestStraightMove : IDisposable {
	private readonly VectorTolerance tolerance;
	private readonly Entity agent;
	private readonly StraightMove move;

	public TestStraightMove() {
		this.tolerance = new(0.001f);
		this.agent = new();
		this.move = new();
	}

	private static FGetCoroutine Fail((IEnumerable<SystemError> system, IEnumerable<PlayerError> player) errors) {
		throw new XunitException((
			string.Join(", ", errors.system.Select(e => (string)e)),
			string.Join(", ", errors.player.Select(e => (string)e))
		).ToString());
	}

	[Fact]
	public void MoveTowardsTarget() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.1f).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target);
		var runner = coroutine.GetEnumerator();

		_ = runner.MoveNext();
		_ = runner.MoveNext();
		_ = runner.MoveNext();

		Assert.Equal(new Vector3(0.3f, 0, 0), this.agent.Transform.Position);
	}

	[Fact(Timeout = 1000)]
	public void YieldsWaitFrames() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.1f).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target);

		Assert.All(coroutine.Select(w => w.UnpackOr(new WaitMilliSeconds(0))), w => Assert.IsType<WaitFrame>(w));
	}

	[Fact]
	public void UseSpeedToGetDelta() {
		var target = new Vector3(1, 0, 0);
		var delta = Mock.Of<FSpeedToDelta>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, delta).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target);
		var runner = coroutine.GetEnumerator();

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

	[Fact]
	public void UseCurrentSpeedToGetDelta() {
		var target = new Vector3(1, 0, 0);
		var delta = Mock.Of<FSpeedToDelta>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, delta).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target);
		var runner = coroutine.GetEnumerator();

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

		Assert.Multiple(
			() => Mock.Get(delta).Verify(func => func(5), Times.Exactly(2)),
			() => Mock.Get(delta).Verify(func => func(3), Times.Exactly(3))
		);
	}

	[Fact]
	public void MoveTowardsTargetEntityAfterChangingTargetPosition() {
		var target = new Entity();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.1f).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target.Transform.Position);
		var runner = coroutine.GetEnumerator();

		target.Transform.Position = new Vector3(1, 0, 0);

		_ = runner.MoveNext();
		_ = runner.MoveNext();

		target.Transform.Position = new Vector3(this.agent.Transform.Position.X, 1, 0);

		_ = runner.MoveNext();
		_ = runner.MoveNext();
		_ = runner.MoveNext();

		Assert.Equal(new Vector3(0.2f, 0.3f, 0), this.agent.Transform.Position);
	}

	[Fact]
	public void MoveTowardsTargetWithChangingDeltas() {
		var target = new Vector3(1, 0, 0);
		var delta = Mock.Of<FSpeedToDelta>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, delta).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target);
		var runner = coroutine.GetEnumerator();

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

		Assert.Equal(new Vector3(0.8f, 0f, 0), this.agent.Transform.Position);
	}

	[Fact]
	public void MoveTowardsTarget0Neg10() {
		var target = new Vector3(0, -1, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.2f).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target);
		var runner = coroutine.GetEnumerator();

		_ = runner.MoveNext();
		_ = runner.MoveNext();

		var position = this.agent.Transform.Position;
		Assert.Equal(new Vector3(0, -0.4f, 0), position, this.tolerance);
	}


	[Fact]
	public void MoveTowardsTargetFromOffsetPosition() {
		var target = new Vector3(1, 1, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.3f).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target);
		var runner = coroutine.GetEnumerator();

		this.agent.Transform.Position = new Vector3(1, 0, 0);
		_ = runner.MoveNext();
		_ = runner.MoveNext();

		var position = this.agent.Transform.Position;
		Assert.Equal(new Vector3(1, 0.6f, 0), position, this.tolerance);
	}

	[Fact]
	public void MoveTowardsTargetWithNotNormalizedInitialDistance() {
		var target = new Vector3(1, 1, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.3f).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target);
		var runner = coroutine.GetEnumerator();

		_ = runner.MoveNext();
		_ = runner.MoveNext();

		var direction = Vector3.Normalize(target);

		Assert.Equal(
			direction * 0.6f,
			this.agent.Transform.Position,
			this.tolerance
		);
	}

	[Fact]
	public void DoNotOvershoot() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.8f).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target);
		var runner = coroutine.GetEnumerator();

		_ = runner.MoveNext();
		_ = runner.MoveNext();

		Assert.Equal(new Vector3(1, 0, 0), this.agent.Transform.Position);
	}


	[Fact]
	public void LookAtTarget() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0f).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target);
		var runner = coroutine.GetEnumerator();

		_ = runner.MoveNext();

		Assert.Equal(
			Quaternion.LookRotation(Vector3.UnitX, Vector3.UnitY),
			this.agent.Transform.Rotation
		);
	}

	[Fact]
	public void LookAtTargetFromOffset() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0f).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target);
		var runner = coroutine.GetEnumerator();

		this.agent.Transform.Position = new Vector3(3, 0, 0);
		_ = runner.MoveNext();

		Assert.Equal(
			Quaternion.LookRotation(-Vector3.UnitX, Vector3.UnitY),
			this.agent.Transform.Rotation
		);
	}

	[Fact]
	public void NoRotationChangeWhenTargetIsCurrentPosition() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0f).Switch(
			TestStraightMove.Fail,
			getCoroutine => getCoroutine
		);
		var (coroutine, _) = getCoroutine(() => target);
		var runner = coroutine.GetEnumerator();
		var expectedRotation = this.agent.Transform.Rotation;

		this.agent.Transform.Position = new Vector3(1, 0, 0);
		_ = runner.MoveNext();

		Assert.Equal(expectedRotation, this.agent.Transform.Rotation);
	}

	[Fact]
	public void SetSpeed() {
		this.move.speed = 10;

		var oldSpeed = this.move.SetSpeed(42).UnpackOr(-1);

		Assert.Multiple(
			() => Assert.Equal(10, oldSpeed),
			() => Assert.Equal(42, this.move.speed)
		);
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
