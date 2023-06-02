namespace ProjectZyheeda;

public interface IInputDispatcher {
	Result Add(IInputStream stream);
	Result Remove(IInputStream stream);
}
