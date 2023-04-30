namespace ProjectZyheeda;

using System.Threading.Tasks;

public enum InputAction {
	Run = 1,
	Chain = 2,
}

public interface IInputStream {
	Task<InputAction> NewAction();
	void ProcessEvent(InputKeys key, bool isDown);
}
