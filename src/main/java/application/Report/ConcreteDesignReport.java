package application.Report;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.InputStream;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import application.domaim.ConcreteDesign;
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

public class ConcreteDesignReport {



	public static void viewConcreteDesignReport(List<ConcreteDesign> list) {
		try {

			JRBeanCollectionDataSource itemsJRBean = new JRBeanCollectionDataSource(list);
			Map<String, Object> data = new HashMap<>();
			data.put("CollectionBeanParam", itemsJRBean);
			
			InputStream input = new FileInputStream(new File(".\\reports\\ConcreteDesignReport.jrxml"));
			JasperDesign jasperDesign = JRXmlLoader.load(input);
			JasperReport jasperReport = JasperCompileManager.compileReport(jasperDesign);
			

			JasperPrint jasperPrint = JasperFillManager.fillReport(jasperReport, data, new JREmptyDataSource());
			JasperViewer.viewReport(jasperPrint, false);

		} catch (FileNotFoundException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		} catch (JRException e) {
			throw new ReportException(e.getMessage());
		}
	}
}
