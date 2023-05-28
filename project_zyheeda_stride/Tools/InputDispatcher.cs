namespace ProjectZyheeda;

using System.Collections.Generic;
using Stride.Input;

public class InputDispatcher :
	IInputEventListener<KeyEvent>,
	IInputEventListener<MouseButtonEvent>,
	IInputDispatcher {

	private static readonly Dictionary<Keys, InputKeys> keysMap = new() {
		{Keys.LeftShift, InputKeys.ShiftLeft},
	};
	private static readonly Dictionary<MouseButton, InputKeys> mouseButtonsMap = new(){
		{MouseButton.Left, InputKeys.MouseLeft},
		{MouseButton.Right, InputKeys.MouseRight},
	};

	private readonly HashSet<IInputStream> streams = new();
	private readonly ISystemMessage systemMessage;

	public InputDispatcher(InputManager inputManager, ISystemMessage systemMessage) {
		this.systemMessage = systemMessage;
		inputManager.AddListener(this);
	}

	public void ProcessEvent(KeyEvent inputEvent) {
		if (!InputDispatcher.keysMap.TryGetValue(inputEvent.Key, out var key)) {
			this.systemMessage.Log(new SystemError($"{inputEvent.Key} is not mapped to InputKeys"));
			return;
		}
		foreach (var stream in this.streams) {
			stream.ProcessEvent(key, inputEvent.IsDown);
		}
	}

	public void ProcessEvent(MouseButtonEvent inputEvent) {
		if (!InputDispatcher.mouseButtonsMap.TryGetValue(inputEvent.Button, out var button)) {
			this.systemMessage.Log(new SystemError($"{inputEvent.Button} is not mapped to InputKeys"));
			return;
		}
		foreach (var stream in this.streams) {
			stream.ProcessEvent(button, inputEvent.IsDown);
		}
	}

	public void Add(IInputStream stream) {
		_ = this.streams.Add(stream);
	}

	public void Remove(IInputStream stream) {
		_ = this.streams.Remove(stream);
	}
}
