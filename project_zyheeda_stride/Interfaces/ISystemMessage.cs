namespace ProjectZyheeda;

public readonly struct SystemStr {
	public readonly string value;

	public SystemStr(string value) {
		this.value = value;
	}
}

public interface ISystemMessage {
	void Log(SystemStr message);
}
