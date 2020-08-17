package gui;

import java.io.IOException;
import java.net.URL;
import java.util.Date;
import java.util.List;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.TimeZone;
import java.util.function.Consumer;
import java.util.logging.Level;

import application.Program;
import application.Report.ReportFactory;
import application.db.DbException;
import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.domaim.CompresionTestList;
import application.domaim.CorpoDeProva;
import application.exceptions.ReportException;
import application.log.LogUtils;
import application.service.ClientService;
import application.service.CompresionTestListService;
import application.service.CompresionTestService;
import application.service.ConcreteDesignService;
import application.service.CorpoDeProvaService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.event.EventHandler;
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
import javafx.scene.layout.AnchorPane;
import javafx.scene.layout.Pane;
import javafx.stage.Modality;
import javafx.stage.Stage;
import javafx.stage.WindowEvent;

public class LoadCompresionTestViewController implements Initializable, DataChangeListener {

	private CompresionTestListService service;

	private ClientService clientService;

	private CompresionTestService compresionTestService;

	private CorpoDeProvaService corpoDeProvaService;

	private ObservableList<CompresionTestList> obsList;

	private List<CompresionTestList> compresionTestListList;

	private CompresionTest entity;

	private Cliente client;

	private Boolean btCancelPressed;

	private List<CorpoDeProva> lateCorpoDeProvaList;
	
	private LogUtils logger;

	@FXML
	private TableView<CompresionTestList> tableViewClient;

	@FXML
	TableColumn<CompresionTestList, Integer> tableColumnId;

	@FXML
	TableColumn<CompresionTestList, String> tableColumnClientName;

	@FXML
	TableColumn<CompresionTestList, String> tableColumnObra;

	@FXML
	TableColumn<CompresionTestList, String> tableColumnAddress;

	@FXML
	TableColumn<CompresionTestList, Date> tableColumnCreationDate;

	@FXML
	private Button btOpen;

	@FXML
	private Button btDelete;

	@FXML
	private Button btNew;

	@FXML
	private Button btReport;

	@FXML
	private Button btWarning;

