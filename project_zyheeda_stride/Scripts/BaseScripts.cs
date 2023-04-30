namespace ProjectZyheeda;

using System;
using Stride.Core;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

public readonly struct EssentialServices {

	public readonly IInputManagerWrapper inputManager;
	public readonly IAnimation animation;
	public readonly ISystemMessage systemMessage;
	public readonly IPlayerMessage playerMessage;
	public readonly IPrefabLoader prefabLoader;
	public readonly IInputDispatcher inputDispatcher;

	public EssentialServices(IGame game) {
		this.inputManager = EssentialServices.GetOrCreate<IInputManagerWrapper>(game, () => new InputManagerWrapper(game));
		this.animation = EssentialServices.GetOrCreate<IAnimation, Animation>(game);
		this.systemMessage = EssentialServices.GetOrCreate<ISystemMessage, SystemMessage>(game);
		this.playerMessage = EssentialServices.GetOrCreate<IPlayerMessage>(game, () => new PlayerMessage(game));
		this.prefabLoader = EssentialServices.GetOrCreate<IPrefabLoader, PrefabLoader>(game);

		var input = game.Services.GetSafeServiceAs<InputManager>();
		var systemMessage = this.systemMessage;
		this.inputDispatcher = EssentialServices.GetOrCreate<IInputDispatcher>(
			game,
			() => new InputDispatcher(input, systemMessage)
		);
	}

	private static TKey GetOrCreate<TKey>(IGame game, Func<TKey> newService)
		where TKey :
			class {
		var service = game.Services.GetService<TKey>();
		if (service is not null) {
			return service;
		}
		game.Services.AddService(service = newService());
		return service;
	}

	private static TKey GetOrCreate<TKey, TInstance>(IGame game)
		where TKey :
			class
		where TInstance :
			TKey,
			new() {
		var service = game.Services.GetService<TKey>();
		if (service is not null) {
			return service;
		}
		game.Services.AddService(service = new TInstance());
		return service;
	}
}

public abstract class ProjectZyheedaStartupScript : StartupScript {
	private IMaybe<EssentialServices> essentialServices = Maybe.None<EssentialServices>();

	protected EssentialServices EssentialServices => this.essentialServices.Switch(
		service => service,
		this.NewEssentialService
	);

	private EssentialServices NewEssentialService() {
		var services = new EssentialServices(this.Game);
		this.essentialServices = Maybe.Some(services);
		return services;
	}
}

public abstract class ProjectZyheedaSyncScript : SyncScript {
	private IMaybe<EssentialServices> essentialServices = Maybe.None<EssentialServices>();

	protected EssentialServices EssentialServices => this.essentialServices.Switch(
		service => service,
		this.NewEssentialService
	);

	private EssentialServices NewEssentialService() {
		var services = new EssentialServices(this.Game);
		this.essentialServices = Maybe.Some(services);
		return services;
	}
}

public abstract class ProjectZyheedaAsyncScript : AsyncScript {
	private IMaybe<EssentialServices> essentialServices = Maybe.None<EssentialServices>();

	protected EssentialServices EssentialServices => this.essentialServices.Switch(
		service => service,
		this.NewEssentialService
	);

	private EssentialServices NewEssentialService() {
		var services = new EssentialServices(this.Game);
		this.essentialServices = Maybe.Some(services);
		return services;
	}
}
