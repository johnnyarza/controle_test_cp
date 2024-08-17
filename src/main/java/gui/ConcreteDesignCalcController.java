package gui;

import  javafx.scene.control.Button;
import java.net.URL;
import java.util.HashMap;
import java.util.Map;
import java.util.ResourceBundle;
import java.util.Set;

import application.domaim.Material;
import application.exceptions.ValidationException;
import gui.util.Constraints;
import gui.util.Utils;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;

public class ConcreteDesignCalcController implements Initializable{
	
	private ObservableList<Material> obsListMaterial;
	
	private Material target;
	
	private Material origin;
	
	private Double percentage;
	
	
	@FXML
	private ComboBox<Material> comboBoxOrigin;
	
	@FXML
	private ComboBox<Material> comboBoxTarget;
	
	@FXML
	private TextField txtPercentage;
	
	@FXML
	private Button btCalc;
	
	@FXML
	private Button btClose;
	
	@FXML
	private Label labelOriginError;
	
	@FXML
	private Label labelTargetError;
	
	@FXML
	private Label labelPercentageError;
	
	@FXML
	private void onBtCalcAction (ActionEvent event) {
		try {
			setErrorMessages(new HashMap<String, String>());
			getFormData ();
			Utils.currentStage(event).close();
		} catch (ValidationException e) {
			setErrorMessages(e.getErrors());
		}		
	}
	
	@FXML
	private void onBtCloseAction (ActionEvent event) {
		setOrigin(null);
		setTarget(null);
		setPercentage(null);
		Utils.currentStage(event).close();
	}
	
	public void setObjectsToNull() {
		setOrigin(null);
		setTarget(null);
		setPercentage(null);
	}
	
	private void setErrorMessages(Map<String,String> errors) {
		Set<String> key = errors.keySet();
		
		labelOriginError.setText(key.contains("origin") ? errors.get("origin") : "");
		labelTargetError.setText(key.contains("target") ? errors.get("target") : "");
		labelPercentageError.setText(key.contains("percentage") ? errors.get("percentage") : "");
		
	}
	private void getFormData () {
		ValidationException e = new ValidationException("Validation exception");
		if (comboBoxOrigin.getValue() == null) {
			e.addError("origin", "vacío");
		}
		if (comboBoxTarget.getValue() == null) {
			e.addError("target", "vacío");
		}
		if (txtPercentage.getText()==null || txtPercentage.getText().trim().equals("")) {
			e.addError("percentage", "vacío");
		}
		
		if (e.getErrors().size() > 0) {
			throw e;
		}
		
		origin = comboBoxOrigin.getValue();
		target = comboBoxTarget.getValue();
		percentage = Utils.tryParseToDouble(txtPercentage.getText());
	}
	
	public void updateFormData () {
		loadAssociateObjects ();
	}
	
	public void loadAssociateObjects () {
		try {
			if (obsListMaterial.size() == 0) {
				throw new IllegalStateException("Lista de materiales vacía");
			}
			comboBoxOrigin.setItems(obsListMaterial);
			comboBoxTarget.setItems(obsListMaterial);
		} catch (IllegalStateException e) {
			throw e;
		}
		
	}

	@Override
	public void initialize(URL location, ResourceBundle resources) {
		Constraints.setTextFieldDouble(txtPercentage,1);
		
	}

	public void setObsListMaterial(ObservableList<Material> obsListMaterial) {
		this.obsListMaterial = obsListMaterial;
	}

	public Material getTarget() {
		return target;
	}

	public void setTarget(Material target) {
		this.target = target;
	}

	public Material getOrigin() {
		return origin;
	}

	public void setOrigin(Material origin) {
		this.origin = origin;
	}

	public Double getPercentage() {
		return percentage;
	}

	public void setPercentage(Double percentage) {
		this.percentage = percentage;
	}

}
