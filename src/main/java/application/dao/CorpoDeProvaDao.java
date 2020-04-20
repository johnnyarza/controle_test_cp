package application.dao;

import java.util.List;

import application.domaim.CorpoDeProva;

public interface CorpoDeProvaDao {
	
	void insert(CorpoDeProva obj);
	void update(CorpoDeProva obj);
	void deleteById(Integer id);
	CorpoDeProva findById(Integer id);
	List<CorpoDeProva> findAll();
	List<CorpoDeProva> findByCompresionTestId(Integer id);

}
