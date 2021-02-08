package application.Report;

import java.io.IOException;
import java.io.InputStream;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import application.domaim.Cliente;
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

public class ClientReport {



	public static void viewClientReport(List<Cliente> clientsList) throws IOException {
		try {
			if (clientsList.size() == 0) {
				throw new JRException("Lista de clientes vacía");
			}
			
			
			JRBeanCollectionDataSource clientsJRBean = new JRBeanCollectionDataSource(clientsList);
			Map<String, Object> data = new HashMap<>();
			data.put("ClientsCollecionBeanParam", clientsJRBean);
			data.put("logo", FileUtils.getLogoPath());
			
			InputStream input = ClientReport.class.getResourceAsStream("/reports/GetClients.jrxml");
			JasperDesign jasperDesign = JRXmlLoader.load(input);
			JasperReport jasperReport = JasperCompileManager.compileReport(jasperDesign);
			

			JasperPrint jasperPrint = JasperFillManager.fillReport(jasperReport, data, new JREmptyDataSource());
			JasperViewer.viewReport(jasperPrint, false);

		

		} catch (JRException e) {
			throw new ReportException("Error al cargar reporte");
		}
	}
}
