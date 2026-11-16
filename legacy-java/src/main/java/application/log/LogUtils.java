package application.log;

import java.io.IOException;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.logging.FileHandler;
import java.util.logging.Level;
import java.util.logging.Logger;
import java.util.logging.SimpleFormatter;

public class LogUtils {

	Logger logger = Logger.getLogger("MyLog");
	FileHandler fh;
	SimpleFormatter formatter = new SimpleFormatter();
	Path logPath = Paths.get(System.getProperty("user.home"), "cp_configs", "FileLog.log");

	public LogUtils() {
		super();
		try {
			fh = new FileHandler(logPath.toString(), true);
			logger.addHandler(fh);
			fh.setFormatter(formatter);
		} catch (SecurityException e) {
			throw new LogException(e.getMessage());
		} catch (IOException e) {
			throw new LogException(e.getMessage());
		}

	}

	public void doLog(Level level, String msg, Throwable trowable) {
		logger.log(level, msg, trowable);
	}

}
