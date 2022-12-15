namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;

public class Move : StartupScript, IEquipment {
	public float speed;

	public override void Start() { }

	private struct Behavior : IBehaviorStateMachine {
		public Action<U<Vector3, Entity>[]> executeNext;
		public Action resetAndIdle;

		public void ExecuteNext(U<Vector3, Entity>[] targets) {
			this.executeNext(targets);
		}

		public void ResetAndIdle() {
			this.resetAndIdle();
		}
	}

	private Vector3 MoveTowards(Vector3 current, Vector3 target, float speed) {
		var diff = target - current;
		var delta = (float)this.Game.UpdateTime.Elapsed.TotalSeconds * speed;
		if (delta >= diff.Length()) {
			return target;
		}
		diff.Normalize();
		return current + (diff * delta);
	}

	private static Vector3 GetVector3(U<Vector3, Entity> target) {
		return target.Switch(v => v, e => e.Transform.Position);
	}

	public Either<U<Requirement, Type[]>, IBehaviorStateMachine> GetBehaviorFor(Entity agent) {
		var currentTargets = Array.Empty<U<Vector3, Entity>>();
		var currentI = 0;

		var executeNext = (U<Vector3, Entity>[] targets) => {
			_ = this.Script.AddTask(async () => {
				currentTargets = targets;
				currentI = 0;
				while (this.Game.IsRunning && currentI < currentTargets.Length) {
					var target = Move.GetVector3(currentTargets[currentI]);
					agent.Transform.Position = this.MoveTowards(
						agent.Transform.Position,
						target,
						this.speed
					);
					if (agent.Transform.Position == target) {
						++currentI;
					}
					_ = await this.Script.NextFrame();
				}
			});
		};

		var resetAndIdle = () => {
			currentTargets = Array.Empty<U<Vector3, Entity>>();
		};

		return new Move.Behavior {
			executeNext = executeNext,
			resetAndIdle = resetAndIdle
		};
	}
}
