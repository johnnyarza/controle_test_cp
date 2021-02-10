package gui;

import java.io.IOException;
import java.net.URL;
import java.sql.SQLException;
import java.util.Arrays;
import java.util.Date;
import java.util.List;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.TimeZone;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.function.Consumer;
import java.util.logging.Level;

import animatefx.animation.Bounce;
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
import application.service.UserService;
import enums.LogEnum;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.concurrent.WorkerStateEvent;
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
import javafx.scene.image.Image;
import javafx.scene.layout.Pane;
import javafx.scene.shape.Circle;
import javafx.stage.Modality;
import javafx.stage.Stage;
import javafx.stage.WindowEvent;
import task.DBTask;

public class LoadCompresionTestViewController implements Initializable, DataChangeListener {

	ExecutorService exec;

	private Boolean isLocked = true;

	private CompresionTestService compresionTestService;

	private CompresionTestListService service;

	private ClientService clientService;

	private CorpoDeProvaService corpoDeProvaService;

	private List<CompresionTestList> compresionTestListList;

	private CompresionTest entity;

	private Cliente client;

	private Boolean btCancelPressed;

	private Boolean isNewDoc = false;

	private List<CorpoDeProva> lateCorpoDeProvaList;

	private LogUtils logger;

	@FXML
	Circle circle1;

	@FXML

	Circle circle2;

	@FXML
	Circle circle3;

	List<Bounce> bounces;

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

	private List<Button> buttons;

