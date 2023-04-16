namespace ProjectZyheeda;

public enum InputAction {
	None = 0,
	Run = 1,
	Chain = 2,
}

public interface IInput {
	InputAction GetAction(IInputManagerWrapper input);
}
