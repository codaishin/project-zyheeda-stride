namespace ProjectZyheeda;

using System.Collections.Generic;
using System.Linq;
using Stride.Engine;


public abstract class BaseMoveController<TAnimatedMove> :
	ProjectZyheedaStartupScript,
	IEquipment
	where TAnimatedMove :
		IAnimatedMove,
		new() {

	public readonly TAnimatedMove move = new();

	private static Either<U<SystemStr, PlayerStr>, AnimationComponent> AnimatorOnChildOf(Entity agent) {
		return agent
			.GetChildren()
			.Select(c => c.Get<AnimationComponent>())
			.FirstOrDefault()
			.ToEither(new U<SystemStr, PlayerStr>(new SystemStr($"Missing AnimationComponent on {agent.Name}")));
	}

	public Either<IEnumerable<U<SystemStr, PlayerStr>>, FGetCoroutine> PrepareCoroutineFor(Entity agent) {
		var prepareCoroutine = (AnimationComponent agentAnimator) =>
			this.move.PrepareCoroutineFor(
				agent,
				speedPerSecond => (float)this.Game.UpdateTime.Elapsed.TotalSeconds * speedPerSecond,
				key => this.Play(key, agentAnimator)
			);


		var agentAnimator = BaseMoveController<AnimatedStraightMove>.AnimatorOnChildOf(agent);
		return prepareCoroutine.ApplyWeak(agentAnimator);
	}

	private void Play(string animationKey, AnimationComponent agentAnimator) {
		var animation = this.EssentialServices.animation;
		if (!animation.IsPlaying(agentAnimator, animationKey)) {
			_ = animation.Play(agentAnimator, animationKey);
		}
	}
}

public class MoveController : BaseMoveController<AnimatedStraightMove> { }
