package application.test;

import java.io.InputStream;

import application.Report.MaterialReport;
import net.sf.jasperreports.engine.JRException;
import net.sf.jasperreports.engine.design.JasperDesign;
import net.sf.jasperreports.engine.xml.JRXmlLoader;

public class Test {
	public static void main(String[] args) {

	//InputStream input = new FileInputStream(new File("/MaterialsReport.jrxml"));
		InputStream input = MaterialReport.class.getResourceAsStream("/reports/MaterialsReport.jrxml");
		try {
			JasperDesign jasperDesign = JRXmlLoader.load(input);
		} catch (JRException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}
}
