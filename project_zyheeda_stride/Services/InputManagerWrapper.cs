namespace ProjectZyheeda;

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Input;

public enum InputKeys {
	None = default,
	ShiftLeft,
	MouseLeft,
	MouseRight,
}

public interface IInputManagerWrapper {
	Vector2 MousePosition { get; }
	bool IsPressed(InputKeys key);
	bool IsDown(InputKeys key);
	bool IsReleased(InputKeys key);
}

public class InputManagerWrapper : IInputManagerWrapper {
	private static readonly Dictionary<InputKeys, U<Keys, MouseButton>> map = new() {
		{InputKeys.None, Keys.None},
		{InputKeys.MouseLeft, MouseButton.Left},
		{InputKeys.MouseRight, MouseButton.Right},
		{InputKeys.ShiftLeft, Keys.LeftShift},
	};

	private static U<Keys, MouseButton> Map(InputKeys key) {
		return InputManagerWrapper.map.TryGetValue(key, out var result)
			? result
			: throw new ArgumentException($"mapping from custom key ({key}) to stride key not configured");
	}

	private readonly IGame game;
	private InputManager? inputManager;

	private InputManager InputManager => this.inputManager is null
		? this.inputManager = this.game.Services.GetService<InputManager>()
		: this.inputManager;

	public Vector2 MousePosition => this.InputManager.Mouse.Position;

	public InputManagerWrapper(IGame game) {
		this.game = game;
	}

	public bool IsPressed(InputKeys key) {
		return InputManagerWrapper.Map(key).Switch(
			k => this.InputManager.IsKeyPressed(k),
			m => this.InputManager.IsMouseButtonPressed(m)
		);
	}

	public bool IsDown(InputKeys key) {
		return InputManagerWrapper.Map(key).Switch(
			k => this.InputManager.IsKeyDown(k),
			m => this.InputManager.IsMouseButtonDown(m)
		);
	}

	public bool IsReleased(InputKeys key) {
		return InputManagerWrapper.Map(key).Switch(
			k => this.InputManager.IsKeyReleased(k),
			m => this.InputManager.IsMouseButtonReleased(m)
		);
	}
}
