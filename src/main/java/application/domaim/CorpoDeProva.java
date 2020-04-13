package application.domaim;

import java.util.Date;

public class CorpoDeProva {
	
	private Integer id;
	private Cliente client;
	private Date moldeDate;
	private String code;
	private Double fck;
	private Double diameter;
	private Double height;
	
	public CorpoDeProva() {
	}
		
	public CorpoDeProva(Integer id, Cliente client, Date moldeDate, String code, Double fck, Double diameter,
			Double height) {
		super();
		this.id = id;
		this.client = client;
		this.moldeDate = moldeDate;
		this.code = code;
		this.fck = fck;
		this.diameter = diameter;
		this.height = height;
	}

	public Integer getId() {
		return id;
	}

	public void setId(Integer id) {
		this.id = id;
	}

	public Cliente getClient() {
		return client;
	}

	public void setClient(Cliente client) {
		this.client = client;
	}

	public String getCode() {
		return code;
	}

	public void setCode(String code) {
		this.code = code;
	}

	public Double getFck() {
		return fck;
	}

	public void setFck(Double fck) {
		this.fck = fck;
	}

	public Double getDiameter() {
		return diameter;
	}

	public void setDiameter(Double diameter) {
		this.diameter = diameter;
	}

	public Double getHeight() {
		return height;
	}

	public void setHeight(Double height) {
		this.height = height;
	}

	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result + ((id == null) ? 0 : id.hashCode());
		return result;
	}

	@Override
	public boolean equals(Object obj) {
		if (this == obj)
			return true;
		if (obj == null)
			return false;
		if (getClass() != obj.getClass())
			return false;
		CorpoDeProva other = (CorpoDeProva) obj;
		if (id == null) {
			if (other.id != null)
				return false;
		} else if (!id.equals(other.id))
			return false;
		return true;
	}

	public Date getDataMolde() {
		return moldeDate;
	}

	public void setDataMolde(Date dataMolde) {
		this.moldeDate = dataMolde;
	}
	
	

}
