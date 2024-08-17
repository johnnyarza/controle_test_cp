package gui;

import java.net.URL;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.ResourceBundle;
import java.util.Set;
import java.util.logging.Level;

import application.db.DbException;
import application.domaim.ConcreteDesign;
import application.domaim.Material;
import application.domaim.MaterialProporcion;
import application.exceptions.ValidationException;
import application.log.LogUtils;
import application.service.ConcreteDesignService;
import application.service.MaterialService;
import gui.listeners.DataChangeListener;
import gui.util.Alerts;
import gui.util.Constraints;
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;
import javafx.scene.image.Image;
import javafx.stage.Stage;

public class ConcreteDesignRegistrationFormController implements Initializable {

	private ConcreteDesignService service;

	private ConcreteDesign entity;

	private MaterialService materialService;

	private Material target;

	private Material origin;

	private Double percentage;

	private TextField[] vecTextFieldQtt = new TextField[8];

	private Map<Integer, ComboBox<Material>> mapComboBoxMat = new HashMap<>();

	private ObservableList<Material> obsListMaterial;

	private List<DataChangeListener> dataChangeListeners = new ArrayList<>();

	private LogUtils logger;

	@FXML
	private TextField txtGeneric;

	@FXML
	private TextField txtId;

	@FXML
	private TextField txtName;

	@FXML
	private TextField txtFck;

	@FXML
	private TextField txtSlump;

	@FXML
	private TextField txtQtt1;

	@FXML
	private TextField txtQtt2;

	@FXML
	private TextField txtQtt3;

	@FXML
	private TextField txtQtt4;

	@FXML
	private TextField txtQtt5;

	@FXML
	private TextField txtQtt6;

	@FXML
	private TextField txtQtt7;

	@FXML
	private TextField txtQtt8;

	@FXML
	private ComboBox<Material> comboBoxMat1;

	@FXML
	private ComboBox<Material> comboBoxMat2;

	@FXML
	private ComboBox<Material> comboBoxMat3;

	@FXML
	private ComboBox<Material> comboBoxMat4;

	@FXML
	private ComboBox<Material> comboBoxMat5;

	@FXML
	private ComboBox<Material> comboBoxMat6;

	@FXML
	private ComboBox<Material> comboBoxMat7;

	@FXML
	private ComboBox<Material> comboBoxMat8;

	@FXML
	private Label labelErrorName;

	@FXML
	private Label labelErrorFck;

	@FXML
	private Button btSave;

	@FXML
	private Button btCancel;

	@FXML
	private Button btCalc;

