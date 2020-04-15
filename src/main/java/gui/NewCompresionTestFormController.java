package gui;

import java.net.URL;
import java.util.ResourceBundle;

import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.service.ClientService;
import application.service.CompresionTestService;
import gui.util.Utils;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Button;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;

public class NewCompresionTestFormController implements Initializable{
	
	private ClientService clientService;
	
	private CompresionTestService compresionTestService; 
	
	private CompresionTest entity; 
	
	private ObservableList<Cliente> obsList;
	
	@FXML
	private TextField txtId;
	
	@FXML
	private ComboBox<Cliente> comboBoxClient;
	
	@FXML
	private TextField txtObra;
	
	@FXML
	private TextField txtAddress;
	
	@FXML
	private Button btCreate;
	
	@FXML
	private Button btCancel;
	
	@FXML 
	Label labelErrorClient;
	
	public CompresionTest getEntity() {
		return entity;
	}

	public void setEntity(CompresionTest entity) {
		this.entity = entity;
	}
	
	public CompresionTestService getCompresionTestService() {
		return compresionTestService;
	}

	public void setCompresionTestService(CompresionTestService compresionTestService) {
		this.compresionTestService = compresionTestService;
	}
	
	public ClientService getClientService() {
		return clientService;
	}

	public void setClientService(ClientService clientService) {
		this.clientService = clientService;
	}
	
	@FXML
	public void onBtCreateAction() {
		
	}
	
	@FXML
	public void onBtCancelAction(ActionEvent event) {
		Utils.currentStage(event).close();
		
	}
	@Override
	public void initialize(URL url, ResourceBundle rb) {
		initializeNodes();
		
	}
	
	private void initializeNodes() {
		
	}

	public void loadAssociatedObjects



}
