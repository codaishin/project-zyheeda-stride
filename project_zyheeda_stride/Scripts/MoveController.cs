namespace ProjectZyheeda;

using System.Collections.Generic;
using System.Linq;
using Stride.Engine;


public class MoveController : ProjectZyheedaStartupScript, IEquipment {
	public IAnimatedMove? move;

	private static Either<U<SystemStr, PlayerStr>, AnimationComponent> AnimatorOnChildOf(Entity agent) {
		return agent
			.GetChildren()
			.Select(c => c.Get<AnimationComponent>())
			.FirstOrDefault()
			.ToEither(new U<SystemStr, PlayerStr>(new SystemStr($"Missing AnimationComponent on {agent.Name}")));
	}

	private Either<U<SystemStr, PlayerStr>, IAnimatedMove> EitherMoveOrError() {
		return this.move is null
			? new Either<U<SystemStr, PlayerStr>, IAnimatedMove>(new SystemStr(this.MissingField(nameof(this.move))))
			: new Either<U<SystemStr, PlayerStr>, IAnimatedMove>(this.move);
	}

	public Either<IEnumerable<U<SystemStr, PlayerStr>>, FGetCoroutine> PrepareCoroutineFor(Entity agent) {
		var prepareCoroutine = (IAnimatedMove move) => (AnimationComponent agentAnimator) =>
			move.PrepareCoroutineFor(
				agent,
				speedPerSecond => (float)this.Game.UpdateTime.Elapsed.TotalSeconds * speedPerSecond,
				key => this.Play(key, agentAnimator)
			);


		var agentAnimatorOrError = MoveController.AnimatorOnChildOf(agent);
		return prepareCoroutine
			.ApplyWeak(this.EitherMoveOrError())
			.ApplyWeak(agentAnimatorOrError);
	}

	private void Play(string animationKey, AnimationComponent agentAnimator) {
		var animation = this.EssentialServices.animation;
		if (!animation.IsPlaying(agentAnimator, animationKey)) {
			_ = animation.Play(agentAnimator, animationKey);
		}
	}
}
