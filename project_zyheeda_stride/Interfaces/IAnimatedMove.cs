namespace ProjectZyheeda;

using System;
using Stride.Engine;

public interface IAnimatedMove {
	Either<Errors, FGetCoroutine> PrepareCoroutineFor(
		Entity agent,
		FSpeedToDelta delta,
		Action<string> playAnimation
	);
}
