namespace Tests;

using System.Collections.Generic;
using System.Threading.Tasks;

public static class AsyncExtensions {
	public static async IAsyncEnumerable<T> ToTasks<T>(this IEnumerable<T> items) {
		foreach (var item in items) {
			yield return await Task.FromResult(item);
		}
	}
}
