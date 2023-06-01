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
using Stride.Engine.Processors;

public class TestAnimatedMove {
	private IMove move = Mock.Of<IMove>();
	private AnimatedMove animatedMove = new();
	private FGetCoroutine getCoroutine = Mock.Of<FGetCoroutine>();
	private Func<IEnumerable<Result<IWait>>> run = Mock.Of<Func<IEnumerable<Result<IWait>>>>();
	private Cancel cancel = Mock.Of<Cancel>();

	private class MockWait : IWait {
		public Task Wait(ScriptSystem script) {
			throw new NotImplementedException();
		}
	}

	private static FGetCoroutine AssertFail((IEnumerable<SystemError> system, IEnumerable<PlayerError> player) errors) {
		throw new AssertionException((
			string.Join(", ", errors.player),
			string.Join(", ", errors.system)
		).ToString());
	}

	[SetUp]
	public void SetUp() {
		this.animatedMove = new() { move = this.move = Mock.Of<IMove>() };
		this.getCoroutine = Mock.Of<FGetCoroutine>();
		this.run = Mock.Of<Func<IEnumerable<Result<IWait>>>>();
		this.cancel = Mock.Of<Cancel>();

		Mock
			.Get(this.animatedMove.move)
			.SetReturnsDefault(this.getCoroutine);

		Mock
			.Get(this.getCoroutine)
			.SetReturnsDefault<(Func<IEnumerable<Result<IWait>>>, Cancel)>((this.run, this.cancel));

		Mock
			.Get(this.run)
			.SetReturnsDefault<IEnumerable<Result<IWait>>>(new Result<IWait>[] { new MockWait(), new MockWait() });
	}

	[Test]
	public void UseMovesGetCoroutine() {
		var agent = new Entity();
		var delta = Mock.Of<FSpeedToDelta>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(agent, delta, _ => Result.Ok()).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);

		Mock
			.Get(this.move)
			.Verify(m => m.PrepareCoroutineFor(agent, delta), Times.Once);

		var (run, cancel) = getCoroutine(() => new Vector3(1, 2, 3));

