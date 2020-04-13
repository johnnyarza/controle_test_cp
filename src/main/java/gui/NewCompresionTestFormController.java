package gui;

import java.net.URL;
import java.util.ResourceBundle;

import application.domaim.Cliente;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Button;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;

public class NewCompresionTestFormController implements Initializable{
	
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
		System.out.println("Creado");
	}
	
	@FXML
	public void onBtCancelAction() {
		System.out.println("Cancelado");
	}
	@Override
	public void initialize(URL url, ResourceBundle rb) {
		initializeNodes();
		
	}
	
	private void initializeNodes() {
		
	}

}
