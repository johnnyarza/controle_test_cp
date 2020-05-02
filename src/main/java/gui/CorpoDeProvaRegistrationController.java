package gui;

import java.net.URL;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneId;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Map;
import java.util.ResourceBundle;
import java.util.Set;

import application.db.DbException;
import application.domaim.CorpoDeProva;
import application.exceptions.ValidationException;
import application.service.CorpoDeProvaService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Constraints;
import gui.util.Utils;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.DatePicker;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;

public class CorpoDeProvaRegistrationController implements Initializable {

	CorpoDeProvaService corpoDeProvaService;

	CorpoDeProva corpoDeProva;

	List<DataChangeListener> dataChangeListeners = new ArrayList<>();

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

	public void onBtSaveAction(ActionEvent event) {
		try {
			CorpoDeProva obj = getFormData();
			obj.setFckRupture();
			obj.setCompresionTest(this.corpoDeProva.getCompresionTest());

			corpoDeProvaService.saveOrUpdate(obj);
			notifyDataChangeListeners();

			Utils.currentStage(event).close();
		} catch (DbException e) {
			Alerts.showAlert("Error", "Error saving Probeta", e.getMessage(), AlertType.ERROR);
		} catch (ValidationException e1) {
			setErrorMessages(e1.getErrors());
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
		} else {
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

		} else {
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
			LocalDate i =LocalDate.ofInstant(instantBefore, ZoneId.systemDefault());
			LocalDate j =LocalDate.ofInstant(instantAfter, ZoneId.systemDefault());
			
			obj.setDays(Utils.daysBetweenLocalDates(j, i));
		}

		if (txtDiameter.getText() == null || txtDiameter.getText().trim().equals("")) {
			exception.addError("diameter", "Diametro vacío");
		}
		obj.setDiameter(Utils.tryParseToDouble(txtDiameter.getText()));

		if (txtHeight.getText() == null || txtDiameter.getText().trim().equals("")) {
			exception.addError("height", "Altura vacía");
		}
		obj.setHeight(Utils.tryParseToDouble(txtHeight.getText()));

		if (txtWeight.getText() == null || txtWeight.getText().trim().equals("")) {
			exception.addError("weight", "Peso vacío");
		}
		obj.setWeight(Utils.tryParseToDouble(txtWeight.getText()));

		if (txtRuptureTon.getText() == null || txtRuptureTon.getText().trim().equals("")) {
			obj.setTonRupture(0.0);
		} else {
			obj.setTonRupture(Utils.tryParseToDouble(txtRuptureTon.getText()));
		}

		if (exception.getErrors().size() > 0) {
			throw exception;
		}

		return obj;
	}

	private void setErrorMessages(Map<String, String> errors) {
		Set<String> fields = errors.keySet(); // Set é conjunto

		labelErrorCode.setText(fields.contains("code") ? errors.get("code") : "");
		labelErrorSlump.setText(fields.contains("slump") ? errors.get("slump") : "");
		labelErrorMoldeDate.setText(fields.contains("moldeDate") ? errors.get("moldeDate") : "");
		labelErrorRuptureDate.setText(fields.contains("ruptureDate") ? errors.get("ruptureDate") : "");
		labelErrorDiameter.setText(fields.contains("diameter") ? errors.get("diameter") : "");
		labelErrorHeight.setText(fields.contains("height") ? errors.get("height") : "");
		labelErrorWeight.setText(fields.contains("weight") ? errors.get("weight") : "");
		labelErrorRuptureTon.setText(fields.contains("ruptureTon") ? errors.get("ruptureTon") : "");

	}

	public void updateFormData() {

		if (corpoDeProva == null) {
			throw new NullPointerException();
		}

		txtId.setText(String.valueOf(corpoDeProva.getId()));
		txtCode.setText(String.valueOf(corpoDeProva.getCode()));
		txtSlump.setText(String.format("%.2f", corpoDeProva.getSlump()));

		if (corpoDeProva.getMoldeDate() != null) {
			dpMoldeDate.setValue(LocalDate.ofInstant(corpoDeProva.getMoldeDate().toInstant(), ZoneId.systemDefault()));
		}
		if (corpoDeProva.getRuptureDate() != null) {
			dpRuptureDate
					.setValue(LocalDate.ofInstant(corpoDeProva.getRuptureDate().toInstant(), ZoneId.systemDefault()));
		}

		txtDiameter.setText(String.format("%.2f", corpoDeProva.getDiameter()));
		txtHeight.setText(String.format("%.2f", corpoDeProva.getHeight()));
		txtWeight.setText(String.format("%.3f", (corpoDeProva.getWeight())));
		txtRuptureTon.setText(String.format("%.2f", corpoDeProva.getTonRupture()));
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

	public void initializeNodes() {
		Utils.formatDatePicker(dpMoldeDate, "dd/MM/yyyy");
		Utils.formatDatePicker(dpRuptureDate, "dd/MM/yyyy");
		Constraints.setTextFieldInteger(txtId);
		Constraints.setTextFieldDouble(txtSlump);
		Constraints.setTextFieldDouble(txtDiameter);
		Constraints.setTextFieldDouble(txtHeight);
		Constraints.setTextFieldDouble(txtWeight);
		Constraints.setTextFieldDouble(txtRuptureTon);
	}
}
