namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;

public class BehaviorController : ProjectZyheedaStartupScript, IBehavior {
	public IMaybe<IEquipment>? equipment;
	public Entity? agent;

	private static Result<FGetCoroutine> NothingEquipped() {
		static Coroutine Run() {
			yield return Result.PlayerError("nothing equipped");
		}

		static Result Cancel() {
			return Result.Ok();
		}

		return Result.Ok<FGetCoroutine>((Func<Vector3> _) => (Run, Cancel));
	}

	private static Func<Entity, Result<FGetCoroutine>> GetBehaviorFn(IMaybe<IEquipment>? equipment) {
		return (Entity agent) =>
			equipment
				.ToMaybe()
				.Flatten()
				.Switch(
					equipment => equipment.PrepareCoroutineFor(agent),
					BehaviorController.NothingEquipped
				);
	}

	public Result<(Func<Coroutine>, Cancel)> GetCoroutine(Func<Vector3> getTarget) {
		return BehaviorController.GetBehaviorFn(this.equipment)
			.Apply(this.agent.OkOrSystemError(this.MissingField(nameof(this.agent))))
			.Flatten()
			.Map(getBehavior => getBehavior(getTarget));
	}
}
