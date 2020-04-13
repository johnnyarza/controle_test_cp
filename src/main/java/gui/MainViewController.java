package gui;

import java.io.IOException;
import java.net.URL;
import java.util.ResourceBundle;

import application.Program;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.fxml.Initializable;
import javafx.scene.Node;
import javafx.scene.Scene;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.MenuBar;
import javafx.scene.control.MenuItem;
import javafx.scene.control.ScrollPane;
import javafx.scene.layout.Pane;
import javafx.scene.layout.VBox;
import javafx.stage.Modality;
import javafx.stage.Stage;

public class MainViewController implements Initializable{
	
	@FXML
	private MenuItem onBtNewTestAction;
	
	@FXML
	private MenuItem onBtClientAction;
	
	@FXML
	private MenuBar myMenuBar;
	
	@FXML
	public void onBtNewTestAction(ActionEvent event) {
		Stage parentStage = Utils.getMenuBarStage(myMenuBar);
		createDialogForm("/gui/NewCompresionTestForm.fxml", parentStage);
	}
	
	public void onBtClientAction(ActionEvent event) {
		loadView("/gui/ClientList.fxml");
	}
	
	private void createDialogForm(String absoluteName, Stage parentStage) {
		try {
			
			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();
			
			Stage dialogStage = new Stage();
			dialogStage.setTitle("Nueva rotura");
			dialogStage.setScene(new Scene(pane));
			dialogStage.setResizable(false);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.showAndWait();
			
		}catch (IOException e) {
			e.printStackTrace();
		}
	}

	private synchronized void loadView(String absoluteName) {

			try {
				FXMLLoader loader = new FXMLLoader (getClass().getResource(absoluteName));
				VBox newVbox = loader.load();
				
				Scene mainScene = Program.getMainScene();
				VBox mainVBox = (VBox)((ScrollPane)mainScene.getRoot()).getContent();
				
				Node mainMenu = mainVBox.getChildren().get(0);
				mainVBox.getChildren().clear();
				mainVBox.getChildren().add(mainMenu);
				mainVBox.getChildren().addAll(newVbox.getChildren());
			
			} catch (IOException e) {
				Alerts.showAlert("IO Exception", "Error loading view", e.getMessage(), AlertType.ERROR);
			}
	}
	@Override
	public void initialize(URL uri, ResourceBundle rb) {
		// TODO Auto-generated method stub
		
	}

}
