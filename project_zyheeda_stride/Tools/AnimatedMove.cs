namespace ProjectZyheeda;

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

public static class AnimatedMove {
	public static readonly string fallbackAnimationKey = "default";
}

[DataContract]
public abstract class AnimatedMove<TMove> :
	IAnimatedMove
	where TMove :
		IMove {

	public readonly TMove move;
	public string animationKey = "";

	protected AnimatedMove(TMove move) {
		this.move = move;
	}

	public FGetCoroutine PrepareCoroutineFor(Entity agent, FSpeedToDelta delta, Action<string> playAnimation) {
		var getCoroutine = this.move.PrepareCoroutineFor(agent, delta);
		return (U<Vector3, Entity> target) => {
			var (runMove, cancelMove) = getCoroutine(target);

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
		};
	}
}

[DataContract]
public class AnimatedStraightMove : AnimatedMove<StraightMove> {
	public AnimatedStraightMove() : base(new StraightMove()) { }
}
