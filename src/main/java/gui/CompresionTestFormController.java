package gui;

import java.io.IOException;
import java.net.URL;
import java.sql.SQLException;
import java.text.SimpleDateFormat;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneId;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.Set;
import java.util.TimeZone;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.Executor;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.function.Consumer;
import java.util.logging.Level;

import org.joda.time.DateTimeComparator;

import animatefx.animation.Bounce;
import application.Report.ReportFactory;
import application.db.DbException;
import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.domaim.ConcreteDesign;
import application.domaim.CorpoDeProva;
import application.exceptions.ReportException;
import application.exceptions.ValidationException;
import application.log.LogUtils;
import application.service.ClientService;
import application.service.CompresionTestService;
import application.service.ConcreteDesignService;
import application.service.CorpoDeProvaService;
import application.service.UserService;
import enums.LogEnum;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.beans.value.ChangeListener;
import javafx.beans.value.ObservableValue;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.ButtonType;
import javafx.scene.control.ComboBox;
import javafx.scene.control.DatePicker;
import javafx.scene.control.Label;
import javafx.scene.control.ListCell;
import javafx.scene.control.ListView;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.TextField;
import javafx.scene.control.cell.PropertyValueFactory;
import javafx.scene.image.Image;
import javafx.scene.image.ImageView;
import javafx.scene.layout.VBox;
import javafx.scene.shape.Circle;
import javafx.stage.Stage;
import javafx.util.Callback;
import task.DBTask;

public class CompresionTestFormController implements Initializable, DataChangeListener {
	private ExecutorService execService;

	private Executor exec;

	private ObservableList<CorpoDeProva> obsList;

	private CorpoDeProvaService corpoDeProvaService;

	private CompresionTestService compresionTestService;

	private ConcreteDesignService concreteDesignService;

	private ClientService clientService;

	private CompresionTest compresionTest;

	private Boolean isNewDoc = false;

	SimpleDateFormat sdf = new SimpleDateFormat("dd/MM/yyyy");

	private Integer changesCount;

	private LogUtils logger;

	private Boolean isLocked = true;

	Map<String, ImageView> imgViewMap = new HashMap<>();

	List<DataChangeListener> dataChangeListeners = new ArrayList<>();

	@FXML
	Circle circle1;

	@FXML
	Circle circle2;

	@FXML
	Circle circle3;

	List<Bounce> bounces;

	@FXML
	private ComboBox<ConcreteDesign> comboBoxConcreteDesign;

	@FXML
	private TextField txtid;

	@FXML
	private TextField txtObra;

	@FXML
	private TextField txtAddress;

	@FXML
	private Button btEditarCadastro;

	@FXML
	private Button btInserirProbeta;

	@FXML
	private Button btEditarProbeta;

	@FXML
	private Button btApagarProbeta;

	@FXML
	private Button btClose;

	@FXML
	private Button btPrint;

	@FXML
	private Button btFilter;

	@FXML
	private Button btClearFilter;

	@FXML
	private Button btSearchClient;

	@FXML
	private Button btLock;

	@FXML
	private Button btCopy;

	@FXML
	private Button btSearchConcreteProvider;

	@FXML
	private VBox vbox;

	@FXML
	private ComboBox<Cliente> comboBoxClient;

	@FXML
	private ComboBox<Cliente> comboBoxConcreteProvider;

	@FXML
	private TableView<CorpoDeProva> tableViewCorpoDeProva;

	@FXML
	private TableColumn<CorpoDeProva, Integer> tableColumnId;

	@FXML
	private TableColumn<CorpoDeProva, String> tableColumnCodigo;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnSlump;

	@FXML
	private TableColumn<CorpoDeProva, Date> tableColumnFechaMoldeo;

	@FXML
	private TableColumn<CorpoDeProva, Date> tableColumnFechaRotura;

	@FXML
	private TableColumn<CorpoDeProva, Integer> tableColumnEdad;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnDiameter;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnHeight;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnWeight;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnDensid;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnTonRupture;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnfckRupture;

