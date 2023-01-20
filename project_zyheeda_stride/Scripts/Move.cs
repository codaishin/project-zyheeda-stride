namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Core.MicroThreading;
using Stride.Engine;


public class Move : StartupScript, IEquipment {
	public static readonly string fallbackAnimationKey = "default";

	public float speed;
	public string playAnimation = "";

	private IGetAnimation? getAnimation;

	public override void Start() {
		this.getAnimation = this.Game.Services.GetService<IGetAnimation>();
	}

	private static Vector3 GetVector3(U<Vector3, Entity> target) {
		return target.Switch(v => v, e => e.Transform.Position);
	}

	public BehaviorOrErrors GetBehaviorFor(Entity agent) {
		var animationComponent = agent
			.GetChildren()
			.Select(c => c.Get<AnimationComponent>())
			.FirstOrDefault();

		if (this.getAnimation is not null && animationComponent is not null) {
			return new Behavior(
				this,
				this.getAnimation,
				agent.Transform,
				animationComponent
			);
		}

		IEnumerable<U<SystemString, PlayerString>> getErrors() {
			if (this.getAnimation is null) {
				yield return new SystemString("Missing IGetAnimation Service");
			}
			if (animationComponent is null) {
				yield return new SystemString($"Missing AnimationComponent on {agent.Name}");
			}
		}

		return getErrors().ToArray();
	}

	private class Behavior : IBehaviorStateMachine {
		private MicroThread? traverseWaypointsThread;
		private MicroThread? collectWaypointsThread;

		private readonly Move move;
		private readonly IGetAnimation getAnimation;
		private readonly TransformComponent agentTransform;
		private readonly AnimationComponent agentAnimation;

		public Behavior(
			Move move,
			IGetAnimation getAnimation,
			TransformComponent agentTransform,
			AnimationComponent agentAnimation
		) {
			this.move = move;
			this.getAnimation = getAnimation;
			this.agentTransform = agentTransform;
			this.agentAnimation = agentAnimation;
		}

		private bool CollectingWaypoints =>
			this.collectWaypointsThread?.State
				is MicroThreadState.Starting
				or MicroThreadState.Running;

		private Vector3 PositionTowards(Vector3 current, Vector3 target, float speed) {
			var diff = target - current;
			var delta = (float)this.move.Game.UpdateTime.Elapsed.TotalSeconds * speed;
			if (delta >= diff.Length()) {
				return target;
			}
			diff.Normalize();
			return current + (diff * delta);
		}

		private async Task MoveTowards(TransformComponent agent, U<Vector3, Entity> target) {
			var direction = Move.GetVector3(target) - agent.Position;

			if (direction != Vector3.Zero) {
				direction.Normalize();
				agent.Rotation = Quaternion.LookRotation(direction, Vector3.UnitY);
			}

			while (agent.Position != Move.GetVector3(target)) {
				_ = await this.move.Script.NextFrame();
				agent.Position = this.PositionTowards(
					agent.Position,
					Move.GetVector3(target),
					this.move.speed
				);
			}
		}

		private void Play(string animationKey) {
			if (!this.getAnimation.IsPlaying(this.agentAnimation, animationKey)) {
				_ = this.getAnimation.Play(this.agentAnimation, animationKey);
			}
		}

		private (string, Func<Task>) GetAnimationKeyAndTask(
			Queue<U<Vector3, Entity>> waypoints
		) {
			return waypoints.TryDequeue(out var waypoint)
				? (this.move.playAnimation, async () => await this.MoveTowards(this.agentTransform, waypoint))
				: (Move.fallbackAnimationKey, async () => await this.move.Script.NextFrame());
		}

		public void ResetAndIdle() {
			this.collectWaypointsThread?.Cancel();
			this.traverseWaypointsThread?.Cancel();
		}

		public void ExecuteNext(IAsyncEnumerable<U<Vector3, Entity>> targets) {
			var waypoints = new Queue<U<Vector3, Entity>>();

			var collectWaypoints = async () => {
				await foreach (var target in targets) {
					waypoints.Enqueue(target);
				}
			};
			this.collectWaypointsThread?.Cancel();
			this.collectWaypointsThread = this.move.Script.AddTask(collectWaypoints);

			var traverseWaypoints = async () => {
				while (this.CollectingWaypoints || waypoints.Count > 0) {
					var (animationKey, task) = this.GetAnimationKeyAndTask(waypoints);
					this.Play(animationKey);
					await task();
				};
				this.Play(Move.fallbackAnimationKey);
			};
			this.traverseWaypointsThread?.Cancel();
			this.traverseWaypointsThread = this.move.Script.AddTask(traverseWaypoints);
		}
	}
}
