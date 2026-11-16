package application.Report;

import java.util.List;

import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.domaim.ConcreteDesign;
import application.domaim.CorpoDeProva;
import application.domaim.Material;
import application.domaim.Provider;

public class ReportFactory {

	public void clientReportView(List<Cliente> clientsList) {
		ClientReport.viewClientReport(clientsList);
	}
	
	public void concreteDesignReportView(List<ConcreteDesign> list) {
		ConcreteDesignReport.viewConcreteDesignReport(list);
	}
	
	public void materialReportView(List<Material> materialslList) {
		MaterialReport.viewMaterialReport(materialslList);
	}
	
	public void providerReportView(List<Provider> providersList) {
		ProviderReport.viewProviderReport(providersList);
	}
	
	public void compresionTestReportView(List<CorpoDeProva> list,CompresionTest compresionTest) {
		CompresionTestReport.viewReport(list, compresionTest);
	}
	
	public void compresionTestReportViewByClient(List<CorpoDeProva> list, Cliente client) {
		CompresionTestReportByClient.viewReport(list, client);
	}
}
