namespace ProjectZyheeda;

public readonly struct PlayerStr {
	public readonly string value;

	public PlayerStr(string value) {
		this.value = value;
	}

	public static implicit operator PlayerStr(string value) {
		return new PlayerStr(value);
	}

	public static explicit operator string(PlayerStr value) {
		return value.value;
	}
}

public interface IPlayerMessage {
	void Log(PlayerStr message);
}
