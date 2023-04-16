namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;


public class MoveController : StartupScript, IEquipment {
	public static readonly string fallbackAnimationKey = "default";

	public float speed;
	public string playAnimation = "";

	private Either<string, IGetAnimation> getAnimation = new("No IGetAnimation assigned");

	private static Vector3 GetVector3(U<Vector3, Entity> target) {
		return target.Switch(v => v, e => e.Transform.Position);
	}

	private static U<SystemString, PlayerString> ToSystemString(string value) {
		return new SystemString(value);
	}

	private static IEnumerable<U<SystemString, PlayerString>> ToSystemString(IEnumerable<string> value) {
		return value.Select(MoveController.ToSystemString);
	}

	private static Either<string, AnimationComponent> AnimationComponent(Entity agent) {
		return agent
			.GetChildren()
			.Select(c => c.Get<AnimationComponent>())
			.FirstOrDefault()
			.ToEither($"Missing AnimationComponent on {agent.Name}");
	}

	private Either<string, IGetAnimation> GetAnimation() {
		return this
			.Game
			.Services
			.GetService<IGetAnimation>()
			.ToEither("Missing IGetAnimation Service");
	}

	public override void Start() {
		this.getAnimation = this.GetAnimation();
	}

	public BehaviorOrErrors GetBehaviorFor(Entity agent) {
		var getBehavior =
			(IGetAnimation getAnimation) =>
			(AnimationComponent animationComponent) => (IBehaviorStateMachine)new Behavior(
				this,
				getAnimation,
				agent.Transform,
				animationComponent
			);

		return getBehavior
			.ApplyWeak(this.getAnimation)
			.ApplyWeak(MoveController.AnimationComponent(agent))
			.MapError(MoveController.ToSystemString);
	}

	private class Behavior : IBehaviorStateMachine {
		private readonly MoveController move;
		private readonly IGetAnimation getAnimation;
		private readonly TransformComponent agentTransform;
		private readonly AnimationComponent agentAnimation;

		public Behavior(
			MoveController move,
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
			var direction = MoveController.GetVector3(target) - agent.Position;

			if (direction != Vector3.Zero) {
				direction.Normalize();
				agent.Rotation = Quaternion.LookRotation(direction, Vector3.UnitY);
			}

			while (agent.Position != MoveController.GetVector3(target)) {
				agent.Position = this.PositionTowards(
					agent.Position,
					MoveController.GetVector3(target),
					this.move.speed
				);
				_ = await this.move.Script.NextFrame();
			}
		}

		private void Play(string animationKey) {
			if (!this.getAnimation.IsPlaying(this.agentAnimation, animationKey)) {
				_ = this.getAnimation.Play(this.agentAnimation, animationKey);
			}
		}

		public (Func<Task>, Cancel) GetExecution(U<Vector3, Entity> target) {
			async Task run() {
				this.Play(this.move.playAnimation);
				await this.MoveTowards(this.agentTransform, target);
				this.Play(fallbackAnimationKey);
			};
			void cancel() {
				this.Play(fallbackAnimationKey);
			};
			return (run, cancel);
		}
	}
}
