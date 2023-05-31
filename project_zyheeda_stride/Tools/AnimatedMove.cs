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

	public Result<FGetCoroutine> PrepareCoroutineFor(
		Entity agent,
		FSpeedToDelta delta,
		Action<string> playAnimation
	) {
		if (this.move is null) {
			return Result.SystemError(this.MissingField(nameof(this.move)));
		}

		var innerGetCoroutine = this.move.PrepareCoroutineFor(agent, delta);

		(Func<Coroutine>, Cancel) GetCoroutine(Func<Vector3> getTarget) {
			var (runMove, cancelMove) = innerGetCoroutine(getTarget);

			Coroutine run() {
				playAnimation(this.animationKey);
				foreach (var wait in runMove()) {
					yield return wait;
				}
				playAnimation(AnimatedMove.fallbackAnimationKey);
			};

			Result cancel() {
				var result = cancelMove();
				playAnimation(AnimatedMove.fallbackAnimationKey);
				return result;
			};

			return (run, cancel);
		}
		return Result.Ok<FGetCoroutine>(GetCoroutine);
	}
}
