package application.dao;

import java.util.List;

import application.domaim.CompresionTest;

public interface CompresionTestDao {
	
	void insert(CompresionTest obj);
	void update(CompresionTest obj);
	void deleteById(Integer id);
	CompresionTest findById(Integer id);
	List<CompresionTest> findAll();

}
