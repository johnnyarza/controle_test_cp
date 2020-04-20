package gui;

import java.io.IOException;
import java.net.URL;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.List;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.function.Consumer;

import application.db.DbException;
import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.domaim.CorpoDeProva;
import application.service.ClientService;
import application.service.CompresionTestService;
import application.service.CorpoDeProvaService;
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
import javafx.scene.control.Button;
import javafx.scene.control.ButtonType;
import javafx.scene.control.ComboBox;
import javafx.scene.control.ListCell;
import javafx.scene.control.ListView;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.TextField;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.cell.PropertyValueFactory;
import javafx.scene.layout.Pane;
import javafx.scene.layout.VBox;
import javafx.stage.Modality;
import javafx.stage.Stage;
import javafx.util.Callback;

public class CompresionTestFormController implements Initializable,DataChangeListener{
	
	private ObservableList<CorpoDeProva> obsList;
	
	private ObservableList<Cliente> obsListClient;
	
	private CorpoDeProvaService  corpoDeProvaService; 
	
	private CompresionTestService compresionTestService;
	
	private ClientService clientService;
	
	private CompresionTest compresionTest;
	
	SimpleDateFormat sdf = new SimpleDateFormat("dd/MM/yyyy");
	
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
	private ComboBox<Cliente> comboBoxClient;
	
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
	
	public void onBtInserirProbetaAction (ActionEvent event) {	
		Stage parentStage = Utils.currentStage(event);
		CorpoDeProva obj = new CorpoDeProva();
		obj.setCompresionTest(compresionTest);
		
		createDialogForm("/gui/CorpoDeProvaRegistrationForm.fxml", parentStage, (CorpoDeProvaRegistrationController controller) -> {
			controller.setCorpoDeProvaService(new CorpoDeProvaService());
			controller.setCorpoDeProva(obj);
			controller.subscribeDataChangeListener(this);
		});
				
	}
	
	public void onBtApagarProbetaAction() {
		try {
			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmación de accion",
					"Segura que desea apagar probetta?", "Después de apagados los datos seran perdidos");
			
			if (result.get() == ButtonType.OK) {
				CorpoDeProva obj = getCorpoDeProvaView();
				corpoDeProvaService.deleteById(obj.getId());
				updateTableView();
			}
		} catch (DbException e) {
			Alerts.showAlert("Error", "Error deleting probeta", e.getMessage(), AlertType.ERROR);
		}

	}
	
	/*
	private CompresionTest getFormData() {		
		List<CorpoDeProva> list = tableViewCorpoDeProva.getSelectionModel().getSelectedItems();
		return null;
	}*/

	public CorpoDeProvaService getCorpoDeProvaService() {
		return corpoDeProvaService;
	}

	public void setCorpoDeProvaService(CorpoDeProvaService corpoDeProvaService) {
		this.corpoDeProvaService = corpoDeProvaService;
	}

	public CompresionTest getCompresionTest() {
		return compresionTest;
	}

	public void setCompresionTest(CompresionTest compresionTest) {
		this.compresionTest = compresionTest;
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
		
		initializeComboBoxClient();
			
		tableViewCorpoDeProva.prefHeightProperty().bind(vbox.heightProperty());	
	}
	
	public void updateTableView() {
		if (corpoDeProvaService == null) {
			throw new IllegalStateException("Service was null");
		}	
		List<CorpoDeProva> list =corpoDeProvaService.findByCompresionTestId(compresionTest.getId());
		obsList = FXCollections.observableArrayList(list);
		tableViewCorpoDeProva.setItems(obsList);
		tableViewCorpoDeProva.refresh();
	}
	
	public void updateFormData() {
		if (compresionTest == null) {
			throw new IllegalStateException("Service was null");
		}
		
		txtid.setText((compresionTest.getId()).toString());
		
		if (compresionTest.getClient() == null) {
			comboBoxClient.getSelectionModel().selectFirst();
		} else {
			comboBoxClient.setValue(compresionTest.getClient());
		}
		
		txtCreationDate.setText(compresionTest.getDate().toString());
		txtObra.setText(compresionTest.getObra());
		txtAddress.setText(compresionTest.getAddress());
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
	}
	
	public void loadAssociatedObjects() {
		if (clientService == null) {
			throw new IllegalStateException("ClientService was null");
		}
		List<Cliente> list = clientService.findAll();
		obsListClient = FXCollections.observableArrayList(list);
		comboBoxClient.setItems(obsListClient);
	}
	
	private <T> void createDialogForm(String absoluteName, Stage parentStage,Consumer<T> initializingAction) {
		try {
			
			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();
		
			T controller = loader.getController();
			initializingAction.accept(controller);
			
			Stage dialogStage = new Stage();
			dialogStage.setTitle("Nueva rotura");
			dialogStage.setScene(new Scene(pane));
			dialogStage.setResizable(false);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.showAndWait();
					
		}catch (IOException e) {
			e.printStackTrace();
		}
	}

	private CorpoDeProva getCorpoDeProvaView() {
		
		CorpoDeProva cp = tableViewCorpoDeProva.getSelectionModel().getSelectedItem();
		if (cp == null) {
			throw new NullPointerException();
		}
		return cp;
	}
	
	@Override
	public void onDataChange() {
		updateTableView();		
	}

}
