namespace ProjectZyheeda;

using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;


public class MoveController : StartupScript, IEquipment {
	public static readonly string fallbackAnimationKey = "default";

	public float speed;
	public string playAnimation = "";

	private Either<U<SystemString, PlayerString>, IGetAnimation> animationGetter = new U<SystemString, PlayerString>(
		new SystemString("No IGetAnimation assigned")
	);

	private static Either<U<SystemString, PlayerString>, AnimationComponent> AnimationComponentFromChildren(Entity agent) {
		return agent
			.GetChildren()
			.Select(c => c.Get<AnimationComponent>())
			.FirstOrDefault()
			.ToEither(new U<SystemString, PlayerString>(new SystemString($"Missing AnimationComponent on {agent.Name}")));
	}

	private Either<U<SystemString, PlayerString>, IGetAnimation> AnimationGetterFromService() {
		return this
			.Game
			.Services
			.GetService<IGetAnimation>()
			.ToEither(new U<SystemString, PlayerString>(new SystemString("Missing IGetAnimation Service")));
	}

	public override void Start() {
		this.animationGetter = this.AnimationGetterFromService();
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

		var animationComponent = MoveController.AnimationComponentFromChildren(agent);
		return getBehavior
			.ApplyWeak(this.animationGetter)
			.ApplyWeak(animationComponent);
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
			var direction = target.Position() - agent.Position;

			if (direction != Vector3.Zero) {
				direction.Normalize();
				agent.Rotation = Quaternion.LookRotation(direction, Vector3.UnitY);
			}

			while (agent.Position != target.Position()) {
				agent.Position = this.PositionTowards(agent.Position, target.Position(), this.move.speed);
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