	@FXML
	public void onbtNewAction(ActionEvent event) {
		try {
			Stage parentStage = Utils.currentStage(event);
			createDialogForm("/gui/NewCompresionTestForm.fxml", "Nueva Rotura", parentStage,
					(NewCompresionTestFormController controller) -> {
						controller.setCompresionTestService(new CompresionTestService());
						controller.setClientService(new ClientService());
						controller.setConcreteDesignService(new ConcreteDesignService());
						controller.setLogger(logger);
						controller.loadAssociatedObjects();
						controller.subscribeDataChangeListener(this);
					}, (NewCompresionTestFormController controller) -> {
						controller.setBtCancelPressed(true);
					}, (NewCompresionTestFormController controller) -> {
						this.entity = controller.getEntity();
						this.btCancelPressed = controller.getBtCancelPressed();
					}, "/gui/NewCompresionTestForm.css");
			if (!btCancelPressed) {
				showCompresionTestForm(parentStage);
			}

		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error Desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onbtOpenAction(ActionEvent event) {
		try {
			CompresionTest obj = getCompresionTestFromTableView();
			Stage parentStage = Utils.currentStage(event);
			createDialogFormCompresionTest("/gui/CompresionTestForm.fxml", parentStage, obj, "/gui/CompresionTestForm.css");
			updateViewData();
			if (lateCorpoDeProvaList.size() > 0) {
				Alerts.showAlert("Aviso", "Hay probetas con fecha proxima o retrasadas", "", AlertType.WARNING);
			}
		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtDeleteAction(ActionEvent event) {
		try {
			CompresionTest obj = getCompresionTestFromTableView();
			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmación de accion",
					"Seguro que desea apagar probeta?", "Después de apagados los datos seran perdidos");

			if (result.get() == ButtonType.OK) {

				if (compresionTestService == null) {
					throw new IllegalStateException("Service was null");
				} else {
					compresionTestService.deleteById(obj.getId());
					onDataChange();
				}
			}
		} catch (DbException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "DbException", e.getMessage(), AlertType.ERROR);
		} catch (IllegalStateException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "IllegalStateException", e1.getMessage(), AlertType.ERROR);
		} catch (NullPointerException e2) {
			logger.doLog(Level.WARNING, e2.getMessage(), e2);
			Alerts.showAlert("Error", "NullPointerException", e2.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	public void onBtWarningACtion(ActionEvent event) {
		Stage parentStage = Utils.currentStage(event);
		try {

			createDialogForm("/gui/WarningDialog.fxml", "Aviso", Utils.currentStage(event),
					(WarningDialogController controller) -> {
						controller.setCompresionTestListList(compresionTestListList);
						controller.setLateCorpoDeProvaList(lateCorpoDeProvaList);
						controller.setEntity(new CompresionTest());
						controller.setCompresionTestService(new CompresionTestService());
						controller.updateDialogData();
					}, (WarningDialogController controller) -> {
						controller.setIsBtCancelPressed(true);
					}, (WarningDialogController controller) -> {
						this.entity = controller.getEntity();
						this.btCancelPressed = controller.getIsBtCancelPressed();
					}, "/gui/WarningDialog.css");
			if (!btCancelPressed) {
				showCompresionTestForm(parentStage);
			}
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	public void onBtReportAction(ActionEvent event) {
		Stage parentStage = Utils.currentStage(event);
		this.client = null;
		try {
			createDialogForm("/gui/FindClientForm.fxml", "Elegir cliente para el reporte...", parentStage,
					(FindClientFormController controller) -> {// initial
						controller.setEntity(null);
						controller.setService(new ClientService());
						controller.setPressedCancelButton(false);
					}, (FindClientFormController controller) -> {
						controller.setPressedCancelButton(true);
					}, // window close event
					(FindClientFormController controller) -> {
						if (controller.getEntity() != null) {
							this.client = controller.getEntity();
						}
						this.btCancelPressed = controller.getPressedCancelButton();
					}, "");// final
			if (!this.btCancelPressed) {
				if (this.client == null) {
					throw new IllegalStateException("Cliente vacío");
				}
				showCompresionTestReportByClient(this.client.getId());
			}
		} catch (IllegalStateException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalStateException", e.getMessage(), AlertType.ERROR);
		} catch (ReportException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "ReportException", e1.getMessage(), AlertType.ERROR);
		}

	}

	private void showCompresionTestReportByClient(Integer id) {
		CorpoDeProvaService service = new CorpoDeProvaService();
		ReportFactory rF = new ReportFactory();
		rF.compresionTestReportViewByClient(service.findByClientId(id), client);
	}

	private void showCompresionTestForm(Stage parentStage) {
		createDialogFormCompresionTest("/gui/CompresionTestForm.fxml", parentStage, this.entity,
				"/gui/CompresionTestForm.css");
		updateViewData();
		if (lateCorpoDeProvaList.size() > 0) {
			Alerts.showAlert("Aviso", "Hay probetas con fecha proxima o retrasadas", "", AlertType.WARNING);
		}
	}

	@Override
	public void initialize(URL url, ResourceBundle rb) {
		initializeNodes();

	}

	private void initializeNodes() {
		formatTableView();
	}

	private void formatTableView() {
		
		
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("compresionTestId"));
		tableColumnClientName.setCellValueFactory(new PropertyValueFactory<>("clientName"));
		tableColumnAddress.setCellValueFactory(new PropertyValueFactory<>("address"));
		
		tableColumnCreationDate.setCellValueFactory(new PropertyValueFactory<>("creationDate"));
		Utils.formatTableColumnDate(tableColumnCreationDate, "dd/MM/yyyy");
		tableColumnObra.setCellValueFactory(new PropertyValueFactory<>("obra"));

		Stage stage = (Stage) Program.getMainScene().getWindow();	
		
		tableViewClient.prefHeightProperty().bind(stage.heightProperty());
	}



	public void updateViewData() {
		updateTableView();
		updateLateCorpoDeProva();
	}

	private void updateTableView() {
		if (service == null) {
			throw new IllegalStateException("Service was null");
		}

		compresionTestListList = service.findAll();
		obsList = FXCollections.observableArrayList(compresionTestListList);
		tableViewClient.setItems(obsList);
		tableViewClient.refresh();
	}

	private void updateLateCorpoDeProva() {
		try {
			if (corpoDeProvaService == null)
				throw new IllegalStateException("corpoDeProvaService was null");
			lateCorpoDeProvaList = corpoDeProvaService.findLateCorpoDeProva(TimeZone.getDefault());
			formatWarningBtn(lateCorpoDeProvaList);
		} catch (IllegalStateException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalStateException", e.getMessage(), AlertType.ERROR);
		} catch (DbException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "SQLException", e1.getMessage(), AlertType.ERROR);
		}

	}

	private void formatWarningBtn(List<CorpoDeProva> list) {
		if (list.size() > 0) {

			btWarning.setVisible(true);
			btWarning.setDisable(false);
			return;
		}
		btWarning.setStyle("");
		btWarning.setVisible(false);
		btWarning.setDisable(true);

	}

	@Override
	public void onDataChange() {
		updateViewData();
	}

	private CompresionTest getCompresionTestFromTableView() {
		CompresionTestList compresionTestList = tableViewClient.getSelectionModel().getSelectedItem();
		CompresionTestService compresionTestService = new CompresionTestService();
		if (compresionTestList == null) {
			throw new NullPointerException("Ensayo vacío o no selecionado!");
		}
		CompresionTest compresionTest = compresionTestService.findByIdWithTimeZone(compresionTestList.getCompresionTestId(),
				TimeZone.getDefault());

		return compresionTest;
	}

	private void createDialogFormCompresionTest(String absoluteName, Stage parentStage, CompresionTest obj, String css) {
		try {

			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();

			CompresionTestFormController controller = loader.getController();

			controller.setCorpoDeProvaService(new CorpoDeProvaService());
			controller.setCompresionTestService(new CompresionTestService());
			controller.setClientService(new ClientService());
			controller.setConcreteDesignService(new ConcreteDesignService());
			controller.setCompresionTest(obj);
			controller.setLogger(logger);
			controller.loadAssociatedObjects();
			controller.updateFormData();
			controller.updateTableView();
			controller.setLabelMessageText(obj.getId());
			controller.setTxtListeners();
			controller.setChangesCount(0);

			Stage dialogStage = new Stage();
			dialogStage.setTitle("Ensayo de Rotura");
			dialogStage.setScene(new Scene(pane));
			if (!css.trim().contentEquals(""))
				dialogStage.getScene().getStylesheets().add(css);
			dialogStage.setResizable(true);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.setOnCloseRequest(new EventHandler<WindowEvent>() {

				@Override
				public void handle(WindowEvent we) {
					if (controller.getChangesCount() > 0) {
						Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmar acción", "Segura que desea cerrar?",
								"Hay datos no guardados!!");
						if (result.get() != ButtonType.OK) {
							we.consume();
						}
					}
				}
			});
			dialogStage.showAndWait();

		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error al crear ventana", "IOException", AlertType.ERROR);
		}
	}

	private <T> void createDialogForm(String absoluteName, String title, Stage parentStage,
			Consumer<T> initializingAction, Consumer<T> windowEventAction, Consumer<T> finalAction, String css) {
		try {

			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			AnchorPane pane = loader.load();

			T controller = loader.getController();
			initializingAction.accept(controller);

			Stage dialogStage = new Stage();

			dialogStage.setTitle(title);
			dialogStage.setScene(new Scene(pane));
			dialogStage.getScene().getStylesheets().add("/gui/GlobalStyle.css");
			if (!css.trim().equals("")) {
				dialogStage.getScene().getStylesheets().add(css);
			}
			dialogStage.setResizable(true);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);

			dialogStage.setOnCloseRequest(new EventHandler<WindowEvent>() {

				@Override
				public void handle(WindowEvent we) {
					windowEventAction.accept(controller);

				}
			});

			dialogStage.showAndWait();
			finalAction.accept(controller);

		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error al crear ventana", "IOException", AlertType.ERROR);
		}
	}

	public void setCompresionTestListService(CompresionTestListService service) {
		this.service = service;
	}

	public ClientService getClientService() {
		return clientService;
	}

	public List<CorpoDeProva> getLateCorpoDeProvaList() {
		return lateCorpoDeProvaList;
	}

	public void setClientService(ClientService clientService) {
		this.clientService = clientService;
	}

	public CompresionTestService getCompresionTestService() {
		return compresionTestService;
	}

	public void setCompresionTestService(CompresionTestService compresionTestService) {
		this.compresionTestService = compresionTestService;
	}

	public CorpoDeProvaService getCorpoDeProvaService() {
		return corpoDeProvaService;
	}

	public void setCorpoDeProvaService(CorpoDeProvaService corpoDeProvaService) {
		this.corpoDeProvaService = corpoDeProvaService;
	}

	public TableView<CompresionTestList> getTableViewClient() {
		return tableViewClient;
	}

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}


}
