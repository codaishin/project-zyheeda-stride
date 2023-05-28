namespace ProjectZyheeda;

public readonly struct SystemError {
	public readonly string value;

	public SystemError(string value) {
		this.value = value;
	}

	public static implicit operator SystemError(string value) {
		return new SystemError(value);
	}

	public static implicit operator string(SystemError value) {
		return value.value;
	}
}

public interface ISystemMessage {
	void Log(SystemError message);
}
