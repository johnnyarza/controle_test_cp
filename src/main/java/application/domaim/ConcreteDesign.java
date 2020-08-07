package application.domaim;

import java.io.Serializable;

public class ConcreteDesign implements Serializable{
	private static final long serialVersionUID = 1L;
	
	//TODO implementar slump
	private Integer id;
	private String description;
	private Double fck;
	private Double slump;
	private MaterialProporcion proporcion;
	
	public ConcreteDesign() {
	}

	public ConcreteDesign(Integer id, String description, Double fck,MaterialProporcion proporcion,Double slump) {
		this.id = id;
		this.description = description;
		this.fck = fck;
		this.proporcion = proporcion;
	}

	public Integer getId() {
		return id;
	}

	public void setId(Integer id) {
		this.id = id;
	}

	public String getDescription() {
		return description;
	}

	public void setDescription(String description) {
		this.description = description;
	}

	public Double getFck() {
		return fck;
	}

	public void setFck(Double fck) {
		this.fck = fck;
	}

	public MaterialProporcion getProporcion() {
		return proporcion;
	}

	public void setProporcion(MaterialProporcion proporcion) {
		this.proporcion = proporcion;
	}

	public Double getSlump() {
		return slump;
	}

	public void setSlump(Double slump) {
		this.slump = slump;
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
		ConcreteDesign other = (ConcreteDesign) obj;
		if (id == null) {
			if (other.id != null)
				return false;
		} else if (!id.equals(other.id))
			return false;
		return true;
	}

	@Override
	public String toString() {
		return this.description +" - slump: " +this.slump +" - " + this.proporcion.toString();
	}		

}
