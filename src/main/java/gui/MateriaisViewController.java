package gui;

import java.net.URL;
import gui.util.Utils;

import java.sql.SQLException;
import java.sql.SQLIntegrityConstraintViolationException;
import java.util.Arrays;
import java.util.List;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.function.Consumer;
import java.util.logging.Level;

import application.Report.ReportFactory;
import application.db.DbException;
import application.domaim.Material;
import application.domaim.Provider;
import application.log.LogUtils;
import application.service.MaterialService;
import application.service.ProviderService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
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

public class MateriaisViewController implements Initializable, DataChangeListener {

	MaterialService service;

	ObservableList<Material> obsList;

	LogUtils logger;

	@FXML
	private Button btNew;

	@FXML
	private Button btEdit;

	@FXML
	private Button btDelete;

	@FXML
	private Button btPrint;
	
	private List<Button> buttons = Arrays.asList(btNew,btEdit,btDelete,btPrint);

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

		} catch (SQLException e) {
			Alerts.showAlert("Error", "SQLException", e.getMessage(), AlertType.ERROR);
		}
		;
		createDialogForm("/gui/MaterialRegistrationForm.fxml", "Nuevo material", parentStage,
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
				new Image(MateriaisViewController.class.getResourceAsStream("/images/fileIcons/edit_file.png")));
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

			createDialogForm("/gui/MaterialRegistrationForm.fxml", "Editar material", parentStage,
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
					new Image(MateriaisViewController.class.getResourceAsStream("/images/fileIcons/edit_file.png")));
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
			Alerts.showAlert("Error", "SQLIntegrityConstraintViolationException", e.getMessage(), AlertType.ERROR);
		} catch (IllegalAccessError e) {
			Alerts.showAlert("Error", "IllegalAccessError", e.getMessage(), AlertType.ERROR);
			e.printStackTrace();
		} catch (SQLException e) {
			Alerts.showAlert("Error", "SQLException", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtPrintAction() {
		ReportFactory rF = new ReportFactory();
		rF.materialReportView(tableViewMaterial.getItems());
	}

	private boolean allowEditOrDelete(ActionEvent event) throws SQLException {
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
		List<Material> list = service.findAll();
		obsList = FXCollections.observableArrayList(list);
		tableViewMaterial.setItems(obsList);
		tableViewMaterial.refresh();
	}

	@Override
	public void initialize(URL arg0, ResourceBundle arg1) {
		initializeNodes();
		Utils.setDisableButtons(buttons, true);

	}

	private void initializeNodes() {
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("id"));
		tableColumnName.setCellValueFactory(new PropertyValueFactory<>("name"));
		tableColumnProvider.setCellValueFactory(new PropertyValueFactory<>("provider"));
	}

	private <T> void createDialogForm(String absoluteName, String title, Stage parentStage,
			Consumer<T> initializingAction, Consumer<T> windowEventAction, Consumer<T> finalAction, String css,
			Image icon) {
		Utils.createDialogForm(absoluteName, title, parentStage, initializingAction, windowEventAction, finalAction,
				css, icon, logger);
	}

	@Override
	public void onDataChange() {
		updateTableView();
	}

}
