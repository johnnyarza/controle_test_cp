package application.Report;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.InputStream;
import java.sql.Connection;
import java.util.HashMap;
import java.util.Map;

import application.db.DB;
import net.sf.jasperreports.engine.JRException;
import net.sf.jasperreports.engine.JasperCompileManager;
import net.sf.jasperreports.engine.JasperFillManager;
import net.sf.jasperreports.engine.JasperPrint;
import net.sf.jasperreports.engine.JasperReport;
import net.sf.jasperreports.engine.design.JRDesignQuery;
import net.sf.jasperreports.engine.design.JasperDesign;
import net.sf.jasperreports.engine.xml.JRXmlLoader;
import net.sf.jasperreports.view.JasperViewer;

public class ClientReport {

	private static Connection conn = DB.getConnection();

	public static void viewClientReport() {
		try {
			InputStream input = new FileInputStream(new File(".\\reports\\GetClients.jrxml"));
			// InputStream inputImage = new FileInputStream(new File("./images/logo.png"));

			JasperDesign jasperDesign = JRXmlLoader.load(input);

			String sql = "SELECT * FROM clients";
			JRDesignQuery newQuery = new JRDesignQuery();
			newQuery.setText(sql);
			jasperDesign.setQuery(newQuery);
			JasperReport jasperReport = JasperCompileManager.compileReport(jasperDesign);
			Map<String, Object> data = new HashMap<>();
			// data.put("imagesDir",
			// "D:/NextCloud/CursoJava/ws-Programa_lab_concreto/Controle_de_CP/cp_project/images");
			JasperPrint jasperPrint = JasperFillManager.fillReport(jasperReport, data, conn);
			JasperViewer.viewReport(jasperPrint, false);

		} catch (FileNotFoundException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		} catch (JRException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}
}
