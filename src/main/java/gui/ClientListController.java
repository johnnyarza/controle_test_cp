package gui;

import java.io.IOException;
import java.net.URL;
import java.sql.SQLException;
import java.util.Arrays;
import java.util.List;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.Executor;
import java.util.concurrent.Executors;
import java.util.logging.Level;

import application.Program;
import application.Report.ReportFactory;
import application.db.DbException;
import application.domaim.Cliente;
import application.exceptions.ReportException;
import application.log.LogUtils;
import application.service.ClientService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.collections.FXCollections;
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
import javafx.scene.image.Image;
import javafx.scene.layout.Pane;
import javafx.stage.Modality;
import javafx.stage.Stage;
import task.DBTask;

public class ClientListController implements Initializable, DataChangeListener {

	private ClientService service;

	private LogUtils logger;

	private Executor exec;

	private List<Button> buttons;

	@FXML
	private TableView<Cliente> tableViewClient;

	@FXML
	TableColumn<Cliente, Integer> tableColumnId;

	@FXML
	TableColumn<Cliente, String> tableColumnName;

	@FXML
	TableColumn<Cliente, String> tableColumnPhone;

	@FXML
	TableColumn<Cliente, String> tableColumnAddress;

	@FXML
	TableColumn<Cliente, String> tableColumnEmail;

	@FXML
	private Button btNew;

	@FXML
	private Button btDelete;

	@FXML
	private Button btEdit;

	@FXML
	private Button btPrint;

	@FXML
	public void onBtNewAction(ActionEvent event) {
		Stage parentStage = Utils.currentStage(event);
		Cliente obj = new Cliente();
		createDialogForm(obj, "/gui/ClienteRegistrationForm.fxml", parentStage, "/gui/ClienteRegistrationForm.css",
				new Image(ClientListController.class.getResourceAsStream("/images/fileIcons/new_file.png")));
	}

	@FXML
	public void onBtEditAction(ActionEvent event) {
		try {

			if (allowEditOrDelete(event))
				throw new IllegalAccessError("Accesso denegado");

			Stage parentStage = Utils.currentStage(event);
			Cliente obj = getClientFromTableView();
			createDialogForm(obj, "/gui/ClienteRegistrationForm.fxml", parentStage, "",
					new Image(ClientListController.class.getResourceAsStream("/images/fileIcons/edit_file.png")));

		} catch (IllegalAccessError e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalAccessError", e.getMessage(), AlertType.ERROR);
		} catch (NullPointerException e) {
			Alerts.showAlert("Error", "Ningun cliente seleccionado", e.getMessage(), AlertType.ERROR);
		} catch (SQLException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "SQLException", e.getMessage(), AlertType.ERROR);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconc�do", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtDeleteAction(ActionEvent event) {
		try {

			if (allowEditOrDelete(event))
				throw new IllegalAccessError("Accesso denegado");

			Cliente obj = getClientFromTableView();
			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmacion", null,
					"Segura que desea apagar cliente?");
			if (result.get() == ButtonType.OK) {
				Integer id = obj.getId();
				service.deleteById(id);
				onDataChange();
			}
		} catch (IllegalAccessError e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalAccessError", e.getMessage(), AlertType.ERROR);
		} catch (DbException e) {
			Alerts.showAlert("Error", "DbException", e.getMessage(), AlertType.ERROR);
		} catch (NullPointerException e1) {
			Alerts.showAlert("Error", "NullPointerException", e1.getMessage(), AlertType.ERROR);
		} catch (SQLException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "SQLException", e.getMessage(), AlertType.ERROR);
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", "Error al abrir ventana!", AlertType.ERROR);
		} catch (ReportException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "ReportException", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconc�do", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtPrintAction() throws IOException {
		ReportFactory rF = new ReportFactory();
		rF.clientReportView(tableViewClient.getItems());
	}

	private boolean allowEditOrDelete(ActionEvent event) throws SQLException, IOException {
		return Utils.isUserAdmin(event, logger);
	}

	public void setClientService(ClientService service) {
		this.service = service;
	}

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}

	@Override
	public void initialize(URL url, ResourceBundle rb) {
		initializeNodes();
		buttons = Arrays.asList(btDelete, btEdit, btNew, btPrint);
		Utils.setDisableButtons(buttons, true);

		exec = Executors.newCachedThreadPool(runnable -> {
			Thread t = new Thread(runnable);
			t.setDaemon(true);
			return t;
		});
	}

	private void initializeNodes() {
		formatTableVIiewClient();

	}

	private void formatTableVIiewClient() {
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("id"));
		tableColumnId.getStyleClass().add("custom-align");

		tableColumnName.setCellValueFactory(new PropertyValueFactory<>("name"));
		tableColumnName.getStyleClass().add(".description-column-style");

		tableColumnAddress.setCellValueFactory(new PropertyValueFactory<>("address"));
		tableColumnAddress.getStyleClass().add("custom-align");

		tableColumnEmail.setCellValueFactory(new PropertyValueFactory<>("email"));
		tableColumnEmail.getStyleClass().add("custom-align");

		tableColumnPhone.setCellValueFactory(new PropertyValueFactory<>("phone"));
		tableColumnPhone.getStyleClass().add("custom-align");

		Stage stage = (Stage) Program.getMainScene().getWindow();
		tableViewClient.prefHeightProperty().bind(stage.heightProperty());
	}

	public void updateTableView() {
		if (service == null) {
			throw new IllegalStateException("Service was null");
		}

		DBTask<ClientService, List<Cliente>> task = new DBTask<ClientService, List<Cliente>>(service,
				service -> service.findAll());

		task.setOnSucceeded(e -> {
			try {
				tableViewClient.setItems(FXCollections.observableArrayList(task.get()));
				tableViewClient.refresh();
				Utils.setDisableButtons(buttons, false);
			} catch (InterruptedException e1) {
				// TODO Auto-generated catch block
				e1.printStackTrace();
			} catch (ExecutionException e1) {
				// TODO Auto-generated catch block
				e1.printStackTrace();
			}

		});

		exec.execute(task);

	}

	public void createDialogForm(Cliente obj, String absoluteName, Stage parentStage, String css, Image windowIcon) {
		try {

			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();

			ClienteRegistrationFormController controller = loader.getController();
			controller.setLogger(new LogUtils());
			controller.setEntity(obj);
			controller.setService(new ClientService());
			controller.subscribeDataChangeListener(this);
			controller.updateFormData();

			Stage dialogStage = new Stage();
			dialogStage.getIcons().add(windowIcon);
			dialogStage.setTitle("Cliente");
			dialogStage.setScene(new Scene(pane));

			if (!css.trim().equals("")) {
				dialogStage.getScene().getStylesheets().add(css);
			}

			dialogStage.setResizable(false);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.showAndWait();

		} catch (IOException e) {
			Alerts.showAlert("Error", "Error al abrir ventana", null, AlertType.ERROR);
		} catch (SecurityException e) {
			Alerts.showAlert("Error", "Error inesperado", e.getMessage(), AlertType.ERROR);
		} catch (SQLException e) {
			Alerts.showAlert("Error", "Error en banco de datos", e.getMessage(), AlertType.ERROR);
		}
	}

	@Override
	public void onDataChange() {
		updateTableView();
	}

	private Cliente getClientFromTableView() {

		Cliente client = tableViewClient.getSelectionModel().getSelectedItem();
		if (client == null) {
			throw new NullPointerException("Cliente vac�o o no seleccionado");
		}
		return client;
	}

}
