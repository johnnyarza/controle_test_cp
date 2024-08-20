package gui;

import java.net.URL;
import java.time.Instant;
import java.time.LocalDate;
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
import javafx.scene.control.TextField;
import javafx.scene.paint.Color;

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
	private TextField textFieldRange;

	@FXML
	private Label labelErrorInitialDate;

	@FXML
	private Label labelErrorFinalDate;
	
	@FXML
	private Label labelErrorRange;

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
		} catch (Exception e) {
			Alerts.showAlert("Error", "Error filtrando probetas", e.getMessage(), AlertType.ERROR);
		}
	}

	private void setLabelErrorMessages(Map<String, String> errors) {
		Set<String> errorNames = errors.keySet();

		labelErrorInitialDate.setText(errorNames.contains("initialDate") ? errors.get("initialDate") : "");
		labelErrorFinalDate.setText(errorNames.contains("finalDate") ? errors.get("finalDate") : "");
		labelErrorRange.setText(errorNames.contains("range") ? errors.get("range") : "Ejemplo: 1-3\r\n"
				+ "1-3, 4-8,....");
		labelErrorRange.setTextFill(errorNames.contains("range") ? Color.RED : Color.BLACK);
	}

	private List<CorpoDeProva> getFormData() {
		ValidationException exception = new ValidationException("Validation Exception");
		LocalDate finalDate = dpFinalDate.getValue();
		LocalDate initialDate = dpInitialDate.getValue();
		String range = textFieldRange.getText();
		System.out.println(finalDate);
		System.out.println(initialDate);

		if (service == null) {
			throw new IllegalStateException("Service was null");
		}
		
		if (finalDate == null && initialDate == null && (range == null || range.trim().isEmpty())) {
			exception.addError("finalDate", "vacío");
			exception.addError("initialDate", "vacío");
			exception.addError("range", "vacío");
		}
		
		//Validating input dates
		if ((finalDate != null || initialDate != null)) {
			if (finalDate == null) {
				exception.addError("finalDate", "vacío");
				System.out.println("Range vacio, final date vacío");
			}
			if (initialDate == null) {
				exception.addError("initialDate", "vacío");
				System.out.println("Range vacio, initial date vacío");
			}			
		}
		
		//Validating input dates
		if (initialDate != null && finalDate != null) {
			Instant instantBefore = Utils.getInsTantFromDatePicker(dpInitialDate);
			Instant instantAfter = Utils.getInsTantFromDatePicker(dpFinalDate);

			if (instantBefore.compareTo(instantAfter) > 0) {
				exception.addError("initialDate", "Fecha posterior a rotura");
			}
			if (instantAfter.compareTo(instantBefore) < 0) {
				exception.addError("finalDate", "Fecha anterior a moldeo");
			}
		}
		
		//Validating input id
		if (range != null && !range.trim().isEmpty()) {
			Boolean matchFound = Utils.regexValidator("^[0-9]+(?:(?:\\s*,\\s*|-)[0-9]+)*$", range);
			
			if (!matchFound) {
				exception.addError("range", "Rango inválido");
			}
		};
		
		if (exception.getErrors().size() > 0) {
			throw exception;
		}
		if (this.compresionTest.getId() == null) {
			throw new IllegalStateException("compresionTestId was null");
		}
		
		//Get data from DB by id range only
		if((range != null && !range.trim().isEmpty()) && (initialDate == null && finalDate == null)) {
			System.out.println("just range");
			return service.findByDatesAndIdAndCompresionTestId(TimeZone.getDefault(), null, null, range, this.compresionTest.getId());
		}
		
		//Get data from DB by dates only
		if ((range == null || range.trim().isEmpty()) && (initialDate != null && finalDate != null)) {
			System.out.println("just dates");
			return service.findByDatesAndIdAndCompresionTestId(TimeZone.getDefault(),
					Date.from(Utils.getInsTantFromDatePicker(dpInitialDate)),
					Date.from(Utils.getInsTantFromDatePicker(dpFinalDate)),
					null,
					this.compresionTest.getId());
		}
		
		//filter by date and range		
		System.out.println("date and range");
		return service.findByDatesAndIdAndCompresionTestId(TimeZone.getDefault(),
				Date.from(Utils.getInsTantFromDatePicker(dpInitialDate)),
				Date.from(Utils.getInsTantFromDatePicker(dpFinalDate)),
				range,
				this.compresionTest.getId());
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
		labelErrorRange.setText("Ejemplo: 1-3\r\n"
				+ "1-3, 4-8,....");
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
