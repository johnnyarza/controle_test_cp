package gui;

import java.net.URL;
import java.time.Instant;
import java.util.Date;
import java.util.List;
import java.util.Map;
import java.util.ResourceBundle;
import java.util.Set;
import java.util.TimeZone;

import application.db.DbException;
import application.domaim.CompresionTest;
import application.domaim.CorpoDeProva;
import application.exceptions.ValidationException;
import application.service.CorpoDeProvaService;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.DatePicker;
import javafx.scene.control.Label;

public class CorpoDeProvaFilterFormController implements Initializable {

	private CorpoDeProvaService service;

	private ObservableList<CorpoDeProva> obsList;

	private CompresionTest compresionTest;

	private Boolean isCancelButtonPressed = false;

	@FXML
	private Button btFilter;

	@FXML
	private Button btClose;

	@FXML
	private DatePicker dpInitialDate;

	@FXML
	private DatePicker dpFinalDate;

	@FXML
	private Label labelErrorInitialDate;

	@FXML
	private Label labelErrorFinalDate;

	@FXML
	private void onBtFilterAction(ActionEvent event) {
		try {
			List<CorpoDeProva> list = getFormData();
			this.obsList = FXCollections.observableArrayList(list);
			Utils.currentStage(event).close();

		} catch (DbException e) {
			Alerts.showAlert("Error", "Error filtrando probetas", e.getMessage(), AlertType.ERROR);
		} catch (ValidationException e1) {
			setLabelErrorMessages(e1.getErrors());
		}
	}

	private void setLabelErrorMessages(Map<String, String> errors) {
		Set<String> errorNames = errors.keySet();

		labelErrorInitialDate.setText(errorNames.contains("initialDate") ? errors.get("initialDate") : "");
		labelErrorFinalDate.setText(errorNames.contains("finalDate") ? errors.get("finalDate") : "");

	}

	private List<CorpoDeProva> getFormData() {
		ValidationException exception = new ValidationException("Validation Exception");

		if (service == null) {
			throw new IllegalStateException("Service was null");
		}

		if (dpFinalDate.getValue() == null) {
			exception.addError("finalDate", "vacío");
		}
		if (dpInitialDate.getValue() == null) {
			exception.addError("initialDate", "vacío");
		}

		if (dpInitialDate.getValue() != null && dpFinalDate.getValue() != null) {
			Instant instantBefore = Utils.getInsTantFromDatePicker(dpInitialDate);
			Instant instantAfter = Utils.getInsTantFromDatePicker(dpFinalDate);

			if (instantBefore.compareTo(instantAfter) > 0) {
				exception.addError("initialDate", "Fecha posterior a rotura");
			}
			if (instantAfter.compareTo(instantBefore) < 0) {
				exception.addError("finalDate", "Fecha anterior a moldeo");
			}
		}

		if (exception.getErrors().size() > 0) {
			throw exception;
		}
		if (this.compresionTest.getId() == null) {
			throw new IllegalStateException("compresionTestId was null");
		}

		return service.findByDatesAndCompresionTestId(TimeZone.getDefault(),
				Date.from(Utils.getInsTantFromDatePicker(dpInitialDate)),
				Date.from(Utils.getInsTantFromDatePicker(dpFinalDate)), this.compresionTest.getId());
	}

	@FXML
	public void onBtCloseAction(ActionEvent event) {
		setIsCancelButtonPressed(true);
		Utils.currentStage(event).close();
	}

	@Override
	public void initialize(URL arg0, ResourceBundle arg1) {
		initializeNodes();
	}

	private void initializeNodes() {
		btFilter.getStyleClass().add("safe-button");
		btClose.getStyleClass().add("dangerous-button");
	}

	public Boolean getIsCancelButtonPressed() {
		return isCancelButtonPressed;
	}

	public void setIsCancelButtonPressed(Boolean isCancelButtonPressed) {
		this.isCancelButtonPressed = isCancelButtonPressed;
	}

	public CorpoDeProvaService getService() {
		return service;
	}

	public void setService(CorpoDeProvaService service) {
		this.service = service;
	}

	public ObservableList<CorpoDeProva> getObsList() {
		return obsList;
	}

	public void setObsList(ObservableList<CorpoDeProva> obsList) {
		this.obsList = obsList;
	}

	public CompresionTest getCompresionTest() {
		return compresionTest;
	}

	public void setCompresionTest(CompresionTest compresionTest) {
		this.compresionTest = compresionTest;
	}
}
