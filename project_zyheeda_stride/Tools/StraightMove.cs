namespace ProjectZyheeda;

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

[DataContract]
[Display(Expand = ExpandRule.Always)]
public class StraightMove : IMoveEditor {
	public ISpeedEditor? speed;

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

	private static IWait UpdateAgentPositionAndRotation(
		TransformComponent agent,
		FSpeedToDelta delta,
		Vector3 target,
		ISpeed speed
	) {
		agent.Position = agent.Position.MoveTowards(target, delta(speed));
		return StraightMove.UpdateRotation(agent, target - agent.Position);
	}

	private Coroutine MoveTowards(TransformComponent agent, Func<Result<Vector3>> getTarget, FSpeedToDelta delta) {
		var updateAgentPositionAndRotation =
			(Vector3 target) =>
			(ISpeedEditor speed) =>
				StraightMove.UpdateAgentPositionAndRotation(agent, delta, target, speed);

		Result<Vector3> targetOrError;

		do {
			targetOrError = getTarget();
			var speedOrError = this.speed.OkOrSystemError(this.MissingField(nameof(this.speed)));
			yield return updateAgentPositionAndRotation
				.Apply(targetOrError)
				.Apply(speedOrError);
		} while (StraightMove.NotAtTargetPosition(agent, targetOrError));
	}

	public Result<FGetCoroutine> PrepareCoroutineFor(Entity agent, FSpeedToDelta delta) {
		return Result.Ok<FGetCoroutine>(getTarget => (
			this.MoveTowards(agent.Transform, getTarget, delta),
			() => Result.Ok())
		);
	}

	public Result<OldSpeed> SetSpeed(ISpeedEditor speed) {
		if (this.speed is null) {
			return Result.SystemError(this.MissingField(nameof(this.speed)));
		}
		var oldSpeed = this.speed;
		this.speed = speed;
		return Result.Ok(oldSpeed);
	}
}
