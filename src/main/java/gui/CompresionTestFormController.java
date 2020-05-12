package gui;

import java.io.IOException;
import java.net.URL;
import java.text.SimpleDateFormat;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneId;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.Set;
import java.util.TimeZone;
import java.util.function.Consumer;

import application.Report.ReportFactory;
import application.db.DbException;
import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.domaim.ConcreteDesign;
import application.domaim.CorpoDeProva;
import application.exceptions.ValidationException;
import application.service.ClientService;
import application.service.CompresionTestService;
import application.service.ConcreteDesignService;
import application.service.CorpoDeProvaService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.beans.value.ChangeListener;
import javafx.beans.value.ObservableValue;
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
import javafx.scene.control.ComboBox;
import javafx.scene.control.DatePicker;
import javafx.scene.control.Label;
import javafx.scene.control.ListCell;
import javafx.scene.control.ListView;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.TextField;
import javafx.scene.control.cell.PropertyValueFactory;
import javafx.scene.layout.Pane;
import javafx.scene.layout.VBox;
import javafx.stage.Modality;
import javafx.stage.Stage;
import javafx.util.Callback;

public class CompresionTestFormController implements Initializable, DataChangeListener {

	// TODO diminuir o tamanho do chart para entra na folha
	// TODO mudar as label escrita codigo para descrição
	// TODO acrescentar mais um field referente a quem está fornecendo o concreto
	// TODO Criar uma função para fazer um relatorio com todos os CPs de um cliente

	private ObservableList<CorpoDeProva> obsList;

	private ObservableList<Cliente> obsListClient;

	private ObservableList<ConcreteDesign> obsListConcreteDesign;

	private CorpoDeProvaService corpoDeProvaService;

	private CompresionTestService compresionTestService;

	private ConcreteDesignService concreteDesignService;

	private ClientService clientService;

	private CompresionTest compresionTest;

	SimpleDateFormat sdf = new SimpleDateFormat("dd/MM/yyyy");

	private Integer changesCount;
	
	List<DataChangeListener> dataChangeListeners = new ArrayList<>();

	@FXML
	private ComboBox<ConcreteDesign> comboBoxConcreteDesign;

	@FXML
	private TextField txtid;

	@FXML
	private TextField txtObra;

	@FXML
	private TextField txtAddress;

	@FXML
	private Button btEditarCadastro;

	@FXML
	private Button btInserirProbeta;

	@FXML
	private Button btEditarProbeta;

	@FXML
	private Button btApagarProbeta;

	@FXML
	private Button btClose;

	@FXML
	private Button btPrint;

	@FXML
	private Button btFilter;

	@FXML
	private Button btClearFilter;

	@FXML
	private VBox vbox;

	@FXML
	private ComboBox<Cliente> comboBoxClient;

	@FXML
	private TableView<CorpoDeProva> tableViewCorpoDeProva;

	@FXML
	private TableColumn<CorpoDeProva, Integer> tableColumnId;

	@FXML
	private TableColumn<CorpoDeProva, String> tableColumnCodigo;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnSlump;

	@FXML
	private TableColumn<CorpoDeProva, Date> tableColumnFechaMoldeo;

	@FXML
	private TableColumn<CorpoDeProva, Date> tableColumnFechaRotura;

	@FXML
	private TableColumn<CorpoDeProva, Integer> tableColumnEdad;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnDiameter;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnHeight;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnWeight;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnDensid;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnTonRupture;

	@FXML
	private TableColumn<CorpoDeProva, Double> tableColumnfckRupture;

	@FXML
	private DatePicker dpCreationDate;

	@FXML
	private Label labelErrorClient;

	@FXML
	private Label labelErrorDate;

	@FXML
	private Label labelErrorObra;

	@FXML
	private Label labelErrorAddress;

	@FXML
	private Label labelErrorConcreteDesign;

	@FXML
	private Label labelMessages;

