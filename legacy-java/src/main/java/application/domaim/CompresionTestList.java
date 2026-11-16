package application.domaim;

import java.util.Date;

public class CompresionTestList {
	
	private Integer compresionTestId;
	private Integer clientId;
	private String clientName;
	private String obra;
	private String address;
	private Date creationDate;
	
	public CompresionTestList() {
	}

	public CompresionTestList(Integer compresionTestId,Integer clientId, String clientName, String obra, String address,
			Date creationDate) {
		this.compresionTestId = compresionTestId;
		this.setClientId(clientId);
		this.clientName = clientName;
		this.obra = obra;
		this.address = address;
		this.creationDate = creationDate;
	}

	public Integer getCompresionTestId() {
		return compresionTestId;
	}

	public void setCompresionTestId(Integer compresionTestId) {
		this.compresionTestId = compresionTestId;
	}

	public String getClientName() {
		return clientName;
	}

	public void setClientName(String clientName) {
		this.clientName = clientName;
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

	public Date getCreationDate() {
		return creationDate;
	}

	public void setCreationDate(Date creationDate) {
		this.creationDate = creationDate;
	}

	public Integer getClientId() {
		return clientId;
	}

	public void setClientId(Integer clientId) {
		this.clientId = clientId;
	}

	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result + ((compresionTestId == null) ? 0 : compresionTestId.hashCode());
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
		CompresionTestList other = (CompresionTestList) obj;
		if (compresionTestId == null) {
			if (other.compresionTestId != null)
				return false;
		} else if (!compresionTestId.equals(other.compresionTestId))
			return false;
		return true;
	}

	@Override
	public String toString() {
		return "CompresionTestList [compresionTestId=" + compresionTestId + ", clientName=" + clientName + ", proyecto="
				+ obra + ", address=" + address + ", creationDate=" + creationDate + "]";
	}

}