	@FXML
	public void onbtNewAction(ActionEvent event) {
		isNewDoc = true;
		var wrapper = new Object() {
			private boolean btCancelPressed = true;
			public CompresionTestService compresionTestService;
			public ClientService clientService;
			public ConcreteDesignService concreteDesignService;

			public boolean getBtCancelPressed() {
				return this.btCancelPressed;
			}

			public void setBtCancelPressed(boolean btPressed) {
				this.btCancelPressed = btPressed;
			}
		};

		try {
			wrapper.compresionTestService = new CompresionTestService();
			wrapper.clientService = new ClientService();
			wrapper.concreteDesignService = new ConcreteDesignService();

			Stage parentStage = Utils.currentStage(event);
			Utils.createDialogForm("/gui/NewCompresionTestForm.fxml", "Nuevo Documento", parentStage,
					(NewCompresionTestFormController controller) -> {
						controller.setCompresionTestService(wrapper.compresionTestService);
						controller.setClientService(wrapper.clientService);
						controller.setConcreteDesignService(wrapper.concreteDesignService);
						controller.setLogger(logger);
						controller.loadAssociatedObjects();
						controller.subscribeDataChangeListener(this);
					}, (NewCompresionTestFormController controller) -> {
						controller.setBtCancelPressed(true);
					}, (NewCompresionTestFormController controller) -> {
						this.entity = controller.getEntity();
						wrapper.setBtCancelPressed(controller.getBtCancelPressed());
					}, "/gui/NewCompresionTestForm.css", new Image(LoadCompresionTestViewController.class
							.getResourceAsStream("/images/fileIcons/new_file.png")),
					logger);

			if (!(wrapper.getBtCancelPressed())) {
				showCompresionTestForm(parentStage);
			}

		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error Desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onbtOpenAction(ActionEvent event) {
		isNewDoc = false;
		try {
			initializeBounces();

			CompresionTestList compresionTestList = tableViewClient.getSelectionModel().getSelectedItem();

			DBTask<CompresionTestService, CompresionTest> task = new DBTask<CompresionTestService, CompresionTest>(
					compresionTestService, compresionTestService -> compresionTestService
							.findByIdWithTimeZone(compresionTestList.getCompresionTestId(), TimeZone.getDefault()));
			
			task.setOnSucceeded(t -> {
				try {
					this.entity = task.get();
					Stage parentStage = Utils.currentStage(event);
					Utils.stopBouncers(bounces);
					showCompresionTestForm(parentStage);
				} catch (InterruptedException | ExecutionException | SQLException e) {
					Utils.stopBouncers(bounces);
					logger.doLog(Level.WARNING, e.getMessage(), e);
					Alerts.showAlert("Error", e.getClass().getName(), e.getMessage(), AlertType.ERROR);
				}
			});

			exec.execute(task);

		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", e.getClass().getName(), e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error Desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtDeleteAction(ActionEvent event) {
		var wrapper = new Object() {
			public UserService userService;
		};
		try {
			wrapper.userService = new UserService();
			Stage parentStage = Utils.currentStage(event);
			CompresionTest obj = getCompresionTestFromTableView();
			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmación de accion",
					"Seguro que desea apagar probeta?", "Después de apagados los datos seran perdidos");

			if (result.get() == ButtonType.OK) {
				Utils.createDialogForm("/gui/LoginForm.fxml", "Login", parentStage,
						(LoginFormController controller) -> {
							controller.setUserService(wrapper.userService);
							controller.setEntity(null);
							controller.setIsLoggin(LogEnum.SIGNIN);
							controller.setLogger(logger);
							controller.setTitleLabel("Entrar con datos del administrador");
						}, (LoginFormController controller) -> {
							controller.setEntity(null);
						}, (LoginFormController controller) -> {
							isLocked = controller.getEntity() == null ? true : false;
						}, "", new Image(CompresionTestFormController.class.getResourceAsStream("/images/sign_in.png")),
						logger);

				if (isLocked) {
					throw new IllegalAccessException("Acceso denegado!");
				}

				isLocked = true;

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
		} catch (IllegalAccessException e) {
			Alerts.showAlert("Error", "IllegalAccessException", e.getMessage(), AlertType.ERROR);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	public void onBtWarningACtion(ActionEvent event) {
		Stage parentStage = Utils.currentStage(event);

		var wrapper = new Object() {
			private boolean btCancelPressed = true;
			public CompresionTestService compresionTestService;

			public boolean getBtCancelPressed() {
				return this.btCancelPressed;
			}

			public void setBtCancelPressed(boolean btPressed) {
				this.btCancelPressed = btPressed;
			}
		};

		try {
			if (compresionTestListList == null) {
				throw new NullPointerException("Lista de documentos vacía");
			}
			wrapper.compresionTestService = new CompresionTestService();
			Utils.createDialogForm("/gui/WarningDialog.fxml", "Aviso", Utils.currentStage(event),
					(WarningDialogController controller) -> {
						controller.setCompresionTestListList(compresionTestListList);
						controller.setLateCorpoDeProvaList(lateCorpoDeProvaList);
						controller.setEntity(new CompresionTest());
						controller.setCompresionTestService(wrapper.compresionTestService);
						controller.updateDialogData();
					}, (WarningDialogController controller) -> {
						controller.setIsBtCancelPressed(true);
					}, (WarningDialogController controller) -> {
						this.entity = controller.getEntity();
						wrapper.setBtCancelPressed(controller.getIsBtCancelPressed());
					}, "/gui/WarningDialog.css",
					new Image(LoadCompresionTestViewController.class.getResourceAsStream("/images/alert.png")), logger);
			if (!(wrapper.getBtCancelPressed())) {
				showCompresionTestForm(parentStage);
			}
		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	public void onBtReportAction(ActionEvent event) {
		var wrapper = new Object() {
			ClientService clientService;
		};
		Stage parentStage = Utils.currentStage(event);
		this.client = null;
		try {
			wrapper.clientService = new ClientService();
			Utils.createDialogForm("/gui/FindClientForm.fxml", "Elegir cliente para el reporte...", parentStage,
					(FindClientFormController controller) -> {// initial
						controller.setEntity(null);
						controller.setService(wrapper.clientService);
						controller.setPressedCancelButton(false);
					}, (FindClientFormController controller) -> {
						controller.setPressedCancelButton(true);
					}, (FindClientFormController controller) -> {
						if (controller.getEntity() != null) {
							this.client = controller.getEntity();
						}
						this.btCancelPressed = controller.getPressedCancelButton();
					}, "", new Image(LoadCompresionTestViewController.class.getResourceAsStream("/images/client.png")),
					logger);

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
		} catch (SQLException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "SQLException", e1.getMessage(), AlertType.ERROR);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}

	}

	private void showCompresionTestReportByClient(Integer id) throws SQLException, ReportException, IOException {
		CorpoDeProvaService service = new CorpoDeProvaService();
		ReportFactory rF = new ReportFactory();
		rF.compresionTestReportViewByClient(service.findByClientId(id), client);
	}

	private void showCompresionTestForm(Stage parentStage) throws SQLException {
		createDialogFormCompresionTest("/gui/CompresionTestForm.fxml", parentStage, this.entity,
				"/gui/CompresionTestForm.css");
	}

	@Override
	public void initialize(URL url, ResourceBundle rb) {
		buttons = Arrays.asList(btDelete, btNew, btOpen, btReport, btWarning);
		exec = Executors.newCachedThreadPool(runnable -> {
			Thread t = new Thread(runnable);
			t.setDaemon(true);
			return t;
		});

		initializeNodes();

	}

	private void initializeNodes() {
		formatTableView();
	}

	private void initializeBounces() {
		bounces = Utils.initiateBouncers(Arrays.asList(circle1, circle2, circle3));
	};

	public void updateViewData() {
		Utils.setDisableButtons(buttons, true);
		initializeBounces();
		updateTableView();
		updateLateCorpoDeProva();
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

	private void updateTableView() {
		if (service == null) {
			throw new IllegalStateException("Service was null");
		}
		DBTask<CompresionTestListService, List<CompresionTestList>> task = new DBTask<CompresionTestListService, List<CompresionTestList>>(
				service, service -> service.findAll());

		Consumer<Bounce> bounceFinalAction = (Bounce b) -> {
			b.stop();
			b.getNode().setVisible(false);
		};

		EventHandler<WorkerStateEvent> onSucceeded = (WorkerStateEvent e) -> {
			try {
				compresionTestListList = task.get();
				tableViewClient.setItems(FXCollections.observableArrayList(compresionTestListList));
				tableViewClient.refresh();

			} catch (InterruptedException | ExecutionException e1) {
				logger.doLog(Level.WARNING, e1.getMessage(), e1);
				Alerts.showAlert("Error", e1.toString(), e1.getMessage(), AlertType.ERROR);
			}
		};

		EventHandler<WorkerStateEvent> onFail = (WorkerStateEvent e) -> {
			bounces.forEach(bounceFinalAction);
			logger.doLog(Level.WARNING, task.getException().getMessage(), task.getException());
			Alerts.showAlert("Error", task.getException().toString(), task.getException().getMessage(),
					AlertType.ERROR);
		};

		EventHandler<WorkerStateEvent> onCancel = (WorkerStateEvent e) -> {
			bounces.forEach(bounceFinalAction);
			logger.doLog(Level.WARNING, task.getException().getMessage(), task.getException());
			Alerts.showAlert("Error", task.getException().toString(), task.getException().getMessage(),
					AlertType.ERROR);
		};

		Utils.setTaskEvents(task, onFail, onSucceeded, onCancel);

		exec.execute(task);
	}

	private void updateLateCorpoDeProva() {
		try {
			if (corpoDeProvaService == null)
				throw new IllegalStateException("corpoDeProvaService was null");
			DBTask<CorpoDeProvaService, List<CorpoDeProva>> task = new DBTask<CorpoDeProvaService, List<CorpoDeProva>>(
					corpoDeProvaService,
					corpoDeProvaService -> corpoDeProvaService.findLateCorpoDeProva(TimeZone.getDefault()));
			Consumer<Bounce> bounceFinalAction = (Bounce b) -> {
				b.stop();
				b.getNode().setVisible(false);
			};
			EventHandler<WorkerStateEvent> onSucceeded = (WorkerStateEvent e) -> {
				try {
					lateCorpoDeProvaList = task.get();
					formatWarningBtn(lateCorpoDeProvaList);
					if (lateCorpoDeProvaList.size() > 0) {
						Alerts.showAlert("Aviso", "Hay probetas con fecha proxima o retrasadas", "", AlertType.WARNING);
					}
					Utils.setDisableButtons(buttons, false);
					bounces.forEach(bounceFinalAction);
				} catch (InterruptedException | ExecutionException e1) {
					logger.doLog(Level.WARNING, e1.getMessage(), e1);
					Alerts.showAlert("Error", e1.toString(), e1.getMessage(), AlertType.ERROR);
					bounces.forEach(bounceFinalAction);
				}
			};

			EventHandler<WorkerStateEvent> onFail = (WorkerStateEvent e) -> {
				bounces.forEach(bounceFinalAction);
				logger.doLog(Level.WARNING, task.getException().getMessage(), task.getException());
				Alerts.showAlert("Error", task.getException().toString(), task.getException().getMessage(),
						AlertType.ERROR);
			};

			EventHandler<WorkerStateEvent> onCancel = (WorkerStateEvent e) -> {
				bounces.forEach(bounceFinalAction);
				logger.doLog(Level.WARNING, task.getException().getMessage(), task.getException());
				Alerts.showAlert("Error", task.getException().toString(), task.getException().getMessage(),
						AlertType.ERROR);
			};

			Utils.setTaskEvents(task, onFail, onSucceeded, onCancel);
			exec.execute(task);

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

	private CompresionTest getCompresionTestFromTableView() throws SQLException {
		CompresionTestList compresionTestList = tableViewClient.getSelectionModel().getSelectedItem();
		if (compresionTestList == null) {
			throw new NullPointerException("Ensayo vacío o no selecionado!");
		}

		CompresionTest compresionTest = compresionTestService
				.findByIdWithTimeZone(compresionTestList.getCompresionTestId(), TimeZone.getDefault());

		return compresionTest;
	}

	private void createDialogFormCompresionTest(String absoluteName, Stage parentStage, CompresionTest obj, String css)
			throws SQLException {
		try {

			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();

			CompresionTestFormController controller = loader.getController();

			controller.setCorpoDeProvaService(new CorpoDeProvaService());
			controller.dataChangeListenerSubscribe(this);
			controller.setCompresionTestService(new CompresionTestService());
			controller.setClientService(new ClientService());
			controller.setConcreteDesignService(new ConcreteDesignService());
			controller.setCompresionTest(obj);
			controller.setLogger(logger);
			controller.loadAssociatedObjects();
			controller.setIsNewDoc(this.isNewDoc);
			controller.onDataChange();
			controller.setTxtListeners();
			controller.setChangesCount(0);

			Stage dialogStage = new Stage();
			dialogStage.getIcons()
					.add(new Image(LoadCompresionTestViewController.class.getResourceAsStream("/images/test.png")));
			dialogStage.setTitle("Roturas de probetas de hormigón");
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
						Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmar acción",
								"Segura que desea cerrar?", "Hay datos no guardados!!");
						if (result.get() != ButtonType.OK) {
							controller.notifyListeners();
							we.consume();

						}
					}
					controller.notifyListeners();
				}
			});

			dialogStage.showAndWait();

			if (this.isNewDoc)
				this.isNewDoc = false;
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error al crear ventana", e.getMessage(), AlertType.ERROR);
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
