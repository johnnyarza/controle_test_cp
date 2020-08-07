package application.db;

import java.io.FileInputStream;
import java.io.IOException;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.Properties;

import application.util.EncriptaDecriptaApacheCodec;
import gui.util.Alerts;
import javafx.scene.control.Alert.AlertType;

public class DB {

	private static Connection conn = null;

	public static Boolean testConnection() {
		try {
			if (conn == null) {
				Properties props = loadProperties();
				String url = props.getProperty("dburl");
				DriverManager.registerDriver(new com.mysql.cj.jdbc.Driver());
				conn = DriverManager.getConnection(url, props);
			}
			return true;
			
		} catch (SQLException e) {
			Alerts.showAlert("Error", "SQLException", "Error al conectarse al servidor", AlertType.ERROR);
			return false;
		}
	}

	public static Connection getConnection() {
		try {
			if (conn == null) {
				Properties props = loadProperties();
				String url = props.getProperty("dburl");
				DriverManager.registerDriver(new com.mysql.cj.jdbc.Driver());
				conn = DriverManager.getConnection(url, props);
			}
			return conn;
		} catch (SQLException e) {
			Alerts.showAlert("Error", "SQLException", e.getMessage(), AlertType.ERROR);
			return null;
		}
	}

	public static void closeConnection() {
		if (conn != null) {
			try {
				conn.close();
			} catch (SQLException e) {
				// throw new DbException(e.getMessage());
				Alerts.showAlert("Error", "SQLException", e.getMessage(), AlertType.ERROR);
			}
		}
	}

	private static Properties loadProperties() {
		
		Path path = Paths.get(System.getProperty("user.home"), "cp_configs", "db.properties");
		
		try (FileInputStream fs = new FileInputStream(path.toFile())) {
			Properties props = new Properties();
			props.load(fs);
			if (!props.getProperty("password").trim().equals("")) {
				String pass = EncriptaDecriptaApacheCodec.decodificaBase64Decoder(props.getProperty("password"));
				props.put("password", pass);
			}
			return props;
		} catch (IOException e) {
			throw new DbException(e.getMessage());
		}

	}

	public static void closeStatement(Statement st) {
		try {
			st.close();
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		}
	}

	public static void closeResultSet(ResultSet rs) {
		try {
			rs.close();
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		}
	}
}
