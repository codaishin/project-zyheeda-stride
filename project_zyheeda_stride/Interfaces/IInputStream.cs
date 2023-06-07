namespace ProjectZyheeda;

using System.Threading.Tasks;

public enum InputAction {
	Run = 1,
	Chain = 2,
}

public interface IInputStream {
	Task<Result<InputAction>> NewAction();
	Result ProcessEvent(InputKeys key, bool isDown);
}

public interface IInputStreamEditor : IInputStream { }
