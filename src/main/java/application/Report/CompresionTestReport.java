package application.Report;

import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.domaim.CompresionTestChartData;
import application.domaim.CorpoDeProva;
import application.exceptions.ReportException;
import application.util.FileUtils;
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
	

	public static void viewReport (List<CorpoDeProva> list,CompresionTest cTest) throws IOException,ReportException {
		try {
			if (list.size() == 0) {
				throw new JRException("No hay probetas para imprimir");
			}
			JRBeanCollectionDataSource itemsJRBean = new JRBeanCollectionDataSource(list);
			List<CompresionTest> cTestList = new ArrayList<>();
			List<Cliente> clientList = new ArrayList<>();
			cTestList.add(cTest);
			clientList.add(cTest.getClient());
			
			
			JRBeanCollectionDataSource itemsCTEstRBean = new JRBeanCollectionDataSource(cTestList);
			JRBeanCollectionDataSource itemsCorpoDeProvaChartRBEan =new JRBeanCollectionDataSource(compresionTestChartData(list));
			JRBeanCollectionDataSource itemsClientRBean = new JRBeanCollectionDataSource(clientList);
			
			Map<String, Object> data = new HashMap<>();
			data.put("CorpoDeProvaCollecionBeanParam", itemsJRBean);
			data.put("CompresionTestCollectionBeanParam", itemsCTEstRBean);
			data.put("CorpoDeProvaChartCollectionBeanParam", itemsCorpoDeProvaChartRBEan);
			data.put("ClientCollecionBeanParam", itemsClientRBean);
			data.put("logo", FileUtils.getLogoPath());
			data.put("carimbo", FileUtils.getCarimboPath());
			

			InputStream input = CompresionTestReport.class.getResourceAsStream("/reports/CompresionTestReport.jrxml");
			JasperDesign jasperDesign = JRXmlLoader.load(input);
			JasperReport jasperReport = JasperCompileManager.compileReport(jasperDesign);
			

			JasperPrint jasperPrint = JasperFillManager.fillReport(jasperReport, data, new JREmptyDataSource());
			JasperViewer.viewReport(jasperPrint, false);

		} catch (JRException e) {
			throw new ReportException(e.getMessage());
		} 
	}
	
	private static List<CompresionTestChartData> compresionTestChartData(List<CorpoDeProva> list) {
		List<CompresionTestChartData> chartData = new ArrayList<CompresionTestChartData>();
		for (CorpoDeProva cp : list) {
			if ((cp.getTonRupture() != 0.0) && (cp.getFckRupture() != 0.0))
			chartData.add(new CompresionTestChartData("SERIES 1", cp.getDays(), cp.getFckRupture()));
		}		
		return chartData;		
	}
}
