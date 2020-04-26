package gui;

import java.io.IOException;
import java.net.URL;
import java.util.List;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.function.Consumer;

import application.db.DbException;
import application.domaim.ConcreteDesign;
import application.domaim.MaterialProporcion;
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

public class ConcreteDesignViewController implements Initializable,DataChangeListener {

	private ConcreteDesignService service;

	private CompresionTestService compresionTestService;

	private ObservableList<ConcreteDesign> obsList;

	@FXML
	private Button btNew;

	@FXML
	private Button btEdit;

	@FXML
	private Button btDelete;

	@FXML
	private TableView<ConcreteDesign> tableViewConcreteDesing;

	@FXML
	private TableColumn<ConcreteDesign, Integer> tableColumnId;

	@FXML
	private TableColumn<ConcreteDesign, MaterialProporcion> tableColumnDesc;
	
	@FXML
	private TableColumn<ConcreteDesign, String> tableColumnName;

	@FXML
	public void onBtNewAction(ActionEvent event) {
		try {
			ConcreteDesign obj = new ConcreteDesign();
			Stage parentStage = Utils.currentStage(event);
			createDialogForm("/gui/ConcreteDesignRegistrationForm.fxml", parentStage,
					(ConcreteDesignRegistrationFormController controller) -> {
						controller.setMaterialService(new MaterialService());
						controller.setService(new ConcreteDesignService());
						controller.setEntity(obj);
						controller.loadAssociatedObjects();
						controller.subscribeDataChangeListener(this);
					});
		} catch (Exception e) {
			// TODO: handle exception
		}
	}

	@FXML
	public void onBtEditAction(ActionEvent event) {
		try {
		ConcreteDesign obj = getFormData();
		Stage parentStage = Utils.currentStage(event);
		createDialogForm("/gui/ConcreteDesignRegistrationForm.fxml", parentStage,
				(ConcreteDesignRegistrationFormController controller) -> {
					controller.setMaterialService(new MaterialService());
					controller.setService(new ConcreteDesignService());
					controller.setEntity(obj);
					controller.loadAssociatedObjects();
					controller.updateFormData();
					controller.subscribeDataChangeListener(this);
				});
		} catch (IllegalStateException e) {
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
			if (compresionTestService.compresionTestContainsConcreteDesingId(obj.getId())) {
				Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmacción de acción",
						"Elemento en utilización!!",
						"Hay ensayos de roturas utiliazando este diseño /r/n" + "Seguro que desea apagar?");
				if (result.get() == ButtonType.OK) {
					service.deleteConcreteDesignById(obj.getId());
				}
			} else {
				Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmacción de acción",
						"Seguro que desea apagar?", "");
				if (result.get() == ButtonType.OK) {
					service.deleteConcreteDesignById(obj.getId());
				}
			}
			updateTableView();
		} catch (IllegalStateException e) {
			Alerts.showAlert("Error", "IllegalStateException", e.getMessage(), AlertType.ERROR);
		} catch (DbException e1) {
			Alerts.showAlert("Error", "DbException", e1.getMessage(), AlertType.ERROR);
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

	/*
	 * @FXML private TableColumn<ConcreteDesign, String> tableColumnDescription;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Double> tableColumnFck;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Material> tableColumnMat1;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Material> tableColumnMat2;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Material> tableColumnMat3;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Material> tableColumnMat4;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Material> tableColumnMat5;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Material> tableColumnMat6;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Material> tableColumnMat7;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Material> tableColumnMat8;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Double> tableColumnMat1Qtt;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Double> tableColumnMat2Qtt;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Double> tableColumnMat3Qtt;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Double> tableColumnMat4Qtt;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Double> tableColumnMat5Qtt;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Double> tableColumnMat6Qtt;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Double> tableColumnMat7Qtt;
	 * 
	 * @FXML private TableColumn<ConcreteDesign, Double> tableColumnMat8Qtt;
	 */
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
		tableColumnName.setCellValueFactory(new PropertyValueFactory<>("description"));		
	}

	private ConcreteDesign getConcreteDesingFromTableView() {
		ConcreteDesign obj = tableViewConcreteDesing.getSelectionModel().getSelectedItem();
		if (obj == null) {
			throw new IllegalStateException("Diseño vacío o no seleccionado");
		} else {
			return obj;
		}
	}

	private <T> void createDialogForm(String absoluteName, Stage parentStage, Consumer<T> initializingAction) {
		try {

			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();

			T controller = loader.getController();
			initializingAction.accept(controller);

			Stage dialogStage = new Stage();
			dialogStage.setTitle("Insertar datos del diseño");
			dialogStage.setScene(new Scene(pane));
			dialogStage.setResizable(false);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.showAndWait();

		} catch (IOException e) {
			e.printStackTrace();
		}
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
