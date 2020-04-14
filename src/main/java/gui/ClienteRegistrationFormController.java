package gui;

import java.net.URL;
import java.util.ResourceBundle;

import application.domaim.Cliente;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Button;
import javafx.scene.control.TextField;

public class ClienteRegistrationFormController implements Initializable{
	
	private Cliente entity;
	
	@FXML
	private Button btCadastrar;
	
	@FXML
	private Button btCancelar;
	
	@FXML
	private TextField txtId;
	
	@FXML
	private TextField txtName;
	
	@FXML
	private TextField txtPhone;
	
	@FXML
	private TextField txtAddress;
	
	@FXML
	private TextField txtEmail;
	
	@FXML
	public void onBtCadastrarAction() {
		
	}
	
	@FXML
	public void onBtCancelarAction() {
		
	}

	public void setEntity(Cliente entity) {
		this.entity = entity;
	}

	@Override
	public void initialize(URL arg0, ResourceBundle arg1) {
		// TODO Auto-generated method stub
		
	}
	
	
	

}
