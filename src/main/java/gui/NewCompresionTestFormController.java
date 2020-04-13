package gui;

import java.net.URL;
import java.util.ArrayList;
import java.util.List;
import java.util.ResourceBundle;

import application.domaim.Cliente;
import application.service.ClientService;
import gui.util.Utils;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Button;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;

public class NewCompresionTestFormController implements Initializable{
	
	private ClientService clientService;
	
	@FXML
	private TextField txtId;
	
	@FXML
	private ComboBox<Cliente> comboBoxClient;
	
	@FXML 
	Label labelErrorClient;
	
	@FXML
	private Button btCreate;
	
	@FXML
	private Button btCancel;
	
	@FXML
	public void onBtCreateAction() {
		List<Cliente> list = new ArrayList<>();
		list =clientService.findAll() ;
		System.out.println(list);
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

}