		Mock
			.Get(this.getCoroutine)
			.Verify(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()), Times.Once);

		var waits = run().ToArray();
		var innerWaits = waits.Skip(1).Take(waits.Length - 2);

		Assert.That(innerWaits.Select(w => w.UnpackOr(new WaitMilliSeconds(0))), Is.All.InstanceOf<MockWait>());

		_ = cancel();

		Mock
			.Get(this.cancel)
			.Verify(cancel => cancel(), Times.Once);
	}

	[Test]
	public void UseNoWaitForStartingAnimations() {
		var agent = new Entity();
		var delta = Mock.Of<FSpeedToDelta>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(agent, delta, _ => Result.Ok()).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);

		Mock
			.Get(this.move)
			.Verify(m => m.PrepareCoroutineFor(agent, delta), Times.Once);

		var (run, cancel) = getCoroutine(() => new Vector3(1, 2, 3));

		Mock
			.Get(this.getCoroutine)
			.Verify(getCoroutine => getCoroutine(It.IsAny<Func<Vector3>>()), Times.Once);

		var waits = run().ToArray();
		Assert.Multiple(() => {
			Assert.That(waits.First().UnpackOr(new WaitMilliSeconds(0)), Is.InstanceOf<NoWait>());
			Assert.That(waits.Last().UnpackOr(new WaitMilliSeconds(0)), Is.InstanceOf<NoWait>());
		});
	}

	[Test]
	public void PlayAnimationWalk() {
		var target = new Vector3(1, 0, 0);
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0f, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);
		var (run, _) = getCoroutine(() => target);
		var runner = run().GetEnumerator();

		this.animatedMove.animationKey = "walk";
		_ = runner.MoveNext();

		Mock
			.Get(play)
			.Verify(func => func("walk"), Times.Once);
	}

	[Test]
	public void PlayAnimationRun() {
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0f, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);
		var (run, _) = getCoroutine(() => new Vector3(1, 0, 0));
		var runner = run().GetEnumerator();

		this.animatedMove.animationKey = "run";
		_ = runner.MoveNext();

		Mock
			.Get(play)
			.Verify(func => func("run"), Times.Once);
	}

	[Test]
	public void PlayAnimationRunError() {
		var play = (string _) => (Result)Result.PlayerError("ZZZ");
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0f, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);
		var (run, _) = getCoroutine(() => new Vector3(1, 0, 0));
		var runner = run().GetEnumerator();

		this.animatedMove.animationKey = "run";
		_ = runner.MoveNext();

		var result = runner.Current;
		result.Switch(
			errors => Assert.That((string)errors.player.First(), Is.EqualTo("ZZZ")),
			_ => Assert.Fail("NO ERRORS")
		);
	}

	[Test, Timeout(1000)]
	public void PlayAnimationIdleOnDone() {
		var target = new Vector3(0.3f, 0, 0);
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0.1f, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);
		var (run, _) = getCoroutine(() => target);
		var runner = run().GetEnumerator();

		while (runner.MoveNext()) { }

		Mock
			.Get(play)
			.Verify(func => func(AnimatedMove.fallbackAnimationKey), Times.Once);
	}

	[Test, Timeout(1000)]
	public void PlayAnimationErrorOnDone() {
		var target = new Vector3(0.3f, 0, 0);
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0.1f, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);
		var (run, _) = getCoroutine(() => target);

		_ = Mock
			.Get(play)
			.SetupSequence(p => p(It.IsAny<string>()))
			.Returns(Result.Ok())
			.Returns(Result.PlayerError("UUU"));

		var waits = run().ToArray();

		waits.Last().Switch(
			errors => Assert.That((string)errors.player.First(), Is.EqualTo("UUU")),
			_ => Assert.Fail("NO ERRORS")
		);
	}

	[Test]
	public void PlayIdleOnCancel() {
		var target = new Vector3(1, 0, 0);
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);
		var (run, cancel) = getCoroutine(() => target);
		var runner = run().GetEnumerator();

		_ = runner.MoveNext();
		_ = cancel();

		Mock
			.Get(play)
			.Verify(func => func(AnimatedMove.fallbackAnimationKey), Times.Once);
	}

	[Test]
	public void ReturnInnerCancelResult() {
		var target = new Vector3(1, 0, 0);
		var play = Mock.Of<Func<string, Result>>();
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);

		_ = Mock
			.Get(this.cancel)
			.Setup(c => c()).Returns(Result.PlayerError("AAA"));

		var (_, cancel) = getCoroutine(() => target);

		var error = cancel().Switch<string>(
			errors => errors.player.First(),
			() => "no error"
		);
		Assert.That(error, Is.EqualTo("AAA"));
	}

	[Test]
	public void ReturnPlayAnimationError() {
		var target = new Vector3(1, 0, 0);
		var play = (string _) => (Result)Result.PlayerError("AAA");
		var getCoroutine = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0, play).Switch(
			errors => TestAnimatedMove.AssertFail(errors),
			value => value
		);

		var (_, cancel) = getCoroutine(() => target);

		var error = cancel().Switch<string>(
			errors => errors.player.First(),
			() => "no error"
		);
		Assert.That(error, Is.EqualTo("AAA"));
	}

	[Test]
	public void NoMoveSet() {
		this.animatedMove.move = null;
		var play = Mock.Of<Func<string, Result>>();
		var (system, _) = this.animatedMove.PrepareCoroutineFor(new Entity(), _ => 0, play).Switch(
			error => error,
			value => throw new AssertionException("had a value, but shouldn't have had one")
		);

		Assert.That(
			system,
			Contains.Item((SystemError)this.animatedMove.MissingField(nameof(this.animatedMove.move)))
		);
	}
}
