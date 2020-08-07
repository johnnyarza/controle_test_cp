package application.Report;

import java.io.IOException;
import java.io.InputStream;
import java.sql.Connection;
import java.util.HashMap;
import java.util.Map;

import application.db.DB;
import application.exceptions.ReportException;
import application.util.FileUtils;
import net.sf.jasperreports.engine.JRException;
import net.sf.jasperreports.engine.JasperCompileManager;
import net.sf.jasperreports.engine.JasperFillManager;
import net.sf.jasperreports.engine.JasperPrint;
import net.sf.jasperreports.engine.JasperReport;
import net.sf.jasperreports.engine.design.JRDesignQuery;
import net.sf.jasperreports.engine.design.JasperDesign;
import net.sf.jasperreports.engine.xml.JRXmlLoader;
import net.sf.jasperreports.view.JasperViewer;

public class MaterialReport {

	private static Connection conn = DB.getConnection();

	public static void viewMaterialReport() {
		try {
			InputStream input = MaterialReport.class.getResourceAsStream("/reports/MaterialsReport.jrxml");
			JasperDesign jasperDesign = JRXmlLoader.load(input);

			String sql = "SELECT materials.id,materials.name,(providers.name) "
					+ "as proveedor FROM materials inner join providers " + "where materials.providerId = providers.id";
			JRDesignQuery newQuery = new JRDesignQuery();
			
			newQuery.setText(sql);
			jasperDesign.setQuery(newQuery);
			JasperReport jasperReport = JasperCompileManager.compileReport(jasperDesign);
			
			Map<String, Object> data = new HashMap<>();
			data.put("logo", FileUtils.getLogoPath());

			JasperPrint jasperPrint = JasperFillManager.fillReport(jasperReport, data, conn);
			JasperViewer.viewReport(jasperPrint, false);

		} catch (JRException e) {
			throw new ReportException(e.getMessage());
		} catch (IOException e1) {
			throw new ReportException(e1.getMessage());
		}
	}
}
