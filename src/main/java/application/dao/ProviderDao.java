package application.dao;

import java.util.List;

import application.domaim.Provider;

public interface ProviderDao {
	
	void insert(Provider obj);
	void update(Provider obj);
	void deleteById(Integer id);
	Provider findById(Integer id);
	List<Provider> findAll();

}
