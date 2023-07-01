namespace ProjectZyheeda;

using System;
using Stride.Engine;

public class ReferenceAnimatedMoveDependency : Reference<CharacterDependencies>, IAnimatedMoveEditor {
	public CharacterDependencies? Target {
		get => this.GetRef();
		set => this.SetRef(value);
	}

	private Result<IAnimatedMove> Move =>
		this.target
			.FlatMap(t => t.move.OkOrSystemError(t.MissingField(nameof(t.move))));

	public Result<FGetCoroutine> PrepareCoroutineFor(
		Entity agent,
		FSpeedToDelta delta,
		Func<string, Result> playAnimation
	) {
		return this.Move.FlatMap(m => m.PrepareCoroutineFor(agent, delta, playAnimation));
	}

	public Result<string> SetAnimation(string animationKey) {
		return this.Move.FlatMap(m => m.SetAnimation(animationKey));
	}

	public Result<float> SetSpeed(float unitsPerSecond) {
		return this.Move.FlatMap(m => m.SetSpeed(unitsPerSecond));
	}
}
