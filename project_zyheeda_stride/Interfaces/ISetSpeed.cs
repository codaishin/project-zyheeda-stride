global using OldSpeed = ProjectZyheeda.ISpeedEditor;

namespace ProjectZyheeda;

public interface ISetSpeed {
	Result<OldSpeed> SetSpeed(ISpeedEditor speed);
}
