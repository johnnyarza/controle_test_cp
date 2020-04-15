package application.domaim;

import java.util.Date;
import java.util.List;

public class CompresionTest {
	
	private Integer id;
	private Cliente client;
	private String obra;
	private String address;
	private Date date;
	private List<CorpoDeProva> listCps;
		
	public CompresionTest() {
	}

	public CompresionTest(Integer id,Cliente client, String obra,String address,Date date) {
		this.id = id;
		this.client = client;
		this.setObra(obra);
		this.setAddress(address);
		this.date = date;
	}

	public Cliente getClient() {
		return client;
	}

	public void setClient(Cliente client) {
		this.client = client;
	}

	public Date getDate() {
		return date;
	}

	public void setDate(Date date) {
		this.date = date;
	}

	public List<CorpoDeProva> getListCps() {
		return listCps;
	}

	public void addCptoList(CorpoDeProva cp) {
		this.listCps.add(cp);
	}

	public Integer getId() {
		return id;
	}

	public void setId(Integer id) {
		this.id = id;
	}

	public String getObra() {
		return obra;
	}

	public void setObra(String obra) {
		this.obra = obra;
	}

	public String getAddress() {
		return address;
	}

	public void setAddress(String address) {
		this.address = address;
	}
}
