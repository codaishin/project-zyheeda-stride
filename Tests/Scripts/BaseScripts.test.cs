namespace Tests;

using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Input;

public class TestEssentialServices {
	private readonly IGame game = Mock.Of<IGame>();

	[SetUp]
	public void Setup() {
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

	[Test]
	public void GetEssentialServices() {
		var essentialServices = new EssentialServices(this.game);

		Assert.Multiple(() => {
			Assert.That(essentialServices.inputWrapper, Is.SameAs(this.game.Services.GetService<IInputWrapper>()));
			Assert.That(essentialServices.animation, Is.SameAs(this.game.Services.GetService<IAnimation>()));
			Assert.That(essentialServices.systemMessage, Is.SameAs(this.game.Services.GetService<ISystemMessage>()));
			Assert.That(essentialServices.playerMessage, Is.SameAs(this.game.Services.GetService<IPlayerMessage>()));
			Assert.That(essentialServices.prefabLoader, Is.SameAs(this.game.Services.GetService<IPrefabLoader>()));
			Assert.That(essentialServices.inputDispatcher, Is.SameAs(this.game.Services.GetService<IInputDispatcher>()));
		});
	}

	[Test]
	public void MissingInputManager() {
		this.game.Services.RemoveService<IInputWrapper>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			Assert.That(essentialServices.inputWrapper, Is.InstanceOf<InputWrapper>());
			Assert.That(this.game.Services.GetSafeServiceAs<IInputWrapper>, Is.SameAs(essentialServices.inputWrapper));
		});
	}

	[Test]
	public void MissingAnimation() {
		this.game.Services.RemoveService<IAnimation>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			Assert.That(essentialServices.animation, Is.InstanceOf<Animation>());
			Assert.That(this.game.Services.GetSafeServiceAs<IAnimation>, Is.SameAs(essentialServices.animation));
		});
	}

	[Test]
	public void MissingSystemMessage() {
		this.game.Services.RemoveService<ISystemMessage>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			Assert.That(essentialServices.systemMessage, Is.InstanceOf<ISystemMessage>());
			Assert.That(this.game.Services.GetSafeServiceAs<ISystemMessage>, Is.SameAs(essentialServices.systemMessage));
		});
	}

	[Test]
	public void MissingPlayerMessage() {
		this.game.Services.RemoveService<IPlayerMessage>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			Assert.That(essentialServices.playerMessage, Is.InstanceOf<IPlayerMessage>());
			Assert.That(this.game.Services.GetSafeServiceAs<IPlayerMessage>, Is.SameAs(essentialServices.playerMessage));
		});
	}

	[Test]
	public void MissingPrefabLoader() {
		this.game.Services.RemoveService<IPrefabLoader>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			Assert.That(essentialServices.prefabLoader, Is.InstanceOf<IPrefabLoader>());
			Assert.That(this.game.Services.GetSafeServiceAs<IPrefabLoader>, Is.SameAs(essentialServices.prefabLoader));
		});
	}

	[Test]
	public void MissingInputDispatcher() {
		this.game.Services.RemoveService<IPrefabLoader>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			Assert.That(essentialServices.inputDispatcher, Is.InstanceOf<IInputDispatcher>());
			Assert.That(this.game.Services.GetSafeServiceAs<IInputDispatcher>, Is.SameAs(essentialServices.inputDispatcher));
		});
	}
}
