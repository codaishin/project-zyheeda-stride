namespace ProjectZyheeda;

using Stride.Core.Diagnostics;

public class SystemMessage : ISystemMessage {
	private ILogger? logger;

	private ILogger Logger =>
		this.logger
			??= GlobalLogger.GetLogger(this.GetType().Name);

	public void Log(SystemStr message) {
		this.Logger.Error(message.value);
	}
}
