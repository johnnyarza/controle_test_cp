package application.dao;

import application.domaim.User;

public interface UserDao {

	User insert (User user);
	User findById(Integer id);
	User findByNameAndPassword(User user);
	void update(User user);
	void deleteById(Integer id);
	User findByName(String name);
	
}
