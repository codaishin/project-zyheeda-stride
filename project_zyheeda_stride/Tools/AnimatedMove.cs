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

	private static Result<IWait> CombineResultsAndWaitFirst(Result<IWait> fst, Result<IWait> snd) {
		var returnFirst = (IWait f) => (IWait _) => f;
		return returnFirst
			.Apply(fst)
			.Apply(snd);
	}

	private Result<FGetCoroutine> GetCoroutine(FGetCoroutine innerGetCoroutine, Func<string, Result<IWait>> play) {
		(Func<Coroutine>, Cancel) GetCoroutine(Func<Vector3> getTarget) {
			var (runMove, cancelMove) = innerGetCoroutine(getTarget);

			Coroutine Run() {
				yield return play(this.animationKey);
				foreach (var wait in runMove()) {
					yield return wait;
				}
				yield return play(AnimatedMove.fallbackAnimationKey);
			};

			Result<IWait> Cancel() {
				return AnimatedMove.CombineResultsAndWaitFirst(
					cancelMove(),
					play(AnimatedMove.fallbackAnimationKey)
				);
			};

			return (Run, Cancel);
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
