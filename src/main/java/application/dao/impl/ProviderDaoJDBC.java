package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.ArrayList;
import java.util.List;
import java.util.logging.Level;

import application.dao.ProviderDao;
import application.db.DB;
import application.db.DbException;
import application.domaim.Provider;
import application.log.LogUtils;
import gui.util.Alerts;
import javafx.scene.control.Alert.AlertType;

public class ProviderDaoJDBC implements ProviderDao {

	private Connection conn;

	public ProviderDaoJDBC(Connection conn) {
		this.conn = conn;
	}

	@Override
	public void insert(Provider obj) {
		PreparedStatement st = null;
		try {
			if (conn.getAutoCommit())
				conn.setAutoCommit(false);
			st = conn.prepareStatement("INSERT INTO providers " + "(name,phone,address,email) " + "VALUES " + "(? ,? ,? ,?)",
					Statement.RETURN_GENERATED_KEYS);

			st.setString(1, obj.getName());
			st.setString(2, obj.getPhone());
			st.setString(3, obj.getAddress());
			st.setString(4, obj.getEmail());

			int rowsAffected = st.executeUpdate();

			if (rowsAffected > 0) {
				ResultSet rs = st.getGeneratedKeys();
				if (rs.next()) {
					int id = rs.getInt(1);
					obj.setId(id);
				}
				DB.closeResultSet(rs);
			} else {
				throw new DbException("Unexpected Error. No rows affected");
			}
			conn.commit();
		} catch (SQLException e) {
			rollback();
			throw new DbException(e.getMessage());
		} finally {
			finallyActions(st, null);
		}
	}

	@Override
	public void update(Provider obj) {
		PreparedStatement st = null;
		try {
			if (conn.getAutoCommit())
				conn.setAutoCommit(false);

			st = conn.prepareStatement("update providers set " + "providers.name = ?, " + "providers.phone =?, "
					+ "providers.address = ?, " + "providers.email = ? " + "WHERE providers.id = ?");

			st.setString(1, obj.getName());
			st.setString(2, obj.getPhone());
			st.setString(3, obj.getAddress());
			st.setString(4, obj.getEmail());
			st.setInt(5, obj.getId());

			int rowsAffected = st.executeUpdate();

			if (rowsAffected == 0) {
				throw new DbException("Unexpected Error. No rows affected");
			}
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
			if (conn.getAutoCommit())
				conn.setAutoCommit(false);

			st = conn.prepareStatement("DELETE from providers where ID = ?");
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
	public Provider findById(Integer id) {
		PreparedStatement st = null;
		ResultSet rs = null;
		Provider obj = new Provider();
		try {
			st = conn.prepareStatement("SELECT * FROM providers WHERE id = ?");
			st.setInt(1, id);

			rs = st.executeQuery();

			if (rs.next()) {
				obj.setId(rs.getInt(1));
				obj.setName(rs.getString(2));
				obj.setPhone(rs.getString(3));
				obj.setAddress(rs.getString(4));
				obj.setEmail(rs.getString(5));
			}
			return obj;

		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		}
	}

	@Override
	public List<Provider> findAll() {
		PreparedStatement st = null;
		ResultSet rs = null;
		List<Provider> list = new ArrayList<>();
		try {
			st = conn.prepareStatement("SELECT * FROM providers");

			rs = st.executeQuery();

			while (rs.next()) {
				Provider obj = new Provider();
				obj.setId(rs.getInt(1));
				obj.setName(rs.getString(2));
				obj.setPhone(rs.getString(3));
				obj.setAddress(rs.getString(4));
				obj.setEmail(rs.getString(5));
				list.add(obj);
			}
			return list;
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		}
	}

	@Override
	public Provider findByName(String name) {

		PreparedStatement st = null;
		ResultSet rs = null;
		Provider obj = new Provider();
		try {
			st = conn.prepareStatement("SELECT * FROM cp_db.providers where name= ?");
			st.setString(1, name);

			rs = st.executeQuery();

			if (rs.next()) {
				obj.setId(rs.getInt(1));
				obj.setName(rs.getString(2));
				obj.setPhone(rs.getString(3));
				obj.setAddress(rs.getString(4));
				obj.setEmail(rs.getString(5));
				return obj;
			}
			return null;

		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		}
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
