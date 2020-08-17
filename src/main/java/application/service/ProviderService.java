package application.service;

import java.util.List;

import application.dao.ProviderDao;
import application.db.DbException;
import application.dao.DaoFactory;
import application.domaim.Provider;

public class ProviderService {

	private ProviderDao dao = DaoFactory.createProviderDao();

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

	public void deleteById(Integer id) {
		dao.deleteById(id);
	}

	public Provider findById(Integer id) {
		return dao.findById(id);
	}

	public Provider findByName(String name) {
		return dao.findByName(name);
	}
}
