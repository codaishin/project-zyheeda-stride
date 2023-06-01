namespace ProjectZyheeda;

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

[DataContract]
public class AnimatedMove : IAnimatedMove {
	public static readonly string fallbackAnimationKey = "default";

	public IMove? move;
	public string animationKey = "";

	private static Func<string, Result<IWait>> Run(Func<string, Result> playAnimation) {
		return key => playAnimation(key).Map<IWait>(() => new NoWait());
	}

	public Result<FGetCoroutine> PrepareCoroutineFor(
		Entity agent,
		FSpeedToDelta delta,
		Func<string, Result> playAnimation
	) {
		if (this.move is null) {
			return Result.SystemError(this.MissingField(nameof(this.move)));
		}
		var playWithNoWait = AnimatedMove.Run(playAnimation);
		var innerGetCoroutine = this.move.PrepareCoroutineFor(agent, delta);

		(Func<Coroutine>, Cancel) GetCoroutine(Func<Vector3> getTarget) {
			var (runMove, cancelMove) = innerGetCoroutine(getTarget);

			Coroutine run() {
				yield return playWithNoWait(this.animationKey);
				foreach (var wait in runMove()) {
					yield return wait;
				}
				yield return playWithNoWait(AnimatedMove.fallbackAnimationKey);
			};

			Result cancel() {
				return cancelMove()
					.FlatMap(() => playAnimation(AnimatedMove.fallbackAnimationKey));
			};

			return (run, cancel);
		}
		return Result.Ok<FGetCoroutine>(GetCoroutine);
	}
}
