namespace Tests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

public abstract class TestInput : GameTestCollection {
	protected IGetTarget getTarget = Mock.Of<IGetTarget>();
	protected IInputManagerWrapper inputManager = Mock.Of<IInputManagerWrapper>();

	protected Func<bool> TrueInFrames(params int[] frames) {
		var index = 0;
		return () => {
			if (index < frames.Length && this.game.UpdateTime.FrameCount == frames[index]) {
				++index;
				return true;
			}
			return false;
		};
	}

	protected U<Vector3, Entity>[] Unpack(IAsyncEnumerable<U<Vector3, Entity>> asyncTargets) {
		var targets = new List<U<Vector3, Entity>>();
		var token = new TaskCompletionSource<bool>();
		_ = this.game.Script.AddTask(
			async () => {
				await foreach (var target in asyncTargets) {
					targets.Add(target);
				}
				token.SetResult(true);
			}
		);

		token.Task.Wait();
		return targets.ToArray();
	}
}

public class TestMouseInput : TestInput {
	[Test]
	public void OnPressOneTarget() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some((U<Vector3, Entity>)new Vector3(1, 2, 3)));

		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsMouseButtonPressed(MouseButton.Left))
			.Returns(this.TrueInFrames(this.game.UpdateTime.FrameCount));

		var input = new MouseInput {
			key = MouseButton.Left,
			mode = InputMode.OnPress,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack)
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.EqualTo(new U<Vector3, Entity>[] { new Vector3(1, 2, 3) }));
	}

	[Test]
	public void OnPressNoTargetHit() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.None<U<Vector3, Entity>>());

		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsMouseButtonPressed(MouseButton.Left))
			.Returns(this.TrueInFrames(this.game.UpdateTime.FrameCount));

		var input = new MouseInput {
			key = MouseButton.Left,
			mode = InputMode.OnPress,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack)
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.EqualTo(Array.Empty<U<Vector3, Entity>>()));
	}

	[Test]
	public void OnPressNone() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some((U<Vector3, Entity>)new Vector3(1, 2, 3)));

		var input = new MouseInput {
			key = MouseButton.Left,
			mode = InputMode.OnPress,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack)
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.Empty);
	}

	[Test]
	public void OnReleaseOneTarget() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some((U<Vector3, Entity>)new Vector3(1, 2, 3)));

		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsMouseButtonReleased(MouseButton.Right))
			.Returns(this.TrueInFrames(this.game.UpdateTime.FrameCount));

		var input = new MouseInput {
			key = MouseButton.Right,
			mode = InputMode.OnRelease,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack)
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.EqualTo(new U<Vector3, Entity>[] { new Vector3(1, 2, 3) }));
	}

	[Test]
	public void OnReleaseNone() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some((U<Vector3, Entity>)new Vector3(1, 2, 3)));

		var input = new MouseInput {
			key = MouseButton.Right,
			mode = InputMode.OnRelease,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack)
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.Empty);
	}
}

public class TestKeyInput : TestInput {
	[Test]
	public void OnPressOneTarget() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some((U<Vector3, Entity>)new Vector3(1, 2, 3)));

		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsKeyPressed(Keys.B))
			.Returns(this.TrueInFrames(this.game.UpdateTime.FrameCount));

		var input = new KeyInput {
			key = Keys.B,
			mode = InputMode.OnPress,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack)
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.EqualTo(new U<Vector3, Entity>[] { new Vector3(1, 2, 3) }));
	}

	[Test]
	public void OnPressNone() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some((U<Vector3, Entity>)new Vector3(1, 2, 3)));

		var input = new KeyInput {
			key = Keys.B,
			mode = InputMode.OnPress,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack)
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.Empty);
	}

	[Test]
	public void OnReleaseOneTarget() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some((U<Vector3, Entity>)new Vector3(1, 2, 3)));

		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsKeyReleased(Keys.Up))
			.Returns(this.TrueInFrames(this.game.UpdateTime.FrameCount));

		var input = new KeyInput {
			key = Keys.Up,
			mode = InputMode.OnRelease,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack)
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.EqualTo(new U<Vector3, Entity>[] { new Vector3(1, 2, 3) }));
	}

	[Test]
	public void OnReleaseNone() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some((U<Vector3, Entity>)new Vector3(1, 2, 3)));

		var input = new KeyInput {
			key = Keys.Up,
			mode = InputMode.OnRelease,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack)
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.Empty);
	}
}
