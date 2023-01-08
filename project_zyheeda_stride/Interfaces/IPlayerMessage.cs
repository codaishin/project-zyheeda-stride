namespace ProjectZyheeda;

public readonly struct PlayerString {
	public readonly string value;

	public PlayerString(string value) {
		this.value = value;
	}
}

public interface IPlayerMessage {
	void Log(PlayerString message);
}
