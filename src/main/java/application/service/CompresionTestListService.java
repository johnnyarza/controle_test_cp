package application.service;

import java.sql.SQLException;
import java.util.List;

import application.dao.CompresionTestListDao;
import application.dao.DaoFactory;
import application.domaim.CompresionTestList;

public class CompresionTestListService {
	
	CompresionTestListDao dao;
	
	public List<CompresionTestList> findAll() {
		return dao.findAll();
	}

	public CompresionTestListService() throws SQLException {
		super();
		this.dao = DaoFactory.createCompresionTestListDao();
	}

}
