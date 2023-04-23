using ProjectZyheeda;
using Stride.Engine;

using var game = new Game();
game.Services.AddService<IInputManagerWrapper>(new InputManagerWrapper(game));
game.Services.AddService<IAnimation>(new Animation());
game.Services.AddService<ISystemMessage>(new SystemMessage());
game.Services.AddService<IPlayerMessage>(new PlayerMessage(game));
game.Services.AddService<IPrefabLoader>(new PrefabLoader());
game.Run();
