package application.Report;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import application.domaim.Cliente;
import application.domaim.CorpoDeProva;
import gui.util.Alerts;
import javafx.scene.control.Alert.AlertType;
import net.sf.jasperreports.engine.JREmptyDataSource;
import net.sf.jasperreports.engine.JRException;
import net.sf.jasperreports.engine.JasperCompileManager;
import net.sf.jasperreports.engine.JasperFillManager;
import net.sf.jasperreports.engine.JasperPrint;
import net.sf.jasperreports.engine.JasperReport;
import net.sf.jasperreports.engine.data.JRBeanCollectionDataSource;
import net.sf.jasperreports.engine.design.JasperDesign;
import net.sf.jasperreports.engine.xml.JRXmlLoader;
import net.sf.jasperreports.view.JasperViewer;

public class CompresionTestReport {

	

	public static void viewReport (List<CorpoDeProva> list,Cliente client) {
		try {
			//TODO terminar report (colocar detalhes do CompresionTest tais como obra, numero de ensaio etc)
			JRBeanCollectionDataSource itemsJRBean = new JRBeanCollectionDataSource(list);
			List<Cliente> clientList = new ArrayList<>();
			clientList.add(client);
			JRBeanCollectionDataSource itemsClientJRBean = new JRBeanCollectionDataSource(clientList);
			
			
			Map<String, Object> data = new HashMap<>();
			data.put("CorpoDeProvaCollecionBeanParam", itemsJRBean);
			data.put("ClientCollecionBeanParam", itemsClientJRBean);
			
			
			InputStream input = new FileInputStream(new File(".\\reports\\CompresionTestReport.jrxml"));
			JasperDesign jasperDesign = JRXmlLoader.load(input);
			JasperReport jasperReport = JasperCompileManager.compileReport(jasperDesign);
			

			JasperPrint jasperPrint = JasperFillManager.fillReport(jasperReport, data, new JREmptyDataSource());
			JasperViewer.viewReport(jasperPrint, false);

		} catch (FileNotFoundException e) {
			Alerts.showAlert("Error", "Archivo no encontrado", e.getMessage(), AlertType.ERROR);
		} catch (JRException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}
}
