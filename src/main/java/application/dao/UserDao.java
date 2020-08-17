package application.dao;

import application.domaim.User;

public interface UserDao {

	User insert (User user);
	User findById(Integer id);
	void update(User user);
	void deleteById(Integer id);
}
