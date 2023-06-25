global using OldSpeed = System.Single;

namespace ProjectZyheeda;

public interface ISetSpeed {
	Result<OldSpeed> SetSpeed(float unitsPerSecond);
}
