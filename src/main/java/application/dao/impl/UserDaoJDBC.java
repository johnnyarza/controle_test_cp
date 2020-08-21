package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Savepoint;
import java.sql.Statement;
import java.util.logging.Level;

import application.dao.UserDao;
import application.db.DB;
import application.db.DbException;
import application.domaim.User;
import application.log.LogUtils;

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
			conn.setAutoCommit(false);
			st = conn.prepareStatement("INSERT INTO `users` (`name`,`password`) VALUES (?,?)",
					Statement.RETURN_GENERATED_KEYS);

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
			} else {
				throw new DbException("Unexpected Error. No rows affected");
			}
			
			conn.commit();
		} catch (SQLException e) {
			LogUtils lg = new LogUtils();
			
			if (conn != null) {
				try {
					lg.doLog(Level.INFO, "Rollbacking", e);
					conn.rollback();
				} catch (SQLException excep) {

					lg.doLog(Level.SEVERE, excep.getMessage(), excep);
				}
			}
			
			throw new DbException(e.getMessage());
		} finally {
			DB.closeResultSet(rs);
			DB.closeStatement(st);

			try {
				conn.setAutoCommit(true);
			} catch (SQLException e) {
				LogUtils lg = new LogUtils();
				lg.doLog(Level.SEVERE, e.getMessage(), e);
			}
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
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("DELETE FROM `users` WHERE `id`=?");
			st.setInt(1, id);
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

			st = conn.prepareStatement("SELECT * FROM cp_db.users where name=? and password=?");
			st.setString(1, user.getName());
			st.setString(2, user.getPassword());

			rs = st.executeQuery();

			if (rs.next()) {

				if (rs.getString(2).equals(user.getName()) && rs.getString(3).equals(user.getPassword())) {
					return new User(rs.getInt(1), rs.getString(2), rs.getString(3));
				}
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
