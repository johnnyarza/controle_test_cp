package application;

import java.io.IOException;
import java.nio.file.Paths;
import java.util.HashMap;
import java.util.Map;

import application.db.DB;
import application.util.EncriptaDecriptaApacheCodec;
import application.util.FileUtils;
import javafx.application.Application;
import javafx.fxml.FXMLLoader;
import javafx.scene.Scene;
import javafx.scene.control.ScrollPane;
import javafx.stage.Stage;

public class Program extends Application {

	private static Scene mainScene;

	@Override
	public void start(Stage primaryStage) {
		try {
			FXMLLoader loader = new FXMLLoader(getClass().getResource("/gui/MainView.fxml"));
			ScrollPane scrollPane = loader.load();

			scrollPane.setFitToHeight(true);
			scrollPane.setFitToWidth(true);

			mainScene = new Scene(scrollPane);
			mainScene.getStylesheets().add("/gui/application.css");
			primaryStage.setScene(mainScene);
			
			primaryStage.setTitle("Probeta Control");
			primaryStage.show();
			
			createConfigFiles();

			DB.testConnection();
		} catch (IOException e) {
			e.printStackTrace();
		}
	}

	public static Scene getMainScene() {
		return mainScene;
	}

	public static void main(String[] args) {
		launch(args);

	}

	private static void createConfigFiles() throws IOException {
		if (!Paths.get(System.getProperty("user.home"), "cp_configs", "ReportImage.properties").toFile().isFile()) {
			Map<String, String> initialProps = new HashMap<>();
			initialProps.put("carimboPath", "");
			initialProps.put("logoPath", "");
			FileUtils.writeProperties("ReportImage.properties", initialProps);
		}
		
		if (!Paths.get(System.getProperty("user.home"), "cp_configs", "db.properties").toFile().isFile()) {
			Map<String, String> initialProps = new HashMap<>();
			initialProps.put("password", EncriptaDecriptaApacheCodec.codificaBase64Encoder("1a2b3c4d5e6f"));
			initialProps.put("user", "johnny");
			initialProps.put("dburl", "jdbc:mysql://kkreco.duckdns.org:3333/cp_db?useTimezone=true&serverTimezone=UTC");
			initialProps.put("useSSL", "false");
			
			FileUtils.writeProperties("db.properties", initialProps);
		}
	
		
	}
}
