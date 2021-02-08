package application.Report;

import java.io.IOException;
import java.util.List;

import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.domaim.ConcreteDesign;
import application.domaim.CorpoDeProva;
import application.domaim.Material;
import application.domaim.Provider;
import application.exceptions.ReportException;

public class ReportFactory {

	public void clientReportView(List<Cliente> clientsList) throws IOException,ReportException {
		ClientReport.viewClientReport(clientsList);
	}
	
	public void concreteDesignReportView(List<ConcreteDesign> list) throws IOException,ReportException{
		ConcreteDesignReport.viewConcreteDesignReport(list);
	}
	
	public void materialReportView(List<Material> materialslList) throws ReportException, IOException {
		MaterialReport.viewMaterialReport(materialslList);
	}
	
	public void providerReportView(List<Provider> providersList) throws IOException, ReportException {
		ProviderReport.viewProviderReport(providersList);
	}
	
	public void compresionTestReportView(List<CorpoDeProva> list,CompresionTest compresionTest) throws IOException, ReportException {
		CompresionTestReport.viewReport(list, compresionTest);
	}
	
	public void compresionTestReportViewByClient(List<CorpoDeProva> list, Cliente client) throws ReportException, IOException {
		CompresionTestReportByClient.viewReport(list, client);
	}
}
