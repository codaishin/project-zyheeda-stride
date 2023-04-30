namespace ProjectZyheeda;

using System.Threading.Tasks;

public interface IInputStream {
	Task<InputAction> NewAction();
	void ProcessEvent(InputKeys key, bool isDown);
}
