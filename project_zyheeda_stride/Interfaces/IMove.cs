namespace ProjectZyheeda;

using Stride.Engine;

public interface IMove {
	Result<FGetCoroutine> PrepareCoroutineFor(Entity agent, FSpeedToDelta delta);
}
