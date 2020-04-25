package application.service;

import java.util.List;
import java.util.TimeZone;

import application.dao.CompresionTestDao;
import application.dao.DaoFactory;
import application.domaim.CompresionTest;

public class CompresionTestService {

	private CompresionTestDao dao = DaoFactory.createCompresionTestDao();

	public List<CompresionTest> findAll() {
		return dao.findAll();
	}

	public CompresionTest findById(int id) {		
		return dao.findById(id);
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
	
	public CompresionTest findByIdWithTimeZone(Integer id, TimeZone tZ) {
		return dao.findByIdWithTimeZone(id, tZ);
	}
	
	public Boolean compresionTestContainsConcreteDesingId(Integer concreteDesignId) {
		return dao.compresionTestContainsConcreteDesingId(concreteDesignId);
	}
}
