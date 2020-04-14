package gui;

import java.net.URL;
import java.util.ResourceBundle;

import application.db.DbException;
import application.domaim.Cliente;
import application.service.ClientService;
import gui.util.Alerts;
import gui.util.Constraints;
import gui.util.Utils;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;
import javafx.scene.control.Alert.AlertType;

public class ClienteRegistrationFormController implements Initializable{
	
	private Cliente entity;
	
	private ClientService service;
	
	@FXML
	private Button btCadastrar;
	
	@FXML
	private Button btCancelar;
	
	@FXML
	private TextField txtId;
	
	@FXML
	private TextField txtName;
	
	@FXML
	private TextField txtPhone;
	
	@FXML
	private TextField txtAddress;
	
	@FXML
	private TextField txtEmail;
	
	@FXML
	private Label labelErrorName;
	
	@FXML
	private Label labelErrorPhone;
	
	@FXML
	private Label labelErrorAddress;
	
	@FXML
	private Label labelErrorEmail;
	
	@FXML
	public void onBtCadastrarAction(ActionEvent event) {
		if (entity == null) {
			throw new IllegalStateException("Entity was null");
		}
		
		if (service == null) {
			throw new IllegalStateException("Service was null");
		}
		try {
			entity = getFormData();
			service.saveOrUpdate(entity);
			Utils.currentStage(event).close();
		} catch (DbException e) {
			Alerts.showAlert("Error saving object", null, e.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	public void onBtCancelarAction(ActionEvent event) {
		Utils.currentStage(event).close();
	}

	public void setEntity(Cliente entity) {
		this.entity = entity;
	}
	
	public void setService(ClientService service) {
		this.service = service;
	}

	@Override
	public void initialize(URL url, ResourceBundle rb) {
		initializeNodes();	
	}
	
	private void initializeNodes() {
		Constraints.setTextFieldInteger(txtId);
	}
	
	public void updateFormData() {
		if (entity == null) {
			throw new IllegalStateException("Entity was null");
		}
		txtId.setText(String.valueOf(entity.getId()));
		txtName.setText(entity.getName());
		txtPhone.setText(entity.getPhone());
		txtAddress.setText(entity.getAddress());
		txtEmail.setText(entity.getEmail());
	}

	private Cliente getFormData() {
		Cliente obj = new Cliente();
		
		obj.setId(Utils.tryParseToInt(txtId.getText()));
		obj.setName(txtName.getText());
		obj.setPhone(txtPhone.getText());
		obj.setAddress(txtAddress.getText());
		obj.setEmail(txtEmail.getText());
		
		return obj;
	}
}
