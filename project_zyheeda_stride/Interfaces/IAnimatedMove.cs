namespace ProjectZyheeda;

using System;
using Stride.Engine;

public interface IAnimatedMove : ISetAnimation, ISetSpeed {
	Result<FGetCoroutine> PrepareCoroutineFor(
		Entity agent,
		FSpeedToDelta delta,
		Func<string, Result> playAnimation
	);
}

public interface IAnimatedMoveEditor : IAnimatedMove { }
