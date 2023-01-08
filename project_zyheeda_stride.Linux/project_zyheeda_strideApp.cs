using ProjectZyheeda;
using Stride.Engine;

using var game = new Game();
game.Services.AddService<IInputManagerWrapper>(new InputManagerWrapper(game));
game.Services.AddService<IGetAnimation>(new GetAnimations());
game.Services.AddService<ISystemMessage>(new SystemMessage());
game.Services.AddService<IPlayerMessage>(new PlayerMessage(game));
game.Run();
