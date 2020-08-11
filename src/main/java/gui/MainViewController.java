package gui;

import java.io.IOException;
import java.net.URL;
import java.nio.file.Paths;
import java.util.ResourceBundle;
import java.util.function.Consumer;

import application.Program;
import application.domaim.CompresionTest;
import application.service.ClientService;
import application.service.CompresionTestListService;
import application.service.CompresionTestService;
import application.service.ConcreteDesignService;
import application.service.CorpoDeProvaService;
import application.service.MaterialService;
import application.service.ProviderService;
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
import javafx.scene.layout.VBox;

public class MainViewController implements Initializable {

	private Boolean btCancelPressed;

	private CompresionTest compresionTest;
	

	@FXML
	private MenuItem BtTest;
	
	@FXML
	private MenuItem BtConfigReport;

	@FXML
	private MenuItem onBtClientAction;

	@FXML
	private MenuItem btProvider;

	@FXML
	private MenuItem btMaterial;

	@FXML
	private MenuItem btDesign;
	
	@FXML
	private MenuItem btConfigConnection;
	 
	@FXML
	private MenuItem btAbout;

	@FXML
	private MenuBar myMenuBar;


	@FXML
	public void onBtTestAction(ActionEvent event) {
		loadView("/gui/LoadCompresionTestView.fxml", (LoadCompresionTestViewController controller) -> {
			controller.setCompresionTestListService(new CompresionTestListService());
			controller.setClientService(new ClientService());
			controller.setCorpoDeProvaService(new CorpoDeProvaService());
			controller.setCompresionTestService(new CompresionTestService());
			controller.updateViewData();
			if (controller.getLateCorpoDeProvaList().size() > 0) {
				Alerts.showAlert("Aviso", "Hay probetas con fecha proxima o retrasadas", "", AlertType.WARNING);
			}
		});
	}

	@FXML
	public void onBtClientAction(ActionEvent event) {
		loadView("/gui/ClientList.fxml", (ClientListController controller) -> {
			controller.setClientService(new ClientService());
			controller.updateTableView();
		});
	}

	@FXML
	public void onBtProviderAction(ActionEvent event) {
		loadView("/gui/ProveedoresView.fxml", (ProveedoresViewController controller) -> {
			controller.setService(new ProviderService());
			controller.updateTableView();
		});
	}

	@FXML
	public void onBtMaterialAction(ActionEvent event) {
		loadView("/gui/MateriaisView.fxml", (MateriaisViewController controller) -> {
			controller.setService(new MaterialService());
			controller.updateTableView();
		});
	}

	@FXML
	public void onBtDesignAction(ActionEvent event) {
		loadView("/gui/ConcreteDesignView.fxml", (ConcreteDesignViewController controller) -> {
			controller.setService(new ConcreteDesignService());
			controller.setCompresionTestService(new CompresionTestService());
			controller.updateTableView();
		});
	}
	
	@FXML
	public void onBtAboutAction(ActionEvent event) {
		loadView("/gui/AboutView.fxml", (ConcreteDesignViewController controller) -> {});
	}
	
	@FXML
	public void onBtConfigReportAction(ActionEvent event) {
		loadView("/gui/ReportConfigView.fxml", (ReportConfigViewController controller) -> {
			
			controller.setImagesProp(Paths.get(System.getProperty("user.home"), "cp_configs", "ReportImage.properties").toFile());
			controller.loadImages();
		});
	}
	
	@FXML
	public void onBtConfigConnectionAction(ActionEvent event) {
		loadView("/gui/ConnectionConfigView.fxml", (ConnectionConfigViewController controller) -> {
			controller.updateViewData();
		});
	}

	public Boolean getBtCancelPressed() {
		return btCancelPressed;
	}

	public void setBtCancelPressed(Boolean btNewTestForm) {
		this.btCancelPressed = btNewTestForm;
	}

	public CompresionTest getCompresionTest() {
		return compresionTest;
	}

	public void setCompresionTest(CompresionTest compresionTest) {
		this.compresionTest = compresionTest;
	}
	

	/*private <T> void createDialogForm(String absoluteName, Stage parentStage, Consumer<T> initializingAction) {
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

		} catch (IOException e) {
			e.printStackTrace();
		}
	}

	private void createDialogFormNewCompresionTest(String absoluteName, Stage parentStage) {
		try {

			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();

			NewCompresionTestFormController controller = loader.getController();
			controller.setCompresionTestService(new CompresionTestService());
			controller.setClientService(new ClientService());
			controller.setConcreteDesignService(new ConcreteDesignService());
			controller.loadAssociatedObjects();


			Stage dialogStage = new Stage();
			dialogStage.setTitle("Nueva rotura");
			dialogStage.setScene(new Scene(pane));
			dialogStage.setResizable(false);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.showAndWait();

			this.compresionTest = controller.getEntity();

		} catch (IOException e) {
			e.printStackTrace();
		}
	}*/

	private synchronized <T> void loadView(String absoluteName, Consumer<T> initializingAction) {

		try {
			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			VBox newVbox = loader.load();

			Scene mainScene = Program.getMainScene();
			VBox mainVBox = (VBox) ((ScrollPane) mainScene.getRoot()).getContent();

			Node mainMenu = mainVBox.getChildren().get(0);
			mainVBox.getChildren().clear();
			mainVBox.getChildren().add(mainMenu);
			mainVBox.getChildren().addAll(newVbox.getChildren());

			T controller = loader.getController();
			initializingAction.accept(controller);

		} catch (IOException e) {
			Alerts.showAlert("IO Exception", "Error loading view", e.getMessage(),AlertType.ERROR);
		}
	}

	@Override
	public void initialize(URL uri, ResourceBundle rb) {


	}

}
