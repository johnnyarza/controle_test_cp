package gui;

import java.io.FileOutputStream;
import java.io.IOException;
import java.net.URL;

import org.apache.poi.ss.format.CellFormatType;
import org.apache.poi.ss.usermodel.Cell;
import org.apache.poi.ss.usermodel.CellStyle;
import org.apache.poi.ss.usermodel.CellType;
import org.apache.poi.ss.usermodel.CreationHelper;
import org.apache.poi.ss.usermodel.Row;
import org.apache.poi.ss.usermodel.Sheet;
import org.apache.poi.ss.usermodel.Workbook;
import org.apache.poi.xssf.usermodel.XSSFWorkbook;
import org.joda.time.DateTimeComparator;

import java.text.SimpleDateFormat;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.ResourceBundle;
import java.util.Set;
import java.util.TimeZone;
import java.util.function.Consumer;
import java.util.logging.Level;

import application.Report.ReportFactory;
import application.db.DbException;
import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.domaim.ConcreteDesign;
import application.domaim.CorpoDeProva;
import application.exceptions.ReportException;
import application.exceptions.SaveFileException;
import application.exceptions.ValidationException;
import application.log.LogUtils;
import application.service.ClientService;
import application.service.CompresionTestService;
import application.service.ConcreteDesignService;
import application.service.CorpoDeProvaService;
import application.service.UserService;
import enums.LogEnum;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.beans.value.ChangeListener;
import javafx.beans.value.ObservableValue;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
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
import javafx.scene.image.Image;
import javafx.scene.image.ImageView;
import javafx.scene.layout.VBox;
import javafx.stage.FileChooser;
import javafx.stage.Stage;
import javafx.util.Callback;

public class CompresionTestFormController implements Initializable, DataChangeListener {

	// TODO mudar as label escrita codigo para descricao

	private ObservableList<CorpoDeProva> obsList;

	private ObservableList<Cliente> obsListClient;

	private ObservableList<ConcreteDesign> obsListConcreteDesign;

	private CorpoDeProvaService corpoDeProvaService;

	private CompresionTestService compresionTestService;

	private ConcreteDesignService concreteDesignService;

	private ClientService clientService;

	private CompresionTest compresionTest;

	private Boolean isNewDoc = false;

	SimpleDateFormat sdf = new SimpleDateFormat("dd/MM/yyyy");

	private Integer changesCount;

	private LogUtils logger;

	private Boolean isLocked = true;

	Map<String, ImageView> imgViewMap = new HashMap<>();

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
	private Button btSearchClient;

	@FXML
	private Button btLock;

	@FXML
	private Button btCopy;

	@FXML
	private Button btExcel;
	
	@FXML
	private Button btSearchConcreteProvider;

	@FXML
	private VBox vbox;

	@FXML
	private ComboBox<Cliente> comboBoxClient;

	@FXML
	private ComboBox<Cliente> comboBoxConcreteProvider;

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
	private Label labelErrorConcreteProvider;

	@FXML
	private Label labelMessages;
	
