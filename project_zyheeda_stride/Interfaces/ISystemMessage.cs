namespace ProjectZyheeda;

public readonly struct SystemStr {
	public readonly string value;

	public SystemStr(string value) {
		this.value = value;
	}

	public static implicit operator SystemStr(string value) {
		return new SystemStr(value);
	}

	public static explicit operator string(SystemStr value) {
		return value.value;
	}
}

public interface ISystemMessage {
	void Log(SystemStr message);
}
