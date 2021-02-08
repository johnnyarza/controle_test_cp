package task;

import java.util.function.Function;

import javafx.concurrent.Task;

public class DBTask<S, R> extends Task<R> {
	private Function<S, R> func;
	private S s;

	public DBTask(S service,Function<S, R> func) {
		this.s = service;
		this.func = func;
	}

	@Override
	protected R call() throws Exception {
		return func.apply(s);
	};

}