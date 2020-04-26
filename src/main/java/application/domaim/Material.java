package application.domaim;

import java.io.Serializable;

public class Material implements Serializable{
	private static final long serialVersionUID = 1L;
	
	private Integer id;
	private String name;
	private Provider provider;
		
	public Material() {
	}

	public Material(Integer id, String name, Provider provider) {
		super();
		this.id = id;
		this.name = name;
		this.provider = provider;
	}

	public Integer getId() {
		return id;
	}

	public void setId(Integer id) {
		this.id = id;
	}

	public String getName() {
		return name;
	}

	public void setName(String name) {
		this.name = name;
	}

	public Provider getProvider() {
		return provider;
	}

	public void setProvider(Provider provider) {
		this.provider = provider;
	}

	public Boolean isAllNull() {
		if (this.id == null && this.name == null && this.provider == null) {
			return true;
		} else {
			return false;
		}
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
		Material other = (Material) obj;
		if (id == null) {
			if (other.id != null)
				return false;
		} else if (!id.equals(other.id))
			return false;
		return true;
	}

	@Override
	public String toString() {
		if (this.id == null) {
			return "";
		} else {
			return  id + ", Nombre: " + name + ", Proveedor:" + provider;
		}
	}
	
}
