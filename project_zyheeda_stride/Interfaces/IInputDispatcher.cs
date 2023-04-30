namespace ProjectZyheeda;

public interface IInputDispatcher {
	void Add(IInputStream stream);
	void Remove(IInputStream stream);
}
