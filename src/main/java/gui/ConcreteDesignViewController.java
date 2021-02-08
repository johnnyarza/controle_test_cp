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
import java.util.logging.Level;

import application.Report.ReportFactory;
import application.db.DbException;
import application.domaim.ConcreteDesign;
import application.domaim.MaterialProporcion;
import application.exceptions.ReportException;
import application.log.LogUtils;
import application.service.CompresionTestService;
import application.service.ConcreteDesignService;
import application.service.MaterialService;
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

public class ConcreteDesignViewController implements Initializable, DataChangeListener {

	private ConcreteDesignService service;

	private CompresionTestService compresionTestService;

	private Executor exec;

	private LogUtils logger;

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
	private TableView<ConcreteDesign> tableViewConcreteDesing;

	@FXML
	private TableColumn<ConcreteDesign, Integer> tableColumnId;

	@FXML
	private TableColumn<ConcreteDesign, MaterialProporcion> tableColumnDesc;

	@FXML
	private TableColumn<ConcreteDesign, String> tableColumnName;

	@FXML
	private TableColumn<ConcreteDesign, Double> tableColumnSlump;

	@FXML
	public void onBtNewAction(ActionEvent event) {
		var wrapper = new Object() {
			public MaterialService matService;
			public ConcreteDesignService concreteDesignService;
		};
		try {
			wrapper.concreteDesignService = new ConcreteDesignService();
			wrapper.matService = new MaterialService();

			ConcreteDesign obj = new ConcreteDesign();
			Stage parentStage = Utils.currentStage(event);
			Utils.createDialogForm("/gui/ConcreteDesignRegistrationForm.fxml", "Insertar probeta", parentStage,
					(ConcreteDesignRegistrationFormController controller) -> {
						controller.setMaterialService(wrapper.matService);
						controller.setService(wrapper.concreteDesignService);
						controller.setEntity(obj);
						controller.setLogger(logger);
						controller.loadAssociatedObjects();
						controller.subscribeDataChangeListener(this);
					}, (ConcreteDesignRegistrationFormController controller) -> {
					}, (ConcreteDesignRegistrationFormController controller) -> {
					}, "/gui/ConcreteDesignRegistrationForm.css",
					new Image(ConcreteDesignViewController.class.getResourceAsStream("/images/fileIcons/new_file.png")),
					logger);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error al crear ventana", "IOException", AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtEditAction(ActionEvent event) {
		var wrapper = new Object() {
			public MaterialService matService;
			public ConcreteDesignService concreteDesignService;
		};
		try {
			ConcreteDesign obj = getFormData();
			wrapper.concreteDesignService = new ConcreteDesignService();
			wrapper.matService = new MaterialService();

			if (allowEditOrDelete(event))
				throw new IllegalAccessError("Accesso denegado");

			if (obj == null) {
				throw new NullPointerException("ConcreteDesing was null");
			}

			Stage parentStage = Utils.currentStage(event);

			Utils.createDialogForm("/gui/ConcreteDesignRegistrationForm.fxml", "Editar probeta", parentStage,
					(ConcreteDesignRegistrationFormController controller) -> {
						controller.setMaterialService(wrapper.matService);
						controller.setService(wrapper.concreteDesignService);
						controller.setEntity(obj);
						controller.loadAssociatedObjects();
						controller.updateFormData();
						controller.subscribeDataChangeListener(this);
					}, (ConcreteDesignRegistrationFormController controller) -> {
					}, (ConcreteDesignRegistrationFormController controller) -> {
					}, "/gui/ConcreteDesignRegistrationForm.css",
					new Image(
							ConcreteDesignViewController.class.getResourceAsStream("/images/fileIcons/edit_file.png")),
					logger);

		} catch (IllegalStateException e) {
			Alerts.showAlert("Error", "IllegalStateException", e.getMessage(), AlertType.ERROR);
		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		} catch (IllegalAccessError e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalAccessError", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtDeleteAction(ActionEvent event) {
		try {

			if (allowEditOrDelete(event))
				throw new IllegalAccessError("Accesso denegado");

			ConcreteDesign obj = getConcreteDesingFromTableView();
			if (obj == null) {
				throw new NullPointerException("ConcreteDesing was null");
			}

			if (service == null || compresionTestService == null) {
				throw new IllegalStateException("Service(s) was null");
			}

			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmacción de acción",
					"Seguro que desea apagar?", "");
			if (result.get() == ButtonType.OK) {
				service.deleteConcreteDesignById(obj.getId());
			}

			updateTableView();
		} catch (IllegalStateException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalStateException", e.getMessage(), AlertType.ERROR);
		} catch (DbException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "DbException", e1.getMessage(), AlertType.ERROR);
		} catch (SQLIntegrityConstraintViolationException e) {
			Alerts.showAlert("Error", "SQLIntegrityConstraintViolationException", e.getMessage(), AlertType.ERROR);
		} catch (NullPointerException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "NullPointerException", e1.getMessage(), AlertType.ERROR);
		} catch (IllegalAccessError e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalAccessError", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	public void onBtPrintAction() {
		try {
			if (service == null) {
				throw new IllegalStateException("Service was null");
			}
			List<ConcreteDesign> list = service.findAllConcreteDesign();
			ReportFactory rF = new ReportFactory();
			rF.concreteDesignReportView(list);
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

	public ConcreteDesignService getService() {
		return service;
	}

	public void setService(ConcreteDesignService service) {
		this.service = service;
	}

	public CompresionTestService getCompresionTestService() {
		return compresionTestService;
	}

	public void setCompresionTestService(CompresionTestService compresionTestService) {
		this.compresionTestService = compresionTestService;
	}

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}

	private ConcreteDesign getFormData() {
		ConcreteDesign obj = tableViewConcreteDesing.getSelectionModel().getSelectedItem();
		if (obj == null) {
			throw new IllegalStateException("Diseño vacío o no seleccionado");
		}
		return obj;
	}

	public void updateTableView() {
		Task<List<ConcreteDesign>> task = new Task<List<ConcreteDesign>>() {

			@Override
			protected List<ConcreteDesign> call() throws Exception {
				return service.findAllConcreteDesign();
			}
		};

		Utils.setTaskEvents(task, e -> {
			logger.doLog(Level.WARNING, task.getException().getMessage(), task.getException());
			Alerts.showAlert("Error", task.getException().toString(), task.getException().getMessage(),
					AlertType.ERROR);
		}, e -> {
			try {
				tableViewConcreteDesing.setItems(FXCollections.observableArrayList(task.get()));
				tableViewConcreteDesing.refresh();
				Utils.setDisableButtons(buttons, false);
			} catch (InterruptedException | ExecutionException e1) {
				logger.doLog(Level.WARNING, e1.getMessage(), e1);
				Alerts.showAlert("Error", e1.toString(), e1.getMessage(), AlertType.ERROR);
			}
		}, e -> {
			logger.doLog(Level.WARNING, task.getException().getMessage(), task.getException());
			Alerts.showAlert("Error", task.getException().toString(), task.getException().getMessage(),
					AlertType.ERROR);
		});
		exec.execute(task);
	}

	private void initializeNodes() {
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("id"));
		tableColumnDesc.setCellValueFactory(new PropertyValueFactory<>("proporcion"));
		tableColumnDesc.getStyleClass().add("description-column-style");
		tableColumnName.setCellValueFactory(new PropertyValueFactory<>("description"));
		tableColumnSlump.setCellValueFactory(new PropertyValueFactory<>("slump"));
		Utils.formatTableColumnDouble(tableColumnSlump, 1);
	}

	private ConcreteDesign getConcreteDesingFromTableView() {
		ConcreteDesign obj = tableViewConcreteDesing.getSelectionModel().getSelectedItem();
		if (obj == null) {
			throw new IllegalStateException("Diseño vacío o no seleccionado");
		} else {
			return obj;
		}
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
