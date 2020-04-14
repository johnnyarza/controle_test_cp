package gui;

import java.io.IOException;
import java.net.URL;
import java.util.List;
import java.util.ResourceBundle;

import application.Program;
import application.domaim.Cliente;
import application.service.ClientService;
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.fxml.Initializable;
import javafx.scene.Scene;
import javafx.scene.control.Button;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.cell.PropertyValueFactory;
import javafx.scene.layout.Pane;
import javafx.stage.Modality;
import javafx.stage.Stage;

public class ClientListController implements Initializable{
	
	private ClientService service;
	
	
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
	
	private ObservableList<Cliente> obsList;
	
	@FXML
	private Button btEdit;
	
	@FXML
	public void onBtNewAction(ActionEvent event) {
		Stage parentStage = Utils.currentStage(event);
		createDialogForm("/gui/ClienteRegistrationForm.fxml", parentStage);
	}
	
	@FXML
	public void onBtEditAction() {
		System.out.println("Edit cliente");	
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
		List<Cliente> list = service.findAll();
		obsList = FXCollections.observableArrayList(list);
		tableViewClient.setItems(obsList);
	}
	
	public  void createDialogForm(String absoluteName, Stage parentStage) {
		try {
			
			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();
			
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
	
}
