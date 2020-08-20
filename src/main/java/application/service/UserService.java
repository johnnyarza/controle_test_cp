package application.service;

import application.dao.UserDao;
import application.dao.impl.UserDaoJDBC;
import application.db.DB;
import application.domaim.User;

public class UserService {
	private UserDao dao = new UserDaoJDBC(DB.getConnection());

	public void saveOrInsert(User user) {
		if (user.getId() != null) {
			dao.update(user);
		} else {
			dao.insert(user);
		}
	}
	
	public User findById(Integer id) {
		return dao.findById(id);
	}
	
	public void deleteById(Integer id) {
		dao.deleteById(id);
	}
	
	public User findByNameAndPassword(User user) {
		return dao.findByNameAndPassword(user);
	}
}