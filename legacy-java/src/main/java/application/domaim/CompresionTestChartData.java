package application.domaim;

public class CompresionTestChartData {
	
	private String serie;
	private Integer days;
	private Double fck;
	
	
	public CompresionTestChartData() {
	}
	
	public CompresionTestChartData(String serie, Integer days, Double fck) {
		this.serie = serie;
		this.days = days;
		this.fck = fck;
	}
	
	public String getSerie() {
		return serie;
	}
	
	public void setSerie(String serie) {
		this.serie = serie;
	}
	public Integer getDays() {
		return days;
	}
	
	public void setDays(Integer days) {
		this.days = days;
	}
	
	public Double getFck() {
		return fck;
	}
	
	public void setFck(Double fck) {
		this.fck = fck;
	}

	
}
