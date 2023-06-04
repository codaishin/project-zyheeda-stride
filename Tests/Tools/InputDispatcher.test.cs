namespace Tests;

using System.Linq;
using Moq;
using ProjectZyheeda;
using Stride.Input;
using Xunit;

public class TestInputDispatcher {
	private readonly IInputStream stream;
	private readonly InputDispatcher dispatcher;
	private readonly ISystemMessage systemMessage;

	public TestInputDispatcher() {
		this.stream = Mock.Of<IInputStream>();
		this.systemMessage = Mock.Of<ISystemMessage>();
		this.dispatcher = new(new InputManager(), this.systemMessage);
	}

	[Fact]
	public void DispatchKeyInput() {
		_ = this.dispatcher.Add(this.stream);

		this.dispatcher.ProcessEvent(new KeyEvent { Key = Keys.LeftShift, IsDown = false });
		Mock
			.Get(this.stream)
			.Verify(s => s.ProcessEvent(InputKeys.ShiftLeft, false), Times.Once);

		this.dispatcher.ProcessEvent(new KeyEvent { Key = Keys.LeftShift, IsDown = true });
		Mock
			.Get(this.stream)
			.Verify(s => s.ProcessEvent(InputKeys.ShiftLeft, true), Times.Once);
	}

	[Fact]
	public void DispatchUnmappedKeyInput() {
		_ = this.dispatcher.Add(this.stream);

		this.dispatcher.ProcessEvent(new KeyEvent { Key = Keys.Home, IsDown = false });

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log(new SystemError($"{Keys.Home} is not mapped to InputKeys")));
	}

	[Fact]
	public void DispatchMouseInput() {
		_ = this.dispatcher.Add(this.stream);

		this.dispatcher.ProcessEvent(new MouseButtonEvent { Button = MouseButton.Left, IsDown = false });
		Mock
			.Get(this.stream)
			.Verify(s => s.ProcessEvent(InputKeys.MouseLeft, false), Times.Once);

		this.dispatcher.ProcessEvent(new MouseButtonEvent { Button = MouseButton.Right, IsDown = true });
		Mock
			.Get(this.stream)
			.Verify(s => s.ProcessEvent(InputKeys.MouseRight, true), Times.Once);
	}

	[Fact]
	public void DispatchUnmappedMouseInput() {
		_ = this.dispatcher.Add(this.stream);

		this.dispatcher.ProcessEvent(new MouseButtonEvent { Button = MouseButton.Extended1, IsDown = false });

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log(new SystemError($"{MouseButton.Extended1} is not mapped to InputKeys")));
	}

	[Fact]
	public void MultipleAddsAreProcessedJustOnce() {
		_ = this.dispatcher.Add(this.stream);
		_ = this.dispatcher.Add(this.stream);
		_ = this.dispatcher.Add(this.stream);

		this.dispatcher.ProcessEvent(new MouseButtonEvent { Button = MouseButton.Left, IsDown = false });

		Mock
			.Get(this.stream)
			.Verify(s => s.ProcessEvent(InputKeys.MouseLeft, false), Times.Once);
	}

	[Fact]
	public void AddResults() {
		var ok = this.dispatcher.Add(this.stream).Switch(_ => false, () => true);
		Assert.True(ok);

		var error = this.dispatcher.Add(this.stream).Switch<string>(
			errors => errors.system.First(),
			() => "no error"
		);
		Assert.Equal($"{this.stream}: Can only add one input stream once", error);
	}

	[Fact]
	public void RemoveStream() {
		_ = this.dispatcher.Add(this.stream);
		_ = this.dispatcher.Remove(this.stream);

		this.dispatcher.ProcessEvent(new MouseButtonEvent { Button = MouseButton.Left, IsDown = false });

		Mock
			.Get(this.stream)
			.Verify(s => s.ProcessEvent(InputKeys.MouseLeft, false), Times.Never);
	}

	[Fact]
	public void RemoveResults() {
		_ = this.dispatcher.Add(this.stream).Switch(_ => false, () => true);
		var ok = this.dispatcher.Remove(this.stream).Switch(_ => false, () => true);
		Assert.True(ok);

		var error = this.dispatcher.Remove(this.stream).Switch<string>(
			errors => errors.system.First(),
			() => "no error"
		);
		Assert.Equal($"{this.stream}: Could not be removed, due to not being in the set", error);
	}
}
