namespace ProjectZyheeda;

public readonly struct PlayerStr {
	public readonly string value;

	public PlayerStr(string value) {
		this.value = value;
	}
}

public interface IPlayerMessage {
	void Log(PlayerStr message);
}
