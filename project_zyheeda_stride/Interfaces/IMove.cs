namespace ProjectZyheeda;

using Stride.Engine;

public interface IMove : ISetSpeed {
	Result<FGetCoroutine> PrepareCoroutineFor(Entity agent, FSpeedToDelta delta);
}

public interface IMoveEditor : IMove { }
