namespace ProjectZyheeda;

using System;
using Stride.Engine;


public interface IMove {
	public delegate float FDelta(float speedPerSecond);
	FGetCoroutine PrepareCoroutineFor(Entity agent, Action<string> playAnimation, FDelta delta);
}
