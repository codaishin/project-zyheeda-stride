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

	private static Func<string, Result<IWait>> RunAnimation(Func<string, Result> playAnimation) {
		return key => playAnimation(key).Map<IWait>(() => new NoWait());
	}

	private Result<FGetCoroutine> GetCoroutine(FGetCoroutine innerGetCoroutine, Func<string, Result<IWait>> play) {
		(Func<Coroutine>, Cancel) GetCoroutine(Func<Vector3> getTarget) {
			var (runMove, cancelMove) = innerGetCoroutine(getTarget);

			Coroutine run() {
				yield return play(this.animationKey);
				foreach (var wait in runMove()) {
					yield return wait;
				}
				yield return play(AnimatedMove.fallbackAnimationKey);
			};

			Result cancel() {
				return cancelMove()
					.FlatMap(() => play(AnimatedMove.fallbackAnimationKey));
			};

			return (run, cancel);
		}
		return Result.Ok<FGetCoroutine>(GetCoroutine);
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
}
