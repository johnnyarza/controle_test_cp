package gui;

import java.net.URL;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneId;
import java.util.ArrayList;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.NoSuchElementException;
import java.util.ResourceBundle;
import java.util.Set;
import java.util.logging.Level;

import org.apache.commons.lang3.StringUtils;

import application.db.DbException;
import application.domaim.CorpoDeProva;
import application.exceptions.ValidationException;
import application.log.LogUtils;
import application.service.CorpoDeProvaService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Constraints;
import gui.util.Utils;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.Node;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.DatePicker;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;
import javafx.scene.control.TextInputDialog;
import javafx.scene.image.Image;
import javafx.scene.layout.GridPane;
import javafx.stage.Stage;

public class CorpoDeProvaRegistrationController implements Initializable {

	CorpoDeProvaService corpoDeProvaService;

	CorpoDeProva corpoDeProva;

	Boolean isLocked = false;

	List<DataChangeListener> dataChangeListeners = new ArrayList<>();

	private LogUtils logger;

	@FXML
	private Label labelErrorCode;

	@FXML
	private Label labelErrorSlump;

	@FXML
	private Label labelErrorMoldeDate;

	@FXML
	private Label labelErrorRuptureDate;

	@FXML
	private Label labelErrorHeight;

	@FXML
	private Label labelErrorDiameter;

	@FXML
	private Label labelErrorWeight;

	@FXML
	private Label labelErrorRuptureTon;

	@FXML
	private TextField txtId;

	@FXML
	private TextField txtCode;

	@FXML
	private TextField txtSlump;

	@FXML
	private TextField txtDiameter;

	@FXML
	private TextField txtHeight;

	@FXML
	private TextField txtWeight;

	@FXML
	private TextField txtRuptureTon;

	@FXML
	private DatePicker dpMoldeDate;

	@FXML
	private DatePicker dpRuptureDate;

	@FXML
	private Button btSave;

	@FXML
	private Button btCancel;

	@FXML
	private Button btToday;

	@FXML
	private Button btDateCalc;

	@FXML
	private Button btDiameterCalc;

	@FXML
	private Button btHeightCalc;

	@FXML
	private GridPane gridPane;

