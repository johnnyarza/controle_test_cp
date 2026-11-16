package gui;

import java.net.URL;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.ResourceBundle;
import java.util.Set;

import application.domaim.Cliente;
import application.exceptions.ValidationException;
import application.service.ClientService;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.beans.value.ChangeListener;
import javafx.beans.value.ObservableValue;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.control.RadioButton;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.TextField;
import javafx.scene.control.cell.PropertyValueFactory;

public class FindClientFormController implements Initializable {

	private ClientService service;

	private ObservableList<Cliente> obsListClient;

	private Cliente entity;

	private Boolean pressedCancelButton;
	
	private ChangeListener<String> setTextFieldInteger = new ChangeListener<>() { 
		@Override
		public void changed(ObservableValue<? extends String> observable, String oldValue, String newValue) {
	        if (newValue != null && !newValue.matches("\\d*")) {
	        	txtId.setText(oldValue);
	        }
		}
		
	};

	@FXML
	private TextField txtId;

	@FXML
	private TextField txtName;

	@FXML
	private RadioButton rdId;

	@FXML
	private RadioButton rdName;

	@FXML
	private Button btSearch;

	@FXML
	private Button btSelect;

	@FXML
	private Button btClose;

	@FXML
	private Label labelErrorId;

	@FXML
	private Label labelErrorName;

	@FXML
	private TableView<Cliente> tableViewClient;

	@FXML
	private TableColumn<Cliente, Integer> tableColumnId;

	@FXML
	private TableColumn<Cliente, String> tableColumnName;

	@FXML
	private TableColumn<Cliente, String> tableColumnAddress;

	public ClientService getService() {
		return service;
	}

	public void setService(ClientService service) {
		this.service = service;

	}

	public Cliente getEntity() {
		return entity;
	}

	public void setEntity(Cliente entity) {
		this.entity = entity;
	}

	public Boolean getPressedCancelButton() {
		return pressedCancelButton;
	}

	public void setPressedCancelButton(Boolean pressedCancelButton) {
		this.entity = null;
		this.pressedCancelButton = pressedCancelButton;
	}

	public void onBtSelectAction(ActionEvent event) {
		try {
			this.entity = getClientFromTableView();
			Utils.currentStage(event).close();
		} catch (NullPointerException e) {
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtSearchAction(ActionEvent event) {
		try {
			List<Cliente> list = getFormData();
			if (list.size() == 0) {
				Alerts.showAlert("Aviso", "La busqueda no retorno resultados", "", AlertType.INFORMATION);
			}
			tableViewClient.setItems(FXCollections.observableArrayList(list));
			tableViewClient.refresh();
		} catch (ValidationException e) {
			setLabelErrorMessage(e.getErrors());
		}
	}

	@FXML
	public void onRdIdAction(ActionEvent event) {
		formatTxtId();
	}

	@FXML
	public void onRdNameAction(ActionEvent event) {
		formatTxtId();		
	}

	public void onBtCloseAction(ActionEvent event) {
		Utils.currentStage(event).close();
		this.pressedCancelButton = true;
	}

	private Cliente getClientFromTableView() {
		Cliente client = tableViewClient.getSelectionModel().getSelectedItem();
		if (client == null) {
			throw new NullPointerException("Cliente vacío o no seleccionado");
		}
		return client;
	}

	private List<Cliente> getFormData() {
		List<Cliente> list = new ArrayList<Cliente>();
		ValidationException exception = new ValidationException("ValidationException");

		if (service == null) {
			throw new IllegalStateException("Service was null");
		}
		if (!rdId.isSelected() && !rdName.isSelected()) {
			throw new ValidationException("Seleccionar opción de busca");
		}
		
		if (txtId.getText() == null || txtId.getText().trim().equals("")) {
			exception.addError("id", "vacío");
		} else {
			if (rdId.isSelected()) {
				list.add(service.findById(Utils.tryParseToInt(txtId.getText())));
			} else {
				list = service.findByNameLike(txtId.getText());
			}
		}
		
		if (exception.getErrors().size() > 0) {
			throw exception;
		}
		labelErrorId.setText("");

		return list;
	}

	private void setLabelErrorMessage(Map<String, String> errors) {
		Set<String> errorNames = errors.keySet();

		errorNames.forEach((String x) -> {
			labelErrorId.setText(x.equals("id") ? errors.get(x) : "");
		});
	}

	@Override
	public void initialize(URL location, ResourceBundle resources) {
		initializeNodes();

	}

	private void initializeNodes() {
		setCellValueFactory();
		formatTxtId();
	}

	private void formatTxtId() {
		txtId.setText("");
		if (rdId.isSelected()) {
			txtId.textProperty().addListener(setTextFieldInteger);
		}
		if(rdName.isSelected()) {
			txtId.textProperty().removeListener(setTextFieldInteger);
		}
	}

	private void setCellValueFactory() {
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("id"));
		tableColumnName.setCellValueFactory(new PropertyValueFactory<>("name"));
		tableColumnAddress.setCellValueFactory(new PropertyValueFactory<>("address"));
	}

	public void updateTableView() {
		if (service == null) {
			throw new IllegalStateException("Service was null");
		}
		List<Cliente> list = service.findAll();
		obsListClient = FXCollections.observableArrayList(list);
		tableViewClient.setItems(obsListClient);
		tableViewClient.refresh();
	}

}
