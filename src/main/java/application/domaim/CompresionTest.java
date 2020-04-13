package application.domaim;

import java.util.Date;
import java.util.List;

public class CompresionTest {
	
	private Integer id;
	private Cliente client;
	private Date date;
	private List<CorpoDeProva> listCps;
		
	public CompresionTest() {
	}

	public CompresionTest(Integer id,Cliente client, Date date) {
		this.id = id;
		this.client = client;
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
}
