package application.Report;

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

public class CompresionTestReportByClient {

	public static void viewReport(List<CorpoDeProva> list, Cliente client) {
		try {
			List<Cliente> clientList = new ArrayList<Cliente>();
			clientList.add(client);
			JRBeanCollectionDataSource itemsJRBean = new JRBeanCollectionDataSource(list);
			JRBeanCollectionDataSource clientJRBean = new JRBeanCollectionDataSource(clientList);

			Map<String, Object> data = new HashMap<>();
			data.put("CorpoDeProvaBeanPar", itemsJRBean);
			data.put("ClientBeanPar", clientJRBean);
			data.put("logo", "images/logo.png");
			data.put("carimbo", "images/carimbo.png");
			

			InputStream input = CompresionTestReportByClient.class
					.getResourceAsStream("/reports/CompresionTestReportByClient.jrxml");
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
