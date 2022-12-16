using ProjectZyheeda;
using Stride.Engine;
using Stride.Input;

using var game = new Game();
var input = game.Services.GetService<InputManager>();
game.Services.AddService<IInputWrapper>(new InputWrapper(input));
game.Run();
