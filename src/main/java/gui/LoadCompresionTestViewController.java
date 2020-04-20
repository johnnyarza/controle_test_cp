package gui;

import java.io.IOException;
import java.net.URL;
import java.util.Date;
import java.util.List;
import java.util.ResourceBundle;
import java.util.function.Consumer;

import application.Program;
import application.domaim.CompresionTest;
import application.domaim.CompresionTestList;
import application.service.ClientService;
import application.service.CompresionTestListService;
import application.service.CompresionTestService;
import application.service.CorpoDeProvaService;
import gui.listeners.DataChangeListener;
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

public class LoadCompresionTestViewController implements Initializable, DataChangeListener {

	private CompresionTestListService service;
	
	private ClientService clientService;

	private ObservableList<CompresionTestList> obsList;

	@FXML
	private TableView<CompresionTestList> tableViewClient;

	@FXML
	TableColumn<CompresionTestList, Integer> tableColumnId;

	@FXML
	TableColumn<CompresionTestList, String> tableColumnClientName;

	@FXML
	TableColumn<CompresionTestList, String> tableColumnObra;

	@FXML
	TableColumn<CompresionTestList, String> tableColumnAddress;

	@FXML
	TableColumn<CompresionTestList, Date> tableColumnCreationDate;

	@FXML
	private Button btOpen;

	@FXML
	private Button btCancel;

	@FXML
	private Button btEdit;

	@FXML
	public void onbtOpenAction(ActionEvent event) {
		try {
			CompresionTest obj = getCompresionTestFromTableView();
			Stage parentStage = Utils.currentStage(event);

			createDialogForm("/gui/CompresionTestForm.fxml", parentStage,
					(CompresionTestFormController controller) -> { 
						controller.setCorpoDeProvaService(new CorpoDeProvaService());
						controller.setCompresionTestService(new CompresionTestService());
						controller.setClientService(new ClientService());
						controller.setCompresionTest(obj);
						controller.loadAssociatedObjects();
						controller.updateFormData();
						controller.updateTableView();
					});
		} catch (Exception e) {
			e.printStackTrace();
		}
	}

	@FXML
	public void onBtEditAction(ActionEvent event) {
	}

	@FXML
	public void onBtCancelAction(ActionEvent event) {
		Utils.currentStage(event).close();
	}

	public void setCompresionTestService(CompresionTestListService service) {
		this.service = service;
	}

	public ClientService getClientService() {
		return clientService;
	}

	public void setClientService(ClientService clientService) {
		this.clientService = clientService;
	}

	@Override
	public void initialize(URL url, ResourceBundle rb) {
		initializeNodes();

	}

	private void initializeNodes() {
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("compresionTestId"));
		tableColumnClientName.setCellValueFactory(new PropertyValueFactory<>("clientName"));
		tableColumnAddress.setCellValueFactory(new PropertyValueFactory<>("address"));
		tableColumnCreationDate.setCellValueFactory(new PropertyValueFactory<>("creationDate"));
		tableColumnObra.setCellValueFactory(new PropertyValueFactory<>("obra"));

		Stage stage = (Stage) Program.getMainScene().getWindow();
		tableViewClient.prefHeightProperty().bind(stage.heightProperty());
	}

	public void updateTableView() {
		if (service == null) {
			throw new IllegalStateException("Service was null");
		}
		// List<CompresionTest> list = new ArrayList<>();
		// obsList = FXCollections.observableArrayList(list);
		// tableViewClient.setItems(obsList);

		List<CompresionTestList> list = service.findAll();
		obsList = FXCollections.observableArrayList(list);
		tableViewClient.setItems(obsList);
		tableViewClient.refresh();
	}

	@Override
	public void onDataChange() {
		updateTableView();
	}

	private CompresionTest getCompresionTestFromTableView() {
		CompresionTestList compresionTestList = tableViewClient.getSelectionModel().getSelectedItem();
		if (compresionTestList == null) {
			throw new NullPointerException();
		}
		CompresionTest compresionTest = new CompresionTest(
				compresionTestList.getCompresionTestId(), 
				clientService.findById((compresionTestList.getClientId())), 
				compresionTestList.getObra(), 
				compresionTestList.getAddress(), 
				compresionTestList.getCreationDate());
		return compresionTest;
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
			dialogStage.setResizable(true);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.showAndWait();
			
		
		}catch (IOException e) {
			e.printStackTrace();
		}
	}

}
