namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;

public class BehaviorController : ProjectZyheedaStartupScript, IBehavior {
	public IMaybe<IEquipment>? equipment;

	public Entity? agent;

	private Func<Entity, Result<FGetCoroutine>> GetBehavior {
		get {
			var getBehaviorAndEquipment =
				(Entity agent) =>
					this.equipment.ToMaybe().Flatten().Switch(
						equipment => equipment.PrepareCoroutineFor(agent),
						() => Result.Ok<FGetCoroutine>(this.NothingEquipped)
					);
			return getBehaviorAndEquipment;
		}
	}

	private (Func<Coroutine>, Cancel) NothingEquipped(Func<Vector3> _) {
		Coroutine run() {
			this.EssentialServices.playerMessage.Log("nothing equipped");
			yield break;
		}
		void cancel() { }
		return (run, cancel);
	}

	private void LogErrors((SystemErrors system, PlayerErrors player) errors) {
		foreach (var error in errors.system) {
			this.EssentialServices.systemMessage.Log(error);
		}
		foreach (var error in errors.player) {
			this.EssentialServices.playerMessage.Log(error);
		}
	}

	public (Func<Coroutine>, Cancel) GetCoroutine(Func<Vector3> getTarget) {
		return this.GetBehavior
			.ApplyWeak(this.agent.OkOrSystemError(this.MissingField(nameof(this.agent))))
			.Flatten()
			.Switch(
				errors => {
					this.LogErrors(errors);
					return this.NothingEquipped(getTarget);
				},
				value => value(getTarget)
			);
	}
}
