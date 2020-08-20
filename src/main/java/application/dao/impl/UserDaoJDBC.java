package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;

import application.dao.UserDao;
import application.db.DB;
import application.db.DbException;
import application.domaim.User;

public class UserDaoJDBC implements UserDao {

	private Connection conn;

	public UserDaoJDBC(Connection conn) {
		this.conn = conn;
	}

	@Override
	public User insert(User user) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("INSERT INTO `users` (`name`,`password`) VALUES (?,?)");
			st.setString(1, user.getName());
			st.setString(2, user.getPassword());

			int rowsAffected = st.executeUpdate();

			if (rowsAffected > 0) {
				rs = st.getGeneratedKeys();
				if (rs.next()) {
					int id = rs.getInt(1);
					user.setId(id);
					return user;
				}
				DB.closeResultSet(rs);
			} else {
				throw new DbException("Unexpected Error. No rows affected");
			}

		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		}
		return null;
	}

	@Override
	public User findById(Integer id) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("Select * from `users` where `id`=?");
			st.setInt(1, id);

			rs = st.executeQuery();

			if (rs.next()) {
				return new User(rs.getInt(1), rs.getString(2), rs.getString(3));
			}

		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeResultSet(rs);
			DB.closeStatement(st);
		}
		return null;
	}

	@Override
	public void update(User user) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("UPDATE `cp_db`.`users` SET `name` = ?,`password` = ? WHERE `id` = ?");
			st.setString(1, user.getName());
			st.setString(2, user.getPassword());
			st.setInt(3, user.getId());
			
			st.executeUpdate();
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}
	
	}

	@Override
	public void deleteById(Integer id) {
		PreparedStatement st= null;
		try {
			st = conn.prepareStatement("DELETE FROM `users` WHERE `id`=?");
			st.setInt(1,id);
			st.executeUpdate();
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}

	}

	@Override
	public User findByNameAndPassword(User user) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("SELECT * FROM cp_db.users where name collate utf8mb4_0900_as_cs =? and password collate utf8mb4_0900_as_cs =?");
			st.setString(1, user.getName());
			st.setString(2, user.getPassword());

			rs = st.executeQuery();

			if (rs.next()) {
				return new User(rs.getInt(1), rs.getString(2), rs.getString(3));
			}

		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeResultSet(rs);
			DB.closeStatement(st);
		}
		return null;
	}

}
