namespace Tests;

using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Input;

public class TestInputDispatcher {
	private IInputStream stream = Mock.Of<IInputStream>();
	private InputDispatcher dispatcher = new(new InputManager(), Mock.Of<ISystemMessage>());
	private ISystemMessage systemMessage = Mock.Of<ISystemMessage>();

	[SetUp]
	public void SetUp() {
		this.stream = Mock.Of<IInputStream>();
		this.dispatcher = new(new InputManager(), this.systemMessage = Mock.Of<ISystemMessage>());
	}

	[Test]
	public void DispatchKeyInput() {
		this.dispatcher!.Streams.Add(this.stream);

		this.dispatcher.ProcessEvent(new KeyEvent { Key = Keys.LeftShift, IsDown = false });
		Mock
			.Get(this.stream)
			.Verify(s => s.ProcessEvent(InputKeys.ShiftLeft, false), Times.Once);

		this.dispatcher.ProcessEvent(new KeyEvent { Key = Keys.LeftShift, IsDown = true });
		Mock
			.Get(this.stream)
			.Verify(s => s.ProcessEvent(InputKeys.ShiftLeft, true), Times.Once);
	}

	[Test]
	public void DispatchUnmappedKeyInput() {
		this.dispatcher!.Streams.Add(this.stream);

		this.dispatcher.ProcessEvent(new KeyEvent { Key = Keys.Home, IsDown = false });

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log(new SystemStr($"{Keys.Home} is not mapped to InputKeys")));
	}

	[Test]
	public void DispatchMouseInput() {
		this.dispatcher!.Streams.Add(this.stream);

		this.dispatcher.ProcessEvent(new MouseButtonEvent { Button = MouseButton.Left, IsDown = false });
		Mock
			.Get(this.stream)
			.Verify(s => s.ProcessEvent(InputKeys.MouseLeft, false), Times.Once);

		this.dispatcher.ProcessEvent(new MouseButtonEvent { Button = MouseButton.Right, IsDown = true });
		Mock
			.Get(this.stream)
			.Verify(s => s.ProcessEvent(InputKeys.MouseRight, true), Times.Once);
	}

	[Test]
	public void DispatchUnmappedMouseInput() {
		this.dispatcher!.Streams.Add(this.stream);

		this.dispatcher.ProcessEvent(new MouseButtonEvent { Button = MouseButton.Extended1, IsDown = false });

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log(new SystemStr($"{MouseButton.Extended1} is not mapped to InputKeys")));
	}
}
