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

import animatefx.animation.Bounce;
import application.Report.ReportFactory;
import application.db.DbException;
import application.domaim.Material;
import application.domaim.Provider;
import application.exceptions.ReportException;
import application.log.LogUtils;
import application.service.MaterialService;
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
import javafx.scene.shape.Circle;
import javafx.stage.Stage;
import task.DBTask;

public class MateriaisViewController implements Initializable, DataChangeListener {

	private Executor exec;

	MaterialService service;

	ObservableList<Material> obsList;

	LogUtils logger;

	@FXML
	Circle circle1;

	@FXML
	Circle circle2;

	@FXML
	Circle circle3;

	List<Bounce> bounces;

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
	private TableView<Material> tableViewMaterial;

	@FXML
	private TableColumn<Material, Integer> tableColumnId;

	@FXML
	private TableColumn<Material, String> tableColumnName;

	@FXML
	private TableColumn<Material, Provider> tableColumnProvider;

	@FXML
	private void onBtNewAction(ActionEvent event) {
		Stage parentStage = Utils.currentStage(event);
		Material obj = new Material();
		var wrapper = new Object() {
			public MaterialService materialService;
			public ProviderService providerService;
		};
		try {
			wrapper.materialService = new MaterialService();
			wrapper.providerService = new ProviderService();
			Utils.createDialogForm("/gui/MaterialRegistrationForm.fxml", "Nuevo material", parentStage,
					(MaterialRegistrationFormController controller) -> {
						controller.setService(wrapper.materialService);
						controller.setProviderService(wrapper.providerService);
						controller.setEntity(obj);
						controller.setLogger(logger);
						controller.loadAssociatedObjects();
						controller.subscribeDataChangeListener(this);
					}, (MaterialRegistrationFormController controller) -> {
					}, (MaterialRegistrationFormController controller) -> {
					}, "/gui/MaterialRegistrationForm.css",
					new Image(MateriaisViewController.class.getResourceAsStream("/images/fileIcons/edit_file.png")),
					logger);

		} catch (SQLException e) {
			Alerts.showAlert("Error", "SQLException", e.getMessage(), AlertType.ERROR);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		}
	}

	@FXML
	private void onBtEditAction(ActionEvent event) {
		var wrapper = new Object() {
			public MaterialService materialService;
			public ProviderService providerService;
		};

		try {
			wrapper.materialService = new MaterialService();
			wrapper.providerService = new ProviderService();

			Stage parentStage = Utils.currentStage(event);
			Material obj = getMarialFromTableView();

			if (allowEditOrDelete(event))
				throw new IllegalAccessError("Accesso denegado");

			Utils.createDialogForm("/gui/MaterialRegistrationForm.fxml", "Editar material", parentStage,
					(MaterialRegistrationFormController controller) -> {
						controller.setService(wrapper.materialService);
						controller.setProviderService(wrapper.providerService);
						controller.setLogger(logger);
						controller.setEntity(obj);
						controller.loadAssociatedObjects();
						controller.updateFormData();
						controller.subscribeDataChangeListener(this);
					}, (MaterialRegistrationFormController controller) -> {
					}, (MaterialRegistrationFormController controller) -> {
					}, "/gui/MaterialRegistrationForm.css",
					new Image(MateriaisViewController.class.getResourceAsStream("/images/fileIcons/edit_file.png")),
					logger);
		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		} catch (IllegalAccessError e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalAccessError", e.getMessage(), AlertType.ERROR);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtDeleteAction(ActionEvent event) {

		try {
			if (allowEditOrDelete(event))
				throw new IllegalAccessError("Accesso denegado");

			if (service == null) {
				throw new IllegalStateException("Material service was null");
			}
			Material obj = getMarialFromTableView();
			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmación de acción",
					"Seguro que desea apagar material?", "Los datos seleccionados seran perdidos");
			if (result.get() == ButtonType.OK) {

				service.deleteById(obj.getId());
				updateTableView();
			}
		} catch (IllegalStateException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalStateException", e.getMessage(), AlertType.ERROR);
		} catch (DbException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "DbException", e.getMessage(), AlertType.ERROR);
		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		} catch (SQLIntegrityConstraintViolationException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "SQLIntegrityConstraintViolationException", e.getMessage(), AlertType.ERROR);
		} catch (IllegalAccessError e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalAccessError", e.getMessage(), AlertType.ERROR);
		} catch (SQLException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "SQLException", e.getMessage(), AlertType.ERROR);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		}
	}

	@FXML
	private void onBtPrintAction() {
		ReportFactory rF = new ReportFactory();
		try {
			rF.materialReportView(tableViewMaterial.getItems());
		} catch (ReportException e) {
			Alerts.showAlert("Error", "Error al crear reporte!", e.getMessage(), AlertType.ERROR);
			logger.doLog(Level.WARNING, e.getMessage(), e);
		} catch (IOException e) {
			Alerts.showAlert("Error", "Error al abrir ventana", e.getMessage(), AlertType.ERROR);
			logger.doLog(Level.WARNING, e.getMessage(), e);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconcído", e.getMessage(), AlertType.ERROR);
		}
	}

	private boolean allowEditOrDelete(ActionEvent event) throws SQLException, IOException {
		return Utils.isUserAdmin(event, logger);
	}

	public MaterialService getService() {
		return service;
	}

	public void setService(MaterialService service) {
		this.service = service;
	}

	public LogUtils getLogger() {
		return logger;
	}

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}

	private Material getMarialFromTableView() {
		Material obj = new Material();
		obj = tableViewMaterial.getSelectionModel().getSelectedItem();
		if (obj == null) {
			throw new NullPointerException("Material no seleccionado o vacío");
		}
		return obj;
	}

	public void updateTableView() {
		if (service == null) {
			throw new NullPointerException("Material Service was null");
		}

		DBTask<MaterialService, List<Material>> task = new DBTask<MaterialService, List<Material>>(service,
				service -> service.findAll());

		Consumer<Bounce> bounceFinalAction = (Bounce b) -> {
			b.stop();
			b.getNode().setVisible(false);
		};

		Utils.setTaskEvents(task, e -> {
			logger.doLog(Level.WARNING, task.getException().toString(), task.getException());
			Alerts.showAlert("Error", task.getException().toString(), task.getException().getMessage(),
					AlertType.ERROR);
		}, e -> {
			try {
				tableViewMaterial.setItems(FXCollections.observableArrayList(task.get()));
				Utils.setDisableButtons(buttons, false);
				bounces.forEach(bounceFinalAction);
			} catch (InterruptedException | ExecutionException e1) {
				bounces.forEach(bounceFinalAction);
				logger.doLog(Level.WARNING, e1.toString(), e1);
				Alerts.showAlert("Error", e1.toString(), e1.getMessage(), AlertType.ERROR);
			}
			tableViewMaterial.refresh();
		}, e -> {
			bounces.forEach(bounceFinalAction);
			Alerts.showAlert("Error", "Error de conexión", "Se agotó el tiempo de espera de la conexión",
					AlertType.ERROR);

		});
		
		exec.execute(task);
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

	private void initializeNodes() {
		initializeTable();
		bounces = Utils.initiateBouncers(Arrays.asList(circle1, circle2, circle3));
	}

	private void initializeTable() {
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("id"));
		tableColumnName.setCellValueFactory(new PropertyValueFactory<>("name"));
		tableColumnProvider.setCellValueFactory(new PropertyValueFactory<>("provider"));
	};

	@Override
	public void onDataChange() {
		updateTableView();
	}

}
