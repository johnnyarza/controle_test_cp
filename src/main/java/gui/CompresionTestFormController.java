package gui;

import java.net.URL;
import java.util.Date;
import java.util.List;
import java.util.ResourceBundle;

import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.domaim.CorpoDeProva;
import application.service.CorpoDeProvaService;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Button;
import javafx.scene.control.ComboBox;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.TextField;
import javafx.scene.control.cell.PropertyValueFactory;
import javafx.scene.layout.VBox;

public class CompresionTestFormController implements Initializable{
//TODO implemntar controller e formulario
	
	private ObservableList<CorpoDeProva> obsList;
	
	private CorpoDeProvaService  corpoDeProvaService; 
	
	@FXML
	private TextField txtid;
	
	@FXML
	private TextField txtCreationDate;
	
	@FXML
	private TextField txtObra;
	
	@FXML
	private TextField txtAddress;
	
	@FXML
	private Button btEditarCadastro;
	
	@FXML
	private Button btInserirProbeta;
	
	@FXML
	private Button btApagarProbeta;	
	
	@FXML
	private VBox vbox;
	
	@FXML
	private ComboBox<CompresionTest> comboBoxClient;
	
	@FXML
	private TableView<CorpoDeProva> tableViewCorpoDeProva;
	
	@FXML 
	TableColumn<CorpoDeProva, Integer> tableColumnId;
	
	@FXML 
	TableColumn<CorpoDeProva, String> tableColumnCodigo;
	
	@FXML 
	TableColumn<CorpoDeProva, Double> tableColumnSlump;
	
	@FXML 
	TableColumn<CorpoDeProva, Date> tableColumnFechaMoldeo;
	
	@FXML 
	TableColumn<CorpoDeProva, Date> tableColumnFechaRotura;
	
	@FXML 
	TableColumn<CorpoDeProva, Integer> tableColumnEdad;
	
	@FXML 
	TableColumn<CorpoDeProva, Double> tableColumnDiameter;
	
	@FXML 
	TableColumn<CorpoDeProva, Double> tableColumnHeight;
	
	@FXML 
	TableColumn<CorpoDeProva, Double> tableColumnWeight;
	
	@FXML 
	TableColumn<CorpoDeProva, Double> tableColumnDensid;
	
	@FXML 
	TableColumn<CorpoDeProva, Double> tableColumnTonRupture;
	
	@FXML 
	TableColumn<CorpoDeProva, Double> tableColumnfckRupture;
	
	public CorpoDeProvaService getCorpoDeProvaService() {
		return corpoDeProvaService;
	}

	public void setCorpoDeProvaService(CorpoDeProvaService corpoDeProvaService) {
		this.corpoDeProvaService = corpoDeProvaService;
	}

	@Override
	public void initialize(URL arg0, ResourceBundle arg1) {
		initializeNodes();
		
	}
	
	private void initializeNodes() {
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("id"));
		tableColumnCodigo.setCellValueFactory(new PropertyValueFactory<>("code"));
		tableColumnSlump.setCellValueFactory(new PropertyValueFactory<>("slump"));
		tableColumnFechaMoldeo.setCellValueFactory(new PropertyValueFactory<>("moldeDate"));
		tableColumnFechaRotura.setCellValueFactory(new PropertyValueFactory<>("ruptureDate"));
		tableColumnEdad.setCellValueFactory(new PropertyValueFactory<>("days"));
		tableColumnDiameter.setCellValueFactory(new PropertyValueFactory<>("diameter"));
		tableColumnHeight.setCellValueFactory(new PropertyValueFactory<>("height"));
		tableColumnWeight.setCellValueFactory(new PropertyValueFactory<>("weight"));
		tableColumnDensid.setCellValueFactory(new PropertyValueFactory<>("densid"));
		tableColumnTonRupture.setCellValueFactory(new PropertyValueFactory<>("tonRupture"));
		tableColumnfckRupture.setCellValueFactory(new PropertyValueFactory<>("fckRupture"));
		
		
		tableViewCorpoDeProva.prefHeightProperty().bind(vbox.heightProperty());	
		

	}
	
	public void updateTableView() {
		if (corpoDeProvaService == null) {
			throw new IllegalStateException("Service was null");
		}
		//List<Cliente> list = new ArrayList<>();
		//obsList = FXCollections.observableArrayList(list);
		//tableViewClient.setItems(obsList);
		
		List<CorpoDeProva> list =corpoDeProvaService.findAll();
		list.add(new CorpoDeProva(1, "aaa", new Cliente(), new CompresionTest(), 12.0, new Date(), new Date(), 12.0, 13.0, 14.0, 15.0));
		//TODO mock values
		obsList = FXCollections.observableArrayList(list);
		tableViewCorpoDeProva.setItems(obsList);
		tableViewCorpoDeProva.refresh();
	}

}
