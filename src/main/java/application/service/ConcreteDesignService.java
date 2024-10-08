package application.service;

import java.sql.SQLIntegrityConstraintViolationException;
import java.util.List;

import application.dao.ConcreteDesignDao;
import application.dao.DaoFactory;
import application.domaim.ConcreteDesign;

public class ConcreteDesignService {
	
	ConcreteDesignDao dao = DaoFactory.ConcreteDesignlDao();
	
	CompresionTestService compresionTestService = new CompresionTestService();
	
	public void deleteConcreteDesignById(Integer id) throws SQLIntegrityConstraintViolationException {
		if (compresionTestService.compresionTestContainsConcreteDesingId(id)) {
			throw new SQLIntegrityConstraintViolationException("La dosificación esta en uso");
		}
		dao.deleteById(id);
	}
	public ConcreteDesign findConcreteDesignById(Integer id) {
		return dao.findById(id);
	}
	public List<ConcreteDesign> findAllConcreteDesign() {
		return dao.findAll();
	}
	
	public void saveOrUpdate(ConcreteDesign obj) {
		if(obj.getId() == null) {
			dao.insert(obj);
		}
		else {
			dao.update(obj);
		}
	}
	
	public List<ConcreteDesign> findByMaterialId(int materialId) {
		return dao.findByMaterialId(materialId);
	}
	

}
