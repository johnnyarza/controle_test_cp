package gui.util;


import java.util.Optional;

import javafx.scene.control.Alert;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.ButtonType;
import javafx.scene.image.Image;
import javafx.stage.Stage;

public class Alerts {

	public static void showAlert(String title, String header, String content, AlertType type) {
		Alert alert = new Alert(type);
		alert.setTitle(title);
		alert.setHeaderText(header);
		alert.setContentText(content);
		//alert.show();
		Stage stage = (Stage) alert.getDialogPane().getScene().getWindow();
		stage.getIcons().add(new Image(Alerts.class.getResourceAsStream("/images/alert.png")));
		stage.showAndWait();
	}
	
	public static Optional<ButtonType> showConfirmationDialog(String title, String header, String content) {
		Alert alert = new Alert(AlertType.CONFIRMATION);
		alert.setTitle(title);
		alert.setHeaderText(header);
		alert.setContentText(content);
		Stage stage = (Stage)alert.getDialogPane().getScene().getWindow();
		stage.getIcons().add(new Image(Alerts.class.getResourceAsStream("/images/confirm.png")));
		return alert.showAndWait();
	}
}