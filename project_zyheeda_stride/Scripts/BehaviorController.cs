namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

public class BehaviorController : ProjectZyheedaStartupScript, IBehavior {
	public IMaybe<IEquipment>? equipment;

	public Entity? agent;

	private void LogMessage(U<SystemStr, PlayerStr> error) {
		error.Switch(
			this.EssentialServices.systemMessage.Log,
			this.EssentialServices.playerMessage.Log
		);
	}

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

	private (Func<Coroutine>, Cancel) NothingEquipped(U<Vector3, Entity> target) {
		Coroutine run() {
			this.LogMessage(new PlayerStr("nothing equipped"));
			yield break;
		}
		void cancel() { }
		return (run, cancel);
	}

	private void LogErrors(IEnumerable<U<SystemStr, PlayerStr>> errors) {
		foreach (var error in errors) {
			this.LogMessage(error);
		}
	}

	public (Func<Coroutine>, Cancel) GetCoroutine(U<Vector3, Entity> target) {
		return this.GetBehavior
			.ApplyWeak(this.agent.OkOrSystemError(this.MissingField(nameof(this.agent))))
			.Flatten()
			.Switch(
				errors => {
					this.LogErrors(errors);
					return this.NothingEquipped(target);
				},
				value => value(target)
			);
	}
}
