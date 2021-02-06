package gui;

import java.io.IOException;
import java.net.URL;
import java.nio.file.Paths;
import java.util.ResourceBundle;
import java.util.concurrent.Executor;
import java.util.concurrent.Executors;
import java.util.function.Consumer;
import java.util.logging.Level;

import application.Program;
import application.db.DB;
import application.domaim.CompresionTest;
import application.log.LogUtils;
import application.service.ClientService;
import application.service.CompresionTestListService;
import application.service.CompresionTestService;
import application.service.ConcreteDesignService;
import application.service.CorpoDeProvaService;
import application.service.MaterialService;
import application.service.ProviderService;
import gui.util.Alerts;
import javafx.concurrent.Task;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.fxml.Initializable;
import javafx.scene.Node;
import javafx.scene.Scene;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.MenuBar;
import javafx.scene.control.MenuItem;
import javafx.scene.control.ScrollPane;
import javafx.scene.layout.VBox;

public class MainViewController implements Initializable {

	private Executor exec;
	private Boolean btCancelPressed;

	private CompresionTest compresionTest;

	private LogUtils logger;

	@FXML
	private MenuItem BtTest;

	@FXML
	private MenuItem BtConfigReport;

	@FXML
	private MenuItem onBtClientAction;

	@FXML
	private MenuItem btProvider;

	@FXML
	private MenuItem btMaterial;

	@FXML
	private MenuItem btDesign;

	@FXML
	private MenuItem btConfigConnection;

	@FXML
	private MenuItem btAbout;

	@FXML
	private MenuBar myMenuBar;