	@FXML
	private void onBtExcelAction (ActionEvent event) {
		List<CorpoDeProva> list = getCorpoDeProvaListFromTable();
		String filePath = null;
		DateTimeFormatter dateFormatter = DateTimeFormatter.ofPattern("yyyy-MM-dd");
		String date = dateFormatter.format(compresionTest.getDate().toInstant()
				.atZone(ZoneId.systemDefault()).toLocalDate());

		try {
			String suggestedFileName = date + "-" + compresionTest.getId().toString() + "-" 
					+ compresionTest.getClient().getName() + "-" + compresionTest.getObra();

			if (list.isEmpty()) {
				throw new Exception("Lista vacía");
			}
		filePath = Utils.getExistingOrNewFilePath(suggestedFileName, "Excel (.xlsx)", "xlsx");
		
			writeExcelFile(list, filePath);
			Alerts.showAlert("Aviso", "Archivo guardado", "El archivo se ha guardado correctamente", 
					AlertType.INFORMATION);
		} catch (SaveFileException e) {
			Alerts.showAlert("Error", "Error al salvar", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			Alerts.showAlert("Error", e.getStackTrace().toString(), e.getMessage(), AlertType.ERROR);
		}
	}
	
	private void writeExcelFile(List<CorpoDeProva> list,String filePath) throws IOException {
		try (Workbook workbook = new XSSFWorkbook()){
			Sheet sheet = workbook.createSheet("Sheet1");
			//FORMAT CELL STYLE TO DATE
			 CreationHelper creationHelper = workbook.getCreationHelper();
		     CellStyle cellStyle = workbook.createCellStyle();
		     cellStyle.setDataFormat(creationHelper.createDataFormat().getFormat("dd-MM-yyyy"));

            // Create header row
            Row headerRow = sheet.createRow(0);
            Cell headerCell1 = headerRow.createCell(0);
            headerCell1.setCellValue("Probeta iD");
            Cell headerCell2 = headerRow.createCell(1);
            headerCell2.setCellValue("Descripción");
            Cell headerCell3 = headerRow.createCell(2);
            headerCell3.setCellValue("Slump");         
            Cell headerCell4 = headerRow.createCell(3);
            headerCell4.setCellValue("Fecha de moldeo");         
            Cell headerCell5 = headerRow.createCell(4);
            headerCell5.setCellValue("Fecha de rotura");            
            Cell headerCell6 = headerRow.createCell(5);
            headerCell6.setCellValue("Edad");
            Cell headerCell7 = headerRow.createCell(6);
            headerCell7.setCellValue("DIA (cm)");
            Cell headerCell8 = headerRow.createCell(7);
            headerCell8.setCellValue("Altura (cm)");           
            Cell headerCell9 = headerRow.createCell(8);
            headerCell9.setCellValue("Peso (kg)");
            Cell headerCell10 = headerRow.createCell(9);
            headerCell10.setCellValue("Densidad (kg/m3)");
            Cell headerCell11 = headerRow.createCell(10);
            headerCell11.setCellValue("Rotura (ton)");
            Cell headerCell12 = headerRow.createCell(11);
            headerCell12.setCellValue("Resistencia (kg/cm2)");
            
            // Populate data rows
            int rowNum = 1;
            for (CorpoDeProva corpoDeProva : list) {
                Row row = sheet.createRow(rowNum++);
                row.createCell(0).setCellValue(corpoDeProva.getId());
                row.createCell(1).setCellValue(corpoDeProva.getCode());
                row.createCell(2).setCellValue(corpoDeProva.getSlump());
                
                Cell moldeDateCell = row.createCell(3);
                moldeDateCell.setCellStyle(cellStyle);
                moldeDateCell.setCellValue(corpoDeProva.getMoldeDate());
                
                Cell ruptureDate = row.createCell(4);
                ruptureDate.setCellStyle(cellStyle);
                ruptureDate.setCellValue(corpoDeProva.getRuptureDate());
                
                
                row.createCell(5).setCellValue(corpoDeProva.getDays());
                row.createCell(6).setCellValue(corpoDeProva.getDiameter());
                row.createCell(7).setCellValue(corpoDeProva.getHeight());
                row.createCell(8).setCellValue(corpoDeProva.getWeight());
                row.createCell(9).setCellValue(corpoDeProva.getDensid());
                row.createCell(10).setCellValue(corpoDeProva.getTonRupture());
                row.createCell(11).setCellValue(corpoDeProva.getFckRupture());
                
            } 
            //Adjust cols width
            for (int colIndex = 0; colIndex <= 11; colIndex++) {
            	sheet.setColumnWidth(colIndex, 15 * 256);
            }
            // Write the output to a file
            try (FileOutputStream fileOut = new FileOutputStream(filePath)) {
                workbook.write(fileOut);
            }
		}
	}

	@FXML
	private void onBtCopyAction(ActionEvent event) {
		try {
			CorpoDeProva obj = getCorpoDeProvaView();
			Stage parentStage = Utils.currentStage(event);
			obj.setId(null);
			obj.setCompresionTest(compresionTest);

			createDialogForm("/gui/CorpoDeProvaRegistrationForm.fxml", "Copiar Probeta", parentStage,
					(CorpoDeProvaRegistrationController controller) -> {
						controller.setCorpoDeProvaService(new CorpoDeProvaService());
						controller.setLogger(logger);
						controller.setCorpoDeProva(obj);
						controller.updateFormData();
						controller.setIsLocked(isLocked);
						controller.subscribeDataChangeListener(this);
					}, (CorpoDeProvaRegistrationController controller) -> {
					}, (CorpoDeProvaRegistrationController controller) -> {
					}, "/gui/CompresionTestForm.css",
					new Image(CompresionTestFormController.class.getResourceAsStream("/images/fileIcons/new_file.png")));
			

		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtLockAction(ActionEvent event) {
		Stage parentStage = Utils.currentStage(event);
		try {
			if (isLocked) {
				createDialogForm("/gui/LoginForm.fxml", "Login", parentStage, (LoginFormController controller) -> {
					controller.setUserService(new UserService());
					controller.setEntity(null);
					controller.setIsLoggin(LogEnum.SIGNIN);
					controller.setLogger(logger);
					controller.setTitleLabel("Entrar con datos del administrador");
				}, (LoginFormController controller) -> {
					controller.setEntity(null);
				}, (LoginFormController controller) -> {
					isLocked = controller.getEntity() == null ? true : false;
				}, "", new Image(CompresionTestFormController.class.getResourceAsStream("/images/sign_in.png")));
			} else {
				isLocked = true;
			}

			setLockButtonStyle();
			setNodesDisable();

		} catch (Exception e) {
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	private void onBtSearchClientAction(ActionEvent event) {
		Stage parentStage = Utils.currentStage(event);
		createDialogForm("/gui/FindClientForm.fxml", "Busca Cliente", parentStage,
				(FindClientFormController controller) -> {
					controller.setEntity(null);
					controller.setService(new ClientService());
				}, (FindClientFormController controller) -> {
					controller.setEntity(null);
				}, (FindClientFormController controller) -> {
					if (controller.getEntity() != null) {
						comboBoxClient.setValue(controller.getEntity());
					}
				}, "", new Image(CompresionTestFormController.class.getResourceAsStream("/images/lupa.png")));
	}

	@FXML
	public void onBtSearchConcreteProviderAction(ActionEvent event) {
		Stage parentStage = Utils.currentStage(event);
		createDialogForm("/gui/FindClientForm.fxml", "Busca Cliente", parentStage,
				(FindClientFormController controller) -> {
					controller.setEntity(null);
					controller.setService(new ClientService());
				}, (FindClientFormController controller) -> {
					controller.setEntity(null);
				}, (FindClientFormController controller) -> {
					if (controller.getEntity() != null) {
						comboBoxConcreteProvider.setValue(controller.getEntity());
					}
				}, "", new Image(CompresionTestFormController.class.getResourceAsStream("/images/lupa.png")));
	}

	@FXML
	private void onBtInserirProbetaAction(ActionEvent event) {
		try {
			Stage parentStage = Utils.currentStage(event);
			CorpoDeProva obj = new CorpoDeProva();
			obj.setCompresionTest(compresionTest);

			createDialogForm("/gui/CorpoDeProvaRegistrationForm.fxml", "Inserir Probeta", parentStage,
					(CorpoDeProvaRegistrationController controller) -> {
						controller.setCorpoDeProvaService(new CorpoDeProvaService());
						controller.setLogger(logger);
						controller.setCorpoDeProva(obj);
						controller.setIsLocked(isLocked);
						controller.subscribeDataChangeListener(this);
					}, (CorpoDeProvaRegistrationController controller) -> {
					}, (CorpoDeProvaRegistrationController controller) -> {
					}, "/gui/CompresionTestForm.css",
					new Image(CompresionTestFormController.class.getResourceAsStream("/images/fileIcons/new_file.png")));
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtEditarProbetaAction(ActionEvent event) {
		try {
			Stage parentStage = Utils.currentStage(event);
			CorpoDeProva obj = getCorpoDeProvaView();
			
			if (isLocked && (obj.getTonRupture() != null && obj.getTonRupture() != 0.0)) {
				throw new IllegalAccessException("La alteraciín de este documento esta bloqueada");
			}
			
			if (isLocked && (DateTimeComparator.getDateOnlyInstance().compare(obj.getRuptureDate(), new Date()) < 0)) 
			{
				throw new IllegalAccessException("Probeta Retrasada. El administrador debe desbloquear el documento!");
			}

			createDialogForm("/gui/CorpoDeProvaRegistrationForm.fxml", "Editar Probeta", parentStage,
					(CorpoDeProvaRegistrationController controller) -> {
						controller.setCorpoDeProvaService(new CorpoDeProvaService());
						controller.setLogger(logger);
						controller.setCorpoDeProva(obj);
						controller.updateFormData();
						controller.setIsLocked(isLocked);
						controller.formLockedState();
						controller.subscribeDataChangeListener(this);
					}, (CorpoDeProvaRegistrationController controller) -> {
					}, (CorpoDeProvaRegistrationController controller) -> {
					}, "/gui/CompresionTestForm.css",
					new Image(CompresionTestFormController.class.getResourceAsStream("/images/fileIcons/edit_file.png")));
		} catch (NullPointerException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		} catch (IllegalAccessException e) {
			Alerts.showAlert("Error", "Acceso denegado", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtApagarProbetaAction() {
		try {
			Optional<ButtonType> result = Alerts.showConfirmationDialog("Confirmación de accion",
					"Seguro que desea apagar probeta?", "Después de apagados los datos seran perdidos");

			if (result.get() == ButtonType.OK) {
				CorpoDeProva obj = getCorpoDeProvaView();
				corpoDeProvaService.deleteById(obj.getId());
				onDataChange();
			}
		} catch (DbException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error deleting probeta", e.getMessage(), AlertType.ERROR);
		} catch (NullPointerException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "NullPointerException", e1.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	private void onBtEditarCadastroAction() {
		try {
			if (isLocked) {
				throw new IllegalAccessException("La alteracién de este documento esta bloqueada");
			}
			setErrorMessages(new HashMap<String, String>());
			setCompresionTestFormData();
			compresionTestService.saveOrUpdate(this.compresionTest);
			onDataChange();
			setChangesCount(0);
			Alerts.showAlert("Acción conluida", "Cadastro actualizado", null, AlertType.INFORMATION);
		} catch (ValidationException e) {
			setErrorMessages(e.getErrors());
		} catch (DbException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "Error al salvar el ensayo", e1.getMessage(), AlertType.ERROR);
		} catch (IllegalAccessException e) {
			Alerts.showAlert("Error", "Acceso denegado", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);

		}
	}

	@FXML
	private void onBtCloseAction(ActionEvent event) {
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
	private void onbtPrintAction() {
		try {
			ReportFactory rF = new ReportFactory();
			List<CorpoDeProva> list = getCorpoDeProvaListFromTable();
			rF.compresionTestReportView(list, this.compresionTest);
		} catch (ReportException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error al abrir el reporte", e.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	private void onBtFilterAction(ActionEvent event) {
		try {
			Stage parentStage = Utils.currentStage(event);

			createDialogForm("/gui/CorpoDeProvaFilterForm.fxml", "Filtrar Probetas por Fecha", parentStage,
					(CorpoDeProvaFilterFormController controller) -> {
						controller.setService(new CorpoDeProvaService());
						controller.setCompresionTest(this.compresionTest);
					}, (CorpoDeProvaFilterFormController controller) -> {
						controller.setIsCancelButtonPressed(true);
					}, (CorpoDeProvaFilterFormController controller) -> {
						if (!controller.getIsCancelButtonPressed()) {
							obsList = controller.getObsList();
						}
					}, "/gui/GlobalStyle.css",
					new Image(CompresionTestFormController.class.getResourceAsStream("/images/filter_on.png")));
			tableViewCorpoDeProva.setItems(this.obsList);
			tableViewCorpoDeProva.refresh();
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
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

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}

	private void setLockButtonStyle() {
		if (!isLocked) {
			btLock.setGraphic(imgViewMap.get("btUnlock"));
			btLock.getStyleClass().clear();
			btLock.getStyleClass().add("button");
			btLock.getStyleClass().add("unlocked-button");
			return;
		}

		btLock.setGraphic(imgViewMap.get("btLock"));
		btLock.getStyleClass().clear();
		btLock.getStyleClass().add("button");
		btLock.getStyleClass().add("locked-button");

	}

	private void setNodesDisable() {
		comboBoxClient.setDisable(isLocked);
		comboBoxConcreteProvider.setDisable(isLocked);
		btSearchClient.setDisable(isLocked);
		btSearchConcreteProvider.setDisable(isLocked);
		txtObra.setDisable(isLocked);
		txtAddress.setDisable(isLocked);
		dpCreationDate.setDisable(isLocked);
		comboBoxConcreteDesign.setDisable(isLocked);
		btApagarProbeta.setDisable(isLocked);
		btInserirProbeta.setDisable(isLocked);
		btEditarCadastro.setDisable(isLocked);
		btCopy.setDisable(isLocked);
	}

	@Override
	public void initialize(URL arg0, ResourceBundle arg1) {
		initializeNodes();
	}

	private void initializeNodes() {
		formatTableView();
		initializeComboBoxClient();
		tableViewCorpoDeProva.prefHeightProperty().bind(vbox.heightProperty());
		btLock.getStyleClass().add("button");
		btLock.getStyleClass().add("locked-button");
		setButtonsGraphics();
	}

	private void formatTableView() {
		setTableColumnsCellValueFactory();
		tableColumnCodigo.getStyleClass().add("description-column-style");
	}

	private void setButtonsGraphics() {
		imgViewMap.put("btLock", Utils.createImageView("/images/lock.png", 20.0, 20.0));
		imgViewMap.put("btUnlock", Utils.createImageView("/images/unlock.png", 20.0, 20.0));
		btLock.setGraphic(imgViewMap.get("btLock"));

		Utils.setButtonGraphic("/images/print.png", btPrint, 20.0, 20.0);
		Utils.setButtonGraphic("/images/lupa.png", btSearchClient, 15.0, 15.0);
		Utils.setButtonGraphic("/images/lupa.png", btSearchConcreteProvider, 15.0, 15.0);
		
		Utils.setButtonGraphic("/images/fileIcons/copy.png", btCopy, 20.0, 20.0);
		Utils.setButtonGraphic("/images/filter_off.png", btClearFilter, 20.0, 20.0);
		Utils.setButtonGraphic("/images/filter_on.png", btFilter, 20.0, 20.0);		
		Utils.setButtonGraphic("/images/excel.png", btExcel, 20.0, 20.0);	
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

		if (compresionTest.getConcreteProvider() == null) {
			comboBoxConcreteProvider.getSelectionModel().selectFirst();
		} else {
			comboBoxConcreteProvider.setValue(compresionTest.getConcreteProvider());
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
		comboBoxConcreteProvider.setCellFactory(factory);
		comboBoxConcreteProvider.setButtonCell(factory.call(null));
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
		comboBoxConcreteProvider.setItems(obsListClient);
	}

	private void loadCompresionDesignComboBox() {
		if (concreteDesignService == null) {
			throw new IllegalStateException("concreteDesignService was null");
		}
		List<ConcreteDesign> list = concreteDesignService.findAllConcreteDesign();
		obsListConcreteDesign = FXCollections.observableArrayList(list);
		comboBoxConcreteDesign.setItems(obsListConcreteDesign);
	}

	private <T> void createDialogForm(String absoluteName, String title, Stage parentStage,
			Consumer<T> initializingAction, Consumer<T> windowEventAction, Consumer<T> finalAction, String css, Image icon) {
		Utils.createDialogForm(absoluteName, title, parentStage, initializingAction, windowEventAction, finalAction, css,
				icon, logger);
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

	public void setFormaLockedState() {
		btApagarProbeta.setDisable(!isNewDoc);
		btInserirProbeta.setDisable(!isNewDoc);
		btEditarCadastro.setDisable(!isNewDoc);
		btCopy.setDisable(!isNewDoc);
	}

	public Boolean getIsLocked() {
		return isLocked;
	}

	public void setIsLocked(Boolean isLocked) {
		this.isLocked = isLocked;
	}

	public Boolean getIsNewDoc() {
		return isNewDoc;
	}

	public void setIsNewDoc(Boolean isNewDoc) {
		this.isNewDoc = isNewDoc;
	}

	private void setCompresionTestFormData() {

		ValidationException exception = new ValidationException("getCompresionTestFormData Error");

		if (comboBoxClient.getValue() == null) {
			exception.addError("client", "vacío");
		}

		if (comboBoxConcreteProvider.getValue() == null) {
			exception.addError("provider", "vacío");
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

		this.compresionTest.setConcreteProvider(comboBoxConcreteProvider.getValue());

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

	public void dataChangeListenerSubscribe(DataChangeListener dataChangeListener) {
		this.dataChangeListeners.add(dataChangeListener);
	}
	
}
