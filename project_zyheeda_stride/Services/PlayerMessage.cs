namespace ProjectZyheeda;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Profiling;

public class PlayerMessage : IPlayerMessage {
	private readonly IGame game;
	private readonly ScriptSystem script;
	private DebugTextSystem? debugText;

	private DebugTextSystem DebugText =>
		this.debugText
			??= this.game.Services.GetService<DebugTextSystem>();

	public PlayerMessage(IGame game) {
		this.game = game;
		this.script = game.Services.GetSafeServiceAs<ScriptSystem>();
	}

	public void Log(params PlayerError[] errors) {
		_ = this.script.AddTask(async () => {
			var start = this.game.UpdateTime.Total;
			do {
				var height = 20;
				foreach (var error in errors) {
					this.DebugText.Print(error.value, new Int2(20, height += 20));
				}
				_ = await this.script.NextFrame();
			} while (this.game.UpdateTime.Total.Seconds - start.Seconds < 2);
		});
	}
}
