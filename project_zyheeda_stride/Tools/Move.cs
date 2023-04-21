namespace ProjectZyheeda;

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

[DataContract]
public class Move : IMove {
	public static readonly string fallbackAnimationKey = "default";

	public float speed;
	public string animationKey;

	public Move() {
		this.speed = 0;
		this.animationKey = "";
	}

	private Coroutine MoveTowards(TransformComponent agent, U<Vector3, Entity> target, IMove.FDelta delta) {
		var direction = target.Position() - agent.Position;

		if (direction != Vector3.Zero) {
			direction.Normalize();
			agent.Rotation = Quaternion.LookRotation(direction, Vector3.UnitY);
		}

		while (agent.Position != target.Position()) {
			agent.Position = agent.Position.MoveTowards(target.Position(), delta(this.speed));
			yield return new WaitFrame();
		}
	}

	public FGetCoroutine PrepareCoroutineFor(Entity agent, Action<string> playAnimation, IMove.FDelta delta) {
		return (U<Vector3, Entity> target) => {
			Coroutine run() {
				playAnimation(this.animationKey);
				foreach (var wait in this.MoveTowards(agent.Transform, target, delta)) {
					yield return wait;
				}
				playAnimation(Move.fallbackAnimationKey);
			};

			void cancel() {
				playAnimation(Move.fallbackAnimationKey);
			};

			return (run, cancel);
		};
	}
}
