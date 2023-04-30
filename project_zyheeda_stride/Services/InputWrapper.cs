namespace ProjectZyheeda;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Input;

public class InputWrapper : IInputWrapper {
	private readonly InputManager inputManager;

	public Vector2 MousePosition => this.inputManager.Mouse.Position;

	public InputWrapper(IGame game) {
		this.inputManager = game.Services.GetSafeServiceAs<InputManager>();
	}
}
