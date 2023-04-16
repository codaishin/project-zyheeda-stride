namespace ProjectZyheeda;

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
		private TaskCompletionSource<bool>? traverseWaypointsToken;

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

		public void ResetAndIdle() {
			this.traverseWaypointsThread?.Cancel();
		}

		public Task<bool> Execute(U<Vector3, Entity> target) {
			if (this.traverseWaypointsThread?.State is not MicroThreadState.Completed) {
				this.traverseWaypointsToken?.SetResult(false);
			}
			this.traverseWaypointsToken = new TaskCompletionSource<bool>();

			var traverseWaypoints = async () => {
				this.Play(this.move.playAnimation);
				await this.MoveTowards(this.agentTransform, target);
				this.Play(fallbackAnimationKey);
				this.traverseWaypointsToken.SetResult(true);
			};

			this.traverseWaypointsThread?.Cancel();
			this.traverseWaypointsThread = this.move.Script.AddTask(traverseWaypoints);

			return this.traverseWaypointsToken.Task;
		}
	}
}
