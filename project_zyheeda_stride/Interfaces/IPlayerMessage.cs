namespace ProjectZyheeda;

public readonly struct PlayerError {
	public readonly string value;

	public PlayerError(string value) {
		this.value = value;
	}

	public static implicit operator PlayerError(string value) {
		return new PlayerError(value);
	}

	public static implicit operator string(PlayerError value) {
		return value.value;
	}
}

public interface IPlayerMessage {
	void Log(params PlayerError[] errors);
}
