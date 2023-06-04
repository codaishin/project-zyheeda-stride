namespace Tests;

using Moq;
using ProjectZyheeda;
using Stride.Core;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Input;
using Xunit;

public class TestEssentialServices {
	private readonly IGame game;

	public TestEssentialServices() {
		this.game = Mock.Of<IGame>();
		var service = new ServiceRegistry();
		_ = Mock
			.Get(this.game)
			.Setup(g => g.Services)
			.Returns(service);

		service.AddService(Mock.Of<IInputWrapper>());
		service.AddService(Mock.Of<IAnimation>());
		service.AddService(Mock.Of<ISystemMessage>());
		service.AddService(Mock.Of<IPlayerMessage>());
		service.AddService(Mock.Of<IPrefabLoader>());
		service.AddService(Mock.Of<IInputDispatcher>());
		service.AddService(new InputManager());
		service.AddService(new ScriptSystem(service));
	}

	[Fact]
	public void GetEssentialServices() {
		var essentialServices = new EssentialServices(this.game);

		Assert.Multiple(() => {
			Assert.Same(this.game.Services.GetService<IInputWrapper>(), essentialServices.inputWrapper);
			Assert.Same(this.game.Services.GetService<IAnimation>(), essentialServices.animation);
			Assert.Same(this.game.Services.GetService<ISystemMessage>(), essentialServices.systemMessage);
			Assert.Same(this.game.Services.GetService<IPlayerMessage>(), essentialServices.playerMessage);
			Assert.Same(this.game.Services.GetService<IPrefabLoader>(), essentialServices.prefabLoader);
			Assert.Same(this.game.Services.GetService<IInputDispatcher>(), essentialServices.inputDispatcher);
		});
	}

	[Fact]
	public void MissingInputManager() {
		this.game.Services.RemoveService<IInputWrapper>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			_ = Assert.IsType<InputWrapper>(essentialServices.inputWrapper);
			Assert.Same(essentialServices.inputWrapper, this.game.Services.GetSafeServiceAs<IInputWrapper>());
		});
	}

	[Fact]
	public void MissingAnimation() {
		this.game.Services.RemoveService<IAnimation>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			_ = Assert.IsType<Animation>(essentialServices.animation);
			Assert.Same(essentialServices.animation, this.game.Services.GetSafeServiceAs<IAnimation>());
		});
	}

	[Fact]
	public void MissingSystemMessage() {
		this.game.Services.RemoveService<ISystemMessage>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			_ = Assert.IsAssignableFrom<ISystemMessage>(essentialServices.systemMessage);
			Assert.Same(essentialServices.systemMessage, this.game.Services.GetSafeServiceAs<ISystemMessage>());
		});
	}

	[Fact]
	public void MissingPlayerMessage() {
		this.game.Services.RemoveService<IPlayerMessage>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			_ = Assert.IsAssignableFrom<IPlayerMessage>(essentialServices.playerMessage);
			Assert.Same(essentialServices.playerMessage, this.game.Services.GetSafeServiceAs<IPlayerMessage>());
		});
	}

	[Fact]
	public void MissingPrefabLoader() {
		this.game.Services.RemoveService<IPrefabLoader>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			_ = Assert.IsAssignableFrom<IPrefabLoader>(essentialServices.prefabLoader);
			Assert.Same(essentialServices.prefabLoader, this.game.Services.GetSafeServiceAs<IPrefabLoader>());
		});
	}

	[Fact]
	public void MissingInputDispatcher() {
		this.game.Services.RemoveService<IPrefabLoader>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			_ = Assert.IsAssignableFrom<IInputDispatcher>(essentialServices.inputDispatcher);
			Assert.Same(essentialServices.inputDispatcher, this.game.Services.GetSafeServiceAs<IInputDispatcher>());
		});
	}
}
