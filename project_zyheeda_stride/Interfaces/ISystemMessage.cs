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

	public override string ToString() {
		return $"SystemError({this.value})";
	}
}

public interface ISystemMessage {
	void Log(params SystemError[] errors);
}