	@FXML
	private void onBtSaveAction(ActionEvent event) {
		try {
			ConcreteDesign obj = getFormData();
			if (obj == null) {
				throw new IllegalStateException("ConcreteDesign was null");
			} else {
				service.saveOrUpdate(obj);
				notifyDataChangeListeners();
				Utils.currentStage(event).close();
			}
		} catch (IllegalStateException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IllegalStateException", e.getMessage(), AlertType.ERROR);
		} catch (DbException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "IllegalStateException", e1.getMessage(), AlertType.ERROR);
		} catch (ValidationException e2) {
			logger.doLog(Level.WARNING, e2.getMessage(), e2);
			Alerts.showAlert("Error", "ValidationException", setErrorMessages(e2.getErrors()), AlertType.ERROR);
		}
	}

	@FXML
	private void onBtCancelAction(ActionEvent event) {
		Utils.currentStage(event).close();
	}

	private ObservableList<Material> getUsingMaterials() {
		ObservableList<Material> obsListUsingMaterials = FXCollections.observableArrayList();
		mapComboBoxMat.forEach((key, comboMat) -> {
			if (comboMat.getValue() != null) {
				obsListUsingMaterials.add(comboMat.getValue());
			}
		});
		return obsListUsingMaterials;
	}

	@FXML
	private void onBtCalcAction(ActionEvent event) {
		try {
			ObservableList<Material> obsListUsingMaterials = getUsingMaterials();
			if (obsListUsingMaterials.size() == 0) {
				Alerts.showAlert("Aviso", "A�n no existen materiales en uso", null, AlertType.INFORMATION);
				return;
			}

			Stage parentStage = Utils.currentStage(event);
			Utils.createDialogForm("/gui/ConcreteDesignCalc.fxml", "Calculadora", parentStage,
					(ConcreteDesignCalcController controller) -> {
						controller.setObsListMaterial(obsListUsingMaterials);
						controller.setOrigin(null);
						controller.setTarget(null);
						controller.updateFormData();
					}, (ConcreteDesignCalcController controller) -> {
						controller.setObjectsToNull();
					}, (ConcreteDesignCalcController controller) -> {
						target = controller.getTarget();
						origin = controller.getOrigin();
						percentage = controller.getPercentage();
					}, "", new Image(ConcreteDesignCalcController.class.getResourceAsStream("/images/calculator.png")),
					new LogUtils());

			setValueFromCalc();
		} catch (Exception e) {
			e.printStackTrace();
		}

	}

	private void setValueFromCalc() {
		if (target != null && origin != null && percentage != null) {
			Integer originIndex = null;
			Integer targetIndex = null;
			for (Integer key : mapComboBoxMat.keySet()) {

				if (mapComboBoxMat.get(key).getValue().getId() == origin.getId()) {
					originIndex = key;
				}
				if (mapComboBoxMat.get(key).getValue().getId() == target.getId()) {
					targetIndex = key;
				}

			}
			Double result = Utils.tryParseToDouble(vecTextFieldQtt[originIndex].getText()) * (percentage / 100);
			vecTextFieldQtt[targetIndex].setText(Utils.doubleFormat(result));

		}
	}

	public TextField[] getVecTextField() {
		return vecTextFieldQtt;
	}

	public void setVecTextField(TextField[] vecTextField) {
		this.vecTextFieldQtt = vecTextField;
	}

	public ConcreteDesignService getService() {
		return service;
	}

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}

	public void setService(ConcreteDesignService service) {
		this.service = service;
	}

	public MaterialService getMaterialService() {
		return materialService;
	}

	public void setMaterialService(MaterialService materialService) {
		this.materialService = materialService;
	}

	public ConcreteDesign getEntity() {
		return entity;
	}

	public void setEntity(ConcreteDesign entity) {
		this.entity = entity;
	}

	private ConcreteDesign getFormData() {

		ConcreteDesign obj = new ConcreteDesign();
		MaterialProporcion prop = instantiateMaterialProporcion();
		ValidationException exception = new ValidationException("Validation Exception");

		if (countEmptyComboBoxes() == 8) {
			exception.addError("general", "El disi�o debe contener por lo menos 1 material");
		}

		if (txtName.getText() == null || txtName.getText().trim().equals("")) {
			exception.addError("name", "Campo Nombre no puede ser vacío");
		}

		if (txtFck.getText() == null || txtFck.getText().trim().equals("")) {
			exception.addError("resistencia", "Campo resistencia no puede ser vacío");
		}

		if (txtSlump.getText() == null || txtSlump.getText().trim().equals("")) {
			exception.addError("slump", "Campo slump no puede ser vacío");
		}

		if (this.materialService == null) {
			throw new IllegalStateException("ConcreteDesignRegistrationFormController: materialService was null");
		}

		if (countRepeatedMaterialInComboBoxes() > 1) {
			exception.addError("equal", "Hay materiales repetidos");
		}

		if (exception.getErrors().size() > 0) {
			throw exception;
		}
		prop.setMaterialService(this.materialService);
		obj.setId(Utils.tryParseToInt(txtId.getText()));
		obj.setFck(Utils.tryParseToDouble(txtFck.getText()));
		obj.setSlump(Utils.tryParseToDouble(txtSlump.getText()));
		obj.setDescription(txtName.getText());
		obj.setProporcion(prop);

		return obj;
	}

	private Integer countEmptyComboBoxes() {
		Integer matEmptyCount = 0;

		for (int i = 0; i <= 7; i = i + 1) {
			if (vecTextFieldQtt[i].getText() == null || vecTextFieldQtt[i].getText().trim().equals("")) {
				matEmptyCount += 1;
			}
		}
		return matEmptyCount;
	}

	private Integer countRepeatedMaterialInComboBoxes() {
		int repetead = 0;
		for (int i = 0; i <= 7; i += 1) {
			repetead = 0;
			if (mapComboBoxMat.get(i).getValue() != null) {
				for (int j = 0; j <= 7; j += 1) {
					if (mapComboBoxMat.get(j).getValue() != null
							&& mapComboBoxMat.get(j).getValue() == mapComboBoxMat.get(i).getValue()
							&& mapComboBoxMat.get(j).getValue().getId() != null) {
						repetead += 1;
					}
				}
			}
			if (repetead > 1) {

				break;
			}
		}
		return repetead;
	}

	private MaterialProporcion instantiateMaterialProporcion() {
		MaterialProporcion prop = new MaterialProporcion();

		int i = 0;
		prop.setMat1(mapComboBoxMat.get(i).getValue());
		prop.setMat1Qtt(Utils.tryParseToDouble(
				(vecTextFieldQtt[i].getText() == null || vecTextFieldQtt[i].getText().trim().equals("")) ? "0.0"
						: vecTextFieldQtt[i].getText()));
		i += 1;
		prop.setMat2(mapComboBoxMat.get(i).getValue());
		prop.setMat2Qtt(Utils.tryParseToDouble(
				(vecTextFieldQtt[i].getText() == null || vecTextFieldQtt[i].getText().trim().equals("")) ? "0.0"
						: vecTextFieldQtt[i].getText()));
		i += 1;
		prop.setMat3(mapComboBoxMat.get(i).getValue());
		prop.setMat3Qtt(Utils.tryParseToDouble(
				(vecTextFieldQtt[i].getText() == null || vecTextFieldQtt[i].getText().trim().equals("")) ? "0.0"
						: vecTextFieldQtt[i].getText()));
		i += 1;
		prop.setMat4(mapComboBoxMat.get(i).getValue());
		prop.setMat4Qtt(Utils.tryParseToDouble(
				(vecTextFieldQtt[i].getText() == null || vecTextFieldQtt[i].getText().trim().equals("")) ? "0.0"
						: vecTextFieldQtt[i].getText()));
		i += 1;
		prop.setMat5(mapComboBoxMat.get(i).getValue());
		prop.setMat5Qtt(Utils.tryParseToDouble(
				(vecTextFieldQtt[i].getText() == null || vecTextFieldQtt[i].getText().trim().equals("")) ? "0.0"
						: vecTextFieldQtt[i].getText()));
		i += 1;
		prop.setMat6(mapComboBoxMat.get(i).getValue());
		prop.setMat6Qtt(Utils.tryParseToDouble(
				(vecTextFieldQtt[i].getText() == null || vecTextFieldQtt[i].getText().trim().equals("")) ? "0.0"
						: vecTextFieldQtt[i].getText()));
		i += 1;
		prop.setMat7(mapComboBoxMat.get(i).getValue());
		prop.setMat7Qtt(Utils.tryParseToDouble(
				(vecTextFieldQtt[i].getText() == null || vecTextFieldQtt[i].getText().trim().equals("")) ? "0.0"
						: vecTextFieldQtt[i].getText()));

		i += 1;
		prop.setMat8(mapComboBoxMat.get(i).getValue());
		prop.setMat8Qtt(Utils.tryParseToDouble(
				(vecTextFieldQtt[i].getText() == null || vecTextFieldQtt[i].getText().trim().equals("")) ? "0.0"
						: vecTextFieldQtt[i].getText()));

		return prop;
	}

	private void initializeNodes() {
		Constraints.setTextFieldDouble(txtFck,0);
		Constraints.setTextFieldDouble(txtSlump,1);
		setVecTxtFieldsQtt();
		setComboBoxesMat();
		setTextFieldQttConstraints();
		btCalc.setGraphic(Utils.createImageView("/images/calculator.png", 20.0, 20.0));
	}

	private void setTextFieldQttConstraints() {
		for (TextField txt : vecTextFieldQtt) {
			Constraints.setTextFieldDouble(txt,2);
		}
	}

	private void setVecTxtFieldsQtt() {
		this.vecTextFieldQtt[0] = txtQtt1;
		this.vecTextFieldQtt[1] = txtQtt2;
		this.vecTextFieldQtt[2] = txtQtt3;
		this.vecTextFieldQtt[3] = txtQtt4;
		this.vecTextFieldQtt[4] = txtQtt5;
		this.vecTextFieldQtt[5] = txtQtt6;
		this.vecTextFieldQtt[6] = txtQtt7;
		this.vecTextFieldQtt[7] = txtQtt8;
	}

	private void setComboBoxesMat() {
		mapComboBoxMat.put(0, comboBoxMat1);
		mapComboBoxMat.put(1, comboBoxMat2);
		mapComboBoxMat.put(2, comboBoxMat3);
		mapComboBoxMat.put(3, comboBoxMat4);
		mapComboBoxMat.put(4, comboBoxMat5);
		mapComboBoxMat.put(5, comboBoxMat6);
		mapComboBoxMat.put(6, comboBoxMat7);
		mapComboBoxMat.put(7, comboBoxMat8);
	}

	public void loadAssociatedObjects() {
		List<Material> list = materialService.findAll();
		list.add(new Material());
		if (list.size() == 0) {
			throw new IllegalStateException("Lista de materiales vacía");
		}
		obsListMaterial = FXCollections.observableArrayList(list);
		for (int i = 0; i <= 7; i++) {
			mapComboBoxMat.get(i).setItems(obsListMaterial);
		}
	}

	public void updateFormData() {
		txtId.setText(entity.getId().toString());
		txtName.setText(entity.getDescription());
		txtFck.setText(String.format("%.0f", entity.getFck()));
		txtSlump.setText(String.format("%.1f", entity.getSlump()));
		Double[] qtt = materialProporcionQttToArray(entity.getProporcion());
		Material[] mat = materialProporcionMatToArray(entity.getProporcion());
		for (int i = 0; i <= 7; i += 1) {
			vecTextFieldQtt[i].setText((qtt[i] == null || qtt[i] == 0.0) ? "" : Utils.doubleFormat(qtt[i]));
			if (mat[i] != null) {
				mapComboBoxMat.get(i).setValue(mat[i]);
			}
		}
	}

	private Double[] materialProporcionQttToArray(MaterialProporcion materialProporcion) {
		Double[] arr = new Double[8];

		arr[0] = materialProporcion.getMat1Qtt();
		arr[1] = materialProporcion.getMat2Qtt();
		arr[2] = materialProporcion.getMat3Qtt();
		arr[3] = materialProporcion.getMat4Qtt();
		arr[4] = materialProporcion.getMat5Qtt();
		arr[5] = materialProporcion.getMat6Qtt();
		arr[6] = materialProporcion.getMat7Qtt();
		arr[7] = materialProporcion.getMat8Qtt();

		return arr;
	}

	private Material[] materialProporcionMatToArray(MaterialProporcion materialProporcion) {
		Material[] arr = new Material[8];

		arr[0] = materialProporcion.getMat1();
		arr[1] = materialProporcion.getMat2();
		arr[2] = materialProporcion.getMat3();
		arr[3] = materialProporcion.getMat4();
		arr[4] = materialProporcion.getMat5();
		arr[5] = materialProporcion.getMat6();
		arr[6] = materialProporcion.getMat7();
		arr[7] = materialProporcion.getMat8();

		return arr;
	}

	private String setErrorMessages(Map<String, String> errors) {
		Set<String> fields = errors.keySet();
		String str = "";
		labelErrorName.setText(fields.contains("name") ? errors.get("name") : "");
		labelErrorFck.setText(fields.contains("resistencia") ? errors.get("resistencia") : "");
		labelErrorFck.setText(fields.contains("slump") ? errors.get("slump") : "");

		for (String s : fields) {
			str = str + errors.get(s) + ". ";
		}
		return str;
	}

	public void subscribeDataChangeListener(DataChangeListener listener) {
		dataChangeListeners.add(listener);
	}

	private void notifyDataChangeListeners() {
		dataChangeListeners.forEach((DataChangeListener x) -> x.onDataChange());
	}

	@Override
	public void initialize(URL url, ResourceBundle rb) {
		initializeNodes();
	}

}
