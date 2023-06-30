namespace ProjectZyheeda;

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

[DataContract]
public class AnimatedMove : IAnimatedMoveEditor {
	public static readonly string fallbackAnimationKey = "default";

	public IMoveEditor? move;
	public string animationKey = "";

	private static Func<string, Result<IWait>> RunAnimation(Func<string, Result> playAnimation) {
		return key => playAnimation(key).Map<IWait>(() => new NoWait());
	}

	private static Cancel GetCancel(Cancel cancelMove, Func<string, Result<IWait>> play) {
		Result Cancel() {
			return cancelMove()
				.FlatMap(() => play(AnimatedMove.fallbackAnimationKey));
		};

		return Cancel;
	}

	private Coroutine GetRun(Coroutine runMove, Func<string, Result<IWait>> play) {
		Coroutine Coroutine() {
			var lastAnimationKey = this.animationKey;

			bool AnimationKeyChanged() {
				if (this.animationKey != lastAnimationKey) {
					lastAnimationKey = this.animationKey;
					return true;
				}
				return false;
			}

			yield return play(this.animationKey);
			foreach (var step in runMove) {
				if (AnimationKeyChanged()) {
					yield return play(this.animationKey);
				}
				yield return step;
			}
			yield return play(AnimatedMove.fallbackAnimationKey);
		};

		return Coroutine();
	}

	private Result<FGetCoroutine> GetCoroutine(FGetCoroutine innerGetCoroutine, Func<string, Result<IWait>> play) {
		FGetCoroutine getCoroutine = (Func<Vector3> getTarget) => {
			var (runMove, cancelMove) = innerGetCoroutine(getTarget);
			var run = this.GetRun(runMove, play);
			var cancel = AnimatedMove.GetCancel(cancelMove, play);
			return (run, cancel);
		};
		return getCoroutine;
	}

	public Result<FGetCoroutine> PrepareCoroutineFor(
		Entity agent,
		FSpeedToDelta delta,
		Func<string, Result> playAnimation
	) {
		var getCoroutine =
			(FGetCoroutine innerGetCoroutine) =>
			(Func<string, Result<IWait>> play) =>
				this.GetCoroutine(innerGetCoroutine, play);

		var innerGetCoroutine = this.move
			.OkOrSystemError(this.MissingField(nameof(this.move)))
			.FlatMap(move => move.PrepareCoroutineFor(agent, delta));

		return getCoroutine
			.Apply(innerGetCoroutine)
			.Apply(AnimatedMove.RunAnimation(playAnimation))
			.Flatten();
	}

	public Result<OldAnimationKey> SetAnimation(string animationKey) {
		var oldAnimationKey = this.animationKey;
		this.animationKey = animationKey;
		return oldAnimationKey;
	}

	public Result<OldSpeed> SetSpeed(float unitsPerSecond) {
		return this.move
			.OkOrSystemError(this.MissingField(nameof(this.move)))
			.FlatMap(m => m.SetSpeed(unitsPerSecond));
	}
}
