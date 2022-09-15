namespace ProjectZyheeda;

public interface IEvent<TData> {
	void Invoke(TData data);
}
