package application.dao;

import java.util.List;

import application.domaim.Material;

public interface MaterialDao {
	
	void insert(Material obj);
	void update(Material obj);
	void deleteById(Integer id);
	Material findById(Integer id);
	List<Material> findByDiffrentId(Integer id);
	List<Material> findAll();
	List<Material> findByProviderId(Integer providerId);

}
