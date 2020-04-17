package application.domaim;

import java.util.Date;

public class CorpoDeProva {
	
	private Integer id;
	private String code;
	private Cliente client;
	private CompresionTest compresionTest;
	private Double slump;
	private Date moldeDate;	
	private Date ruptureDate;
	private Integer days;
	private Double diameter;
	private Double height;
	private Double weight;
	private Double densid;
	private Double tonRupture;
	private Double fckRupture;

	
	public CorpoDeProva() {
	}
		

	public CorpoDeProva(Integer id, String code, Cliente client, CompresionTest compresionTest, Double slump,
			Date moldeDate, Date ruptureDate, Double diameter, Double height, Double weight, Double tonRupture) {
		super();
		this.id = id;
		this.code = code;
		this.client = client;
		this.compresionTest = compresionTest;
		this.slump = slump;
		this.moldeDate = moldeDate;
		this.ruptureDate = ruptureDate;
		this.diameter = diameter;
		this.height = height;
		this.weight = weight;
		this.tonRupture = tonRupture;
	}


	public Integer getId() {
		return id;
	}



	public void setId(Integer id) {
		this.id = id;
	}



	public String getCode() {
		return code;
	}



	public void setCode(String code) {
		this.code = code;
	}



	public Cliente getClient() {
		return client;
	}



	public void setClient(Cliente client) {
		this.client = client;
	}



	public CompresionTest getCompresionTest() {
		return compresionTest;
	}



	public void setCompresionTest(CompresionTest compresionTest) {
		this.compresionTest = compresionTest;
	}



	public Double getSlump() {
		return slump;
	}



	public void setSlump(Double slump) {
		this.slump = slump;
	}



	public Date getMoldeDate() {
		return moldeDate;
	}



	public void setMoldeDate(Date moldeDate) {
		this.moldeDate = moldeDate;
	}



	public Date getRuptureDate() {
		return ruptureDate;
	}



	public void setRuptureDate(Date ruptureDate) {
		this.ruptureDate = ruptureDate;
	}



	public Integer getDays() {
		return days;
	}



	public void setDays(Integer days) {
		//TODO Implementar days
		//this.days = days;
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



	public Double getWeight() {
		return weight;
	}



	public void setWeight(Double weight) {
		this.weight = weight;
	}



	public Double getDensid() {
		return densid;
	}



	public void setDensid(Double densid) {
		//TODO Implementar densidade
		//this.densid = densid;
	}

	public Double getTonRupture() {
		return tonRupture;
	}



	public void setTonRupture(Double tonRupture) {
		this.tonRupture = tonRupture;
	}

	public Double getFckRupture() {
		return fckRupture;
	}


	public void setFckRupture(Double fckRupture) {
		//TODO implementar
		
		//this.fckRupture = fckRupture;
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