	@FXML
	private DatePicker dpCreationDate;

	@FXML
	private Label labelErrorClient;

	@FXML
	private Label labelErrorDate;

	@FXML
	private Label labelErrorObra;

	@FXML
	private Label labelErrorAddress;

	@FXML
	private Label labelErrorConcreteDesign;

	@FXML
	private Label labelErrorConcreteProvider;

	@FXML
	private Label labelMessages;

	@FXML
	private void onBtCopyAction(ActionEvent event) {
		try {
			CorpoDeProva obj = getCorpoDeProvaView();
			Stage parentStage = Utils.currentStage(event);
			obj.setId(null);
			obj.setCompresionTest(compresionTest);

			Utils.createDialogForm("/gui/CorpoDeProvaRegistrationForm.fxml", "Copiar Probeta", parentStage,
					(CorpoDeProvaRegistrationController controller) -> {
						try {
							controller.setCorpoDeProvaService(new CorpoDeProvaService());
						} catch (SQLException e) {
							Alerts.showAlert("Error", "SQLException", e.getMessage(), AlertType.ERROR);
						}
						controller.setLogger(logger);
						controller.setCorpoDeProva(obj);
						controller.updateFormData();
						controller.setIsLocked(isLocked);
						controller.subscribeDataChangeListener(this);
					}, (CorpoDeProvaRegistrationController controller) -> {
					}, (CorpoDeProvaRegistrationController controller) -> {
					}, "/gui/CompresionTestForm.css",
					new Image(CompresionTestFormController.class.getResourceAsStream("/images/fileIcons/new_file.png")),
					logger);

		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconcído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtLockAction(ActionEvent event) {
		Stage parentStage = Utils.currentStage(event);
		try {
			if (isLocked) {
				Utils.createDialogForm("/gui/LoginForm.fxml", "Login", parentStage,
						(LoginFormController controller) -> {
							try {
								controller.setUserService(new UserService());
							} catch (SQLException e) {
								Alerts.showAlert("Error", "SQLException", e.getMessage(), AlertType.ERROR);
							}
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
			} else {
				isLocked = true;
			}

			setLockButtonStyle();
			setNodesDisable();

		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconcído", e.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	private void onBtSearchClientAction(ActionEvent event) {
		var wrapper = new Object() {
			private ClientService clientService;

			public void setClientService(ClientService clientService) {
				this.clientService = clientService;
			}

			public ClientService getClientService() {
				return this.clientService;
			}
		};
		try {
			wrapper.setClientService(new ClientService());
			Stage parentStage = Utils.currentStage(event);
			Utils.createDialogForm("/gui/FindClientForm.fxml", "Busca Cliente", parentStage,
					(FindClientFormController controller) -> {
						controller.setEntity(null);
						controller.setService(wrapper.getClientService());
					}, (FindClientFormController controller) -> {
						controller.setEntity(null);
					}, (FindClientFormController controller) -> {
						if (controller.getEntity() != null) {
							comboBoxClient.setValue(controller.getEntity());
						}
					}, "", new Image(CompresionTestFormController.class.getResourceAsStream("/images/lupa.png")),
					logger);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		} catch (SQLException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error en de conexión", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconcído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtSearchConcreteProviderAction(ActionEvent event) {
		var wrapper = new Object() {
			private ClientService clientService;

			public void setClientService(ClientService clientService) {
				this.clientService = clientService;
			}

			public ClientService getClientService() {
				return this.clientService;
			}
		};
		try {
			wrapper.setClientService(new ClientService());
			Stage parentStage = Utils.currentStage(event);
			Utils.createDialogForm("/gui/FindClientForm.fxml", "Busca Cliente", parentStage,
					(FindClientFormController controller) -> {
						controller.setEntity(null);
						controller.setService(wrapper.getClientService());
					}, (FindClientFormController controller) -> {
						controller.setEntity(null);
					}, (FindClientFormController controller) -> {
						if (controller.getEntity() != null) {
							comboBoxConcreteProvider.setValue(controller.getEntity());
						}
					}, "", new Image(CompresionTestFormController.class.getResourceAsStream("/images/lupa.png")),
					logger);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		} catch (SQLException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error en de conexión", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconcído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtInserirProbetaAction(ActionEvent event) {
		var wrapper = new Object() {
			private CorpoDeProvaService corpoDeProvaService;

			public void setCorpoDeProvaService(CorpoDeProvaService corpoDeProvaService) {
				this.corpoDeProvaService = corpoDeProvaService;
			}

			public CorpoDeProvaService getCorpoDeProvaService() {
				return this.corpoDeProvaService;
			}
		};
		try {
			wrapper.setCorpoDeProvaService(new CorpoDeProvaService());
			Stage parentStage = Utils.currentStage(event);
			CorpoDeProva obj = new CorpoDeProva();
			obj.setCompresionTest(compresionTest);

			Utils.createDialogForm("/gui/CorpoDeProvaRegistrationForm.fxml", "Inserir Probeta", parentStage,
					(CorpoDeProvaRegistrationController controller) -> {
						controller.setCorpoDeProvaService(wrapper.getCorpoDeProvaService());
						controller.setLogger(logger);
						controller.setCorpoDeProva(obj);
						controller.setIsLocked(isLocked);
						controller.subscribeDataChangeListener(this);
					}, (CorpoDeProvaRegistrationController controller) -> {
					}, (CorpoDeProvaRegistrationController controller) -> {
					}, "/gui/CompresionTestForm.css",
					new Image(CompresionTestFormController.class.getResourceAsStream("/images/fileIcons/new_file.png")),
					logger);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		} catch (SQLException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error en de conexión", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtEditarProbetaAction(ActionEvent event) {
		var wrapper = new Object() {
			private CorpoDeProvaService corpoDeProvaService;

			public void setCorpoDeProvaService(CorpoDeProvaService corpoDeProvaService) {
				this.corpoDeProvaService = corpoDeProvaService;
			}

			public CorpoDeProvaService getCorpoDeProvaService() {
				return this.corpoDeProvaService;
			}
		};
		try {
			wrapper.setCorpoDeProvaService(new CorpoDeProvaService());
			Stage parentStage = Utils.currentStage(event);
			CorpoDeProva obj = getCorpoDeProvaView();

			if (isLocked && (obj.getTonRupture() != null && obj.getTonRupture() != 0.0)) {
				throw new IllegalAccessException("La alteración de este documento esta bloqueada");
			}

			if (isLocked && (DateTimeComparator.getDateOnlyInstance().compare(obj.getRuptureDate(), new Date()) < 0)) {
				throw new IllegalAccessException("Probeta Retrasada. El administrador debe desbloquear el documento!");
			}

			Utils.createDialogForm("/gui/CorpoDeProvaRegistrationForm.fxml", "Editar Probeta", parentStage,
					(CorpoDeProvaRegistrationController controller) -> {
						controller.setCorpoDeProvaService(wrapper.getCorpoDeProvaService());
						controller.setLogger(logger);
						controller.setCorpoDeProva(obj);
						controller.updateFormData();
						controller.setIsLocked(isLocked);
						controller.formLockedState();
						controller.subscribeDataChangeListener(this);
					}, (CorpoDeProvaRegistrationController controller) -> {
					}, (CorpoDeProvaRegistrationController controller) -> {
					}, "/gui/CompresionTestForm.css",
					new Image(
							CompresionTestFormController.class.getResourceAsStream("/images/fileIcons/edit_file.png")),
					logger);
		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		} catch (IllegalAccessException e) {
			Alerts.showAlert("Error", "Acceso denegado", e.getMessage(), AlertType.ERROR);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		} catch (SQLException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error en de conexión", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconcído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtApagarProbetaAction() {
		try {
			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmación de accion",
					"Seguro que desea apagar probeta?", "Después de apagados los datos seran perdidos");

			if (result.get() == ButtonType.OK) {
				CorpoDeProva obj = getCorpoDeProvaView();
				corpoDeProvaService.deleteById(obj.getId());
				onDataChange();
			}
		} catch (DbException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error al apagar probeta", e.getMessage(), AlertType.ERROR);
		} catch (NullPointerException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "NullPointerException", e1.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	private void onBtEditarCadastroAction() {
		try {
			if (isLocked) {
				throw new IllegalAccessException("La alteración de este documento esta bloqueada");
			}
			setErrorMessages(new HashMap<String, String>());
			setCompresionTestFormData();
			compresionTestService.saveOrUpdate(this.compresionTest);
			onDataChange();
			setChangesCount(0);
			Alerts.showAlert("Acción conluida", "Cadastro actualizado", null, AlertType.INFORMATION);
		} catch (ValidationException e) {
			setErrorMessages(e.getErrors());
		} catch (DbException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "Error al salvar el ensayo", e1.getMessage(), AlertType.ERROR);
		} catch (IllegalAccessException e) {
			Alerts.showAlert("Error", "Acceso denegado", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);

		}
	}

	@FXML
	private void onBtCloseAction(ActionEvent event) {
		if (this.changesCount > 0) {
			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmar acción", "Segura que desea cerrar?",
					"Hay datos no guardados!!");
			if (result.get() == ButtonType.OK) {
				Utils.currentStage(event).close();
				notifyListeners();
			}
		} else {
			Utils.currentStage(event).close();
			notifyListeners();
		}
	}

	@FXML
	private void onbtPrintAction() {
		try {
			ReportFactory rF = new ReportFactory();
			List<CorpoDeProva> list = getCorpoDeProvaListFromTable();
			rF.compresionTestReportView(list, this.compresionTest);
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

	@FXML
	private void onBtFilterAction(ActionEvent event) {
		var wrapper = new Object() {
			private CorpoDeProvaService corpoDeProvaService;

			public void setCorpoDeProvaService(CorpoDeProvaService corpoDeProvaService) {
				this.corpoDeProvaService = corpoDeProvaService;
			}

			public CorpoDeProvaService getCorpoDeProvaService() {
				return this.corpoDeProvaService;
			}
		};
		try {
			wrapper.setCorpoDeProvaService(new CorpoDeProvaService());
			Stage parentStage = Utils.currentStage(event);
			Image img = new Image(CompresionTestFormController.class.getResourceAsStream("/images/filter_on.png"));

			Utils.createDialogForm("/gui/CorpoDeProvaFilterForm.fxml", "Filtrar Probetas por Fecha", parentStage,
					(CorpoDeProvaFilterFormController controller) -> {
						controller.setService(wrapper.getCorpoDeProvaService());
						controller.setCompresionTest(this.compresionTest);
					}, (CorpoDeProvaFilterFormController controller) -> {
						controller.setIsCancelButtonPressed(true);
					}, (CorpoDeProvaFilterFormController controller) -> {
						if (!controller.getIsCancelButtonPressed()) {
							obsList = controller.getObsList();
						}
					}, "/gui/GlobalStyle.css", img, logger);
			tableViewCorpoDeProva.setItems(this.obsList);
			tableViewCorpoDeProva.refresh();
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		} catch (SQLException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error en de conexión", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconcído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onbtClearFilterAction() {
		updateTableView();
	}

	@FXML
	public void onChangeCount() {
		this.changesCount += 1;
	}

	@FXML
	public void onTableViewSort() {
		Utils.formatCorpoDeProvaTableViewRowColor(tableViewCorpoDeProva);
	}

	public void dataChangeListenerSubscribe(DataChangeListener dataChangeListener) {
		this.dataChangeListeners.add(dataChangeListener);
	}

	private void formatTableView() {
		setTableColumnsCellValueFactory();
		tableColumnCodigo.getStyleClass().add("description-column-style");
	}

	public CorpoDeProvaService getCorpoDeProvaService() {
		return corpoDeProvaService;
	}

	public void setCorpoDeProvaService(CorpoDeProvaService corpoDeProvaService) {
		this.corpoDeProvaService = corpoDeProvaService;
	}

	public void setLabelMessageText(Integer compresionTestId) {
		if (corpoDeProvaService == null) {
			throw new NullPointerException("Corpo de prova service was null");
		}
		DBTask<CorpoDeProvaService, Integer> task = new DBTask<CorpoDeProvaService, Integer>(corpoDeProvaService,
				corpoDeProvaService -> {
//					try {
//						Thread.sleep(5000);
//					} catch (InterruptedException e1) {
//						// TODO Auto-generated catch block
//						e1.printStackTrace();
//					}
					return corpoDeProvaService.countCorpoDeProvasToTestbyCompresionTestId(compresionTestId);
				});

		task.setOnSucceeded(e -> {
			Integer cpCount;
			try {
				cpCount = task.get();
				if (cpCount > 0) {
					labelMessages.setText("Hay " + Integer.toString(cpCount) + " probeta(s) para Romper");
				} else {
					labelMessages.setText("");
				}
			} catch (InterruptedException | ExecutionException e1) {
				// TODO Auto-generated catch block
				e1.printStackTrace();
			}

		});

		exec.execute(task);

	}

	public void setCompresionTest(CompresionTest compresionTest) {
		this.compresionTest = compresionTest;
	}

	public CompresionTestService getCompresionTestService() {
		return compresionTestService;
	}

	public CompresionTest getCompresionTest() {
		return compresionTest;
	}

	public ClientService getClientService() {
		return clientService;
	}

	public ConcreteDesignService getConcreteDesignService() {
		return concreteDesignService;
	}

	public Integer getChangesCount() {
		return changesCount;
	}

	private void initializeComboBoxClient() {
		Callback<ListView<Cliente>, ListCell<Cliente>> factory = lv -> new ListCell<Cliente>() {
			@Override
			protected void updateItem(Cliente item, boolean empty) {
				super.updateItem(item, empty);
				setText(empty ? "" : item.getName());
			}
		};
		comboBoxClient.setCellFactory(factory);
		comboBoxClient.setButtonCell(factory.call(null));
		comboBoxConcreteProvider.setCellFactory(factory);
		comboBoxConcreteProvider.setButtonCell(factory.call(null));
	}

	public void setCompresionTestService(CompresionTestService compresionTestService) {
		this.compresionTestService = compresionTestService;
	}

	public void setClientService(ClientService clientService) {
		this.clientService = clientService;
	}

	public void setConcreteDesignService(ConcreteDesignService concreteDesignService) {
		this.concreteDesignService = concreteDesignService;
	}

	@Override
	public void initialize(URL arg0, ResourceBundle arg1) {
		initializeNodes();
		exec = Executors.newCachedThreadPool(runnable -> {
			Thread t = new Thread(runnable);
			t.setDaemon(true);
			return t;
		});

		execService = Executors.newFixedThreadPool(2);
	}

	private void initializeNodes() {

		formatTableView();
		initializeComboBoxClient();
		tableViewCorpoDeProva.prefHeightProperty().bind(vbox.heightProperty());
		btLock.getStyleClass().add("button");
		btLock.getStyleClass().add("locked-button");
		setButtonsGraphics();
	}

	public void loadAssociatedObjects() {
		loadClientComboBox();
		loadCompresionDesignComboBox();
	}

	private void loadClientComboBox() {
		if (clientService == null) {
			throw new IllegalStateException("ClientService was null");
		}

		DBTask<ClientService, List<Cliente>> task = new DBTask<ClientService, List<Cliente>>(clientService,
				clientService -> clientService.findAll());

		task.setOnSucceeded(e -> {
			try {
				List<Cliente> list = task.get();
				comboBoxClient.setItems(FXCollections.observableArrayList(list));
				comboBoxConcreteProvider.setItems(FXCollections.observableArrayList(list));
			} catch (InterruptedException | ExecutionException e1) {
				// TODO Auto-generated catch block
				e1.printStackTrace();
			}
		});

		exec.execute(task);
	}

	private void loadCompresionDesignComboBox() {
		if (concreteDesignService == null) {
			throw new IllegalStateException("concreteDesignService was null");
		}

		DBTask<ConcreteDesignService, List<ConcreteDesign>> task = new DBTask<ConcreteDesignService, List<ConcreteDesign>>(
				concreteDesignService, concreteDesignService -> concreteDesignService.findAllConcreteDesign());

		task.setOnSucceeded(e -> {
			try {
				comboBoxConcreteDesign.setItems(FXCollections.observableArrayList(task.get()));
			} catch (InterruptedException | ExecutionException e1) {
				// TODO Auto-generated catch block
				e1.printStackTrace();
			}
		});

		exec.execute(task);

	}

	private CorpoDeProva getCorpoDeProvaView() {

		CorpoDeProva cp = tableViewCorpoDeProva.getSelectionModel().getSelectedItem();
		if (cp == null) {
			throw new NullPointerException("Probeta vacía o no seleccionada");
		}
		return cp;
	}

	private List<CorpoDeProva> getCorpoDeProvaListFromTable() {
		List<CorpoDeProva> list = tableViewCorpoDeProva.getItems();
		if (list == null) {
			throw new IllegalStateException("Lista de probetas vacía");
		}
		return list;
	}

	public Boolean getIsLocked() {
		return isLocked;
	}

	public Boolean getIsNewDoc() {
		return isNewDoc;
	}

	@Override
	public void onDataChange() {
		bounces = Utils.initiateBouncers(Arrays.asList(circle1, circle2, circle3));
		tableViewCorpoDeProva.setDisable(true);
		Utils.setDisableButtons(Arrays.asList(btApagarProbeta, btClearFilter, btClose, btCopy, btEditarCadastro,
				btEditarProbeta, btFilter, btInserirProbeta, btLock, btPrint, btSearchClient, btSearchConcreteProvider),
				true);
		updateFormData();
		setLabelMessageText(this.compresionTest.getId());
		updateTableView();
		
	}

	public void setFormaLockedState() {
		btApagarProbeta.setDisable(!isNewDoc);
		btInserirProbeta.setDisable(!isNewDoc);
		btEditarCadastro.setDisable(!isNewDoc);
		btCopy.setDisable(!isNewDoc);
	}

	public void setIsLocked(Boolean isLocked) {
		this.isLocked = isLocked;
	}

	public void setChangesCount(Integer changesCount) {
		this.changesCount = changesCount;
	}

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}

	private void setLockButtonStyle() {
		if (!isLocked) {
			btLock.setGraphic(imgViewMap.get("btUnlock"));
			btLock.getStyleClass().clear();
			btLock.getStyleClass().add("button");
			btLock.getStyleClass().add("unlocked-button");
			return;
		}

		btLock.setGraphic(imgViewMap.get("btLock"));
		btLock.getStyleClass().clear();
		btLock.getStyleClass().add("button");
		btLock.getStyleClass().add("locked-button");

	}

	private void setNodesDisable() {
		comboBoxClient.setDisable(isLocked);
		comboBoxConcreteProvider.setDisable(isLocked);
		btSearchClient.setDisable(isLocked);
		btSearchConcreteProvider.setDisable(isLocked);
		txtObra.setDisable(isLocked);
		txtAddress.setDisable(isLocked);
		dpCreationDate.setDisable(isLocked);
		comboBoxConcreteDesign.setDisable(isLocked);
		btApagarProbeta.setDisable(isLocked);
		btInserirProbeta.setDisable(isLocked);
		btEditarCadastro.setDisable(isLocked);
		btCopy.setDisable(isLocked);
	}

	public void setIsNewDoc(Boolean isNewDoc) {
		this.isNewDoc = isNewDoc;
	}

	private void setCompresionTestFormData() {

		ValidationException exception = new ValidationException("getCompresionTestFormData Error");

		if (comboBoxClient.getValue() == null) {
			exception.addError("client", "vacío");
		}

		if (comboBoxConcreteProvider.getValue() == null) {
			exception.addError("provider", "vacío");
		}

		if (comboBoxConcreteDesign.getValue() == null) {
			exception.addError("concreteDesign", "vacío");
		}

		if (dpCreationDate.getValue() == null) {
			exception.addError("date", "vacío");
		}

		if (txtObra.getText() == null || txtObra.getText().trim().equals("")) {
			exception.addError("obra", "vacío");
		}

		if (txtAddress.getText() == null || txtAddress.getText().trim().equals("")) {
			exception.addError("address", "vacío");
		}

		if (exception.getErrors().size() > 0) {
			throw exception;
		}

		if (txtid.getText() != null) {
			this.compresionTest.setId(Utils.tryParseToInt(txtid.getText()));
		}

		this.compresionTest.setClient(comboBoxClient.getValue());

		this.compresionTest.setConcreteProvider(comboBoxConcreteProvider.getValue());

		this.compresionTest.setConcreteDesign(comboBoxConcreteDesign.getValue());

		if (dpCreationDate.getValue() != null) {
			Instant instant = Instant.from(dpCreationDate.getValue().atStartOfDay(ZoneId.systemDefault()));
			this.compresionTest.setDate(Date.from(instant));
		}

		this.compresionTest.setObra(txtObra.getText());

		this.compresionTest.setAddress(txtAddress.getText());

	}

	private void setErrorMessages(Map<String, String> errors) {
		Set<String> fields = errors.keySet();

		labelErrorClient.setText(fields.contains("client") ? errors.get("client") : "");
		labelErrorDate.setText(fields.contains("date") ? errors.get("date") : "");
		labelErrorObra.setText(fields.contains("obra") ? errors.get("obra") : "");
		labelErrorAddress.setText(fields.contains("address") ? errors.get("address") : "");
		labelErrorConcreteDesign.setText(fields.contains("concreteDesign") ? errors.get("concreteDesign") : "");
	}

	private void setButtonsGraphics() {
		imgViewMap.put("btLock", Utils.createImageView("/images/lock.png", 20.0, 20.0));
		imgViewMap.put("btUnlock", Utils.createImageView("/images/unlock.png", 20.0, 20.0));
		btLock.setGraphic(imgViewMap.get("btLock"));

		Utils.setButtonGraphic("/images/print.png", btPrint, 20.0, 20.0);
		Utils.setButtonGraphic("/images/lupa.png", btSearchClient, 15.0, 15.0);
		Utils.setButtonGraphic("/images/lupa.png", btSearchConcreteProvider, 15.0, 15.0);

		Utils.setButtonGraphic("/images/fileIcons/copy.png", btCopy, 20.0, 20.0);
		Utils.setButtonGraphic("/images/filter_off.png", btClearFilter, 20.0, 20.0);
		Utils.setButtonGraphic("/images/filter_on.png", btFilter, 20.0, 20.0);
	}

	private void setTableColumnsCellValueFactory() {
		
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("id"));
		tableColumnCodigo.setCellValueFactory(new PropertyValueFactory<>("code"));

		tableColumnSlump.setCellValueFactory(new PropertyValueFactory<>("slump"));
		Utils.formatTableColumnDouble(tableColumnSlump, 2);

		tableColumnFechaMoldeo.setCellValueFactory(new PropertyValueFactory<>("moldeDate"));
		Utils.formatTableColumnDate(tableColumnFechaMoldeo, "dd/MM/yyyy");

		tableColumnFechaRotura.setCellValueFactory(new PropertyValueFactory<>("ruptureDate"));
		Utils.formatTableColumnDate(tableColumnFechaRotura, "dd/MM/yyyy");

		tableColumnEdad.setCellValueFactory(new PropertyValueFactory<>("days"));

		tableColumnDiameter.setCellValueFactory(new PropertyValueFactory<>("diameter"));
		Utils.formatTableColumnDouble(tableColumnDiameter, 2);

		tableColumnHeight.setCellValueFactory(new PropertyValueFactory<>("height"));
		Utils.formatTableColumnDouble(tableColumnHeight, 2);

		tableColumnWeight.setCellValueFactory(new PropertyValueFactory<>("weight"));
		Utils.formatTableColumnDouble(tableColumnWeight, 3);

		tableColumnDensid.setCellValueFactory(new PropertyValueFactory<>("densid"));
		Utils.formatTableColumnDouble(tableColumnDensid, 2);

		tableColumnTonRupture.setCellValueFactory(new PropertyValueFactory<>("tonRupture"));
		Utils.formatTableColumnDouble(tableColumnTonRupture, 2);

		tableColumnfckRupture.setCellValueFactory(new PropertyValueFactory<>("fckRupture"));
		Utils.formatTableColumnDouble(tableColumnfckRupture, 2);
	}

	public void setTxtListeners() {

		txtObra.textProperty().addListener(new ChangeListener<String>() {
			@Override
			public void changed(ObservableValue<? extends String> observable, String oldValue, String newValue) {
				onChangeCount();
			}
		});

		txtAddress.textProperty().addListener(new ChangeListener<String>() {
			@Override
			public void changed(ObservableValue<? extends String> observable, String oldValue, String newValue) {
				onChangeCount();
			}
		});
	}

	public void updateTableView() {
		if (corpoDeProvaService == null) {
			throw new IllegalStateException("Service was null");
		}

		DBTask<CorpoDeProvaService, List<CorpoDeProva>> task = new DBTask<CorpoDeProvaService, List<CorpoDeProva>>(
				corpoDeProvaService, corpoDeProvaService -> {

					return corpoDeProvaService.findByCompresionTestIdWithTimeZone(compresionTest.getId(),
							TimeZone.getDefault());
				});

		Consumer<Bounce> bounceFinalAction = (Bounce b) -> {
			b.stop();
			b.getNode().setVisible(false);
		};

		task.setOnSucceeded(e -> {
			try {
				tableViewCorpoDeProva.setItems(FXCollections.observableArrayList(task.get()));
				Utils.formatCorpoDeProvaTableViewRowColor(tableViewCorpoDeProva);
				tableViewCorpoDeProva.refresh();
				tableViewCorpoDeProva.setDisable(false);
				bounces.forEach(bounceFinalAction);
				setFormaLockedState();
				Utils.setDisableButtons(
						Arrays.asList(btEditarProbeta, btPrint, btClose, btLock, btFilter, btClearFilter), false);

			} catch (ExecutionException | InterruptedException e1) {
				logger.doLog(Level.WARNING, e1.getMessage(), e1);
				Alerts.showAlert("Error", e1.getCause().toString(), e1.getMessage(), AlertType.ERROR);
			}
		});
		exec.execute(task);

	}

	public void updateFormData() {
		if (compresionTest == null) {
			throw new IllegalStateException("Documento vacío");
		}

		txtid.setText((compresionTest.getId()).toString());

		if (compresionTest.getClient() == null) {
			comboBoxClient.getSelectionModel().selectFirst();
		} else {
			comboBoxClient.setValue(compresionTest.getClient());
		}

		if (compresionTest.getConcreteProvider() == null) {
			comboBoxConcreteProvider.getSelectionModel().selectFirst();
		} else {
			comboBoxConcreteProvider.setValue(compresionTest.getConcreteProvider());
		}

		if (compresionTest.getConcreteDesign() == null) {
			comboBoxConcreteDesign.getSelectionModel().selectFirst();
		} else {
			comboBoxConcreteDesign.setValue(compresionTest.getConcreteDesign());
		}

		if (compresionTest.getDate() != null) {
			dpCreationDate.setValue(LocalDate.ofInstant(compresionTest.getDate().toInstant(), ZoneId.systemDefault()));

		}
		txtObra.setText(compresionTest.getObra());
		txtAddress.setText(compresionTest.getAddress());
	}

	public void notifyListeners() {
		this.dataChangeListeners.forEach(l -> l.onDataChange());
	};

}
