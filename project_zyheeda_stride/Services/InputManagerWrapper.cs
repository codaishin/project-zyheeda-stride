namespace ProjectZyheeda;

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

public interface IInputManagerWrapper {
	Vector2 MousePosition { get; }
	bool IsKeyPressed(Keys key);
	bool IsKeyReleased(Keys key);
	bool IsMouseButtonPressed(MouseButton button);
	bool IsMouseButtonReleased(MouseButton button);
}

public class InputManagerWrapper : IInputManagerWrapper {
	private readonly Game game;
	private InputManager? inputManager;

	private InputManager InputManager => this.inputManager is null
		? this.inputManager = this.game.Services.GetService<InputManager>()
		: this.inputManager;

	public Vector2 MousePosition => this.InputManager.Mouse.Position;

	public InputManagerWrapper(Game game) {
		this.game = game;
	}

	public bool IsKeyPressed(Keys key) {
		return this.InputManager.IsKeyPressed(key);
	}

	public bool IsKeyReleased(Keys key) {
		return this.InputManager.IsKeyReleased(key);
	}

	public bool IsMouseButtonPressed(MouseButton button) {
		return this.InputManager.IsMouseButtonPressed(button);
	}

	public bool IsMouseButtonReleased(MouseButton button) {
		return this.InputManager.IsMouseButtonReleased(button);
	}
}
