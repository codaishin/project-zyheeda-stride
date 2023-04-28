namespace Tests;

using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core;
using Stride.Engine.Processors;
using Stride.Games;

public class TestEssentialServices {
	private readonly IGame game = Mock.Of<IGame>();

	[SetUp]
	public void Setup() {
		var service = new ServiceRegistry();
		_ = Mock
			.Get(this.game)
			.Setup(g => g.Services)
			.Returns(service);

		service.AddService(Mock.Of<IInputManagerWrapper>());
		service.AddService(Mock.Of<IAnimation>());
		service.AddService(Mock.Of<ISystemMessage>());
		service.AddService(Mock.Of<IPlayerMessage>());
		service.AddService(Mock.Of<IPrefabLoader>());
		service.AddService(new ScriptSystem(service));
	}

	[Test]
	public void GetEssentialServices() {
		var essentialServices = new EssentialServices(this.game);

		Assert.Multiple(() => {
			Assert.That(essentialServices.inputManager, Is.SameAs(this.game.Services.GetService<IInputManagerWrapper>()));
			Assert.That(essentialServices.animation, Is.SameAs(this.game.Services.GetService<IAnimation>()));
			Assert.That(essentialServices.systemMessage, Is.SameAs(this.game.Services.GetService<ISystemMessage>()));
			Assert.That(essentialServices.playerMessage, Is.SameAs(this.game.Services.GetService<IPlayerMessage>()));
			Assert.That(essentialServices.prefabLoader, Is.SameAs(this.game.Services.GetService<IPrefabLoader>()));
		});
	}

	[Test]
	public void MissingInputManager() {
		this.game.Services.RemoveService<IInputManagerWrapper>();
		var essentialServices = new EssentialServices(this.game);
		Assert.Multiple(() => {
			Assert.That(essentialServices.inputManager, Is.InstanceOf<InputManagerWrapper>());
			Assert.That(this.game.Services.GetSafeServiceAs<IInputManagerWrapper>, Is.SameAs(essentialServices.inputManager));
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
}
