package gui;

import java.io.IOException;
import java.net.URL;
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
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.fxml.Initializable;
import javafx.scene.Scene;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.ButtonType;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.cell.PropertyValueFactory;
import javafx.scene.layout.Pane;
import javafx.stage.Modality;
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

		createDialogForm("/gui/MaterialRegistrationForm.fxml", parentStage,
				(MaterialRegistrationFormController controller) -> {
					controller.setService(new MaterialService());
					controller.setProviderService(new ProviderService());
					controller.setEntity(obj);
					controller.setLogger(logger);
					controller.loadAssociatedObjects();
					controller.subscribeDataChangeListener(this);
				}, "/gui/MaterialRegistrationForm.css");
	}

	@FXML
	private void onBtEditAction(ActionEvent event) {
		try {
			Stage parentStage = Utils.currentStage(event);
			Material obj = getMarialFromTableView();

			createDialogForm("/gui/MaterialRegistrationForm.fxml", parentStage,
					(MaterialRegistrationFormController controller) -> {
						controller.setService(new MaterialService());
						controller.setProviderService(new ProviderService());
						controller.setEntity(obj);
						controller.loadAssociatedObjects();
						controller.updateFormData();
						controller.subscribeDataChangeListener(this);
					}, "/gui/MaterialRegistrationForm.css");
		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtDeleteAction(ActionEvent event) {
		try {
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
		}catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtPrintAction() {
		ReportFactory rF = new ReportFactory();
		rF.materialReportView();
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

	}

	private void initializeNodes() {
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("id"));
		tableColumnName.setCellValueFactory(new PropertyValueFactory<>("name"));
		tableColumnProvider.setCellValueFactory(new PropertyValueFactory<>("provider"));
	}

	private <T> void createDialogForm(String absoluteName, Stage parentStage, Consumer<T> initializingAction,
			String css) {
		try {

			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();

			T controller = loader.getController();
			initializingAction.accept(controller);

			Stage dialogStage = new Stage();
			dialogStage.setTitle("Insertar datos del material");
			dialogStage.setScene(new Scene(pane));
			if (!css.trim().equals("")) {
				dialogStage.getScene().getStylesheets().add(css);
			}
			dialogStage.setResizable(false);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.showAndWait();

		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error al abrir ventana", e.getMessage(), AlertType.ERROR);
		}
	}

	@Override
	public void onDataChange() {
		updateTableView();
	}

}
