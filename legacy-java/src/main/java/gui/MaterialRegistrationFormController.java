package gui;

import java.io.IOException;
import java.net.URL;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.ResourceBundle;
import java.util.Set;
import java.util.function.Consumer;
import java.util.logging.Level;

import application.db.DbException;
import application.domaim.Material;
import application.domaim.Provider;
import application.exceptions.ValidationException;
import application.log.LogUtils;
import application.service.MaterialService;
import application.service.ProviderService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Constraints;
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.image.ImageView;
import javafx.scene.layout.Pane;
import javafx.stage.Modality;
import javafx.stage.Stage;
import javafx.scene.Scene;
import javafx.scene.control.Button;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.control.ListCell;
import javafx.scene.control.ListView;
import javafx.scene.control.TextField;
import javafx.util.Callback;

public class MaterialRegistrationFormController implements Initializable {

	private Material entity;

	private MaterialService service;

	private ProviderService providerService;

	private List<DataChangeListener> dataChangeListeners = new ArrayList<>();

	private ObservableList<Provider> obsList;

	private LogUtils logger;

	@FXML
	private Button btCadastrar;

	@FXML
	private Button btCancelar;

	@FXML
	private Button btAddProvider;

	@FXML
	private TextField txtId;

	@FXML
	private TextField txtName;

	@FXML
	private Label labelErrorName;

	@FXML
	private Label labelErrorProvider;

	@FXML
	private ComboBox<Provider> comboBoxProvider;

	@FXML
	private void onBtAddProviderAction(ActionEvent event) {
		Stage parentStage = Utils.currentStage(event);
		Provider obj = new Provider();
		
		createDialogForm("/gui/ProviderRegistrationForm.fxml","Entre con los datos del Proveedor", parentStage, (ProviderRegistrationFormController controller) -> {
			controller.setService(new ProviderService());
			controller.setEntity(obj);
			controller.setLogger(logger);
		},"/gui/ProviderRegistrationForm.css");
		loadAssociatedObjects();
	}

	@FXML
	private void onBtCadastrarAction(ActionEvent event) {
		try {
			entity = getFormData();
			if (entity == null) {
				throw new IllegalStateException("Entity was null");
			}
			if (service == null && providerService == null) {
				throw new IllegalStateException("Service was null");
			}

			service.saveOrUpdate(entity);
			notifyDataChangeListeners();
			Utils.currentStage(event).close();

		} catch (DbException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "DbException", e.getMessage(), AlertType.ERROR);
		} catch (ValidationException e) {
			setErrorMessages(e.getErrors());
		} catch (IllegalStateException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalStateException", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtCancelarAction(ActionEvent event) {
		Utils.currentStage(event).close();
	}

	public void setEntity(Material entity) {
		this.entity = entity;
	}

	public void setService(MaterialService service) {
		this.service = service;
	}

	public ProviderService getProviderService() {
		return providerService;
	}

	public void setProviderService(ProviderService providerService) {
		this.providerService = providerService;
	}

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}

	public void subscribeDataChangeListener(DataChangeListener listener) {
		dataChangeListeners.add(listener);
	}

	public void updateFormData() {
		if (entity == null) {
			throw new IllegalStateException("Entity was null");
		}
		txtId.setText(String.valueOf(entity.getId()));
		txtName.setText(entity.getName());
		comboBoxProvider.setValue(entity.getProvider());
	}

	private Material getFormData() {
		Material obj = new Material();

		ValidationException exception = new ValidationException("Validation error");

		obj.setId(Utils.tryParseToInt(txtId.getText()));

		if (txtName.getText() == null || txtName.getText().trim().equals("")) {
			exception.addError("name", "Campo vacío");
		}
		obj.setName(txtName.getText());

		if (comboBoxProvider.getValue() == null) {
			exception.addError("provider", "Campo vacío");
		}
		obj.setProvider(comboBoxProvider.getValue());

		if (exception.getErrors().size() > 0) {
			throw exception;
		}

		return obj;
	}

	private void notifyDataChangeListeners() {
		dataChangeListeners.forEach((DataChangeListener x) -> x.onDataChange());
	}

	private void setErrorMessages(Map<String, String> errors) {
		Set<String> fields = errors.keySet(); // Set � conjunto

		labelErrorName.setText(fields.contains("name") ? errors.get("name") : "");
		labelErrorProvider.setText(fields.contains("provider") ? errors.get("provider") : "");
	}

	public void loadAssociatedObjects() {
		if (service == null || providerService == null) {
			throw new IllegalStateException("Service was null");
		}
		List<Provider> list = providerService.findAll();
		obsList = FXCollections.observableArrayList(list);
		comboBoxProvider.setItems(obsList);
	}

	private void initializeNodes() {
		Constraints.setTextFieldInteger(txtId);
		formatButtons();
		initializeComboBoxClient();
	}

	private void formatButtons() {
		btCadastrar.getStyleClass().add("custom-button");
		btCancelar.getStyleClass().add("close-button");
		btAddProvider.getStyleClass().add("custom-button");
		setButtonsGraphics();
	}

	private void setButtonsGraphics() {
		ImageView imgView = Utils.createImageView("/images/plus_icon.png", 15.0, 15.0);
		btAddProvider.setGraphic(imgView);
	}

	private void initializeComboBoxClient() {
		Callback<ListView<Provider>, ListCell<Provider>> factory = lv -> new ListCell<Provider>() {
			@Override
			protected void updateItem(Provider item, boolean empty) {
				super.updateItem(item, empty);
				setText(empty ? "" : item.getName());
			}
		};
		comboBoxProvider.setCellFactory(factory);
		comboBoxProvider.setButtonCell(factory.call(null));
	}

	@Override
	public void initialize(URL url, ResourceBundle rb) {
		initializeNodes();
	}
	
	private <T> void createDialogForm(String absoluteName,String title, Stage parentStage,Consumer<T> initializingAction, String css) {
		try {
			
			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();
		
			T controller = loader.getController();
			initializingAction.accept(controller);
			
			Stage dialogStage = new Stage();
			dialogStage.setTitle(title);
			dialogStage.setScene(new Scene(pane));
			if (!css.trim().equals("")) {
				dialogStage.getScene().getStylesheets().add(css);
			}
			dialogStage.setResizable(false);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.showAndWait();
					
		}catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error al cargar ventana",e.getMessage() , AlertType.ERROR);
		}
	}
}
