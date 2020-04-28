package gui;

import java.net.URL;
import java.util.Date;
import java.util.List;
import java.util.Map;
import java.util.ResourceBundle;
import java.util.Set;

import application.db.DbException;
import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.domaim.ConcreteDesign;
import application.exceptions.ValidationException;
import application.service.ClientService;
import application.service.CompresionTestService;
import application.service.ConcreteDesignService;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.control.ListCell;
import javafx.scene.control.ListView;
import javafx.scene.control.TextField;
import javafx.util.Callback;

public class NewCompresionTestFormController implements Initializable {

	private ClientService clientService;
	
	private CompresionTestService compresionTestService;
	
	private ConcreteDesignService concreteDesignService;
	
	private Boolean btCancelPressed;
	
	private CompresionTest entity;
	
	private ObservableList<Cliente> obsList;
	
	private ObservableList<ConcreteDesign> obsListConcreteDesign;

	@FXML
	private ComboBox<Cliente> comboBoxClient;
	
	@FXML
	private ComboBox<ConcreteDesign> comboBoxConcreteDesign;

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
	private Label labelErrorClient;

	@FXML
	private Label labelErrorObra;

	@FXML
	private Label labelErrorAddress;
	
	@FXML
	private Label labelErrorConcreteDesign;

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

	public Boolean getBtCancelPressed() {
		return btCancelPressed;
	}

	public void setBtCancelPressed(Boolean btCancelPressed) {
		this.btCancelPressed = btCancelPressed;
	}

	public CompresionTest getEntity() {
		return entity;
	}

	public void setEntity(CompresionTest entity) {
		this.entity = entity;
	}

	public ConcreteDesignService getConcreteDesignService() {
		return concreteDesignService;
	}

	public void setConcreteDesignService(ConcreteDesignService concreteDesignService) {
		this.concreteDesignService = concreteDesignService;
	}

	@FXML
	public void onBtCreateAction(ActionEvent event) {
		try {			

		if (clientService == null || compresionTestService == null || concreteDesignService == null) {
			throw new IllegalStateException("Service(s) was null");
		}
			entity = getFormData();
			compresionTestService.saveOrUpdate(entity);
			btCancelPressed = false;
			
			Utils.currentStage(event).close();
		} catch (DbException e) {
			Alerts.showAlert("Error saving object", null, e.getMessage(), AlertType.ERROR);
		} catch (ValidationException e) {
			setErrorMessages(e.getErrors());
		}
	}

	@FXML
	public void onBtCancelAction(ActionEvent event) {
		btCancelPressed = true;
		Utils.currentStage(event).close();
		
	}

	@Override
	public void initialize(URL url, ResourceBundle rb) {
		initializeNodes();

	}

	private void initializeNodes() {
		initializeComboBoxes();
	}

	public void loadAssociatedObjects() {
		loadAssociatedObjectsComboBoxClient();
		loadAssociatedObjectsComboBoxConcreteDesign();
	}
	
	private void loadAssociatedObjectsComboBoxConcreteDesign() {
		if (concreteDesignService == null) {
			throw new IllegalStateException("concreteDesignService was null");
		}
		List<ConcreteDesign> list = concreteDesignService.findAllConcreteDesign();
		obsListConcreteDesign = FXCollections.observableArrayList(list);
		comboBoxConcreteDesign.setItems(obsListConcreteDesign);
	}
	
	private void loadAssociatedObjectsComboBoxClient () {
		if (clientService == null) {
			throw new IllegalStateException("ClientService was null");
		}
		List<Cliente> list = clientService.findAll();
		obsList = FXCollections.observableArrayList(list);
		comboBoxClient.setItems(obsList);
	}
	
	private void initializeComboBoxes() {
		initializeComboBoxClient();
		initializeComboBoxConcreteDesign();
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
	
	private void initializeComboBoxConcreteDesign() {
		Callback<ListView<ConcreteDesign>, ListCell<ConcreteDesign>> factory = lv -> new ListCell<ConcreteDesign>() {
			@Override
			protected void updateItem(ConcreteDesign item, boolean empty) {
				super.updateItem(item, empty);
				setText(empty ? "" : item.toString());
			}
		};
		comboBoxConcreteDesign.setCellFactory(factory);
		comboBoxConcreteDesign.setButtonCell(factory.call(null));
	}


	private CompresionTest getFormData()  {
		
		CompresionTest obj = new CompresionTest();
		
		ValidationException exception =  new ValidationException("Validation error");
		
		obj.setId(Utils.tryParseToInt(txtId.getText()));
		
		if (comboBoxClient.getValue() == null) {
			exception.addError("client","cliente vac�o");
		}
		obj.setClient(comboBoxClient.getValue());
		
		if (txtObra.getText() == null || txtObra.getText().trim().equals("")) {
			exception.addError("obra","obra vac�a");
		}
		obj.setObra(txtObra.getText());
		
		if (txtAddress.getText() == null || txtAddress.getText().trim().equals("")) {
			exception.addError("address","ubicaci�n vac�a");
		}
		obj.setAddress(txtAddress.getText());
		
		if (comboBoxConcreteDesign.getValue() == null) {
			exception.addError("design", "dise�o vac�o");
		}
		obj.setConcreteDesign(comboBoxConcreteDesign.getValue());
		
		obj.setDate(new Date());
		
		if (exception.getErrors().size() > 0) {
			throw exception;
		}
		
		return obj;
	}

	private void setErrorMessages(Map<String, String> errors) {
		Set<String> fields = errors.keySet(); // Set � conjunto

		labelErrorObra.setText(fields.contains("obra") ? errors.get("obra") : "");		
		labelErrorClient.setText(fields.contains("client") ? errors.get("client") : "");
		labelErrorAddress.setText(fields.contains("address") ? errors.get("address") : "");
		labelErrorConcreteDesign.setText(fields.contains("design") ? errors.get("design") : "");

	}


}
