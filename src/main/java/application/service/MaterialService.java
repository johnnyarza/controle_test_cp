package application.service;

import java.util.List;

import application.dao.MaterialDao;
import application.dao.DaoFactory;
import application.domaim.Material;

public class MaterialService {
	
	private MaterialDao dao = DaoFactory.createMaterialDao();
	
	public List<Material> findAll() {
		return dao.findAll();
	}
	
	public void saveOrUpdate(Material obj) {
		if(obj.getId() == null) {
			dao.insert(obj);
		}
		else {
			dao.update(obj);
		}
	}
	
	public void deleteById(Integer id) {
		dao.deleteById(id);
	}
	
	public Material findById (Integer id) {	
		return dao.findById(id);
	}
	
	public List<Material> findByDiffrentId(Integer id) {
		return dao.findByDiffrentId(id);
	}
	
	public List<Material> findByProviderId(Integer providerId) {
		return dao.findByProviderId(providerId);
	}
}
