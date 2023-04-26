namespace ProjectZyheeda;

using Stride.Engine;

public interface IMove {
	FGetCoroutine PrepareCoroutineFor(Entity agent, FSpeedToDelta delta);
}
