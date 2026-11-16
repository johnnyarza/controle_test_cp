package application.log;

public class LogException extends RuntimeException{

	private static final long serialVersionUID = 1L;
	
	public LogException (String msg) {
		super(msg);
	}

}
