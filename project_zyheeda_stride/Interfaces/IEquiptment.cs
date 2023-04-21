namespace ProjectZyheeda;

using Stride.Engine;


public interface IEquipment {
	Either<Errors, FGetCoroutine> PrepareCoroutineFor(Entity agent);
}
