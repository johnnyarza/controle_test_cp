package application.service;

import java.util.List;

import application.dao.CompresionTestDao;
import application.dao.DaoFactory;
import application.domaim.CompresionTest;

public class CompresionTestService {

	private CompresionTestDao dao = DaoFactory.createCompresionTestDao();

	public List<CompresionTest> findAll() {
		return dao.findAll();
	}

	public CompresionTest findById(int id) {
		dao.findById(id);
		return null;
	}

	public void saveOrUpdate(CompresionTest obj) {
		if (obj.getId() == null) {
			dao.insert(obj);
		} else {
			dao.update(obj);
		}
	}

	public void deleteById(Integer id) {
		dao.deleteById(id);
	}
}