	@FXML
	private void onBtTodayAction(ActionEvent event) {
		try {
			dpMoldeDate.setValue(LocalDate.now());
		} catch (Exception e) {
			Alerts.showAlert("Error", "Error saving Probeta", e.getMessage(), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtAvgCalc(ActionEvent event) {
		Double avg = calcAvg();
		if (avg == null) {
			return;
		}

		if (btDiameterCalc.getId().equals(((Node) (event.getSource())).getId())) {
			txtDiameter.setText(String.format("%.2f", avg));
		}
		if (btHeightCalc.getId().equals(((Node) (event.getSource())).getId())) {
			txtHeight.setText(String.format("%.1f", avg));
		}

	}

	private Double calcAvg() {

		TextInputDialog dlg = new TextInputDialog();
		List<Double> lst = new ArrayList<Double>();
		Double avg = 0.0;

		dlg.setTitle("Promedio");
		Stage stage = (Stage) dlg.getDialogPane().getScene().getWindow();
		stage.getIcons().add(new Image(CorpoDeProva.class.getResourceAsStream("/images/calculator.png")));

		Constraints.setTextFieldDouble(dlg.getEditor(), 2);

		for (int i = 0; i <= 1; i++) {
			dlg.setHeaderText("Insertar numero " + (i + 1));
			String response = dlg.showAndWait().orElse("");

			if (Utils.tryParseToDouble(response) == null) {
				Alerts.showAlert("Error", "Hay valores no numericos", null, AlertType.ERROR);
				return null;
			}

			lst.add(Utils.tryParseToDouble(response));
			dlg.getEditor().setText("");
		}
		for (Double n : lst) {
			avg += n;
		}
		return (avg / lst.size());
	}

	@FXML
	private void onBtDateCalcAction(ActionEvent event) {
		try {

			String response = Utils.getStringWithDialog("Sumar días", "Insertar días para sumar \na la fecha de moldeo",
					new Image(CorpoDeProva.class.getResourceAsStream("/images/calculator.png")));

			if (StringUtils.isNumeric(response) && dpMoldeDate.getValue() != null) {
				dpRuptureDate.setValue(dpMoldeDate.getValue().plusDays(Utils.tryParseToInt(response)));
			}
		} catch (NoSuchElementException e) {
			Alerts.showAlert("Error", "Campo vacío o valor no numerico", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}

	}

	public void onBtSaveAction(ActionEvent event) {
		try {
			setErrorMessages(new HashMap<String, String>());
			CorpoDeProva obj = getFormData();
			obj.setFckRupture();
			obj.setCompresionTest(this.corpoDeProva.getCompresionTest());
			obj.setIsLocked(true);

			corpoDeProvaService.saveOrUpdate(obj);
			notifyDataChangeListeners();

			Utils.currentStage(event).close();
		} catch (DbException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "DbException", e.getMessage(), AlertType.ERROR);
		} catch (ValidationException e1) {
			setErrorMessages(e1.getErrors());
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error desconocído", e.getMessage(), AlertType.ERROR);
		}
	}

	public void onBtCancelAction(ActionEvent event) {
		Utils.currentStage(event).close();
	}

	public CorpoDeProvaService getCorpoDeProvaService() {
		return corpoDeProvaService;
	}

	public void setCorpoDeProvaService(CorpoDeProvaService corpoDeProvaService) {
		this.corpoDeProvaService = corpoDeProvaService;
	}

	public CorpoDeProva getCorpoDeProva() {
		return corpoDeProva;
	}

	public void setCorpoDeProva(CorpoDeProva corpoDeProva) {
		this.corpoDeProva = corpoDeProva;
	}

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}

	private CorpoDeProva getFormData() {

		CorpoDeProva obj = new CorpoDeProva();

		ValidationException exception = new ValidationException("Valdiation Error");

		obj.setId(Utils.tryParseToInt(txtId.getText()));

		if (txtCode.getText() == null || txtCode.getText().trim().equals("")) {
			exception.addError("code", "codigo vacío");
		}
		obj.setCode(txtCode.getText());

		if (txtSlump.getText() == null || txtSlump.getText().trim().equals("")) {
			exception.addError("slump", "Slump vacío");
		}
		obj.setSlump(Utils.tryParseToDouble(txtSlump.getText()));

		if (dpMoldeDate.getValue() == null) {
			exception.addError("moldeDate", "Fecha vacía");
		} else if (dpRuptureDate.getValue() != null) {
			Instant instantBefore = Utils.getInsTantFromDatePicker(dpMoldeDate);
			Instant instantAfter = Utils.getInsTantFromDatePicker(dpRuptureDate);
			if (instantBefore.compareTo(instantAfter) > 0) {
				exception.addError("moldeDate", "Fecha posterior a rotura");
			} else {
				obj.setMoldeDate(Date.from(instantBefore));
			}
		}

		if (dpRuptureDate.getValue() == null) {
			exception.addError("ruptureDate", "Fecha vacía");

		} else if (dpMoldeDate.getValue() != null) {
			Instant instantBefore = Utils.getInsTantFromDatePicker(dpMoldeDate);
			Instant instantAfter = Utils.getInsTantFromDatePicker(dpRuptureDate);
			if (instantAfter.compareTo(instantBefore) < 0) {
				exception.addError("ruptureDate", "Fecha anterior a moldeo");
			} else {
				obj.setRuptureDate(Date.from(instantAfter));
			}
		}

		if (dpRuptureDate.getValue() != null && dpMoldeDate.getValue() != null) {
			Instant instantBefore = Utils.getInsTantFromDatePicker(dpMoldeDate);
			Instant instantNow = Instant.from(LocalDate.now().atStartOfDay(ZoneId.systemDefault()));
			if (instantBefore.compareTo(instantNow) > 0) {
				exception.addError("moldeDate", "Fecha posterior a hoy");
			}
		}

		if (dpRuptureDate.getValue() != null && dpMoldeDate.getValue() != null) {
			Instant instantBefore = Utils.getInsTantFromDatePicker(dpMoldeDate);
			Instant instantAfter = Utils.getInsTantFromDatePicker(dpRuptureDate);
			LocalDate i = LocalDate.ofInstant(instantBefore, ZoneId.systemDefault());
			LocalDate j = LocalDate.ofInstant(instantAfter, ZoneId.systemDefault());

			obj.setDays(Utils.daysBetweenLocalDates(j, i));
		}

		if (txtDiameter.getText() == null || txtDiameter.getText().trim().equals("")) {
			exception.addError("diameter", "Diametro vacío");
		}
		obj.setDiameter(Utils.tryParseToDouble(txtDiameter.getText()));

		if (txtHeight.getText() == null || txtHeight.getText().trim().equals("")) {
			exception.addError("height", "Altura vacía");
		}
		obj.setHeight(Utils.tryParseToDouble(txtHeight.getText()));

		if (txtWeight.getText() == null || txtWeight.getText().trim().equals("")) {
			exception.addError("weight", "Peso vacío");
		}
		obj.setWeight(Utils.tryParseToDouble(txtWeight.getText()));

		if (txtRuptureTon.getText() == null || txtRuptureTon.getText().trim().equals("")) {
			obj.setTonRupture(0.0);
			obj.setIsLocked(false);
		} else {
			obj.setTonRupture(Utils.tryParseToDouble(txtRuptureTon.getText()));
			obj.setIsLocked(true);
		}

		if (exception.getErrors().size() > 0) {
			throw exception;
		}

		return obj;
	}

	private void setErrorMessages(Map<String, String> errors) {
		Set<String> fields = errors.keySet(); // Set � conjunto

		labelErrorCode.setText(fields.contains("code") ? errors.get("code") : "");
		labelErrorSlump.setText(fields.contains("slump") ? errors.get("slump") : "");
		labelErrorMoldeDate.setText(fields.contains("moldeDate") ? errors.get("moldeDate") : "");
		labelErrorRuptureDate.setText(fields.contains("ruptureDate") ? errors.get("ruptureDate") : "");
		labelErrorDiameter.setText(fields.contains("diameter") ? errors.get("diameter") : "");
		labelErrorHeight.setText(fields.contains("height") ? errors.get("height") : "");
		labelErrorWeight.setText(fields.contains("weight") ? errors.get("weight") : "");
		labelErrorRuptureTon.setText(fields.contains("ruptureTon") ? errors.get("ruptureTon") : "");

	}

	public Boolean getIsLocked() {
		return isLocked;
	}

	public void setIsLocked(Boolean isLocked) {
		this.isLocked = isLocked;
	}

	public void updateFormData() {

		if (corpoDeProva == null) {
			throw new NullPointerException();
		}

		txtId.setText(String.valueOf(corpoDeProva.getId()));
		txtCode.setText(String.valueOf(corpoDeProva.getCode()));
		txtSlump.setText(String.format("%.1f", corpoDeProva.getSlump()));

		if (corpoDeProva.getMoldeDate() != null) {
			dpMoldeDate.setValue(LocalDate.ofInstant(corpoDeProva.getMoldeDate().toInstant(), ZoneId.systemDefault()));
		}
		if (corpoDeProva.getRuptureDate() != null) {
			dpRuptureDate.setValue(LocalDate.ofInstant(corpoDeProva.getRuptureDate().toInstant(), ZoneId.systemDefault()));
		}

		txtDiameter.setText(String.format("%.2f", corpoDeProva.getDiameter()));
		txtHeight.setText(String.format("%.1f", corpoDeProva.getHeight()));
		txtWeight.setText(String.format("%.3f", (corpoDeProva.getWeight())));
		txtRuptureTon.setText(String.format("%.1f", corpoDeProva.getTonRupture()));
	}

	public void subscribeDataChangeListener(DataChangeListener listener) {
		dataChangeListeners.add(listener);
	}

	private void notifyDataChangeListeners() {
		dataChangeListeners.forEach((DataChangeListener x) -> x.onDataChange());
	}

	@Override
	public void initialize(URL arg0, ResourceBundle arg1) {
		initializeNodes();
	}

	public void formLockedState() {
		if (corpoDeProva.getIsLocked()) {
			txtDiameter.setDisable(isLocked);
			txtHeight.setDisable(isLocked);
			txtCode.setDisable(isLocked);
			txtSlump.setDisable(isLocked);
			txtWeight.setDisable(isLocked);
			dpMoldeDate.setDisable(isLocked);
			dpRuptureDate.setDisable(isLocked);
			btToday.setDisable(isLocked);
			btDateCalc.setDisable(isLocked);
			btDiameterCalc.setDisable(isLocked);
			btHeightCalc.setDisable(isLocked);
		}
	}

	public void initializeNodes() {
		Utils.formatDatePicker(dpMoldeDate, "dd/MM/yyyy");
		Utils.formatDatePicker(dpRuptureDate, "dd/MM/yyyy");
		Constraints.setTextFieldInteger(txtId);
		Constraints.setTextFieldDouble(txtSlump, 1);
		Constraints.setTextFieldDouble(txtDiameter, 2);
		Constraints.setTextFieldDouble(txtHeight, 1);
		Constraints.setTextFieldDouble(txtWeight, 3);
		Constraints.setTextFieldDouble(txtRuptureTon, 2);
		btToday.setGraphic(Utils.createImageView("/images/pin.png", 20.0, 20.0));
		btDateCalc.setGraphic(Utils.createImageView("/images/calculator.png", 20.0, 20.0));
		btDiameterCalc.setGraphic(Utils.createImageView("/images/calculator.png", 20.0, 20.0));
		btHeightCalc.setGraphic(Utils.createImageView("/images/calculator.png", 20.0, 20.0));
	}
}
