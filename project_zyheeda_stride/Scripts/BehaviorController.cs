﻿namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;

public class BehaviorController : ProjectZyheedaStartupScript, IBehavior {
	public IGetTargetEditor? getTarget;
	public IEquipmentEditor? equipment;
	public Entity? agent;

	private struct VoidEquipment : IEquipmentEditor {
		public Result<FGetCoroutine> PrepareCoroutineFor(Entity agent) {
			static Coroutine Coroutine() {
				yield return Result.PlayerError("nothing equipped");
			}

			static Result Cancel() {
				return Result.Ok();
			}

			return Result.Ok<FGetCoroutine>(_ => (Coroutine(), Cancel));
		}
	}

	private static Result<(Coroutine, Cancel)> GetCoroutine(
		IEquipmentEditor equipment,
		IGetTargetEditor getTarget,
		Entity agent
	) {
		var getCoroutine =
			(FGetCoroutine getCoroutine) =>
			(Func<Result<Vector3>> target) =>
				getCoroutine(target);

		return getCoroutine
			.Apply(equipment.PrepareCoroutineFor(agent))
			.Apply(getTarget.GetTarget());
	}

	public Result<(Coroutine, Cancel)> GetExecution() {
		var getCoroutine =
			(IEquipmentEditor equipment) =>
			(IGetTargetEditor getTarget) =>
			(Entity agent) =>
				BehaviorController.GetCoroutine(equipment, getTarget, agent);

		return getCoroutine(this.equipment ?? new VoidEquipment())
			.Apply(this.getTarget.OkOrSystemError(this.MissingField(nameof(this.getTarget))))
			.Apply(this.agent.OkOrSystemError(this.MissingField(nameof(this.agent))))
			.Flatten();
	}
}
