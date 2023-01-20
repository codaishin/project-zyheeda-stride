namespace Tests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;

public class TestInput : GameTestCollection {
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

	protected Func<bool> TrueUntilFrame(int frame) {
		return () => this.game.UpdateTime.FrameCount < frame;
	}

	protected Func<IAsyncEnumerable<U<Vector3, Entity>>, U<Vector3, Entity>[]> Unpack(
		int maxCount = int.MaxValue
	) {
		return asyncTargets => {
			var targets = new List<U<Vector3, Entity>>();
			var token = new TaskCompletionSource();
			_ = this.game.Script.AddTask(
				async () => {
					await foreach (var target in asyncTargets) {
						targets.Add(target);
						if (targets.Count >= maxCount) {
							break;
						}
					}
					token.SetResult();
				}
			);

			token.Task.Wait();
			return targets.ToArray();
		};
	}

	protected (U<Vector3, Entity>, int)[] UnpackWithFrame(
		IAsyncEnumerable<U<Vector3, Entity>> asyncTargets
	) {
		var targets = new List<(U<Vector3, Entity>, int)>();
		var token = new TaskCompletionSource();
		_ = this.game.Script.AddTask(
			async () => {
				await foreach (var target in asyncTargets) {
					targets.Add((target, this.game.UpdateTime.FrameCount));
				}
				token.SetResult();
			}
		);

		token.Task.Wait();
		return targets.ToArray();
	}

	[Test]
	public void OnPressOneTarget() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some((U<Vector3, Entity>)new Vector3(1, 2, 3)));

		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsPressed(InputKeys.MouseLeft))
			.Returns(this.TrueInFrames(this.game.UpdateTime.FrameCount));

		var input = new Input {
			key = InputKeys.MouseLeft,
			mode = InputMode.OnPress,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack())
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.EqualTo(new U<Vector3, Entity>[] { new Vector3(1, 2, 3) }));
	}

	[Test]
	public void OnPressNoTargetHit() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.None<U<Vector3, Entity>>());

		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsPressed(InputKeys.MouseLeft))
			.Returns(this.TrueInFrames(this.game.UpdateTime.FrameCount));

		var input = new Input {
			key = InputKeys.MouseLeft,
			mode = InputMode.OnPress,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack())
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.EqualTo(Array.Empty<U<Vector3, Entity>>()));
	}

	[Test]
	public void OnPressNone() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some((U<Vector3, Entity>)new Vector3(1, 2, 3)));

		var input = new Input {
			key = InputKeys.MouseLeft,
			mode = InputMode.OnPress,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack())
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.Empty);
	}

	[Test]
	public void OnReleaseOneTarget() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some((U<Vector3, Entity>)new Vector3(1, 2, 3)));

		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsReleased(InputKeys.MouseRight))
			.Returns(this.TrueInFrames(this.game.UpdateTime.FrameCount));

		var input = new Input {
			key = InputKeys.MouseRight,
			mode = InputMode.OnRelease,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack())
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.EqualTo(new U<Vector3, Entity>[] { new Vector3(1, 2, 3) }));
	}

	[Test]
	public void OnReleaseNone() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some((U<Vector3, Entity>)new Vector3(1, 2, 3)));

		var input = new Input {
			key = InputKeys.MouseRight,
			mode = InputMode.OnRelease,
		};
		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack())
			.UnpackOr(Array.Empty<U<Vector3, Entity>>());

		Assert.That(targets, Is.Empty);
	}

	[Test]
	public void PressAndHoldMultipleTargets() {
		var entity = new Entity();
		var targetsDefinition = new U<Vector3, Entity>[] {
			new Vector3(1, 2, 3),
			entity,
			new Vector3(3, 2, 1),
		};
		var index = 0;

		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(
				() => index < targetsDefinition.Length
					? Maybe.Some(targetsDefinition[index++])
					: Maybe.None<U<Vector3, Entity>>()
			);

		var thisFrame = this.game.UpdateTime.FrameCount;
		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsReleased(InputKeys.MouseRight))
			.Returns(this.TrueInFrames(thisFrame, thisFrame + 3, thisFrame + 10));

		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsDown(InputKeys.ShiftLeft))
			.Returns(this.TrueUntilFrame(thisFrame + 100));

		var input = new Input {
			key = InputKeys.MouseRight,
			mode = InputMode.OnRelease,
			hold = InputKeys.ShiftLeft,
		};

		var targets = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.UnpackWithFrame)
			.UnpackOr(Array.Empty<(U<Vector3, Entity>, int)>());

		Assert.That(
			targets,
			Is.EqualTo(
				new (U<Vector3, Entity>, int)[] {
					(new Vector3(1, 2, 3), thisFrame + 1),
					(entity, thisFrame + 3),
					(new Vector3(3, 2, 1), thisFrame + 10),
				}
			)
		);
	}

	[Test]
	public void NewGetTargetsBlockedDuringHold() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some<U<Vector3, Entity>>(new Vector3(1, 2, 3)));

		var thisFrame = this.game.UpdateTime.FrameCount;
		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsPressed(InputKeys.MouseRight))
			.Returns(true);

		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsDown(InputKeys.ShiftLeft))
			.Returns(this.TrueUntilFrame(thisFrame + 100));

		var input = new Input {
			key = InputKeys.MouseRight,
			mode = InputMode.OnPress,
			hold = InputKeys.ShiftLeft,
		};

		_ = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack(1));

		var newTargets = input
			.GetTargets(this.inputManager)
			.Map(_ => true)
			.UnpackOr(false);

		Assert.That(newTargets, Is.False);
	}

	[Test]
	public void IfNotHoldingUnblockNewGetTargetsCall() {
		_ = Mock.Get(this.getTarget)
			.Setup(g => g.GetTarget())
			.Returns(Maybe.Some<U<Vector3, Entity>>(new Vector3(1, 2, 3)));

		var thisFrame = this.game.UpdateTime.FrameCount;
		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsPressed(InputKeys.MouseRight))
			.Returns(true);

		_ = Mock.Get(this.inputManager)
			.Setup(i => i.IsDown(InputKeys.ShiftLeft))
			.Returns(this.TrueUntilFrame(this.game.UpdateTime.FrameCount + 2));

		var input = new Input {
			key = InputKeys.MouseRight,
			mode = InputMode.OnPress,
			hold = InputKeys.ShiftLeft,
		};

		_ = input
			.GetTargets(this.inputManager)
			.Map(getTargets => getTargets(this.getTarget, this.game.Script))
			.Map(this.Unpack());

		var newTargets = input
			.GetTargets(this.inputManager)
			.Map(_ => true)
			.UnpackOr(false);

		Assert.That(newTargets, Is.True);
	}
}
