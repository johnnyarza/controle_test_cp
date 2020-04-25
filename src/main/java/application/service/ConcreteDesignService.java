package application.service;

import java.util.List;

import application.dao.ConcreteDesignDao;
import application.dao.DaoFactory;
import application.domaim.ConcreteDesign;

public class ConcreteDesignService {
	
	ConcreteDesignDao dao = DaoFactory.ConcreteDesignlDao();
	
	public void insertConcreteDesign(ConcreteDesign obj) {
		dao.insert(obj);
	}
	public void updateConcreteDesign(ConcreteDesign obj) {
		dao.update(obj);
	}
	public void deleteConcreteDesignById(Integer id) {
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

}
