package application.Report;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import application.domaim.CompresionTest;
import application.domaim.CompresionTestChartData;
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

	

	public static void viewReport (List<CorpoDeProva> list,CompresionTest cTest) {
		try {
			//TODO terminar report criar uma classe para o grafico e um dataset exclusivo
			JRBeanCollectionDataSource itemsJRBean = new JRBeanCollectionDataSource(list);
			List<CompresionTest> cTestList = new ArrayList<>();
			cTestList.add(cTest);
			JRBeanCollectionDataSource itemsCTEstRBean = new JRBeanCollectionDataSource(cTestList);
			JRBeanCollectionDataSource itemsCorpoDeProvaChartRBEan =new JRBeanCollectionDataSource(compresionTestChartData(list));
			
			//CorpoDeProvaChartCollectionBeanParam
			Map<String, Object> data = new HashMap<>();
			data.put("CorpoDeProvaCollecionBeanParam", itemsJRBean);
			data.put("CompresionTestCollectionBeanParam", itemsCTEstRBean);
			data.put("CorpoDeProvaChartCollectionBeanParam", itemsCorpoDeProvaChartRBEan);
			
			
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
	
	private static List<CompresionTestChartData> compresionTestChartData(List<CorpoDeProva> list) {
		List<CompresionTestChartData> chartData = new ArrayList<CompresionTestChartData>();
		for (CorpoDeProva cp : list) {
			chartData.add(new CompresionTestChartData("SERIES 1", cp.getDays(), cp.getFckRupture()));
		}		
		return chartData;		
	}
}
