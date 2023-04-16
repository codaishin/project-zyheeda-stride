namespace Tests;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;

public class TestInput : GameTestCollection {
	private IInputManagerWrapper inputManager = Mock.Of<IInputManagerWrapper>();

	[SetUp]
	public void SetupInputManager() {
		this.inputManager = Mock.Of<IInputManagerWrapper>();
		Mock
			.Get(this.inputManager)
			.SetReturnsDefault(false);
	}

	[Test]
	public void ActionOff() {
		_ = Mock
			.Get(this.inputManager)
			.Setup(i => i.IsPressed(InputKeys.MouseLeft))
			.Returns(false);

		var input = new Input {
			activationKey = InputKeys.MouseLeft,
			activation = InputActivation.OnPress,
		};
		var state = input.GetAction(this.inputManager);

		Assert.That(state, Is.EqualTo(InputAction.None));
	}

	[Test]
	public void ActionRunOnPressLeftMouse() {
		_ = Mock
			.Get(this.inputManager)
			.Setup(i => i.IsPressed(InputKeys.MouseLeft))
			.Returns(true);

		var input = new Input {
			activationKey = InputKeys.MouseLeft,
			activation = InputActivation.OnPress,
		};
		var state = input.GetAction(this.inputManager);

		Assert.That(state, Is.EqualTo(InputAction.Run));
	}

	[Test]
	public void ActionRunOnPressRightMouse() {
		_ = Mock
			.Get(this.inputManager)
			.Setup(i => i.IsPressed(InputKeys.MouseRight))
			.Returns(true);

		var input = new Input {
			activationKey = InputKeys.MouseRight,
			activation = InputActivation.OnPress,
		};
		var state = input.GetAction(this.inputManager);

		Assert.That(state, Is.EqualTo(InputAction.Run));
	}

	[Test]
	public void ActionRunOnReleaseLeftMouse() {
		_ = Mock
			.Get(this.inputManager)
			.Setup(i => i.IsReleased(InputKeys.MouseLeft))
			.Returns(true);

		var input = new Input {
			activationKey = InputKeys.MouseLeft,
			activation = InputActivation.OnRelease,
		};
		var state = input.GetAction(this.inputManager);

		Assert.That(state, Is.EqualTo(InputAction.Run));
	}

	[Test]
	public void ActionRunOnReleaseRightMouse() {
		_ = Mock
			.Get(this.inputManager)
			.Setup(i => i.IsReleased(InputKeys.MouseRight))
			.Returns(true);

		var input = new Input {
			activationKey = InputKeys.MouseRight,
			activation = InputActivation.OnRelease,
		};
		var state = input.GetAction(this.inputManager);

		Assert.That(state, Is.EqualTo(InputAction.Run));
	}

	[Test]
	public void ActionChainOnPressLeftMouseViaShiftLeft() {
		_ = Mock
			.Get(this.inputManager)
			.Setup(i => i.IsPressed(InputKeys.MouseLeft))
			.Returns(true);
		_ = Mock
			.Get(this.inputManager)
			.Setup(i => i.IsDown(InputKeys.ShiftLeft))
			.Returns(true);

		var input = new Input {
			activationKey = InputKeys.MouseLeft,
			activation = InputActivation.OnPress,
			chainKey = InputKeys.ShiftLeft,
		};
		var state = input.GetAction(this.inputManager);

		Assert.That(state, Is.EqualTo(InputAction.Chain));
	}

	[Test]
	public void ActionChainOnPressLeftMouseViaRightMouse() {
		_ = Mock
			.Get(this.inputManager)
			.Setup(i => i.IsPressed(InputKeys.MouseLeft))
			.Returns(true);
		_ = Mock
			.Get(this.inputManager)
			.Setup(i => i.IsDown(InputKeys.MouseRight))
			.Returns(true);

		var input = new Input {
			activationKey = InputKeys.MouseLeft,
			activation = InputActivation.OnPress,
			chainKey = InputKeys.MouseRight,
		};
		var state = input.GetAction(this.inputManager);

		Assert.That(state, Is.EqualTo(InputAction.Chain));
	}
}
