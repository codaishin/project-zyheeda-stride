namespace ProjectZyheeda;

public readonly struct WaitFrame { };

public readonly struct WaitMilliSeconds {
	public readonly int milliSeconds;

	public WaitMilliSeconds(int seconds) {
		this.milliSeconds = seconds;
	}
}
