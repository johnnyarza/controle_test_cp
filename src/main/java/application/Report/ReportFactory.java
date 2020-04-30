package application.Report;

import java.util.List;

import application.domaim.Cliente;
import application.domaim.ConcreteDesign;
import application.domaim.CorpoDeProva;

public class ReportFactory {
	
	public void clientReportView() {
		ClientReport.viewClientReport();
	}
	
	public void concreteDesignReportView(List<ConcreteDesign> list) {
		ConcreteDesignReport.viewConcreteDesignReport(list);
	}
	
	public void materialReportView() {
		MaterialReport.viewMaterialReport();
	}
	
	public void providerReportView() {
		ProviderReport.viewProviderReport();
	}
	
	public void compresionTestReportView(List<CorpoDeProva> list,Cliente client) {
		CompresionTestReport.viewReport(list, client);
	}
}
