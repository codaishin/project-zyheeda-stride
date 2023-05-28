namespace ProjectZyheeda;

using Stride.Core.Diagnostics;

public class SystemMessage : ISystemMessage {
	private ILogger? logger;

	private ILogger Logger =>
		this.logger
			??= GlobalLogger.GetLogger(this.GetType().Name);

	public void Log(SystemError message) {
		this.Logger.Error(message.value);
	}
}
