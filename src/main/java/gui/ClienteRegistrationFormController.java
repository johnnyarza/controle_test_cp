package gui;

import java.net.URL;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.ResourceBundle;
import java.util.Set;
import java.util.logging.Level;

import application.db.DbException;
import application.domaim.Cliente;
import application.exceptions.ValidationException;
import application.log.LogException;
import application.log.LogUtils;
import application.service.ClientService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Constraints;
import gui.util.Utils;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;

public class ClienteRegistrationFormController implements Initializable {

	private Cliente entity;

	private ClientService service;

	private List<DataChangeListener> dataChangeListeners = new ArrayList<>();

	private LogUtils logger;

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
		try {
			if (entity == null) {
				throw new IllegalStateException("Entity was null");
			}

			if (service == null) {
				throw new IllegalStateException("Service was null");
			}
			if (logger == null) {
				throw new LogException("Logger was null");
			}
			entity = getFormData();
			
			service.saveOrUpdate(entity);
			notifyDataChangeListeners();
			Utils.currentStage(event).close();
		} catch (DbException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error saving object", null, e.getMessage(), AlertType.ERROR);
		} catch (ValidationException e) {
			setErrorMessages(e.getErrors());
		} catch (LogException e) {
			Alerts.showAlert("Error inesperado", null, e.getMessage(), AlertType.ERROR);
		} catch (IllegalStateException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalStateException", e.getMessage(), AlertType.ERROR);
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

	public void subscribeDataChangeListener(DataChangeListener listener) {
		dataChangeListeners.add(listener);
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

		ValidationException exception = new ValidationException("Validation error");

		obj.setId(Utils.tryParseToInt(txtId.getText()));

		if (txtName.getText() == null || txtName.getText().trim().equals("")) {
			exception.addError("name", "Campo vacío");
		}
		obj.setName(txtName.getText());

		if (txtPhone.getText() == null || txtPhone.getText().trim().equals("")) {
			exception.addError("phone", "Campo vacío");
		}
		obj.setPhone(txtPhone.getText());

		if (txtAddress.getText() == null || txtAddress.getText().trim().equals("")) {
			exception.addError("address", "Campo vacío");
		}
		obj.setAddress(txtAddress.getText());

		if (txtEmail.getText() == null || txtEmail.getText().trim().equals("")) {
			exception.addError("email", "Campo vacío");
		}
		obj.setEmail(txtEmail.getText());

		if (exception.getErrors().size() > 0) {
			throw exception;
		}

		return obj;
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

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}
}
