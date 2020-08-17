package gui;

import java.io.IOException;
import java.net.URL;
import java.util.List;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.function.Consumer;

import application.Program;
import application.Report.ReportFactory;
import application.db.DbException;
import application.domaim.Provider;
import application.service.ProviderService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.fxml.Initializable;
import javafx.scene.Scene;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.cell.PropertyValueFactory;
import javafx.scene.control.Button;
import javafx.scene.control.ButtonType;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.layout.Pane;
import javafx.stage.Modality;
import javafx.stage.Stage;

public class ProveedoresViewController implements Initializable,DataChangeListener{
	
	private ProviderService service;
	
	private ObservableList<Provider> obsList;
	
	@FXML
	private Button btNew;
	
	@FXML
	private Button btEdit;
	
	@FXML
	private Button btDelete;
	
	@FXML
	private Button btPrint;
	
	@FXML
	private TableView<Provider> tableViewProvider;
	
	@FXML
	private TableColumn<Provider, Integer> tableColumnId;
	
	@FXML
	private TableColumn<Provider, String> tableColumnName;
	
	@FXML
	private TableColumn<Provider, String> tableColumnPhone;
	
	@FXML
	private TableColumn<Provider, String> tableColumnAddress;
	
	@FXML
	private TableColumn<Provider, String> tableColumnEmail;
	
	
	public void onBtNewAction(ActionEvent event) {
		Stage parentStage = Utils.currentStage(event);
		Provider obj = new Provider();
		
		createDialogForm("/gui/ProviderRegistrationForm.fxml", parentStage, (ProviderRegistrationFormController controller) -> {
			controller.setService(new ProviderService());
			controller.setEntity(obj);
			controller.subscribeDataChangeListener(this);			
		},"/gui/ProviderRegistrationForm.css");
	}
	
	public void onBtEditAction(ActionEvent event) {
		try {
		Stage parentStage = Utils.currentStage(event);
		Provider obj = getProviderFromTableView();
		
		createDialogForm("/gui/ProviderRegistrationForm.fxml", parentStage, (ProviderRegistrationFormController controller) -> {
			controller.setService(new ProviderService());
			controller.setEntity(obj);
			controller.updateFormData();
			controller.subscribeDataChangeListener(this);			
		},"/gui/ProviderRegistrationForm.css");
		} catch (NullPointerException e) {
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		}
	}
	
	public void onBtDeleteAction(ActionEvent event) {
		try {
			Provider obj = getProviderFromTableView();
			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmación de acción",
					"Seguro que desea apagar?", "Los datos seleccionados serán perdidos");
			if (result.get() == ButtonType.OK) {
				
			if (service == null) {
				throw new IllegalStateException("Provider Service was null");
			}
				service.deleteById(obj.getId());
			}
				updateTableView();
		} catch (DbException e) {
			Alerts.showAlert("Error", "Error deleting probeta", e.getMessage(), AlertType.ERROR);
		} catch (NullPointerException e1) {
			Alerts.showAlert("Error", "NullPointerException", e1.getMessage(), AlertType.ERROR);
		} catch (IllegalStateException e2) {
			Alerts.showAlert("Error", "IllegalStateException", e2.getMessage(), AlertType.ERROR);
		}
	}
	
	@FXML
	public void onBtPrintAction() {
		ReportFactory rF = new ReportFactory();
		rF.providerReportView();
	}
	
	public ProviderService getService() {
		return service;
	}

	public void setService(ProviderService service) {
		this.service = service;
	}
	
	private <T> void createDialogForm(String absoluteName, Stage parentStage,Consumer<T> initializingAction, String css) {
		try {
			
			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();
		
			T controller = loader.getController();
			initializingAction.accept(controller);
			
			Stage dialogStage = new Stage();
			dialogStage.setTitle("Entre con los datos del Proveedor");
			dialogStage.setScene(new Scene(pane));
			if (!css.trim().equals("")) {
				dialogStage.getScene().getStylesheets().add(css);
			}
			dialogStage.setResizable(false);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.showAndWait();
					
		}catch (IOException e) {
			e.printStackTrace();
		}
	}
	
	public void updateTableView() {
		if (service == null) {
			throw new IllegalStateException("Service was null");
		}	
		List<Provider> list =service.findAll();
		obsList = FXCollections.observableArrayList(list);
		tableViewProvider.setItems(obsList);
		tableViewProvider.refresh();
	}
	
	private void initializeNodes() {
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("id"));
		tableColumnName.setCellValueFactory(new PropertyValueFactory<>("name"));
		tableColumnAddress.setCellValueFactory(new PropertyValueFactory<>("address"));
		tableColumnEmail.setCellValueFactory(new PropertyValueFactory<>("email"));
		tableColumnPhone.setCellValueFactory(new PropertyValueFactory<>("phone"));
		
		Stage stage =(Stage) Program.getMainScene().getWindow();
		tableViewProvider.prefHeightProperty().bind(stage.heightProperty());
	}
	
	private Provider getProviderFromTableView() {

		Provider obj = tableViewProvider.getSelectionModel().getSelectedItem();
		if (obj == null) {
			throw new NullPointerException("Proveedor no seleccionado o vacío");
		}
		return obj;
	}
	
	@Override
	public void initialize(URL arg0, ResourceBundle arg1) {
		initializeNodes();		
	}

	@Override
	public void onDataChange() {
		updateTableView();
		
	}

}
