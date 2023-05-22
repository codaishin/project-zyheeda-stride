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

	public Either<Errors, FGetCoroutine> PrepareCoroutineFor(
		Entity agent,
		FSpeedToDelta delta,
		Action<string> playAnimation
	) {
		if (this.move is null) {
			return new Either<Errors, FGetCoroutine>(new[] {
				(U<SystemStr, PlayerStr>)new SystemStr(this.MissingField(nameof(this.move)))
			});
		}

		var innerGetCoroutine = this.move.PrepareCoroutineFor(agent, delta);

		(Func<Coroutine>, Cancel) GetCoroutine(U<Vector3, Entity> target) {
			var (runMove, cancelMove) = innerGetCoroutine(target);

			Coroutine run() {
				playAnimation(this.animationKey);
				foreach (var wait in runMove()) {
					yield return wait;
				}
				playAnimation(AnimatedMove.fallbackAnimationKey);
			};

			void cancel() {
				cancelMove();
				playAnimation(AnimatedMove.fallbackAnimationKey);
			};

			return (run, cancel);
		}
		return new Either<Errors, FGetCoroutine>(GetCoroutine);
	}
}
