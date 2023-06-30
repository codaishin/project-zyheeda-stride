namespace ProjectZyheeda;

using System.Collections.Generic;
using System.Linq;
using Stride.Input;

public class InputDispatcher :
	IInputEventListener<KeyEvent>,
	IInputEventListener<MouseButtonEvent>,
	IInputDispatcher {

	private static readonly Dictionary<Keys, InputKeys> keysMap = new() {
		{Keys.LeftShift, InputKeys.ShiftLeft},
		{Keys.CapsLock, InputKeys.CapsLock},
	};
	private static readonly Dictionary<MouseButton, InputKeys> mouseButtonsMap = new(){
		{MouseButton.Left, InputKeys.MouseLeft},
		{MouseButton.Right, InputKeys.MouseRight},
	};

	private readonly HashSet<IInputStream> streams = new();
	private readonly ISystemMessage systemMessage;
	private readonly IPlayerMessage playerMessage;

	public InputDispatcher(InputManager inputManager, ISystemMessage systemMessage, IPlayerMessage playerMessage) {
		this.systemMessage = systemMessage;
		this.playerMessage = playerMessage;
		inputManager.AddListener(this);
	}

	public void ProcessEvent(KeyEvent inputEvent) {
		if (!InputDispatcher.keysMap.TryGetValue(inputEvent.Key, out var key)) {
			this.systemMessage.Log(new SystemError($"{inputEvent.Key} is not mapped to InputKeys"));
			return;
		}
		foreach (var stream in this.streams) {
			stream.ProcessEvent(key, inputEvent.IsDown).Switch(
				errors => {
					this.systemMessage.Log(errors.system.ToArray());
					this.playerMessage.Log(errors.player.ToArray());
				},
				() => { }
			);
		}
	}

	public void ProcessEvent(MouseButtonEvent inputEvent) {
		if (!InputDispatcher.mouseButtonsMap.TryGetValue(inputEvent.Button, out var button)) {
			this.systemMessage.Log(new SystemError($"{inputEvent.Button} is not mapped to InputKeys"));
			return;
		}
		foreach (var stream in this.streams) {
			stream.ProcessEvent(button, inputEvent.IsDown).Switch(
				errors => {
					this.systemMessage.Log(errors.system.ToArray());
					this.playerMessage.Log(errors.player.ToArray());
				},
				() => { }
			);
		}
	}

	public Result Add(IInputStream stream) {
		return this.streams.Add(stream)
			? Result.Ok()
			: Result.SystemError($"{stream}: Can only add one input stream once");
	}

	public Result Remove(IInputStream stream) {
		return this.streams.Remove(stream)
			? Result.Ok()
			: Result.SystemError($"{stream}: Could not be removed, due to not being in the set");
	}
}
