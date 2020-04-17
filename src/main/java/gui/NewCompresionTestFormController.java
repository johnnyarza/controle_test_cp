package gui;

import java.io.IOException;
import java.net.URL;
import java.util.Date;
import java.util.List;
import java.util.Map;
import java.util.ResourceBundle;
import java.util.Set;
import java.util.function.Consumer;

import application.db.DbException;
import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.exceptions.Teste;
import application.exceptions.ValidationException;
import application.service.ClientService;
import application.service.CompresionTestService;
import application.service.CorpoDeProvaService;
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
import javafx.scene.control.Button;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.control.ListCell;
import javafx.scene.control.ListView;
import javafx.scene.control.TextField;
import javafx.scene.layout.VBox;
import javafx.stage.Modality;
import javafx.stage.Stage;
import javafx.util.Callback;

public class NewCompresionTestFormController implements Initializable {

	private ClientService clientService;

	private CompresionTestService compresionTestService;
	

	//private CompresionTest entity;

	private ObservableList<Cliente> obsList;

	@FXML
	private ComboBox<Cliente> comboBoxClient;

	@FXML
	private TextField txtId;

	@FXML
	private TextField txtObra;

	@FXML
	private TextField txtAddress;

	@FXML
	private Button btCreate;

	@FXML
	private Button btCancel;

	@FXML
	Label labelErrorClient;

	@FXML
	Label labelErrorObra;

	@FXML
	Label labelErrorAddress;

	public CompresionTestService getCompresionTestService() {
		return compresionTestService;
	}

	public void setCompresionTestService(CompresionTestService compresionTestService) {
		this.compresionTestService = compresionTestService;
	}

	public ClientService getClientService() {
		return clientService;
	}

	public void setClientService(ClientService clientService) {
		this.clientService = clientService;
	}

	@FXML
	public void onBtCreateAction(ActionEvent event) {
		CompresionTest entity;
		try {
			entity = getFormData();

		Stage parentStage = (Stage) Utils.currentStage(event).getOwner();
		
		createDialogForm("/gui/CompresionTestForm.fxml", parentStage, 
				(CompresionTestFormController controller) -> {
			controller.setCorpoDeProvaService(new CorpoDeProvaService());
			controller.updateTableView();
			Utils.currentStage(event).close();
		});

		if (clientService == null || compresionTestService == null) {
			throw new IllegalStateException("Service(s) was null");
		}

			entity = getFormData();
			compresionTestService.saveOrUpdate(entity);
		
			Utils.currentStage(event).close();
		} catch (DbException e) {
			Alerts.showAlert("Error saving object", null, e.getMessage(), AlertType.ERROR);
		} catch (ValidationException e) {
			setErrorMessages(e.getErrors());
		} catch (Teste e) {
			e.printStackTrace();
		}
	}

	@FXML
	public void onBtCancelAction(ActionEvent event) {
		Utils.currentStage(event).close();
	}

	@Override
	public void initialize(URL url, ResourceBundle rb) {
		initializeNodes();

	}

	private void initializeNodes() {
		initializeComboBoxClient();
	}

	public void loadAssociatedObjects() {
		if (clientService == null) {
			throw new IllegalStateException("ClientService was null");
		}
		List<Cliente> list = clientService.findAll();
		obsList = FXCollections.observableArrayList(list);
		comboBoxClient.setItems(obsList);
	}

	private void initializeComboBoxClient() {
		Callback<ListView<Cliente>, ListCell<Cliente>> factory = lv -> new ListCell<Cliente>() {
			@Override
			protected void updateItem(Cliente item, boolean empty) {
				super.updateItem(item, empty);
				setText(empty ? "" : item.getName());
			}
		};
		comboBoxClient.setCellFactory(factory);
		comboBoxClient.setButtonCell(factory.call(null));
	}


	private CompresionTest getFormData()  {
		
		CompresionTest obj = new CompresionTest();
		
		ValidationException exception =  new ValidationException("Validation error");
		
		obj.setId(Utils.tryParseToInt(txtId.getText()));
		
		if (comboBoxClient.getValue() == null) {
			exception.addError("client","cliente vacío");
		}
		obj.setClient(comboBoxClient.getValue());
		
		if (txtObra.getText() == null || txtObra.getText().trim().equals("")) {
			exception.addError("obra","obra vacía");
		}
		obj.setObra(txtObra.getText());
		
		if (txtAddress.getText() == null || txtAddress.getText().trim().equals("")) {
			exception.addError("address","ubicación vacía");
		}
		obj.setAddress(txtAddress.getText());
		
		obj.setDate(new Date());
		
		if (exception.getErrors().size() > 0) {
			throw exception;
		}
		
		return obj;
	}

	private void setErrorMessages(Map<String, String> errors) {
		Set<String> fields = errors.keySet(); // Set é conjunto

		labelErrorObra.setText(fields.contains("obra") ? errors.get("obra") : "");		
		labelErrorClient.setText(fields.contains("client") ? errors.get("client") : "");
		labelErrorAddress.setText(fields.contains("address") ? errors.get("address") : "");

	}
	
	private <T> void createDialogForm(String absoluteName, Stage parentStage,Consumer<T> initializingAction) {
		try {
			
			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			VBox vbox = loader.load();
		
			T controller = loader.getController();
			initializingAction.accept(controller);
			
			Stage dialogStage = new Stage();
			dialogStage.setTitle("Nueva rotura");
			dialogStage.setScene(new Scene(vbox));
			dialogStage.setResizable(true);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.showAndWait();
				
		}catch (IOException e) {
			e.printStackTrace();
		}
	}


}
