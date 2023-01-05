namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Core.MicroThreading;
using Stride.Engine;

public class Move : StartupScript, IEquipment {
	public float speed;

	public override void Start() { }

	private struct Behavior : IBehaviorStateMachine {
		public Action<IAsyncEnumerable<U<Vector3, Entity>>> executeNext;
		public Action resetAndIdle;

		public void ExecuteNext(IAsyncEnumerable<U<Vector3, Entity>> targets) {
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

	private async Task MoveTowardsTarget(Entity agent, U<Vector3, Entity> target) {
		var direction = Move.GetVector3(target) - agent.Transform.Position;

		direction.Normalize();
		agent.Transform.Rotation = Quaternion.LookRotation(direction, Vector3.UnitY);
		while (agent.Transform.Position != Move.GetVector3(target)) {
			_ = await this.Script.NextFrame();
			agent.Transform.Position = this.MoveTowards(
				agent.Transform.Position,
				Move.GetVector3(target),
				this.speed
			);
		}
	}

	public Either<U<Requirement, Type[]>, IBehaviorStateMachine> GetBehaviorFor(Entity agent) {
		MicroThread? traverseWaypointsThread = null;
		MicroThread? collectWaypointsThread = null;

		var executeNext = (IAsyncEnumerable<U<Vector3, Entity>> targets) => {
			var waypoints = new List<U<Vector3, Entity>>();
			var index = 0;
			var collectWaypoints = async () => {
				await foreach (var target in targets) {
					waypoints.Add(target);
				}
			};
			var traverseWaypoints = async () => {
				while (true) {
					if (index < waypoints.Count) {
						await this.MoveTowardsTarget(agent, waypoints[index++]);
					}
					_ = await this.Script.NextFrame();
				}
			};

			collectWaypointsThread?.Cancel();
			traverseWaypointsThread?.Cancel();
			collectWaypointsThread = this.Script.AddTask(collectWaypoints);
			traverseWaypointsThread = this.Script.AddTask(traverseWaypoints);
		};

		var resetAndIdle = () => {
			collectWaypointsThread?.Cancel();
			traverseWaypointsThread?.Cancel();
		};

		return new Move.Behavior {
			executeNext = executeNext,
			resetAndIdle = resetAndIdle
		};
	}
}
