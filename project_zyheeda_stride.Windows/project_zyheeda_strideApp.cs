using ProjectZyheeda;
using Stride.Engine;

using var game = new Game();
game.Services.AddService<IInputManagerWrapper>(new InputManagerWrapper(game));
game.Run();
