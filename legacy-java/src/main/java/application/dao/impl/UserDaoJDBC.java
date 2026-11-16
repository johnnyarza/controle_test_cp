package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.logging.Level;

import application.dao.UserDao;
import application.db.DB;
import application.db.DbException;
import application.domaim.User;
import application.log.LogUtils;
import gui.util.Alerts;
import javafx.scene.control.Alert.AlertType;

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
			st = conn.prepareStatement("INSERT INTO cp_db.users (`name`,`password`,`role`) VALUES (?,?,?)",
					Statement.RETURN_GENERATED_KEYS);

			st.setString(1, user.getName());
			st.setString(2, user.getPassword());
			st.setString(3, user.getRole());

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
			rollback();			
			throw new DbException(e.getMessage());
		} finally {
		finallyActions(st, rs);
		}
		return null;
	}

	@Override
	public User findById(Integer id) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("Select * from cp_db.users where `id`=?");
			st.setInt(1, id);

			rs = st.executeQuery();

			if (rs.next()) {
				return new User(rs.getInt(1), rs.getString(2), rs.getString(3),rs.getString(4));
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
			if (conn.getAutoCommit()) conn.setAutoCommit(false);
			
			st = conn.prepareStatement("UPDATE `cp_db`.`users` SET `name` = ?,`password` = ? WHERE `id` = ?");
			st.setString(1, user.getName());
			st.setString(2, user.getPassword());
			st.setInt(3, user.getId());

			st.executeUpdate();
			conn.commit();
		} catch (SQLException e) {
			rollback();
			throw new DbException(e.getMessage());
		} finally {
			finallyActions(st, null);
		}

	}

	@Override
	public void deleteById(Integer id) {
		PreparedStatement st = null;
		try {
			if (conn.getAutoCommit()) conn.setAutoCommit(false);
			
			st = conn.prepareStatement("DELETE FROM cp_db.users WHERE `id`=?");
			st.setInt(1, id);
			st.executeUpdate();
			
			conn.commit();
		} catch (SQLException e) {
			rollback();
			throw new DbException(e.getMessage());
		} finally {
			finallyActions(st, null);
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

			while (rs.next()) {

				if (rs.getString(2).equalsIgnoreCase(user.getName()) && rs.getString(3).equals(user.getPassword())) {
					return new User(rs.getInt(1), rs.getString(2), rs.getString(3),rs.getString(4));
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
	
	@Override
	public User findByName(String name) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {

			st = conn.prepareStatement("SELECT * FROM cp_db.users where name=?");
			st.setString(1, name);
			

			rs = st.executeQuery();

			while (rs.next()) {

				if (rs.getString(2).equalsIgnoreCase(name)) {
					return new User(rs.getInt(1), rs.getString(2), rs.getString(3),rs.getString(4));
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
	
	private void finallyActions(PreparedStatement st, ResultSet rs) {
		try {
			if (!conn.getAutoCommit()) {
				conn.setAutoCommit(true);
			}
			if (st != null) {
				DB.closeStatement(st);
			}
			if (rs != null) {
				DB.closeResultSet(rs);
			}
		} catch (Exception e2) {
			Alerts.showAlert("Error", "Error desconocídos", e2.getMessage(), AlertType.ERROR);
			LogUtils logger = new LogUtils();
			logger.doLog(Level.WARNING, e2.getMessage(), e2);
		}
	}

	private void rollback() {
		try {
			conn.rollback();
		} catch (Exception e) {
			Alerts.showAlert("Error", "Error desconocídos", e.getMessage(), AlertType.ERROR);
			LogUtils logger = new LogUtils();
			logger.doLog(Level.WARNING, e.getMessage(), e);
		}
	}

}
