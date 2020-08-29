package gui;

import java.net.URL;
import java.sql.SQLIntegrityConstraintViolationException;
import java.util.List;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.function.Consumer;
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

public class ConcreteDesignViewController implements Initializable, DataChangeListener {

	private ConcreteDesignService service;

	private CompresionTestService compresionTestService;

	private ObservableList<ConcreteDesign> obsList;

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
		try {
			ConcreteDesign obj = new ConcreteDesign();
			Stage parentStage = Utils.currentStage(event);
			createDialogForm("/gui/ConcreteDesignRegistrationForm.fxml", "Insertar probeta", parentStage,
					(ConcreteDesignRegistrationFormController controller) -> {
						controller.setMaterialService(new MaterialService());
						controller.setService(new ConcreteDesignService());
						controller.setEntity(obj);
						controller.setLogger(logger);
						controller.loadAssociatedObjects();
						controller.subscribeDataChangeListener(this);
					}, (ConcreteDesignRegistrationFormController controller) -> {
					}, (ConcreteDesignRegistrationFormController controller) -> {
					}, "/gui/ConcreteDesignRegistrationForm.css",
					new Image(ConcreteDesignViewController.class.getResourceAsStream("/images/fileIcons/new_file.png")));
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtEditAction(ActionEvent event) {
		try {
			ConcreteDesign obj = getFormData();
			Stage parentStage = Utils.currentStage(event);
			createDialogForm("/gui/ConcreteDesignRegistrationForm.fxml", "Editar probeta", parentStage,
					(ConcreteDesignRegistrationFormController controller) -> {
						controller.setMaterialService(new MaterialService());
						controller.setService(new ConcreteDesignService());
						controller.setEntity(obj);
						controller.loadAssociatedObjects();
						controller.updateFormData();
						controller.subscribeDataChangeListener(this);
					}, (ConcreteDesignRegistrationFormController controller) -> {
					}, (ConcreteDesignRegistrationFormController controller) -> {
					}, "/gui/ConcreteDesignRegistrationForm.css",
					new Image(ConcreteDesignViewController.class.getResourceAsStream("/images/fileIcons/edit_file.png")));
		} catch (IllegalStateException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalStateException", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtDeleteAction(ActionEvent event) {
		try {
			ConcreteDesign obj = getConcreteDesingFromTableView();
			if (service == null || compresionTestService == null) {
				throw new IllegalStateException("Service(s) was null");
			}

			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmacción de acción", "Seguro que desea apagar?",
					"");
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
		}

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
		List<ConcreteDesign> list = service.findAllConcreteDesign();
		obsList = FXCollections.observableArrayList(list);
		tableViewConcreteDesing.setItems(obsList);
		tableViewConcreteDesing.refresh();
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

	private <T> void createDialogForm(String absoluteName, String title, Stage parentStage,
			Consumer<T> initializingAction, Consumer<T> windowEventAction, Consumer<T> finalAction, String css, Image icon) {
		Utils.createDialogForm(absoluteName, title, parentStage, initializingAction, windowEventAction, finalAction, css,
				icon, logger);
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
