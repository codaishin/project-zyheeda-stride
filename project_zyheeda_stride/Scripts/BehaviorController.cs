namespace ProjectZyheeda;

using System;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;

public class BehaviorController : ProjectZyheedaStartupScript, IBehavior {
	[NotNull] public IMaybe<IEquipment> equipment = new NoEquipment();
	public Entity? agent;

	private Func<Entity, Result<FGetCoroutine>> GetBehavior {
		get {
			var getBehaviorAndEquipment =
				(Entity agent) =>
					this.equipment.Switch(
						equipment => equipment.PrepareCoroutineFor(agent),
						() => (FGetCoroutine)this.NothingEquipped
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

	public (Func<Coroutine>, Cancel) GetCoroutine(Func<Vector3> getTarget) {
		return this.GetBehavior
			.ApplyWeak(this.agent.OkOrSystemError(this.MissingField(nameof(this.agent))))
			.Flatten()
			.Switch(
				errors => {
					this.EssentialServices.systemMessage.Log(errors.system.ToArray());
					this.EssentialServices.playerMessage.Log(errors.player.ToArray());
					return this.NothingEquipped(getTarget);
				},
				value => value(getTarget)
			);
	}
}
