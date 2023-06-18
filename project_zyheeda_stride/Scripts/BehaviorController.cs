namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;

public class BehaviorController : ProjectZyheedaStartupScript, IBehavior {
	public IGetTargetEditor? getTarget;
	public IEquipmentEditor? equipment;
	public Entity? agent;

	private struct VoidEquipment : IEquipmentEditor {
		public Result<FGetCoroutine> PrepareCoroutineFor(Entity agent) {
			static Coroutine Run() {
				yield return Result.PlayerError("nothing equipped");
			}

			static Result<IWait> Cancel() {
				return Result.Ok<IWait>(new NoWait());
			}

			return Result.Ok<FGetCoroutine>((Func<Vector3> _) => (Run, Cancel));
		}
	}

	private static Result<(Func<Coroutine>, Cancel)> GetCoroutine(
		IEquipmentEditor equipment,
		IGetTargetEditor getTarget,
		Entity agent
	) {
		var getCoroutine =
			(FGetCoroutine getCoroutine) =>
			(Func<Vector3> target) =>
				getCoroutine(target);

		return getCoroutine
			.Apply(equipment.PrepareCoroutineFor(agent))
			.Apply(getTarget.GetTarget());
	}

	public Result<(Func<Coroutine>, Cancel)> GetCoroutine() {
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
