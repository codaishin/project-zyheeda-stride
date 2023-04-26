namespace ProjectZyheeda;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

[DataContract]
public class StraightMove : IMove {
	public float speed;

	private Coroutine MoveTowards(TransformComponent agent, U<Vector3, Entity> target, FSpeedToDelta delta) {
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

	public FGetCoroutine PrepareCoroutineFor(Entity agent, FSpeedToDelta delta) {
		return (U<Vector3, Entity> target) => {
			Coroutine run() {
				foreach (var wait in this.MoveTowards(agent.Transform, target, delta)) {
					yield return wait;
				}
			};

			void cancel() { };

			return (run, cancel);
		};
	}
}
