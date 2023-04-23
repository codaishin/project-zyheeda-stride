namespace ProjectZyheeda;

using System.Collections.Generic;
using System.Linq;
using Stride.Engine;


public abstract class BaseMoveController<TAnimatedMove> :
	StartupScript,
	IEquipment
	where TAnimatedMove :
		IAnimatedMove,
		new() {

	private Either<U<SystemStr, PlayerStr>, IAnimation> animation = new U<SystemStr, PlayerStr>(
		new SystemStr("No IGetAnimation assigned")
	);

	public readonly TAnimatedMove move = new();

	private static Either<U<SystemStr, PlayerStr>, AnimationComponent> AnimatorOnChildOf(Entity agent) {
		return agent
			.GetChildren()
			.Select(c => c.Get<AnimationComponent>())
			.FirstOrDefault()
			.ToEither(new U<SystemStr, PlayerStr>(new SystemStr($"Missing AnimationComponent on {agent.Name}")));
	}

	private Either<U<SystemStr, PlayerStr>, IAnimation> AnimationFromService() {
		return this
			.Game
			.Services
			.GetService<IAnimation>()
			.ToEither(new U<SystemStr, PlayerStr>(new SystemStr("Missing IGetAnimation Service")));
	}

	public override void Start() {
		this.animation = this.AnimationFromService();
	}

	public Either<IEnumerable<U<SystemStr, PlayerStr>>, FGetCoroutine> PrepareCoroutineFor(Entity agent) {
		var prepareCoroutine =
			(IAnimation animation) =>
			(AnimationComponent agentAnimator) => this.move.PrepareCoroutineFor(
				agent,
				speedPerSecond => (float)this.Game.UpdateTime.Elapsed.TotalSeconds * speedPerSecond,
				key => BaseMoveController<TAnimatedMove>.Play(animation, key, agentAnimator)
			);


		var agentAnimator = BaseMoveController<AnimatedStraightMove>.AnimatorOnChildOf(agent);
		return prepareCoroutine
			.ApplyWeak(this.animation)
			.ApplyWeak(agentAnimator);
	}

	private static void Play(IAnimation animation, string animationKey, AnimationComponent agentAnimator) {
		if (!animation.IsPlaying(agentAnimator, animationKey)) {
			_ = animation.Play(agentAnimator, animationKey);
		}
	}
}

public class MoveController : BaseMoveController<AnimatedStraightMove> { }
