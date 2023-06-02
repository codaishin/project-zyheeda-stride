namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;

public class TestMove : System.IDisposable {
	private readonly VectorTolerance tolerance = new(0.001f);
	private Entity agent = new();
	private StraightMove move = new();

	[SetUp]
	public void SetUp() {
		this.agent = new();
		this.move = new();
	}

	private static FGetCoroutine Fail((IEnumerable<SystemError> system, IEnumerable<PlayerError> player) errors) {
		throw new AssertionException((
			string.Join(", ", errors.system.Select(e => (string)e)),
			string.Join(", ", errors.player.Select(e => (string)e))
		).ToString());
	}

	[Test]
	public void MoveTowardsTarget() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.1f).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target);
		var runner = run().GetEnumerator();

		_ = runner.MoveNext();
		_ = runner.MoveNext();
		_ = runner.MoveNext();

		Assert.That(this.agent.Transform.Position, Is.EqualTo(new Vector3(0.3f, 0, 0)));
	}

	[Test, Timeout(1000)]
	public void YieldsWaitFrames() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.1f).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target);

		Assert.That(run().Select(w => w.UnpackOr(new WaitMilliSeconds(0))), Is.All.InstanceOf<WaitFrame>());
	}

	[Test]
	public void UseSpeedToGetDelta() {
		var target = new Vector3(1, 0, 0);
		var delta = Mock.Of<FSpeedToDelta>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, delta).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target);
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
		var delta = Mock.Of<FSpeedToDelta>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, delta).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target);
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
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.1f).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target.Transform.Position);
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
		var delta = Mock.Of<FSpeedToDelta>();
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, delta).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target);
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
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.2f).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target);
		var runner = run().GetEnumerator();

		_ = runner.MoveNext();
		_ = runner.MoveNext();

		var position = this.agent.Transform.Position;
		Assert.That(position, Is.EqualTo(new Vector3(0, -0.4f, 0)).Using(this.tolerance));
	}


	[Test]
	public void MoveTowardsTargetFromOffsetPosition() {
		var target = new Vector3(1, 1, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.3f).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target);
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
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.3f).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target);
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
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0.8f).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target);
		var runner = run().GetEnumerator();

		_ = runner.MoveNext();
		_ = runner.MoveNext();

		Assert.That(this.agent.Transform.Position, Is.EqualTo(new Vector3(1, 0, 0)));
	}


	[Test]
	public void LookAtTarget() {
		var target = new Vector3(1, 0, 0);
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0f).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target);
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
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0f).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target);
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
		var getCoroutine = this.move.PrepareCoroutineFor(this.agent, _ => 0f).Switch(
			TestMove.Fail,
			getCoroutine => getCoroutine
		);
		var (run, _) = getCoroutine(() => target);
		var runner = run().GetEnumerator();
		var expectedRotation = this.agent.Transform.Rotation;

		this.agent.Transform.Position = new Vector3(1, 0, 0);
		_ = runner.MoveNext();

		Assert.That(this.agent.Transform.Rotation, Is.EqualTo(expectedRotation));
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}
}
