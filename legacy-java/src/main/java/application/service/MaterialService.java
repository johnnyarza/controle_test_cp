package application.service;

import java.sql.SQLIntegrityConstraintViolationException;
import java.util.List;

import application.dao.MaterialDao;
import application.dao.DaoFactory;
import application.domaim.Material;

public class MaterialService {
	
	private MaterialDao dao = DaoFactory.createMaterialDao();
	private ConcreteDesignService concreteDesignService = new ConcreteDesignService();
	
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
	
	public void deleteById(Integer id) throws SQLIntegrityConstraintViolationException {
		if (dao.findByProviderId(id).size() > 0) {
			throw new SQLIntegrityConstraintViolationException("Hay proveedor(es) utilizando este material");
		}
		if (concreteDesignService.findByMaterialId(id).size() > 0) {
			throw new SQLIntegrityConstraintViolationException("Hay dosificacion(es) utilizando este material");
		}
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
