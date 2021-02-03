package gui;

import java.net.URL;
import java.sql.SQLIntegrityConstraintViolationException;
import java.util.List;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.function.Consumer;
import java.util.logging.Level;

import application.Program;
import application.Report.ReportFactory;
import application.db.DbException;
import application.domaim.Provider;
import application.log.LogUtils;
import application.service.ProviderService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.ButtonType;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.cell.PropertyValueFactory;
import javafx.scene.image.Image;
import javafx.stage.Stage;

public class ProveedoresViewController implements Initializable, DataChangeListener {

	private ProviderService service;

	private ObservableList<Provider> obsList;

	private LogUtils logger;

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

		createDialogForm("/gui/ProviderRegistrationForm.fxml", "Nuevo proveedor", parentStage,
				(ProviderRegistrationFormController controller) -> {
					controller.setService(new ProviderService());
					controller.setEntity(obj);
					controller.setLogger(logger);
					controller.subscribeDataChangeListener(this);
				}, (ProviderRegistrationFormController controller) -> {
				}, (ProviderRegistrationFormController controller) -> {
				}, "/gui/ProviderRegistrationForm.css",
				new Image(ProveedoresViewController.class.getResourceAsStream("/images/fileIcons/edit_file.png")));
	}

	public void onBtEditAction(ActionEvent event) {
		try {

			if (allowEditOrDelete(event))
				throw new IllegalAccessError("Accesso denegado");

			Stage parentStage = Utils.currentStage(event);
			Provider obj = getProviderFromTableView();

			createDialogForm("/gui/ProviderRegistrationForm.fxml", "Editar proveedor", parentStage,
					(ProviderRegistrationFormController controller) -> {
						controller.setService(new ProviderService());
						controller.setEntity(obj);
						controller.setLogger(logger);
						controller.updateFormData();
						controller.subscribeDataChangeListener(this);
					}, (ProviderRegistrationFormController controller) -> {
					}, (ProviderRegistrationFormController controller) -> {
					}, "/gui/ProviderRegistrationForm.css",
					new Image(ProveedoresViewController.class.getResourceAsStream("/images/fileIcons/edit_file.png")));
		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		} catch (IllegalAccessError e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalAccessError", e.getMessage(), AlertType.ERROR);
		}
	}

	public void onBtDeleteAction(ActionEvent event) {
		try {

			if (allowEditOrDelete(event))
				throw new IllegalAccessError("Accesso denegado");

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
		} catch (SQLIntegrityConstraintViolationException e) {
			Alerts.showAlert("Error", "SQLIntegrityConstraintViolationException", e.getMessage(), AlertType.ERROR);
		} catch (IllegalAccessError e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalAccessError", e.getMessage(), AlertType.ERROR);
		} catch (DbException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error deleting probeta", e.getMessage(), AlertType.ERROR);
		} catch (NullPointerException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "NullPointerException", e1.getMessage(), AlertType.ERROR);
		} catch (IllegalStateException e2) {
			logger.doLog(Level.WARNING, e2.getMessage(), e2);
			Alerts.showAlert("Error", "IllegalStateException", e2.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtPrintAction() {
		ReportFactory rF = new ReportFactory();
		rF.providerReportView(tableViewProvider.getItems());
	}

	private boolean allowEditOrDelete(ActionEvent event) {
		return Utils.isUserAdmin(event, logger);
	}

	public ProviderService getService() {
		return service;
	}

	public void setService(ProviderService service) {
		this.service = service;
	}

	public LogUtils getLogger() {
		return logger;
	}

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}

	private <T> void createDialogForm(String absoluteName, String title, Stage parentStage,
			Consumer<T> initializingAction, Consumer<T> windowEventAction, Consumer<T> finalAction, String css,
			Image icon) {
		Utils.createDialogForm(absoluteName, title, parentStage, initializingAction, windowEventAction, finalAction,
				css, icon, logger);
	}

	public void updateTableView() {
		if (service == null) {
			throw new IllegalStateException("Service was null");
		}
		List<Provider> list = service.findAll();
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

		Stage stage = (Stage) Program.getMainScene().getWindow();
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
