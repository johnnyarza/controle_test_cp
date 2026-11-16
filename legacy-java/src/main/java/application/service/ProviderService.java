package application.service;

import java.sql.SQLIntegrityConstraintViolationException;
import java.util.List;

import application.dao.ProviderDao;
import application.db.DbException;
import application.dao.DaoFactory;
import application.domaim.Provider;

public class ProviderService {

	private ProviderDao dao = DaoFactory.createProviderDao();
	
	private MaterialService materialService = new MaterialService();

	public List<Provider> findAll() {
		return dao.findAll();
	}

	public void saveOrUpdate(Provider obj) {
		if (obj.getId() == null) {
			if (findByName(obj.getName()) != null) {
				throw new DbException("El proveedor: " + obj.getName() + " ya existe");
			}
			dao.insert(obj);
		} else {
			dao.update(obj);
		}
	}

	public void deleteById(Integer id) throws SQLIntegrityConstraintViolationException {
		if (materialService.findByProviderId(id).size() > 0) {
			throw new SQLIntegrityConstraintViolationException("Hay materiales utilizando este proveedor");
		}
		dao.deleteById(id);
	}

	public Provider findById(Integer id) {
		return dao.findById(id);
	}

	public Provider findByName(String name) {
		return dao.findByName(name);
	}
}
