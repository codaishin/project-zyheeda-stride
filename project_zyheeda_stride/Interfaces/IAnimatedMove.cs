namespace ProjectZyheeda;

using System;
using Stride.Engine;

public interface IAnimatedMove {
	Result<FGetCoroutine> PrepareCoroutineFor(
		Entity agent,
		FSpeedToDelta delta,
		Func<string, Result> playAnimation
	);
}
