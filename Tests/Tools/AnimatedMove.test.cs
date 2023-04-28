namespace Tests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;

public class TestAnimatedMove {
	private MockAnimatedMove animatedMove = new();
	private FGetCoroutine getCoroutine = Mock.Of<FGetCoroutine>();
	private Func<IEnumerable<IWait>> run = Mock.Of<Func<IEnumerable<IWait>>>();
	private Action cancel = Mock.Of<Action>();

	private class MockAnimatedMove : AnimatedMove<IMove> {
		public MockAnimatedMove() : base(Mock.Of<IMove>()) { }
	}

	private class MockWait : IWait {
		public Task Wait(ScriptSystem script) {
			throw new NotImplementedException();
		}
	}

	[SetUp]
	public void SetUp() {
		this.animatedMove = new();
		this.getCoroutine = Mock.Of<FGetCoroutine>();
		this.run = Mock.Of<Func<IEnumerable<IWait>>>();
		this.cancel = Mock.Of<Action>();

		Mock
			.Get(this.animatedMove.move)
			.SetReturnsDefault(this.getCoroutine);

		Mock
			.Get(this.getCoroutine)
			.SetReturnsDefault<(Func<IEnumerable<IWait>>, Action)>((this.run, this.cancel));

		Mock
			.Get(this.run)
			.SetReturnsDefault<IEnumerable<IWait>>(new IWait[] { new MockWait(), new MockWait() });
	}

	[Test]
	public void UseMovesGetCoroutine() {
		var agent = new Entity();
		var delta = Mock.Of<FSpeedToDelta>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(agent, delta, _ => { });

		Mock
			.Get(this.animatedMove.move)
			.Verify(m => m.PrepareCoroutineFor(agent, delta), Times.Once);

		var (run, cancel) = getCoroutine(new Vector3(1, 2, 3));

		Mock
			.Get(this.getCoroutine)
			.Verify(getCoroutine => getCoroutine(new Vector3(1, 2, 3)), Times.Once);

		Assert.That(run(), Is.All.InstanceOf<MockWait>());

		cancel();

		Mock
			.Get(this.cancel)
			.Verify(cancel => cancel(), Times.Once);
	}

	[Test]
	public void PlayAnimationWalk() {
		var target = new Vector3(1, 0, 0);
		var play = Mock.Of<Action<string>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0f, play);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		this.animatedMove.animationKey = "walk";
		_ = runner.MoveNext();

		Mock
			.Get(play)
			.Verify(func => func("walk"), Times.Once);
	}

	[Test]
	public void PlayAnimationRun() {
		var play = Mock.Of<Action<string>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0f, play);
		var (run, _) = getCoroutine(new Vector3(1, 0, 0));
		var runner = run().GetEnumerator();

		this.animatedMove.animationKey = "run";
		_ = runner.MoveNext();

		Mock
			.Get(play)
			.Verify(func => func("run"), Times.Once);
	}

	[Test, Timeout(1000)]
	public void PlayAnimationIdleOnDone() {
		var target = new Vector3(0.3f, 0, 0);
		var play = Mock.Of<Action<string>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0.1f, play);
		var (run, _) = getCoroutine(target);
		var runner = run().GetEnumerator();

		while (runner.MoveNext()) { }

		Mock
			.Get(play)
			.Verify(func => func(AnimatedMove.fallbackAnimationKey), Times.Once);
	}

	[Test]
	public void PlayIdleOnCancel() {
		var target = new Vector3(1, 0, 0);
		var play = Mock.Of<Action<string>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0, play);
		var (run, cancel) = getCoroutine(target);
		var runner = run().GetEnumerator();

		_ = runner.MoveNext();
		cancel();

		Mock
			.Get(play)
			.Verify(func => func(AnimatedMove.fallbackAnimationKey), Times.Once);
	}
}