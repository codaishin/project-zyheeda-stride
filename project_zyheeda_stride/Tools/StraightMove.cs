namespace ProjectZyheeda;

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

[DataContract]
public class StraightMove : IMoveEditor {
	public float speed;

	private Coroutine MoveTowards(TransformComponent agent, Func<Vector3> getTarget, FSpeedToDelta delta) {
		var direction = getTarget() - agent.Position;

		if (direction != Vector3.Zero) {
			direction.Normalize();
			agent.Rotation = Quaternion.LookRotation(direction, Vector3.UnitY);
		}

		while (agent.Position != getTarget()) {
			agent.Position = agent.Position.MoveTowards(getTarget(), delta(this.speed));
			yield return new WaitFrame();
		}
	}

	public Result<FGetCoroutine> PrepareCoroutineFor(Entity agent, FSpeedToDelta delta) {
		return Result.Ok<FGetCoroutine>((Func<Vector3> getTarget) => {
			Coroutine Coroutine() {
				foreach (var wait in this.MoveTowards(agent.Transform, getTarget, delta)) {
					yield return wait;
				}
			};

			Result Cancel() {
				return Result.Ok();
			}

			return (Coroutine(), Cancel);
		});
	}

	public Result<OldSpeed> SetSpeed(float unitsPerSecond) {
		var oldSpeed = this.speed;
		this.speed = unitsPerSecond;
		return oldSpeed;
	}
}
