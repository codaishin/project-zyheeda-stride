namespace Tests;

using System.Linq;
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
		this.systemMessage = Mock.Of<ISystemMessage>();
		this.dispatcher = new(new InputManager(), this.systemMessage);
	}

	[Test]
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

	[Test]
	public void DispatchUnmappedKeyInput() {
		_ = this.dispatcher.Add(this.stream);

		this.dispatcher.ProcessEvent(new KeyEvent { Key = Keys.Home, IsDown = false });

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log(new SystemError($"{Keys.Home} is not mapped to InputKeys")));
	}

	[Test]
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

	[Test]
	public void DispatchUnmappedMouseInput() {
		_ = this.dispatcher.Add(this.stream);

		this.dispatcher.ProcessEvent(new MouseButtonEvent { Button = MouseButton.Extended1, IsDown = false });

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log(new SystemError($"{MouseButton.Extended1} is not mapped to InputKeys")));
	}

	[Test]
	public void MultipleAddsAreProcessedJustOnce() {
		_ = this.dispatcher.Add(this.stream);
		_ = this.dispatcher.Add(this.stream);
		_ = this.dispatcher.Add(this.stream);

		this.dispatcher.ProcessEvent(new MouseButtonEvent { Button = MouseButton.Left, IsDown = false });

		Mock
			.Get(this.stream)
			.Verify(s => s.ProcessEvent(InputKeys.MouseLeft, false), Times.Once);
	}

	[Test]
	public void AddResults() {
		var ok = this.dispatcher.Add(this.stream).Switch(_ => false, () => true);
		Assert.That(ok, Is.True);

		var error = this.dispatcher.Add(this.stream).Switch<string>(
			errors => errors.system.First(),
			() => "no error"
		);
		Assert.That(error, Is.EqualTo($"{this.stream}: Can only add one input stream once"));
	}

	[Test]
	public void RemoveStream() {
		_ = this.dispatcher.Add(this.stream);
		_ = this.dispatcher.Remove(this.stream);

		this.dispatcher.ProcessEvent(new MouseButtonEvent { Button = MouseButton.Left, IsDown = false });

		Mock
			.Get(this.stream)
			.Verify(s => s.ProcessEvent(InputKeys.MouseLeft, false), Times.Never);
	}

	[Test]
	public void RemoveResults() {
		_ = this.dispatcher.Add(this.stream).Switch(_ => false, () => true);
		var ok = this.dispatcher.Remove(this.stream).Switch(_ => false, () => true);
		Assert.That(ok, Is.True);

		var error = this.dispatcher.Remove(this.stream).Switch<string>(
			errors => errors.system.First(),
			() => "no error"
		);
		Assert.That(error, Is.EqualTo($"{this.stream}: Could not be removed, due to not being in the set"));
	}
}
