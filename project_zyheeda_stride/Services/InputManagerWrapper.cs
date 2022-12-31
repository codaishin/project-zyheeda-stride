namespace ProjectZyheeda;

using Stride.Core.Mathematics;
using Stride.Input;

public interface IInputManagerWrapper {
	Vector2 MousePosition { get; }
	bool IsKeyPressed(Keys key);
	bool IsKeyReleased(Keys key);
	bool IsMouseButtonPressed(MouseButton button);
	bool IsMouseButtonReleased(MouseButton button);
}

public class InputManagerWrapper : IInputManagerWrapper {
	private readonly InputManager inputManager;

	public Vector2 MousePosition => this.inputManager.Mouse.Position;

	public InputManagerWrapper(InputManager inputManager) {
		this.inputManager = inputManager;
	}

	public bool IsKeyPressed(Keys key) {
		return this.inputManager.IsKeyPressed(key);
	}

	public bool IsKeyReleased(Keys key) {
		return this.inputManager.IsKeyReleased(key);
	}

	public bool IsMouseButtonPressed(MouseButton button) {
		return this.inputManager.IsMouseButtonPressed(button);
	}

	public bool IsMouseButtonReleased(MouseButton button) {
		return this.inputManager.IsMouseButtonReleased(button);
	}
}
