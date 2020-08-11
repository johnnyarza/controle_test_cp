package gui;

import java.net.URL;
import java.util.ArrayList;
import java.util.List;
import java.util.ResourceBundle;

import application.domaim.CompresionTestList;
import application.domaim.CorpoDeProva;
import gui.util.Utils;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Button;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.cell.PropertyValueFactory;

public class WarningDialogController implements Initializable{
	
	List<CorpoDeProva> lateCorpoDeProvaList;
	
	List<CompresionTestList>  compresionTestListList; 	
	
	ObservableList<CompresionTestList> compresionTestListObsList;


	@FXML
	TableView<CompresionTestList> tableView;
	
	@FXML
	TableColumn<CompresionTestList,String> tableColumnClientName;
	
	@FXML
	TableColumn<CompresionTestList,Integer> tableColumTestId;
	
	@FXML
	Button btnClose; 
	
	@FXML
	private void onBtCloseAction(ActionEvent event) {
		Utils.currentStage(event).close();
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


	@Override
	public void initialize(URL location, ResourceBundle resources) {
		tableColumnClientName.setCellValueFactory(new PropertyValueFactory<>("clientName"));
		tableColumTestId.setCellValueFactory(new PropertyValueFactory<>("compresionTestId"));
		tableColumnClientName.getStyleClass().add("client-column-style");
		
		
	}
	
	public void updateDialogData () {
		List<CompresionTestList> lst = new ArrayList<CompresionTestList>();
		for (CorpoDeProva cp : lateCorpoDeProvaList) {
			lst.add(compresionTestListList
					.stream()
					.filter((obj) -> obj.getCompresionTestId().equals(cp.getCompresionTest().getId())).findFirst().orElse(null));
			
		}
		compresionTestListObsList = FXCollections.observableArrayList(lst);
		tableView.setItems(compresionTestListObsList);
		tableView.refresh();
		
	}

}
