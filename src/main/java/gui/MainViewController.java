package gui;

import java.io.IOException;
import java.net.URL;
import java.util.ResourceBundle;
import java.util.function.Consumer;

import application.Program;
import application.service.ClientService;
import application.service.CompresionTestService;
import gui.util.Alerts;
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
		Stage parentStage = (Stage) Program.getMainScene().getWindow();//Utils.getMenuBarStage(myMenuBar);
		createDialogForm("/gui/NewCompresionTestForm.fxml", parentStage, 
				(NewCompresionTestFormController controller) -> {
					controller.setCompresionTestService(new CompresionTestService());
					controller.setClientService(new ClientService());
					controller.loadAssociatedObjects();
				});
	}
	
	public void onBtClientAction(ActionEvent event) {
		loadView("/gui/ClientList.fxml", (ClientListController controller) -> {
			controller.setClientService(new ClientService());
			controller.updateTableView();
		});
	}
	
	private <T> void createDialogForm(String absoluteName, Stage parentStage,Consumer<T> initializingAction) {
		try {
			
			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();
		
			T controller = loader.getController();
			initializingAction.accept(controller);
			
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

	private synchronized <T> void loadView(String absoluteName, Consumer<T> initializingAction) {

			try {
				FXMLLoader loader = new FXMLLoader (getClass().getResource(absoluteName));
				VBox newVbox = loader.load();
				
				Scene mainScene = Program.getMainScene();
				VBox mainVBox = (VBox)((ScrollPane)mainScene.getRoot()).getContent();
				
				Node mainMenu = mainVBox.getChildren().get(0);
				mainVBox.getChildren().clear();
				mainVBox.getChildren().add(mainMenu);
				mainVBox.getChildren().addAll(newVbox.getChildren());
				
				T controller = loader.getController();
				initializingAction.accept(controller);
			
			} catch (IOException e) {
				Alerts.showAlert("IO Exception", "Error loading view", e.getMessage(), AlertType.ERROR);
			}
	}
	
	@Override
	public void initialize(URL uri, ResourceBundle rb) {
		// TODO Auto-generated method stub
		
	}

}
