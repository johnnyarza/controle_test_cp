package gui;

import java.io.IOException;
import java.net.URL;
import java.sql.SQLException;
import java.sql.SQLIntegrityConstraintViolationException;
import java.util.Arrays;
import java.util.List;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.Executor;
import java.util.concurrent.Executors;
import java.util.function.Consumer;
import java.util.logging.Level;

import application.Program;
import application.Report.ReportFactory;
import application.db.DbException;
import application.domaim.Provider;
import application.exceptions.ReportException;
import application.log.LogUtils;
import application.service.ProviderService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.concurrent.Task;
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

	private Executor exec;

	private ProviderService service;

	private LogUtils logger;

	private String iconsDirPath = "/images/fileIcons/";

	@FXML
	private Button btNew;

	@FXML
	private Button btEdit;

	@FXML
	private Button btDelete;

	@FXML
	private Button btPrint;

	private List<Button> buttons;

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
		var wrapper = new Object() {
			ProviderService providerService;
		};
		try {
			wrapper.providerService = new ProviderService();
			Stage parentStage = Utils.currentStage(event);
			Provider obj = new Provider();

			Consumer<ProviderRegistrationFormController> initConsumer = (
					ProviderRegistrationFormController controller) -> {
				controller.setService(wrapper.providerService);
				controller.setEntity(obj);
				controller.setLogger(logger);
				controller.subscribeDataChangeListener(this);
			};

			Image img = new Image(ProveedoresViewController.class.getResourceAsStream(iconsDirPath + "edit_file.png"));

			Utils.createDialogForm("/gui/ProviderRegistrationForm.fxml", "Nuevo proveedor", parentStage, initConsumer,
					(ProviderRegistrationFormController controller) -> {
					}, (ProviderRegistrationFormController controller) -> {
					}, "/gui/ProviderRegistrationForm.css", img, logger);
		} catch (SQLException e) {
			Alerts.showAlert("Error", "SQLException", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Erros desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	public void onBtEditAction(ActionEvent event) {
		var wrapper = new Object() {
			ProviderService providerService;
		};
		try {

			if (allowEditOrDelete(event))
				throw new IllegalAccessError("Accesso denegado");

			Stage parentStage = Utils.currentStage(event);
			Provider obj = getProviderFromTableView();
			Consumer<ProviderRegistrationFormController> initConsumer = (
					ProviderRegistrationFormController controller) -> {
				controller.setService(wrapper.providerService);
				controller.setEntity(obj);
				controller.setLogger(logger);
				controller.updateFormData();
				controller.subscribeDataChangeListener(this);
			};
			Consumer<ProviderRegistrationFormController> finalConsumer = (
					ProviderRegistrationFormController controller) -> {
			};
			Image img = new Image(ProveedoresViewController.class.getResourceAsStream(iconsDirPath + "edit_file.png"));
			wrapper.providerService = new ProviderService();

			Utils.createDialogForm("/gui/ProviderRegistrationForm.fxml", "Editar proveedor", parentStage, initConsumer,
					(ProviderRegistrationFormController controller) -> {
					}, finalConsumer, "/gui/ProviderRegistrationForm.css", img, logger);

		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		} catch (IllegalAccessError e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalAccessError", e.getMessage(), AlertType.ERROR);
		} catch (SQLException e) {
			Alerts.showAlert("Error", "SQLException", e.getMessage(), AlertType.ERROR);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error al crear ventana", "IOException", AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Erros desconocído", e.getMessage(), AlertType.ERROR);
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
		try {
			rF.providerReportView(tableViewProvider.getItems());
		} catch (ReportException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error al abrir reporte", e.getMessage(), AlertType.ERROR);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	private boolean allowEditOrDelete(ActionEvent event) throws SQLException, IOException {
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

	public void updateTableView() {
		if (service == null) {
			throw new IllegalStateException("Service was null");
		}
		Task<List<Provider>> findAllProvidersTask = new Task<List<Provider>>() {

			@Override
			protected List<Provider> call() throws Exception {
				return service.findAll();
			}
		};

		findAllProvidersTask.setOnSucceeded(e -> {
			try {
				tableViewProvider.setItems(FXCollections.observableArrayList(findAllProvidersTask.get()));
				tableViewProvider.refresh();
				Utils.setDisableButtons(buttons, false);
			} catch (ExecutionException | InterruptedException e1) {
				logger.doLog(Level.WARNING, e1.getMessage(), e1);
				Alerts.showAlert("Error", e1.getCause().toString(), e1.getMessage(), AlertType.ERROR);
			}
		});
		exec.execute(findAllProvidersTask);

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
		buttons = Arrays.asList(btNew, btEdit, btDelete, btPrint);
		Utils.setDisableButtons(buttons, true);

		exec = Executors.newCachedThreadPool(runnable -> {
			Thread t = new Thread(runnable);
			t.setDaemon(true);
			return t;
		});
	}

	@Override
	public void onDataChange() {
		updateTableView();

	}

}
