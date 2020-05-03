package gui;

import java.io.IOException;
import java.net.URL;
import java.util.List;
import java.util.Optional;
import java.util.ResourceBundle;

import application.Program;
import application.Report.ReportFactory;
import application.db.DbException;
import application.domaim.Cliente;
import application.service.ClientService;
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

public class ClientListController implements Initializable,DataChangeListener{
	
	private ClientService service;
	
	private ObservableList<Cliente> obsList;
		
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
		createDialogForm(obj,"/gui/ClienteRegistrationForm.fxml", parentStage);
	}
	
	@FXML
	public void onBtEditAction(ActionEvent event) {
		try {
			Stage parentStage = Utils.currentStage(event);
			Cliente obj = getClientFromTableView();
			createDialogForm(obj,"/gui/ClienteRegistrationForm.fxml", parentStage);
			
		} catch (NullPointerException e) {
			Alerts.showAlert("Error", "Ningun cliente seleccionado", e.getMessage(), AlertType.ERROR);
		}
	}
	
	@FXML
	public void onBtDeleteAction(ActionEvent event) {
		try {		
			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmacion", null, "Segura que desea apagar cliente?");
			if (result.get() == ButtonType.OK) {
				Cliente obj = getClientFromTableView();
				Integer id = obj.getId();
				service.deleteById(id);	
				onDataChange();
			}
		} catch (DbException e) {
			Alerts.showAlert("Error", "DbException", e.getMessage(), AlertType.ERROR);
		} catch (NullPointerException e1) {
			Alerts.showAlert("Error", "NullPointerException", e1.getMessage(), AlertType.ERROR);
		}
	}
	@FXML
	public void onBtPrintAction() {
		ReportFactory rF = new ReportFactory();
		rF.clientReportView();
	}
	public void setClientService(ClientService service) {
		this.service = service;
	}
	
	@Override
	public void initialize(URL url, ResourceBundle rb) {
		initializeNodes();
		
	}

	private void initializeNodes() {
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("id"));
		tableColumnName.setCellValueFactory(new PropertyValueFactory<>("name"));
		tableColumnAddress.setCellValueFactory(new PropertyValueFactory<>("address"));
		tableColumnEmail.setCellValueFactory(new PropertyValueFactory<>("email"));
		tableColumnPhone.setCellValueFactory(new PropertyValueFactory<>("phone"));
		
		Stage stage =(Stage) Program.getMainScene().getWindow();
		tableViewClient.prefHeightProperty().bind(stage.heightProperty());	
	}

	public void updateTableView() {
		if (service == null) {
			throw new IllegalStateException("Service was null");
		}
		//List<Cliente> list = new ArrayList<>();
		//obsList = FXCollections.observableArrayList(list);
		//tableViewClient.setItems(obsList);
		
		List<Cliente> list =service.findAll();
		obsList = FXCollections.observableArrayList(list);
		tableViewClient.setItems(obsList);
		tableViewClient.refresh();
	}
	
	public  void createDialogForm(Cliente obj, String absoluteName, Stage parentStage) {
		try {
			
			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();
			
			ClienteRegistrationFormController controller = loader.getController();
			controller.setEntity(obj);
			controller.setService(new ClientService());
			controller.subscribeDataChangeListener(this);
			controller.updateFormData();
			
			Stage dialogStage = new Stage();
			dialogStage.setTitle("Cliente");
			dialogStage.setScene(new Scene(pane));
			dialogStage.setResizable(false);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.showAndWait();
			
			
		}catch (IOException e) {
			e.printStackTrace();
		}
	}

	@Override
	public void onDataChange() {
		updateTableView();		
	}
	
	private Cliente getClientFromTableView() {

		//Integer row = tableViewClient.getSelectionModel().selectedIndexProperty().get();
		Cliente client = tableViewClient.getSelectionModel().getSelectedItem();
		//Integer id = tableColumnId.getCellData(row);
		if (client == null) {
			throw new NullPointerException("Cliente vacío o no seleccionado");
		}
		return client;
	}
	
}

