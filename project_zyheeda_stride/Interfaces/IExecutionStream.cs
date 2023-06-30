namespace ProjectZyheeda;

using System.Threading.Tasks;

public interface IExecutionStream {
	Task<Result<FExecute>> NewExecute();
	Result ProcessEvent(InputKeys key, bool isDown);
}

public interface IExecutionStreamEditor : IExecutionStream { }
