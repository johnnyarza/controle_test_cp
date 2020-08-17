package gui;

import java.net.URL;
import java.util.ArrayList;
import java.util.List;
import java.util.ResourceBundle;
import java.util.TimeZone;

import application.domaim.CompresionTest;
import application.domaim.CompresionTestList;
import application.domaim.CorpoDeProva;
import application.service.CompresionTestService;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.cell.PropertyValueFactory;

public class WarningDialogController implements Initializable {

	List<CorpoDeProva> lateCorpoDeProvaList;

	List<CompresionTestList> compresionTestListList;

	CompresionTest entity;

	ObservableList<CompresionTestList> compresionTestListObsList;

	Boolean isBtCancelPressed = false;

	CompresionTestService compresionTestService;

	@FXML
	TableView<CompresionTestList> tableView;

	@FXML
	TableColumn<CompresionTestList, String> tableColumnClientName;

	@FXML
	TableColumn<CompresionTestList, Integer> tableColumTestId;

	@FXML
	Button btnClose;

	@FXML
	Button btnOpen;

	@FXML
	private void onBtCloseAction(ActionEvent event) {
		setIsBtCancelPressed(true);
		Utils.currentStage(event).close();
	}

	@FXML
	private void onBtOpenAction(ActionEvent event) {
		try {
			setIsBtCancelPressed(false);
			entity = getCompresionTestFromTableView(tableView);
			Utils.currentStage(event).close();

		} catch (NullPointerException e) {
			Alerts.showAlert("Error", "NullPointerException", e.getMessage(), AlertType.ERROR);
		}
	}

	private CompresionTest getCompresionTestFromTableView(TableView<CompresionTestList> tableView) {
		CompresionTestList compresionTestList = tableView.getSelectionModel().getSelectedItem();
		if (compresionTestList == null) {
			throw new NullPointerException("Ensayo vacío o no selecionado!");
		}
		CompresionTest compresionTest = compresionTestService.findByIdWithTimeZone(compresionTestList.getCompresionTestId(),
				TimeZone.getDefault());

		return compresionTest;
	}

	@Override
	public void initialize(URL location, ResourceBundle resources) {
		tableColumnClientName.setCellValueFactory(new PropertyValueFactory<>("clientName"));
		tableColumTestId.setCellValueFactory(new PropertyValueFactory<>("compresionTestId"));
		tableColumnClientName.getStyleClass().add("client-column-style");

	}

	public void updateDialogData() {
		List<CompresionTestList> lst = new ArrayList<CompresionTestList>();
		for (CorpoDeProva cp : lateCorpoDeProvaList) {
			lst.add(compresionTestListList.stream()
					.filter((obj) -> obj.getCompresionTestId().equals(cp.getCompresionTest().getId())).findFirst().orElse(null));

		}
		compresionTestListObsList = FXCollections.observableArrayList(lst);
		tableView.setItems(compresionTestListObsList);
		tableView.refresh();

	}

	public CompresionTest getEntity() {
		return entity;
	}

	public void setEntity(CompresionTest entity) {
		this.entity = entity;
	}

	public Boolean getIsBtCancelPressed() {
		return isBtCancelPressed;
	}

	public void setIsBtCancelPressed(Boolean isBtCancelPressed) {
		this.isBtCancelPressed = isBtCancelPressed;
	}

	public void setCompresionTestListList(List<CompresionTestList> compresionTestListList) {
		this.compresionTestListList = compresionTestListList;
	}

	public List<CorpoDeProva> getLateCorpoDeProvaList() {
		return lateCorpoDeProvaList;
	}

	public void setLateCorpoDeProvaList(List<CorpoDeProva> lateCorpoDeProvaList) {
		this.lateCorpoDeProvaList = lateCorpoDeProvaList;
	}

	public CompresionTestService getCompresionTestService() {
		return compresionTestService;
	}

	public void setCompresionTestService(CompresionTestService compresionTestService) {
		this.compresionTestService = compresionTestService;
	}
}