	@FXML
	public void onBtInserirProbetaAction(ActionEvent event) {
		try {
			Stage parentStage = Utils.currentStage(event);
			CorpoDeProva obj = new CorpoDeProva();
			obj.setCompresionTest(compresionTest);

			createDialogForm("/gui/CorpoDeProvaRegistrationForm.fxml", parentStage, "Inserir Probeta",
					(CorpoDeProvaRegistrationController controller) -> {
						controller.setCorpoDeProvaService(new CorpoDeProvaService());
						controller.setCorpoDeProva(obj);
						controller.subscribeDataChangeListener(this);
					}, (CorpoDeProvaRegistrationController controller) -> {
					});
		} catch (Exception e) {
			Alerts.showAlert("Error", "Exception", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtEditarProbetaAction(ActionEvent event) {
		try {
			Stage parentStage = Utils.currentStage(event);
			CorpoDeProva obj = getCorpoDeProvaView();

			createDialogForm("/gui/CorpoDeProvaRegistrationForm.fxml", parentStage, "Editar Probeta",
					(CorpoDeProvaRegistrationController controller) -> {
						controller.setCorpoDeProvaService(new CorpoDeProvaService());
						controller.setCorpoDeProva(obj);
						controller.updateFormData();
						controller.subscribeDataChangeListener(this);
					}, (CorpoDeProvaRegistrationController controller) -> {
					});
		} catch (NullPointerException e) {
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtApagarProbetaAction() {
		try {
			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmación de accion",
					"Seguro que desea apagar probeta?", "Después de apagados los datos seran perdidos");

			if (result.get() == ButtonType.OK) {
				CorpoDeProva obj = getCorpoDeProvaView();
				corpoDeProvaService.deleteById(obj.getId());
				onDataChange();
			}
		} catch (DbException e) {
			Alerts.showAlert("Error", "Error deleting probeta", e.getMessage(), AlertType.ERROR);
		} catch (NullPointerException e1) {
			Alerts.showAlert("Error", "NullPointerException", e1.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	public void onBtEditarCadastroAction() {
		try {
			setCompresionTestFormData();
			compresionTestService.saveOrUpdate(this.compresionTest);
			onDataChange();
			setChangesCount(0);
			Alerts.showAlert("Acción conluida", "Cadastro actualizado", null, AlertType.INFORMATION);
		} catch (ValidationException e) {
			setErrorMessages(e.getErrors());
		} catch (DbException e1) {
			Alerts.showAlert("Error", "Error saving compresionTest", e1.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onBtCloseAction(ActionEvent event) {
		if (this.changesCount > 0) {
			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmar acción", "Segura que desea cerrar?",
					"Hay datos no guardados!!");
			if (result.get() == ButtonType.OK) {
				Utils.currentStage(event).close();
			}
		} else {
			Utils.currentStage(event).close();
		}
	}

	@FXML
	public void onbtPrintAction() {
		ReportFactory rF = new ReportFactory();
		List<CorpoDeProva> list = getCorpoDeProvaListFromTable();
		rF.compresionTestReportView(list, this.compresionTest);
	}

	public void onBtFilterAction(ActionEvent event) {
		try {
			Stage parentStage = Utils.currentStage(event);

			createDialogForm("/gui/CorpoDeProvaFilterForm.fxml", parentStage, "Filtrar Probetas por Fecha",
					(CorpoDeProvaFilterFormController controller) -> {
						controller.setService(new CorpoDeProvaService());
						controller.setCompresionTest(this.compresionTest);
					}, (CorpoDeProvaFilterFormController controller) -> {
						obsList = controller.getObsList();
					});
			tableViewCorpoDeProva.setItems(this.obsList);
			tableViewCorpoDeProva.refresh();
		} catch (Exception e) {
			Alerts.showAlert("Error", "Exception", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	public void onbtClearFilterAction() {
		updateTableView();
	}

	@FXML
	public void onChangeCount() {
		this.changesCount += 1;
	}
	
	@FXML
	public void onTableViewSort() {
		Utils.formatCorpoDeProvaTableViewRowColor(tableViewCorpoDeProva);
	}

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

	public ConcreteDesignService getConcreteDesignService() {
		return concreteDesignService;
	}

	public void setConcreteDesignService(ConcreteDesignService concreteDesignService) {
		this.concreteDesignService = concreteDesignService;
	}


	public Integer getChangesCount() {
		return changesCount;
	}


	public void setChangesCount(Integer changesCount) {
		this.changesCount = changesCount;
	}

	@Override
	public void initialize(URL arg0, ResourceBundle arg1) {
		initializeNodes();
	}

	private void initializeNodes() {
		setTableColumnsCellValueFactory();
		initializeComboBoxClient();
		tableViewCorpoDeProva.prefHeightProperty().bind(vbox.heightProperty());
	}

	private void setTableColumnsCellValueFactory() {
		tableColumnId.setCellValueFactory(new PropertyValueFactory<>("id"));
		tableColumnCodigo.setCellValueFactory(new PropertyValueFactory<>("code"));

		tableColumnSlump.setCellValueFactory(new PropertyValueFactory<>("slump"));
		Utils.formatTableColumnDouble(tableColumnSlump, 2);

		tableColumnFechaMoldeo.setCellValueFactory(new PropertyValueFactory<>("moldeDate"));
		Utils.formatTableColumnDate(tableColumnFechaMoldeo, "dd/MM/yyyy");

		tableColumnFechaRotura.setCellValueFactory(new PropertyValueFactory<>("ruptureDate"));
		Utils.formatTableColumnDate(tableColumnFechaRotura, "dd/MM/yyyy");

		tableColumnEdad.setCellValueFactory(new PropertyValueFactory<>("days"));

		tableColumnDiameter.setCellValueFactory(new PropertyValueFactory<>("diameter"));
		Utils.formatTableColumnDouble(tableColumnDiameter, 2);

		tableColumnHeight.setCellValueFactory(new PropertyValueFactory<>("height"));
		Utils.formatTableColumnDouble(tableColumnHeight, 2);

		tableColumnWeight.setCellValueFactory(new PropertyValueFactory<>("weight"));
		Utils.formatTableColumnDouble(tableColumnWeight, 3);

		tableColumnDensid.setCellValueFactory(new PropertyValueFactory<>("densid"));
		Utils.formatTableColumnDouble(tableColumnDensid, 2);

		tableColumnTonRupture.setCellValueFactory(new PropertyValueFactory<>("tonRupture"));
		Utils.formatTableColumnDouble(tableColumnTonRupture, 2);

		tableColumnfckRupture.setCellValueFactory(new PropertyValueFactory<>("fckRupture"));
		Utils.formatTableColumnDouble(tableColumnfckRupture, 2);
	}

	public void setTxtListeners() {

		txtObra.textProperty().addListener(new ChangeListener<String>() {
			@Override
			public void changed(ObservableValue<? extends String> observable, String oldValue, String newValue) {
				onChangeCount();
			}
		});

		txtAddress.textProperty().addListener(new ChangeListener<String>() {
			@Override
			public void changed(ObservableValue<? extends String> observable, String oldValue, String newValue) {
				onChangeCount();
			}
		});
	}

	public void updateTableView() {
		if (corpoDeProvaService == null) {
			throw new IllegalStateException("Service was null");
		}
		List<CorpoDeProva> list = corpoDeProvaService.findByCompresionTestIdWithTimeZone(compresionTest.getId(),
				TimeZone.getDefault());
		obsList = FXCollections.observableArrayList(list);
		tableViewCorpoDeProva.setItems(obsList);
		Utils.formatCorpoDeProvaTableViewRowColor(tableViewCorpoDeProva);
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

		if (compresionTest.getConcreteDesign() == null) {
			comboBoxConcreteDesign.getSelectionModel().selectFirst();
		} else {
			comboBoxConcreteDesign.setValue(compresionTest.getConcreteDesign());
		}

		if (compresionTest.getDate() != null) {
			dpCreationDate.setValue(LocalDate.ofInstant(compresionTest.getDate().toInstant(), ZoneId.systemDefault()));

		}
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
		loadClientComboBox();
		loadCompresionDesignComboBox();
	}

	private void loadClientComboBox() {
		if (clientService == null) {
			throw new IllegalStateException("ClientService was null");
		}
		List<Cliente> list = clientService.findAll();
		obsListClient = FXCollections.observableArrayList(list);
		comboBoxClient.setItems(obsListClient);
	}

	private void loadCompresionDesignComboBox() {
		if (concreteDesignService == null) {
			throw new IllegalStateException("concreteDesignService was null");
		}
		List<ConcreteDesign> list = concreteDesignService.findAllConcreteDesign();
		obsListConcreteDesign = FXCollections.observableArrayList(list);
		comboBoxConcreteDesign.setItems(obsListConcreteDesign);
	}

	private <T> void createDialogForm(String absoluteName, Stage parentStage, String fomrName,
			Consumer<T> initializingAction, Consumer<T> initializingActionFinal) {
		try {

			FXMLLoader loader = new FXMLLoader(getClass().getResource(absoluteName));
			Pane pane = loader.load();

			T controller = loader.getController();
			initializingAction.accept(controller);

			Stage dialogStage = new Stage();
			dialogStage.setTitle(fomrName);
			dialogStage.setScene(new Scene(pane));
			dialogStage.setResizable(false);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);
			dialogStage.showAndWait();
			initializingActionFinal.accept(controller);

		} catch (IOException e) {
			e.printStackTrace();
		}
	}

	private CorpoDeProva getCorpoDeProvaView() {

		CorpoDeProva cp = tableViewCorpoDeProva.getSelectionModel().getSelectedItem();
		if (cp == null) {
			throw new NullPointerException("Probeta vacía o no seleccionada");
		}
		return cp;
	}

	private List<CorpoDeProva> getCorpoDeProvaListFromTable() {
		List<CorpoDeProva> list = tableViewCorpoDeProva.getItems();
		if (list == null) {
			throw new IllegalStateException("Lista de probetas vacía");
		}
		return list;
	}

	public void setLabelMessageText(Integer compresionTestId) {
		Integer cpCount = corpoDeProvaService.countCorpoDeProvasToTestbyCompresionTestId(compresionTestId);
		if (cpCount > 0) {
			labelMessages.setText("Hay " + Integer.toString(cpCount) + " probeta(s) para Romper");
		} else {
			labelMessages.setText("");
		}
	}

	@Override
	public void onDataChange() {
		updateFormData();
		updateTableView();
		setLabelMessageText(this.compresionTest.getId());
	}

	private void setCompresionTestFormData() {

		ValidationException exception = new ValidationException("getCompresionTestFormData Error");

		if (comboBoxClient.getValue() == null) {
			exception.addError("client", "vacío");
		}

		if (comboBoxConcreteDesign.getValue() == null) {
			exception.addError("concreteDesign", "vacío");
		}

		if (dpCreationDate.getValue() == null) {
			exception.addError("date", "vacío");
		}

		if (txtObra.getText() == null || txtObra.getText().trim().equals("")) {
			exception.addError("obra", "vacío");
		}

		if (txtAddress.getText() == null || txtAddress.getText().trim().equals("")) {
			exception.addError("address", "vacío");
		}

		if (exception.getErrors().size() > 0) {
			throw exception;
		}

		if (txtid.getText() != null) {
			this.compresionTest.setId(Utils.tryParseToInt(txtid.getText()));
		}

		this.compresionTest.setClient(comboBoxClient.getValue());

		this.compresionTest.setConcreteDesign(comboBoxConcreteDesign.getValue());

		if (dpCreationDate.getValue() != null) {
			Instant instant = Instant.from(dpCreationDate.getValue().atStartOfDay(ZoneId.systemDefault()));
			this.compresionTest.setDate(Date.from(instant));
		}

		this.compresionTest.setObra(txtObra.getText());

		this.compresionTest.setAddress(txtAddress.getText());

	}

	private void setErrorMessages(Map<String, String> errors) {
		Set<String> fields = errors.keySet();

		labelErrorClient.setText(fields.contains("client") ? errors.get("client") : "");
		labelErrorDate.setText(fields.contains("date") ? errors.get("date") : "");
		labelErrorObra.setText(fields.contains("obra") ? errors.get("obra") : "");
		labelErrorAddress.setText(fields.contains("address") ? errors.get("address") : "");
		labelErrorConcreteDesign.setText(fields.contains("concreteDesign") ? errors.get("concreteDesign") : "");
	}
	
	public void dataChangeListenerSubscribe (DataChangeListener dataChangeListener) {
		this.dataChangeListeners.add(dataChangeListener);
	}

}
