package application.domaim;

import java.io.Serializable;
import java.util.HashMap;
import java.util.Map;
import java.util.Set;

import application.service.MaterialService;
import gui.util.Utils;

public class MaterialProporcion implements Serializable {
	private static final long serialVersionUID = 1L;

	private MaterialService materialService = new MaterialService();

	private Material mat1;
	private Material mat2;
	private Material mat3;
	private Material mat4;
	private Material mat5;
	private Material mat6;
	private Material mat7;
	private Material mat8;
	
	private Double mat1Qtt;
	private Double mat2Qtt;
	private Double mat3Qtt;
	private Double mat4Qtt;
	private Double mat5Qtt;
	private Double mat6Qtt;
	private Double mat7Qtt;
	private Double mat8Qtt;

	public MaterialProporcion() {
	}

	public MaterialProporcion(Material mat1, Material mat2, Material mat3, Material mat4, Material mat5, Material mat6,
			Material mat7, Material mat8, Double mat1Qtt, Double mat2Qtt, Double mat3Qtt, Double mat4Qtt,
			Double mat5Qtt, Double mat6Qtt, Double mat7Qtt, Double mat8Qtt) {
		this.mat1 = mat1;
		this.mat2 = mat2;
		this.mat3 = mat3;
		this.mat4 = mat4;
		this.mat5 = mat5;
		this.mat6 = mat6;
		this.mat7 = mat7;
		this.mat8 = mat8;
		
		this.mat1Qtt = mat1Qtt;
		this.mat2Qtt = mat2Qtt;
		this.mat3Qtt = mat3Qtt;
		this.mat4Qtt = mat4Qtt;
		this.mat5Qtt = mat5Qtt;
		this.mat6Qtt = mat6Qtt;
		this.mat7Qtt = mat7Qtt;
		this.mat8Qtt = mat8Qtt;
	}

	public Material getMat1() {
		return mat1;
	}

	public void setMat1(Material mat1) {
		this.mat1 = mat1;
	}

	public Material getMat2() {
		return mat2;
	}

	public void setMat2(Material mat2) {
		this.mat2 = mat2;
	}

	public Material getMat3() {
		return mat3;
	}

	public void setMat3(Material mat3) {
		this.mat3 = mat3;
	}

	public Material getMat4() {
		return mat4;
	}

	public void setMat4(Material mat4) {
		this.mat4 = mat4;
	}

	public Material getMat5() {
		return mat5;
	}

	public void setMat5(Material mat5) {
		this.mat5 = mat5;
	}

	public Material getMat6() {
		return mat6;
	}

	public void setMat6(Material mat6) {
		this.mat6 = mat6;
	}

	public Material getMat7() {
		return mat7;
	}

	public void setMat7(Material mat7) {
		this.mat7 = mat7;
	}

	public Material getMat8() {
		return mat8;
	}

	public void setMat8(Material mat8) {
		this.mat8 = mat8;
	}

	public Double getMat1Qtt() {
		return mat1Qtt;
	}

	public void setMat1Qtt(Double mat1Qtt) {
		this.mat1Qtt = mat1Qtt;
	}

	public Double getMat2Qtt() {
		return mat2Qtt;
	}

	public void setMat2Qtt(Double mat2Qtt) {
		this.mat2Qtt = mat2Qtt;
	}

	public Double getMat3Qtt() {
		return mat3Qtt;
	}

	public void setMat3Qtt(Double mat3Qtt) {
		this.mat3Qtt = mat3Qtt;
	}

	public Double getMat4Qtt() {
		return mat4Qtt;
	}

	public void setMat4Qtt(Double mat4Qtt) {
		this.mat4Qtt = mat4Qtt;
	}

	public Double getMat5Qtt() {
		return mat5Qtt;
	}

	public void setMat5Qtt(Double mat5Qtt) {
		this.mat5Qtt = mat5Qtt;
	}

	public Double getMat6Qtt() {
		return mat6Qtt;
	}

	public void setMat6Qtt(Double mat6Qtt) {
		this.mat6Qtt = mat6Qtt;
	}

	public Double getMat7Qtt() {
		return mat7Qtt;
	}

	public void setMat7Qtt(Double mat7Qtt) {
		this.mat7Qtt = mat7Qtt;
	}

	public Double getMat8Qtt() {
		return mat8Qtt;
	}

	public void setMat8Qtt(Double mat8Qtt) {
		this.mat8Qtt = mat8Qtt;
	}

	public MaterialService getMaterialService() {
		return materialService;
	}

	public void setMaterialService(MaterialService materialService) {
		this.materialService = materialService;
	}
	
	private Map<Material, Double> setMaterialAndQttToMap() {
		Map<Material, Double> map = new HashMap<>();
		map.put(mat1, mat1Qtt);
		map.put(mat2, mat2Qtt);
		map.put(mat3, mat3Qtt);
		map.put(mat4, mat4Qtt);
		map.put(mat5, mat5Qtt);
		map.put(mat6, mat6Qtt);
		map.put(mat7, mat7Qtt);
		map.put(mat8, mat8Qtt);
		
		return map;
	}

	@Override
	public String toString() {
		if (materialService == null) {
			throw new IllegalStateException("MaterialProporcion: materialService was null");
		}
		Map<Material, Double> mapList =  setMaterialAndQttToMap();
		Set<Material> matList = mapList.keySet();
		String str = "";
		for (Material m : matList) {
			if (!m.isAllNull()) {
				str = m.getName() +'/' +m.getProvider().getName()+": " + Utils.doubleFormat(mapList.get(m)) + "kg/m³ - " + str;
			}
		}
		
		return str;
	}
}
