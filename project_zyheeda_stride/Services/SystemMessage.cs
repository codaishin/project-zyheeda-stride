namespace ProjectZyheeda;

using Stride.Core.Diagnostics;

public class SystemMessage : ISystemMessage {
	private ILogger? logger;

	private ILogger Logger =>
		this.logger
			??= GlobalLogger.GetLogger(this.GetType().Name);

	public void Log(params SystemError[] errors) {
		foreach (var error in errors) {
			this.Logger.Error(error.value);
		}
	}
}