	@FXML
	public void onBtTestAction(ActionEvent event) {
		var wrapper = new Object() {
			public CompresionTestService compresionTestService;
			public ClientService clientService;
			public CorpoDeProvaService corpoDeProvaService;
			public CompresionTestListService compresionTestListService;
		};
		try {
			wrapper.compresionTestService = new CompresionTestService();
			wrapper.clientService = new ClientService();
			wrapper.corpoDeProvaService = new CorpoDeProvaService();
			wrapper.compresionTestListService = new CompresionTestListService();

			loadView("/gui/LoadCompresionTestView.fxml", (LoadCompresionTestViewController controller) -> {

				controller.setCompresionTestListService(wrapper.compresionTestListService);
				controller.setClientService(wrapper.clientService);
				controller.setCorpoDeProvaService(wrapper.corpoDeProvaService);
				controller.setCompresionTestService(wrapper.compresionTestService);
				controller.setLogger(logger);

				controller.updateViewData();

				if (controller.getLateCorpoDeProvaList().size() > 0) {
					Alerts.showAlert("Aviso", "Hay probetas con fecha proxima o retrasadas", "", AlertType.WARNING);
				}
			}, "/gui/LoadCompresionTestView.css");
		} catch (Exception e) {
			Alerts.showAlert("Aviso", Exception.class.getName(), e.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	public void onBtClientAction(ActionEvent event) {
		var wrapper = new Object() {
			public ClientService clientService;

		};
		try {
			wrapper.clientService = new ClientService();
			loadView("/gui/ClientList.fxml", (ClientListController controller) -> {
				controller.setClientService(wrapper.clientService);
				controller.setLogger(logger);
				controller.updateTableView();
			}, "/gui/ClientList.css");
		} catch (Exception e) {
			Alerts.showAlert("Aviso", Exception.class.getName(), e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtProviderAction(ActionEvent event) {
		var wrapper = new Object() {
			public ProviderService providerService;

		};

		try {
			wrapper.providerService = new ProviderService();
			loadView("/gui/ProveedoresView.fxml", (ProveedoresViewController controller) -> {
				controller.setService(wrapper.providerService);
				controller.setLogger(logger);
				controller.updateTableView();
			}, "/gui/ProveedoresView.css");
		} catch (Exception e) {
			Alerts.showAlert("Aviso", Exception.class.getName(), e.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	public void onBtMaterialAction(ActionEvent event) {
		var wrapper = new Object() {
			public MaterialService materialService;

		};

		try {
			wrapper.materialService = new MaterialService();
			loadView("/gui/MateriaisView.fxml", (MateriaisViewController controller) -> {
				controller.setService(wrapper.materialService);
				controller.setLogger(logger);
				controller.updateTableView();
			}, "/gui/MateriaisView.css");
		} catch (Exception e) {
			Alerts.showAlert("Aviso", Exception.class.getName(), e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtDesignAction(ActionEvent event) {
		var wrapper = new Object() {
			public ConcreteDesignService concreteDesignService;
			public CompresionTestService compresionTestService;

		};

		try {
			wrapper.concreteDesignService = new ConcreteDesignService();
			wrapper.compresionTestService = new CompresionTestService();

			loadView("/gui/ConcreteDesignView.fxml", (ConcreteDesignViewController controller) -> {
				controller.setService(wrapper.concreteDesignService);
				controller.setLogger(logger);
				controller.setCompresionTestService(wrapper.compresionTestService);
				controller.updateTableView();
			}, "/gui/ConcreteDesignView.css");
		} catch (Exception e) {
			Alerts.showAlert("Aviso", Exception.class.getName(), e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtAboutAction(ActionEvent event) {
		loadView("/gui/AboutView.fxml", (ConcreteDesignViewController controller) -> {
		});
	}

	@FXML
	public void onBtConfigReportAction(ActionEvent event) {
		loadView("/gui/ReportConfigView.fxml", (ReportConfigViewController controller) -> {
			controller.setLogger(logger);
			controller.setImagesProp(
					Paths.get(System.getProperty("user.home"), "cp_configs", "ReportImage.properties").toFile());
			controller.loadImages();
		});
	}

	@FXML
	public void onBtConfigConnectionAction(ActionEvent event) {
		loadView("/gui/ConnectionConfigView.fxml", (ConnectionConfigViewController controller) -> {
			controller.setLogger(logger);
			controller.updateViewData();
		});
	}

	public Boolean getBtCancelPressed() {
		return btCancelPressed;
	}

	public void setBtCancelPressed(Boolean btNewTestForm) {
		this.btCancelPressed = btNewTestForm;
	}

	public CompresionTest getCompresionTest() {
		return compresionTest;
	}

	public void setCompresionTest(CompresionTest compresionTest) {
		this.compresionTest = compresionTest;
	}

	private synchronized <T> void loadView(String absoluteName, Consumer<T> initializingAction) {

		try {
			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			VBox newVbox = loader.load();

			Scene mainScene = Program.getMainScene();
			VBox mainVBox = (VBox) ((ScrollPane) mainScene.getRoot()).getContent();

			Node mainMenu = mainVBox.getChildren().get(0);
			mainVBox.getChildren().clear();
			mainVBox.getChildren().add(mainMenu);
			mainVBox.getChildren().addAll(newVbox.getChildren());

			T controller = loader.getController();
			initializingAction.accept(controller);

		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("IO Exception", "Error loading view", e.getMessage(), AlertType.ERROR);
		}
	}

	private synchronized <T> void loadView(String absoluteName, Consumer<T> initializingAction, String css)
			throws IOException {

		FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
		VBox newVbox = loader.load();

		Scene mainScene = Program.getMainScene();

		if (!css.trim().equals("")) {
			mainScene.getStylesheets().add(css);
		}

		VBox mainVBox = (VBox) ((ScrollPane) mainScene.getRoot()).getContent();

		Node mainMenu = mainVBox.getChildren().get(0);
		mainVBox.getChildren().clear();
		mainVBox.getChildren().add(mainMenu);
		mainVBox.getChildren().addAll(newVbox.getChildren());

		T controller = loader.getController();
		initializingAction.accept(controller);

	}

	@Override
	public void initialize(URL uri, ResourceBundle rb) {
		exec = Executors.newCachedThreadPool(runnable -> {
			Thread t = new Thread(runnable);
			t.setDaemon(true);
			return t;
		});
		
		Task<Boolean> task = new Task<Boolean>() {

			@Override
			protected Boolean call() throws Exception {		
				return DB.testConnection();
			}};
			
			task.setOnFailed(e->{
				Alerts.showAlert("Error", "Error al conectar", task.getException().getMessage(), AlertType.ERROR);
				task.getException().printStackTrace();
			});
			
			task.setOnSucceeded(e -> System.out.println("ok"));
			
			exec.execute(task);

	}

}
