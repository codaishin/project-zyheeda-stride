namespace ProjectZyheeda;

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

[DataContract]
[Display(Expand = ExpandRule.Always)]
public class StraightMove : IMoveEditor {
	public float speed;

	private static bool NotAtTargetPosition(TransformComponent agent, Result<Vector3> targetOrError) {
		return targetOrError
			.Map(newTarget => newTarget != agent.Position)
			.UnpackOr(false);
	}

	private static IWait UpdateRotation(TransformComponent agent, Vector3 direction) {
		if (direction == Vector3.Zero) {
			return new WaitFrame();
		}

		direction.Normalize();
		agent.Rotation = Quaternion.LookRotation(direction, Vector3.UnitY);
		return new WaitFrame();
	}

	private Func<Vector3, IWait> UpdateAgentPositionAndRotation(TransformComponent agent, FSpeedToDelta delta) {
		return newTarget => {
			agent.Position = agent.Position.MoveTowards(newTarget, delta(this.speed));
			return StraightMove.UpdateRotation(agent, newTarget - agent.Position);
		};
	}

	private Coroutine MoveTowards(TransformComponent agent, Func<Result<Vector3>> getTarget, FSpeedToDelta delta) {
		Coroutine Coroutine() {
			Result<Vector3> targetOrError;

			do {
				targetOrError = getTarget();
				yield return targetOrError.Map(this.UpdateAgentPositionAndRotation(agent, delta));
			} while (StraightMove.NotAtTargetPosition(agent, targetOrError));
		}

		return Coroutine();
	}

	public Result<FGetCoroutine> PrepareCoroutineFor(Entity agent, FSpeedToDelta delta) {
		return Result.Ok<FGetCoroutine>(getTarget => (
			this.MoveTowards(agent.Transform, getTarget, delta),
			() => Result.Ok())
		);
	}

	public Result<OldSpeed> SetSpeed(float unitsPerSecond) {
		var oldSpeed = this.speed;
		this.speed = unitsPerSecond;
		return oldSpeed;
	}
}
