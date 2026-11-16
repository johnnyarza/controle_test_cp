package application.dao;

import java.util.List;

import application.domaim.ConcreteDesign;

public interface ConcreteDesignDao {
	
	void insert(ConcreteDesign obj);
	void update(ConcreteDesign obj);
	void deleteById(Integer id);
	ConcreteDesign findById(Integer id);
	List<ConcreteDesign> findAll();
	List<ConcreteDesign> findByMaterialId(Integer id);

}
