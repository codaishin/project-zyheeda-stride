namespace ProjectZyheeda;

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Profiling;


public class PlayerMessage : IPlayerMessage {
	private readonly Game game;
	private DebugTextSystem? debugText;

	private DebugTextSystem DebugText =>
		this.debugText
			??= this.game.Services.GetService<DebugTextSystem>();

	public PlayerMessage(Game game) {
		this.game = game;
	}

	public void Log(PlayerStr message) {
		_ = this.game.Script.AddTask(async () => {
			var start = this.game.UpdateTime.Total;
			do {
				this.DebugText.Print(message.value, new Int2(20, 20));
				_ = await this.game.Script.NextFrame();
			} while (this.game.UpdateTime.Total.Seconds - start.Seconds < 2);
		});
	}
}
