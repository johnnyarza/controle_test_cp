package gui;

import java.net.URL;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.ResourceBundle;
import java.util.Set;

import application.db.DbException;
import application.domaim.Provider;
import application.exceptions.ValidationException;
import application.service.ProviderService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;

public class ProviderRegistrationFormController implements Initializable {

	private ProviderService service;

	private Provider entity;

	List<DataChangeListener> dataChangeListeners = new ArrayList<>();

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
	private void onBtCadastrarAction(ActionEvent event) {
		try {
			Provider obj = getFormData();
			if (service == null) {
				throw new IllegalStateException("Provide Service was null");
			}
			service.saveOrUpdate(obj);
			notifyDataChangeListeners();
			Utils.currentStage(event).close();

		} catch (ValidationException e) {
			setErrorMessages(e.getErrors());
		} catch (DbException e1) {
			Alerts.showAlert("Error", "DbException", e1.getMessage(), AlertType.ERROR);
		} catch (IllegalStateException e2) {
			Alerts.showAlert("Error", "IllegalStateException", e2.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtCancelAction(ActionEvent event) {
		Utils.currentStage(event).close();
	}

	private Provider getFormData() {
		Provider obj = new Provider();
		ValidationException exception = new ValidationException("Validation exception");

		obj.setId(Utils.tryParseToInt(txtId.getText()));

		if (txtName.getText() == null || txtName.getText().trim().equals("")) {
			exception.addError("name", "Nombre vacío");
		}

		if (txtPhone.getText() == null || txtPhone.getText().trim().equals("")) {
			exception.addError("phone", "Telefono vacío");
		}

		if (txtAddress.getText() == null || txtAddress.getText().trim().equals("")) {
			exception.addError("address", "Ubicación vacía");
		}

		if (txtEmail.getText() == null || txtEmail.getText().trim().equals("")) {
			exception.addError("email", "Correo vacío");
		}

		if (exception.getErrors().size() > 0) {
			throw exception;
		}

		obj.setName(txtName.getText());
		obj.setPhone(txtPhone.getText());
		obj.setAddress(txtAddress.getText());
		obj.setEmail(txtEmail.getText());

		return obj;
	}

	public ProviderService getService() {
		return service;
	}

	public void setService(ProviderService service) {
		this.service = service;
	}

	public Provider getEntity() {
		return entity;
	}

	public void setEntity(Provider entity) {
		this.entity = entity;
	}

	public void subscribeDataChangeListener(DataChangeListener listener) {
		dataChangeListeners.add(listener);
	}

	private void notifyDataChangeListeners() {
		dataChangeListeners.forEach((DataChangeListener x) -> x.onDataChange());
	}

	private void setErrorMessages(Map<String, String> errors) {
		Set<String> fields = errors.keySet(); // Set é conjunto

		labelErrorName.setText(fields.contains("name") ? errors.get("name") : "");
		labelErrorPhone.setText(fields.contains("phone") ? errors.get("phone") : "");
		labelErrorAddress.setText(fields.contains("address") ? errors.get("address") : "");
		labelErrorEmail.setText(fields.contains("email") ? errors.get("email") : "");

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

	@Override
	public void initialize(URL arg0, ResourceBundle arg1) {

	}

}
