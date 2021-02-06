package application.service;

import java.sql.SQLException;
import java.util.List;
import java.util.TimeZone;

import application.dao.CompresionTestDao;
import application.dao.DaoFactory;
import application.domaim.CompresionTest;

public class CompresionTestService {

	private CompresionTestDao dao;

	public CompresionTestService() throws SQLException {
		this.dao = DaoFactory.createCompresionTestDao();
	}

	public List<CompresionTest> findAll() {
		return dao.findAll();
	}

	public CompresionTest findById(int id) {
		return dao.findById(id);
	}

	public List<CompresionTest> findByClientId(int id, TimeZone tZ) {
		return dao.findyByClientId(id, tZ);
	}

	public List<CompresionTest> findByConcreteProviderId(int id, TimeZone tZ) {
		return dao.findyByConcreteProviderId(id, tZ);
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

	public List<CompresionTest> findByConcreteDesingId(Integer concreteDesignId, TimeZone tZ) {
		return dao.findByConcreteDesignId(tZ, concreteDesignId);
	}

	public List<CompresionTest> findByConcreteDesingId(Integer concreteDesignId) {
		return dao.findByConcreteDesingId(concreteDesignId);
	}

}
