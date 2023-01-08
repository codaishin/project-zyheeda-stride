namespace ProjectZyheeda;

public readonly struct SystemString {
	public readonly string value;

	public SystemString(string value) {
		this.value = value;
	}
}

public interface ISystemMessage {
	void Log(SystemString message);
}
