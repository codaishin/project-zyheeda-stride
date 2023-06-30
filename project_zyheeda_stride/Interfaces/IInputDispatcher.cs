namespace ProjectZyheeda;

public interface IInputDispatcher {
	Result Add(IExecutionStream stream);
	Result Remove(IExecutionStream stream);
}
