package application;

import java.io.File;
import java.io.IOException;
import java.nio.file.Paths;
import java.util.HashMap;
import java.util.Map;

import application.db.DB;
import application.service.MigrationService;
import application.util.EncriptaDecriptaApacheCodec;
import application.util.FileUtils;
import gui.util.Alerts;
import javafx.application.Application;
import javafx.fxml.FXMLLoader;
import javafx.scene.Scene;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.ScrollPane;
import javafx.stage.Stage;

public class Program extends Application {

	private static Scene mainScene;

	@Override
	public void start(Stage primaryStage) {
		try {
			createConfigFiles();
			FXMLLoader loader = new FXMLLoader(getClass().getResource("/gui/MainView.fxml"));
			ScrollPane scrollPane = loader.load();

			scrollPane.setFitToHeight(true);
			scrollPane.setFitToWidth(true);

			mainScene = new Scene(scrollPane);
			mainScene.getStylesheets().add("/gui/application.css");
			primaryStage.setScene(mainScene);
			
			primaryStage.setTitle("Probeta Control");
			primaryStage.show();
			
			

			if (DB.testConnection()) {
				initiateTables();
			}
			
		} catch (IOException e) {
			e.printStackTrace();
			Alerts.showAlert("Error", "Error al abrir ventana", e.getMessage(), AlertType.ERROR);
		}
	}

	public static Scene getMainScene() {
		return mainScene;
	}

	public static void main(String[] args) {
		launch(args);

	}
	private static void initiateTables()  {
		MigrationService service = new MigrationService();
		try {
			service.initiateDB();	
		} catch (Exception e) {
			Alerts.showAlert("Error", "Error al iniciar el DB", e.getMessage(), AlertType.ERROR);
		}
		
	}
	
	private static void createConfigFiles() throws IOException {
		File directory = new File(Paths.get(System.getProperty("user.home"), "cp_configs").toFile().toURI());
		directory.mkdir();
		
		
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
